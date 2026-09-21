using UnityEngine;

[RequireComponent(typeof(Camera))]
public class Letterbox : MonoBehaviour
{
    [Header("目标宽高比")]
    [Tooltip("例如16:9=1.777，4:3=1.333，21:9=2.333")]
    public float targetAspect = 16f / 9f;

    private Camera mainCam;
    private Camera backgroundCam; // 用于显示黑边背景

    void Start()
    {
        mainCam = GetComponent<Camera>();
        CreateBackgroundCamera();
        UpdateLetterbox();
    }

    void Update()
    {
        // 窗口大小变化时更新黑边（PC端）
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            UpdateLetterbox();
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
        }
    }

    private int lastScreenWidth;
    private int lastScreenHeight;

    void UpdateLetterbox()
    {
        // 计算当前屏幕宽高比
        float windowAspect = (float)Screen.width / Screen.height;
        
        // 计算缩放比例
        float scaleHeight = windowAspect / targetAspect;
        float scaleWidth = targetAspect / windowAspect;

        // 设置视口矩形
        if (windowAspect > targetAspect)
        {
            // 屏幕更宽 → 左右黑边（Pillarbox）
            float letterboxWidth = 1f - scaleHeight;
            mainCam.rect = new Rect(letterboxWidth / 2f, 0f, scaleHeight, 1f);
        }
        else
        {
            // 屏幕更高 → 上下黑边（Letterbox）
            float letterboxHeight = 1f - scaleWidth;
            mainCam.rect = new Rect(0f, letterboxHeight / 2f, 1f, scaleWidth);
        }
    }

    void CreateBackgroundCamera()
    {
        // 创建背景相机显示黑色
        if (!backgroundCam)
        {
            GameObject bgCamObj = new GameObject("BackgroundCamera");
            backgroundCam = bgCamObj.AddComponent<Camera>();
            backgroundCam.depth = int.MinValue; // 渲染在最底层
            backgroundCam.clearFlags = CameraClearFlags.SolidColor;
            backgroundCam.backgroundColor = Color.black;
            backgroundCam.orthographic = true;
            backgroundCam.orthographicSize = 5;
            backgroundCam.cullingMask = 0; // 不渲染任何图层
        }
    }

    void OnValidate()
    {
        if (mainCam) UpdateLetterbox();
    }
}
