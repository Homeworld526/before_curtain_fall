using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Video;

/// <summary>
/// Demo 结束 + 制作人名单 UI 覆盖层
/// 两页式：结束语 → 制作人名单 → 返回主菜单
/// </summary>
public class DemoCreditsOverlay : MonoBehaviour
{
    public static DemoCreditsOverlay Instance { get; private set; }

    [Header("UI 引用")]
    [SerializeField] private CanvasGroup overlayGroup;   // 整体黑底
    [SerializeField] private CanvasGroup titleGroup;     // 结束语页（全部完成）
    [SerializeField] private CanvasGroup titleGroupIncomplete; // 结束语页（未全部完成）
    [SerializeField] private CanvasGroup creditsGroup;   // 名单页
    [SerializeField] private VideoPlayer vc;
    
    [Header("时间参数")]
    [SerializeField] private float fadeInDuration = 1.5f;
    [SerializeField] private float titleFadeDuration = 0.8f;
    [SerializeField] private float titleHoldTime = 2.5f;
    [SerializeField] private float creditsFadeDuration = 0.8f;
    [SerializeField] private float creditsHoldTime = 5f;
    [SerializeField] private float fadeOutDuration = 1f;

    private bool isPlaying = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 初始状态：整个 overlay 隐藏
        if (overlayGroup != null)
        {
            overlayGroup.alpha = 0f;
            overlayGroup.blocksRaycasts = false;
        }
        if (titleGroup != null) titleGroup.alpha = 0f;
        if (creditsGroup != null) creditsGroup.alpha = 0f;

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 播放完整版结束流程（全部剧情完成）
    /// </summary>
    public void Play()
    {
        if (isPlaying) return;

        isPlaying = true;
        gameObject.SetActive(true);
        StartCoroutine(PlaySequence(titleGroup));
    }

    /// <summary>
    /// 播放未完成版结束流程（剧情未全部完成）
    /// </summary>
    public void PlayIncomplete()
    {
        if (isPlaying) return;

        isPlaying = true;
        gameObject.SetActive(true);
        StartCoroutine(PlaySequence(titleGroupIncomplete != null ? titleGroupIncomplete : titleGroup));
    }

    /// <summary>
    /// 强制播放完整版（忽略已播放标记，用于测试）
    /// </summary>
    [ContextMenu("测试播放")]
    public void PlayForce()
    {
        isPlaying = true;
        gameObject.SetActive(true);
        StartCoroutine(PlaySequence(titleGroup));
    }

    private IEnumerator PlaySequence(CanvasGroup activeTitleGroup)
    {
        // 确保初始状态
        overlayGroup.alpha = 0f;
        if (titleGroup != null) titleGroup.alpha = 0f;
        if (titleGroupIncomplete != null) titleGroupIncomplete.alpha = 0f;
        creditsGroup.alpha = 0f;
        overlayGroup.blocksRaycasts = true;

        vc.Prepare();

        // 1) 黑屏淡入
        yield return FadeInOut.FadeIn(overlayGroup, 1f, fadeInDuration, this);
        yield return new WaitForSeconds(0.5f);

        // 2) 结束语淡入 → 停留 → 淡出
        yield return FadeInOut.FadeIn(activeTitleGroup, 1f, titleFadeDuration, this);
        yield return new WaitForSeconds(titleHoldTime);
        yield return FadeInOut.FadeOut(activeTitleGroup, 0f, titleFadeDuration, this);
        yield return new WaitForSeconds(0.5f);

        // 3) 制作人名单淡入（定格第一帧）→ 完全显示后播放 → 停在最后一帧 → 淡出
        yield return new WaitUntil(() => vc.isPrepared);
        vc.isLooping = false;
        // 播放一帧后立即暂停，使渐入期间显示第一帧静帧
        vc.Play();
        yield return null;
        vc.Pause();

        yield return FadeInOut.FadeIn(creditsGroup, 1f, creditsFadeDuration, this);

        bool videoEnded = false;
        vc.loopPointReached += _ => videoEnded = true;
        vc.Play();
        yield return new WaitUntil(() => videoEnded);

        yield return FadeInOut.FadeOut(creditsGroup, 0f, creditsFadeDuration, this);
        yield return new WaitForSeconds(0.5f);

        // 4) 保持黑屏，直接返回主菜单
        isPlaying = false;

        PlayerPrefs.SetInt("dialogIndex", 0);
        PlayerPrefs.SetInt("dialogLine", 0);
        SceneCenter.Instance.ChangeScene("Start");
    }

    private void OnDestroy()
    {
        // 清空静态变量，避免指向已销毁的对象
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
