using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BlackoutTransition : MonoBehaviour
{
    public static BlackoutTransition Instance { get; private set; }

    [SerializeField] private GameObject blackoutPrefab;
    private CanvasGroup blackoutCG;
    private GameObject blackoutInstance;
    private Coroutine _currentRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (blackoutPrefab != null)
        {
            blackoutInstance = Instantiate(blackoutPrefab);
            blackoutInstance.name = "BlackoutPanel";
            blackoutCG = blackoutInstance.GetComponent<CanvasGroup>();
            if (blackoutCG == null) blackoutCG = blackoutInstance.AddComponent<CanvasGroup>();
            blackoutInstance.SetActive(false);
        }
        else
        {
            CreateDefaultBlackPanel();
        }
    }

    private void CreateDefaultBlackPanel()
    {
        blackoutInstance = new GameObject("BlackoutPanel");

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            blackoutInstance.transform.SetParent(canvas.transform, false);
            blackoutInstance.transform.SetAsLastSibling();
        }
        else
        {
            blackoutInstance.transform.SetParent(transform, false);
        }

        var rt = blackoutInstance.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        blackoutInstance.AddComponent<Image>().color = Color.black;
        blackoutCG = blackoutInstance.AddComponent<CanvasGroup>();
        blackoutInstance.SetActive(false);
    }

    public bool IsTransitioning => _currentRoutine != null;

    private void StopCurrent()
    {
        if (_currentRoutine != null)
        {
            StopCoroutine(_currentRoutine);
            _currentRoutine = null;
        }
    }

    /// <summary>
    /// 停止所有协程并隐藏黑幕（读档时调用，防止残留协程回调重新激活 Dialog）
    /// </summary>
    public void ResetState()
    {
        StopCurrent();
        if (blackoutInstance != null)
            blackoutInstance.SetActive(false);
    }

    /// <summary>黑幕淡入 → 停留 → 淡出 → 回调</summary>
    public void FadeInOut(
        Action onComplete = null,
        float fadeInDuration = 0.3f,
        float holdDuration = 0.2f,
        float fadeOutDuration = 0.3f)
    {
        StopCurrent();
        _currentRoutine = StartCoroutine(FadeInOutRoutine(onComplete, fadeInDuration, holdDuration, fadeOutDuration));
    }

    /// <summary>黑幕直接显示 → 停留 → 淡出 → 回调</summary>
    public void FadeOutOnly(
        Action onComplete = null,
        float holdDuration = 0.2f,
        float fadeOutDuration = 0.3f)
    {
        StopCurrent();
        _currentRoutine = StartCoroutine(FadeOutOnlyRoutine(onComplete, holdDuration, fadeOutDuration));
    }

    /// <summary>黑幕淡入 → 全黑时回调 → 停留 → 淡出</summary>
    public void FadeInWithCallback(
        Action onBlackout,
        float fadeInDuration = 0.3f,
        float holdDuration = 0f,
        float fadeOutDuration = 0.3f)
    {
        StopCurrent();
        _currentRoutine = StartCoroutine(FadeInWithCallbackRoutine(onBlackout, fadeInDuration, holdDuration, fadeOutDuration));
    }

    /// <summary>黑幕直接显示 → 立即回调 → 停留 → 淡出 → 最终回调</summary>
    public void FadeOutWithCallback(
        Action onBlackout,
        Action onComplete = null,
        float holdDuration = 0.2f,
        float fadeOutDuration = 0.3f)
    {
        StopCurrent();
        _currentRoutine = StartCoroutine(FadeOutWithCallbackRoutine(onBlackout, onComplete, holdDuration, fadeOutDuration));
    }

    private IEnumerator FadeInWithCallbackRoutine(Action onBlackout, float fadeIn, float hold, float fadeOut)
    {
        blackoutInstance.SetActive(true);
        blackoutCG.blocksRaycasts = false;
        yield return StartCoroutine(Fade(0f, 1f, fadeIn));
        onBlackout?.Invoke();
        if (hold > 0f) yield return new WaitForSeconds(hold);
        yield return StartCoroutine(Fade(1f, 0f, fadeOut));
        blackoutInstance.SetActive(false);
        _currentRoutine = null;
    }

    private IEnumerator FadeInOutRoutine(Action onComplete, float fadeIn, float hold, float fadeOut)
    {
        blackoutInstance.SetActive(true);
        blackoutCG.blocksRaycasts = false;
        yield return StartCoroutine(Fade(0f, 1f, fadeIn));
        if (hold > 0f) yield return new WaitForSeconds(hold);
        yield return StartCoroutine(Fade(1f, 0f, fadeOut));
        blackoutInstance.SetActive(false);
        _currentRoutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator FadeOutOnlyRoutine(Action onComplete, float hold, float fadeOut)
    {
        blackoutInstance.SetActive(true);
        blackoutCG.alpha = 1f;
        blackoutCG.blocksRaycasts = false;
        if (hold > 0f) yield return new WaitForSeconds(hold);
        yield return StartCoroutine(Fade(1f, 0f, fadeOut));
        blackoutInstance.SetActive(false);
        _currentRoutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator FadeOutWithCallbackRoutine(Action onBlackout, Action onComplete, float hold, float fadeOut)
    {
        blackoutInstance.SetActive(true);
        blackoutCG.alpha = 1f;
        blackoutCG.blocksRaycasts = false;
        onBlackout?.Invoke();
        if (hold > 0f) yield return new WaitForSeconds(hold);
        yield return StartCoroutine(Fade(1f, 0f, fadeOut));
        blackoutInstance.SetActive(false);
        _currentRoutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f) { blackoutCG.alpha = to; yield break; }
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            blackoutCG.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        blackoutCG.alpha = to;
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
