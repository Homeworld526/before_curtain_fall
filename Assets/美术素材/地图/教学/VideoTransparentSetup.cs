using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoTransparentSetup : MonoBehaviour
{
    public VideoClip videoClip;
    [Range(0f, 1f)]
    public float overallAlpha = 1f;

    [Tooltip("拖入 UI/VideoTransparent shader，打包后不会丢失")]
    public Shader videoShader;

    private VideoPlayer videoPlayer;
    private RenderTexture renderTexture;
    private Material videoMaterial;

    void Awake()
    {
        RawImage rawImage = GetComponent<RawImage>();
        RectTransform rt = GetComponent<RectTransform>();

        int w = rt != null ? Mathf.Max(64, (int)rt.sizeDelta.x) : 256;
        int h = rt != null ? Mathf.Max(64, (int)rt.sizeDelta.y) : 256;

        renderTexture = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        renderTexture.Create();

        rawImage.texture = renderTexture;

        // 去黑底材质
        Shader shader = videoShader;
        if (shader == null)
        {
            // 兜底：运行时查找（仅编辑器中可靠）
            shader = Shader.Find("UI/VideoTransparent");
        }
        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.SetFloat("_Threshold", 0.02f);
            mat.SetFloat("_OverallAlpha", overallAlpha);
            rawImage.material = mat;
            videoMaterial = mat;
        }
        else
        {
            Debug.LogError("找不到 Shader UI/VideoTransparent，请在 Inspector 中拖入 shader 或确认 VideoTransparent.shader 存在");
        }

        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = videoClip;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.playOnAwake = true;
        videoPlayer.isLooping = true;

        Debug.Log($"VideoTransparentSetup: RT={w}x{h}, clip={videoClip != null}");
    }

    void Update()
    {
        if (videoMaterial != null)
            videoMaterial.SetFloat("_OverallAlpha", overallAlpha);
    }

    void OnDestroy()
    {
        if (videoMaterial != null)
            Destroy(videoMaterial);
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }
}
