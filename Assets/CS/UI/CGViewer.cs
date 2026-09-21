using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CGViewer : MonoBehaviour, IPointerClickHandler
{
    public static CGViewer Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        // 清空静态变量，避免指向已销毁的对象
        if (Instance == this)
        {
            Instance = null;
        }
    }

    [Header("UI References")]
    [SerializeField] private RawImage cgImage;        // 显示CG的Image
    [SerializeField] private TextMeshProUGUI workingText;      // 进度条文本
    // [SerializeField] private Slider progressBar;       // 进度条
    // [SerializeField] private GameObject progressPanel; // 进度条面板（可选）

    [Header("Settings")]
    [SerializeField] private float displayDuration = 2f; // 显示时间
    [SerializeField] private Texture2D[] cgTextures;    // CG图片数组

    private Coroutine showCoroutine;
    private bool isShowing = false;
    [Header("Background Mask")]
    [SerializeField] private GameObject bgMask; // 背景蒙版（Image，黑色半透明）
    [SerializeField] private float maskAlpha = 0.5f; // 蒙版透明度

    // 初始化时隐藏CG和进度条
    private void Start()
    {
        if (cgImage != null)
        {
            cgImage.gameObject.SetActive(false);
        }

        workingText.gameObject.SetActive(false);
        // progressBar.gameObject.SetActive(false);
        // progressPanel.SetActive(false);
        if (bgMask != null)
            bgMask.SetActive(false);
    }

    // 按钮点击事件
    public void ShowCG(int cgIndex = 0)
    {
        if (isShowing) return;

        // 检查CG索引是否有效
        if (cgTextures == null || cgTextures.Length == 0 || cgIndex >= cgTextures.Length)
        {
            Debug.LogError("CG纹理无效或索引越界！");
            return;
        }

        // 启动显示协程
        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
        }
        showCoroutine = StartCoroutine(ShowCGRoutine(cgIndex));
    }

    // 显示CG的协程
    private IEnumerator ShowCGRoutine(int cgIndex)
    {
        isShowing = true;

        if (bgMask != null)
        {
            bgMask.SetActive(true);

            Image maskImg = bgMask.GetComponent<Image>();
            if (maskImg != null)
                maskImg.color = new Color(0, 0, 0, maskAlpha);
        }

        // 设置CG图片并显示
        cgImage.texture = cgTextures[cgIndex];
        cgImage.gameObject.SetActive(true);
        SetCGImageToFront();

        workingText.gameObject.SetActive(true);

        // 显示进度条
        // if (progressBar != null)
        // {
        //     progressBar.gameObject.SetActive(true);
        //     progressBar.value = 0f;
        // }

        // if (progressPanel != null)
        // {
        //     progressPanel.SetActive(true);
        // }

        // 进度条动画
        float timer = 0f;
        while (timer < displayDuration)
        {
            timer += Time.deltaTime;

            // 更新进度条
            // if (progressBar != null)
            // {
            //     progressBar.value = timer / displayDuration;
            // }

            yield return null;
        }

        // 确保进度条满
        // if (progressBar != null)
        // {
        //     progressBar.value = 1f;
        // }

        // 延迟一小段时间显示进度条完成
        yield return new WaitForSeconds(0.2f);

        // 隐藏所有UI
        HideCG();
    }

    // 设置CG图片游戏对象到最前面
    private void SetCGImageToFront()
    {
        if (cgImage != null)
        {
            cgImage.transform.SetAsLastSibling();
        }
    }

    // 隐藏CG
    public void HideCG()
    {
        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
        }

        cgImage.gameObject.SetActive(false);
        workingText.gameObject.SetActive(false);

        // progressBar.gameObject.SetActive(false);
        // progressPanel.SetActive(false);
        if (bgMask != null)
            bgMask.SetActive(false);
        isShowing = false;
    }

    // 点击屏幕任意位置可跳过
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isShowing)
        {
            HideCG();
        }
    }

    // 快捷方法：通过按钮调用
    // 可选：预加载CG
    // public void PreloadCG(int cgIndex)
    // {
    //     if (cgTextures != null && cgIndex < cgTextures.Length)
    //     {
    //         Resources.UnloadUnusedAssets();
    //     }
    // }
}
