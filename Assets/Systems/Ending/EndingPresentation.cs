using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class EndingPresentation : MonoBehaviour
{
    public static EndingPresentation Instance { get; private set; }

    [SerializeField] private Image backgroundImage;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private float fadeDuration = 0.5f;

    private Coroutine _showRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            throw new System.InvalidOperationException("场景中只能存在一个 EndingPresentation 组件。");

        Instance = this;
        backgroundImage = backgroundImage != null ? backgroundImage : GetComponent<Image>();
        canvasGroup = canvasGroup != null ? canvasGroup : GetComponent<CanvasGroup>();
        if (backgroundImage == null || canvasGroup == null)
            throw new System.InvalidOperationException("EndingPresentation 需要同一对象上的 Image 和 CanvasGroup 组件。");

        CreateTextIfNeeded();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void Show(EndingConfig.Entry entry)
    {
        if (entry == null)
            throw new System.ArgumentNullException(nameof(entry));

        backgroundImage.sprite = entry.background;
        backgroundImage.color = Color.white;
        titleText.text = entry.title;
        descriptionText.text = entry.description;

        if (_showRoutine != null)
            StopCoroutine(_showRoutine);
        _showRoutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        yield return FadeInOut.FadeIn(canvasGroup, 1f, fadeDuration, this);

        // Do not consume the click that advanced the final dialog line.
        yield return null;
        while (!Input.GetMouseButtonUp(0))
            yield return null;

        EndingService.CompletePresentation();
        PlayerPrefs.SetInt("dialogIndex", 0);
        PlayerPrefs.SetInt("dialogLine", 0);
        SceneCenter.Instance.ChangeScene("Start");
    }

    private void CreateTextIfNeeded()
    {
        if (titleText == null)
        {
            titleText = CreateText(
                "EndingTitle",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -170f),
                new Vector2(1500f, 120f),
                52f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
        }

        if (descriptionText == null)
        {
            descriptionText = CreateText(
                "EndingDescription",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 150f),
                new Vector2(1360f, 180f),
                28f,
                FontStyles.Normal,
                TextAlignmentOptions.Center);
        }
    }

    private TextMeshProUGUI CreateText(
        string objectName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment)
    {
        if (TMP_Settings.defaultFontAsset == null)
            throw new System.InvalidOperationException("EndingPresentation 需要 TextMeshPro 默认字体。");

        var textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(transform, false);

        var rectTransform = (RectTransform)textObject.transform;
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = Color.white;
        text.enableWordWrapping = true;
        text.enableAutoSizing = true;
        text.fontSizeMin = 20f;
        text.fontSizeMax = fontSize;
        text.raycastTarget = false;
        return text;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
