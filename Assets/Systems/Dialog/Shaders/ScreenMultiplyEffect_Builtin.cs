using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class ScreenMultiplyEffect_Builtin : MonoBehaviour
{
    [Tooltip("使用ScreenMultiplyMask_BuiltIn Shader的材质")]
    public Material multiplyMaterial;
    [Tooltip("蒙版色卡图片（必须赋值）")]
    public Texture2D maskTexture;
    [Tooltip("仅对该层级的UI应用正片叠底效果")]
    public LayerMask targetUILayer;
    [Tooltip("调试模式：打印关键日志，帮助定位问题")]
    public bool debugMode = true;

    // 核心资源
    private RenderTexture _screenRT;
    private Camera _mainCamera;
    private Camera _tempCamera;
    private List<CanvasState> _targetCanvasStates = new List<CanvasState>();

    // 状态标记
    private bool _isInitialized = false;
    // 分辨率监听（兼容低版本Unity）
    private int _lastScreenWidth;
    private int _lastScreenHeight;

    // 辅助类：完整记录Canvas状态
    private class CanvasState
    {
        public Canvas canvas;
        public RenderMode originalRenderMode;
        public Camera originalWorldCamera;
        public float originalPlaneDistance;
        public float originalScaleFactor;
        public bool originalPixelPerfect;
    }

    #region 生命周期：支持反复开关（兼容低版本）
    private void Awake()
    {
        _mainCamera = GetComponent<Camera>();
        // 记录初始分辨率（兼容低版本监听）
        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;
    }

    private void Start()
    {
        if (enabled)
        {
            InitResources();
        }
    }

    private void OnEnable()
    {
        // 二次开启前：先恢复状态+清空旧RT
        RestoreCanvasStates();
        if (_screenRT != null) Destroy(_screenRT);
        _screenRT = null;
        // 重置分辨率记录
        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;

        if (!_isInitialized && _mainCamera != null)
        {
            InitResources();
        }
    }

    private void OnDisable()
    {
        CleanupResources();
    }

    private void OnDestroy()
    {
        CleanupResources();
    }

    // 兼容低版本：Update中监听分辨率变化
    private void Update()
    {
        if (_isInitialized && (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight))
        {
            // 分辨率变化，同步RT和临时相机
            OnScreenResize();
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
        }
    }
    #endregion

    #region 核心方法：初始化/清理/分辨率同步
    private void InitResources()
    {
        if (_mainCamera == null || Screen.width == 0 || Screen.height == 0)
        {
            Debug.LogError("[MultiplyEffect] 初始化失败：主相机/分辨率异常", this);
            _isInitialized = false;
            return;
        }

        // 1. 初始化RT（强制同步当前屏幕分辨率）
        InitRenderTexture();

        // 2. 创建临时相机（仅同步可写参数）
        CreateTempCamera();

        // 3. 收集并修改目标UI Canvas
        CollectAndModifyTargetCanvases();

        _isInitialized = true;

        if (debugMode)
        {
            Debug.Log($"[MultiplyEffect] 初始化完成 | RT分辨率：{_screenRT.width}x{_screenRT.height} | 屏幕分辨率：{Screen.width}x{Screen.height}", this);
        }
    }

    /// <summary>
    /// 初始化RT：强制使用当前屏幕真实分辨率
    /// </summary>
    private void InitRenderTexture()
    {
        // 彻底销毁旧RT（避免低分辨率残留）
        if (_screenRT != null)
        {
            _screenRT.Release();
            DestroyImmediate(_screenRT);
            _screenRT = null;
        }

        // 获取当前屏幕真实分辨率
        int currentScreenWidth = Screen.width;
        int currentScreenHeight = Screen.height;

        // 创建RT：匹配屏幕分辨率，关闭mipmap避免降采样
        _screenRT = new RenderTexture(currentScreenWidth, currentScreenHeight, 24, RenderTextureFormat.Default)
        {
            name = "ScreenWithTargetUI_RT",
            antiAliasing = 1,
            enableRandomWrite = true,
            autoGenerateMips = false,
            filterMode = FilterMode.Bilinear,
            useMipMap = false
        };
        _screenRT.Create();

        // 校验RT分辨率
        if (debugMode)
        {
            if (_screenRT.width != currentScreenWidth || _screenRT.height != currentScreenHeight)
            {
                Debug.LogWarning($"[MultiplyEffect] RT分辨率不匹配！RT：{_screenRT.width}x{_screenRT.height} | 屏幕：{currentScreenWidth}x{currentScreenHeight}", this);
            }
        }
    }

    /// <summary>
    /// 创建临时相机：仅同步可写参数（兼容所有版本）
    /// </summary>
    private void CreateTempCamera()
    {
        if (_tempCamera != null)
        {
            DestroyImmediate(_tempCamera.gameObject);
            _tempCamera = null;
        }

        GameObject tempCamObj = new GameObject("TempRenderCamera", typeof(Camera));
        tempCamObj.hideFlags = HideFlags.HideAndDontSave;
        _tempCamera = tempCamObj.GetComponent<Camera>();

        // 复制基础参数
        _tempCamera.CopyFrom(_mainCamera);
        _tempCamera.enabled = false;
        _tempCamera.targetTexture = null;

        // 同步可写的像素/投影参数
        SyncTempCameraPixelParams();
    }

    /// <summary>
    /// 同步临时相机的可写像素参数
    /// </summary>
    private void SyncTempCameraPixelParams()
    {
        if (_tempCamera == null || _mainCamera == null) return;

        // 仅同步可写参数，间接保证像素分辨率一致
        _tempCamera.aspect = _mainCamera.aspect;
        _tempCamera.pixelRect = _mainCamera.pixelRect;
        _tempCamera.fieldOfView = _mainCamera.fieldOfView;
        _tempCamera.orthographic = _mainCamera.orthographic;

        if (_tempCamera.orthographic)
        {
            _tempCamera.orthographicSize = _mainCamera.orthographicSize;
        }
    }

    private void CollectAndModifyTargetCanvases()
    {
        _targetCanvasStates.Clear();
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        int targetCount = 0;

        foreach (Canvas canvas in allCanvases)
        {
            bool isTargetLayer = ((1 << canvas.gameObject.layer) & targetUILayer) != 0;
            bool isOverlay = canvas.renderMode == RenderMode.ScreenSpaceOverlay;

            if (canvas.enabled && isTargetLayer && isOverlay)
            {
                _targetCanvasStates.Add(new CanvasState
                {
                    canvas = canvas,
                    originalRenderMode = canvas.renderMode,
                    originalWorldCamera = canvas.worldCamera,
                    originalPlaneDistance = canvas.planeDistance,
                    originalScaleFactor = canvas.scaleFactor,
                    originalPixelPerfect = canvas.pixelPerfect
                });

                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = _tempCamera;
                canvas.planeDistance = _mainCamera.nearClipPlane + 0.1f;
                canvas.scaleFactor = canvas.scaleFactor;
                canvas.pixelPerfect = false;
                targetCount++;
            }
        }

        if (debugMode)
        {
            Debug.Log($"[MultiplyEffect] 找到{targetCount}个目标UI Canvas", this);
        }
    }

    private void CleanupResources()
    {
        RestoreCanvasStates();

        // 彻底销毁临时相机
        if (_tempCamera != null)
        {
            DestroyImmediate(_tempCamera.gameObject);
            _tempCamera = null;
        }

        // 彻底销毁RT
        if (_screenRT != null)
        {
            _screenRT.Release();
            DestroyImmediate(_screenRT);
            _screenRT = null;
        }

        _isInitialized = false;

        if (debugMode)
        {
            Debug.Log("[MultiplyEffect] 资源已清理，脚本已禁用", this);
        }
    }

    private void RestoreCanvasStates()
    {
        foreach (var state in _targetCanvasStates)
        {
            if (state.canvas != null && state.canvas.enabled)
            {
                state.canvas.renderMode = state.originalRenderMode;
                state.canvas.worldCamera = state.originalWorldCamera;
                state.canvas.planeDistance = state.originalPlaneDistance;
                state.canvas.scaleFactor = state.originalScaleFactor;
                state.canvas.pixelPerfect = state.originalPixelPerfect;
            }
        }
        _targetCanvasStates.Clear();
    }

    /// <summary>
    /// 分辨率变化时的同步逻辑（兼容低版本）
    /// </summary>
    private void OnScreenResize()
    {
        if (_isInitialized)
        {
            if (debugMode) Debug.Log("[MultiplyEffect] 屏幕分辨率变化，重新初始化RT", this);
            InitRenderTexture(); // 同步新分辨率
            if (_tempCamera != null) SyncTempCameraPixelParams(); // 同步临时相机参数
        }
    }
    #endregion

    #region 渲染逻辑：保证分辨率一致
    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (!_isInitialized || multiplyMaterial == null || maskTexture == null ||
            _screenRT == null || !_screenRT.IsCreated() || _tempCamera == null)
        {
            Graphics.Blit(src, dest);
            return;
        }

        // 实时校验RT分辨率
        if (_screenRT.width != Screen.width || _screenRT.height != Screen.height)
        {
            InitRenderTexture();
        }

        // 清空RT
        RenderTexture.active = _screenRT;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = null;

        // 临时相机渲染到RT
        _tempCamera.targetTexture = _screenRT;
        _tempCamera.Render();
        _tempCamera.targetTexture = null;

        // 应用正片叠底效果
        multiplyMaterial.SetTexture("_MaskTex", maskTexture);
        Graphics.Blit(_screenRT, dest, multiplyMaterial);

        RenderTexture.active = null;
    }
    #endregion

    #region 辅助验证
    private void OnValidate()
    {
        if (multiplyMaterial != null && multiplyMaterial.shader == null)
        {
            Debug.LogError("正片叠底材质的Shader无效！", this);
            multiplyMaterial = null;
        }
    }
    #endregion
}
