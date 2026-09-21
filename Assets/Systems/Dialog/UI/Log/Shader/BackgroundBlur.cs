using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// UI覆盖式背景模糊组件 - 使用CommandBuffer替代GrabPass
/// 模糊此组件下方的所有UI画面
/// 使用方式：将此脚本添加到空UI对象上，会自动创建覆盖模糊区域
/// </summary>
public class BackgroundBlur : MonoBehaviour
{
    [Header("模糊设置")]
    [SerializeField] private float blurSize = 1f;
    [SerializeField] private Color blurColor = new Color(1, 1, 1, 1);
    [SerializeField] private int blurDownsample = 2; // 降采样倍数，越高性能越好但质量越低

    [Header("覆盖区域")]
    [SerializeField] private bool useCustomRect = false; // 是否使用自定义矩形区域
    [SerializeField] private Rect customRect = new Rect(0, 0, 1, 1); // 归一化坐标 (0-1)

    [Header("RenderTexture设置")]
    [SerializeField] private RenderTextureFormat format = RenderTextureFormat.Default;
    [SerializeField] private FilterMode filterMode = FilterMode.Bilinear;

    private Material blurMaterial;
    private CommandBuffer captureBuffer;
    private CommandBuffer renderBuffer;
    private RenderTexture capturedRT;
    private Mesh blurMesh;
    private int lastWidth = 0;
    private int lastHeight = 0;
    private Canvas parentCanvas;
    private RectTransform rectTransform;
    private bool isInitialized = false;

    private static readonly int MainTexID = Shader.PropertyToID("_MainTex");
    private static readonly int SizeID = Shader.PropertyToID("_Size");
    private static readonly int ColorID = Shader.PropertyToID("_Color");

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized) return;

        // 创建模糊材质
        Shader blurShader = Shader.Find("Unlit/UI_BGBlur_NoGrabPass");
        if (blurShader == null)
        {
            Debug.LogError("UIBackgroundBlur: 找不到Shader 'Unlit/UI_BGBlur_NoGrabPass'");
            return;
        }

        blurMaterial = new Material(blurShader);
        blurMaterial.SetFloat(SizeID, blurSize);
        blurMaterial.SetColor(ColorID, blurColor);

        // 创建模糊网格
        CreateBlurMesh();

        isInitialized = true;
    }

    private void OnEnable()
    {
        if (!isInitialized) Initialize();
        
        CreateCommandBuffers();
    }

    private void OnDisable()
    {
        RemoveCommandBuffers();
    }

    private void OnDestroy()
    {
        RemoveCommandBuffers();
        ReleaseRenderTexture();

        if (blurMaterial != null)
        {
            DestroyImmediate(blurMaterial);
        }

        if (blurMesh != null)
        {
            DestroyImmediate(blurMesh);
        }
    }

    private void CreateBlurMesh()
    {
        // 创建一个简单的Quad网格
        blurMesh = new Mesh();
        blurMesh.vertices = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 1, 0),
            new Vector3(0, 1, 0)
        };
        blurMesh.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };
        blurMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        blurMesh.RecalculateBounds();
    }

    private void CreateCommandBuffers()
    {
        Camera canvasCamera = GetCanvasCamera();
        if (canvasCamera == null) return;

        // 捕获CommandBuffer - 在UI渲染之前捕获
        captureBuffer = new CommandBuffer();
        captureBuffer.name = "UI_BackgroundBlur_Capture";
        canvasCamera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, captureBuffer);

        // 渲染CommandBuffer - 在UI渲染之后渲染模糊Quad
        renderBuffer = new CommandBuffer();
        renderBuffer.name = "UI_BackgroundBlur_Render";
        canvasCamera.AddCommandBuffer(CameraEvent.AfterEverything, renderBuffer);

        UpdateCommandBuffers();
    }

    private void RemoveCommandBuffers()
    {
        Camera canvasCamera = GetCanvasCamera();
        
        if (captureBuffer != null && canvasCamera != null)
        {
            canvasCamera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, captureBuffer);
            captureBuffer.Release();
            captureBuffer = null;
        }

        if (renderBuffer != null && canvasCamera != null)
        {
            canvasCamera.RemoveCommandBuffer(CameraEvent.AfterEverything, renderBuffer);
            renderBuffer.Release();
            renderBuffer = null;
        }
    }

    private Camera GetCanvasCamera()
    {
        if (parentCanvas != null)
        {
            return parentCanvas.worldCamera ?? Camera.main;
        }
        return Camera.main;
    }

    private void UpdateCommandBuffers()
    {
        if (captureBuffer == null || renderBuffer == null) return;

        Camera canvasCamera = GetCanvasCamera();
        if (canvasCamera == null) return;

        captureBuffer.Clear();
        renderBuffer.Clear();

        int width = Screen.width / blurDownsample;
        int height = Screen.height / blurDownsample;

        // 如果分辨率变化，重新创建RenderTexture
        if (width != lastWidth || height != lastHeight)
        {
            ReleaseRenderTexture();

            capturedRT = new RenderTexture(width, height, 0, format);
            capturedRT.filterMode = filterMode;
            capturedRT.wrapMode = TextureWrapMode.Clamp;
            capturedRT.Create();

            lastWidth = width;
            lastHeight = height;
        }

        // === 捕获阶段 ===
        captureBuffer.SetRenderTarget(capturedRT);
        captureBuffer.ClearRenderTarget(false, true, Color.clear);
        captureBuffer.Blit(BuiltinRenderTextureType.CurrentActive, capturedRT);

        // === 渲染阶段 ===
        // 计算模糊区域的世界坐标
        Matrix4x4 worldMatrix = CalculateBlurWorldMatrix();

        // 设置材质属性
        renderBuffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        renderBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
        renderBuffer.SetGlobalTexture(MainTexID, capturedRT);
        renderBuffer.SetGlobalFloat(SizeID, blurSize);
        renderBuffer.SetGlobalColor(ColorID, blurColor);
        renderBuffer.DrawMesh(blurMesh, worldMatrix, blurMaterial);
    }

    private Matrix4x4 CalculateBlurWorldMatrix()
    {
        // 获取RectTransform的世界坐标位置
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        Vector3 bottomLeft = corners[0];
        Vector3 topRight = corners[2];

        Vector3 center = (bottomLeft + topRight) * 0.5f;
        Vector3 scale = new Vector3(topRight.x - bottomLeft.x, topRight.y - bottomLeft.y, 1);

        return Matrix4x4.TRS(center, Quaternion.identity, scale);
    }

    private void ReleaseRenderTexture()
    {
        if (capturedRT != null)
        {
            capturedRT.Release();
            DestroyImmediate(capturedRT);
            capturedRT = null;
        }
    }

    private void Update()
    {
        if (!isInitialized || blurMaterial == null) return;

        // 更新模糊参数
        blurMaterial.SetFloat(SizeID, blurSize);
        blurMaterial.SetColor(ColorID, blurColor);

        // 如果RectTransform变化，更新CommandBuffer
        if (rectTransform.hasChanged)
        {
            UpdateCommandBuffers();
            rectTransform.hasChanged = false;
        }
    }

    private void OnValidate()
    {
        if (blurMaterial != null)
        {
            blurMaterial.SetFloat(SizeID, blurSize);
            blurMaterial.SetColor(ColorID, blurColor);
        }
    }

    /// <summary>
    /// 动态更新模糊区域
    /// </summary>
    public void SetBlurRect(Rect rect)
    {
        customRect = rect;
        useCustomRect = true;
        UpdateCommandBuffers();
    }

    /// <summary>
    /// 动态更新模糊强度
    /// </summary>
    public void SetBlurSize(float size)
    {
        blurSize = size;
        if (blurMaterial != null)
        {
            blurMaterial.SetFloat(SizeID, blurSize);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 在编辑器中显示模糊区域
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        
        if (rectTransform != null)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawCube(
                (corners[0] + corners[2]) * 0.5f,
                new Vector3(corners[2].x - corners[0].x, corners[2].y - corners[0].y, 0.01f)
            );
        }
    }
#endif
}
