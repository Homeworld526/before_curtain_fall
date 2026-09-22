using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogPreviewUI : MonoBehaviour
{
    [SerializeField] private DialogPreviewManager manager;
    [SerializeField] private TMP_Text filePathText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Toggle middleToggle;
    [SerializeField] private Button selectBtn;
    [SerializeField] private Button startBtn;
    [SerializeField] private Button resetBtn;

    private string _loadedCsvText;
    private string _loadedFilePath;
    private GameObject _validationWindow;
    private TMP_Text _validationWindowText;
    private TMP_Text _validationWindowTitle;
    private Button _validationDetailsButton;
    private Button _batchValidationButton;
    private bool _hasValidationDetails;

    private void Start()
    {
        selectBtn.onClick.AddListener(OnSelectFile);
        startBtn.onClick.AddListener(OnStart);
        resetBtn.onClick.AddListener(OnReset);
        middleToggle.onValueChanged.AddListener(OnMiddleToggleChanged);
        startBtn.interactable = false;
        CreateValidationDetailsButton();
        CreateBatchValidationButton();
        CreateValidationWindow();
    }

    private void OnDestroy()
    {
        selectBtn.onClick.RemoveListener(OnSelectFile);
        startBtn.onClick.RemoveListener(OnStart);
        resetBtn.onClick.RemoveListener(OnReset);
        middleToggle.onValueChanged.RemoveListener(OnMiddleToggleChanged);

        if (_validationDetailsButton != null)
            _validationDetailsButton.onClick.RemoveListener(ShowLastValidation);
        if (_batchValidationButton != null)
            _batchValidationButton.onClick.RemoveListener(OnBatchValidate);

        if (_validationWindow != null)
            Destroy(_validationWindow);
        if (_validationDetailsButton != null)
            Destroy(_validationDetailsButton.gameObject);
        if (_batchValidationButton != null)
            Destroy(_batchValidationButton.gameObject);
    }

    private void OnMiddleToggleChanged(bool value)
    {
        PicRSetMiddleEffect.Instance.StartEffect(value);
    }

    private void OnSelectFile()
    {
        var path = manager.SelectCsvFile();
        if (string.IsNullOrEmpty(path)) return;

        // 选择另一份文件即结束上一轮预览，避免在校验失败后仍残留旧剧情画面或声音。
        manager.StopPreview();
        try
        {
            string csvText = manager.LoadCsvAsUtf8(path);
            DialogPreviewValidationResult validation = manager.ValidateCsv(csvText);
            filePathText.text = path;
            statusText.text = validation.ToDisplayText();
            UpdateValidationDetails(validation, path);

            if (validation.IsValid)
            {
                _loadedCsvText = csvText;
                _loadedFilePath = path;
                startBtn.interactable = true;
            }
            else
            {
                _loadedCsvText = null;
                _loadedFilePath = null;
                startBtn.interactable = false;
            }
        }
        catch (Exception e)
        {
            _loadedCsvText = null;
            _loadedFilePath = null;
            statusText.text = $"加载失败：{e.Message}";
            startBtn.interactable = false;
            HideValidationDetails();
        }
    }

    private void OnStart()
    {
        if (_loadedCsvText == null) return;
        try
        {
            manager.StartPreview(_loadedCsvText, middleToggle.isOn, _loadedFilePath);
            statusText.text = "预览中...";
        }
        catch (Exception e)
        {
            manager.StopPreview();
            statusText.text = $"无法启动预览：{e.Message}";
            startBtn.interactable = false;
        }
    }

    private void OnBatchValidate()
    {
        string[] paths = manager.SelectCsvFiles();
        if (paths == null || paths.Length == 0)
            return;

        int passedFiles = 0;
        int failedFiles = 0;
        int warningCount = 0;
        int errorCount = 0;
        var builder = new StringBuilder();
        builder.AppendLine($"已选择 {paths.Length} 个文件");
        builder.AppendLine();

        for (int i = 0; i < paths.Length; i++)
        {
            string path = paths[i];
            string fileName = System.IO.Path.GetFileName(path);
            try
            {
                DialogPreviewValidationResult validation = manager.ValidateCsv(manager.LoadCsvAsUtf8(path));
                warningCount += validation.Warnings.Count;
                errorCount += validation.Errors.Count;

                if (validation.IsValid)
                {
                    passedFiles++;
                    if (validation.Warnings.Count == 0)
                    {
                        builder.AppendLine($"[通过] {fileName}");
                    }
                    else
                    {
                        builder.AppendLine($"[通过，有警告] {fileName}（{validation.Warnings.Count} 条）");
                        AppendDetails(builder, validation.Warnings);
                    }
                }
                else
                {
                    failedFiles++;
                    builder.AppendLine($"[失败] {fileName}（{validation.Errors.Count} 项错误）");
                    AppendDetails(builder, validation.Errors);
                    if (validation.Warnings.Count > 0)
                    {
                        builder.AppendLine($"  警告（{validation.Warnings.Count}）");
                        AppendDetails(builder, validation.Warnings);
                    }
                }
            }
            catch (Exception e)
            {
                failedFiles++;
                errorCount++;
                builder.AppendLine($"[失败] {fileName}（无法读取）");
                builder.AppendLine($"  - {e.Message}");
            }

            if (i < paths.Length - 1)
                builder.AppendLine();
        }

        string summary = $"批量校验：{paths.Length} 个文件，通过 {passedFiles}，失败 {failedFiles}，警告 {warningCount}";
        statusText.text = summary;
        builder.Insert(0, summary + $"，错误 {errorCount} 项\n\n");
        SetValidationDetails("批量 CSV 校验结果", builder.ToString(), true);
        Debug.Log("[DialogPreview] " + summary + $"，错误 {errorCount} 项。");
    }

    private void OnReset()
    {
        manager.StopPreview();
        _loadedCsvText = null;
        _loadedFilePath = null;
        filePathText.text = "未选择文件";
        statusText.text = "";
        startBtn.interactable = false;
        HideValidationDetails();
    }

    private void CreateValidationDetailsButton()
    {
        if (_validationDetailsButton != null) return;

        var buttonObject = new GameObject("ValidationDetailsButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(transform, false);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-32f, -112f);
        rect.sizeDelta = new Vector2(180f, 36f);

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.16f, 0.22f, 0.3f, 0.96f);

        _validationDetailsButton = buttonObject.GetComponent<Button>();
        _validationDetailsButton.targetGraphic = image;
        _validationDetailsButton.onClick.AddListener(ShowLastValidation);

        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 2f);
        labelRect.offsetMax = new Vector2(-8f, -2f);

        var label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "查看校验详情";
        label.fontSize = 18f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        if (statusText != null) label.font = statusText.font;

        _validationDetailsButton.gameObject.SetActive(false);
    }

    private void CreateBatchValidationButton()
    {
        if (_batchValidationButton != null) return;

        var buttonObject = new GameObject("BatchValidationButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(transform, false);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-222f, -112f);
        rect.sizeDelta = new Vector2(160f, 36f);

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.14f, 0.3f, 0.22f, 0.96f);

        _batchValidationButton = buttonObject.GetComponent<Button>();
        _batchValidationButton.targetGraphic = image;
        _batchValidationButton.onClick.AddListener(OnBatchValidate);

        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 2f);
        labelRect.offsetMax = new Vector2(-8f, -2f);

        var label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "批量校验";
        label.fontSize = 18f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        if (statusText != null) label.font = statusText.font;
    }

    private void CreateValidationWindow()
    {
        if (_validationWindow != null) return;

        _validationWindow = new GameObject("ValidationWindow", typeof(RectTransform), typeof(Image));
        _validationWindow.transform.SetParent(transform, false);

        var windowRect = _validationWindow.GetComponent<RectTransform>();
        windowRect.anchorMin = new Vector2(0.5f, 0.5f);
        windowRect.anchorMax = new Vector2(0.5f, 0.5f);
        windowRect.pivot = new Vector2(0.5f, 0.5f);
        windowRect.anchoredPosition = Vector2.zero;
        windowRect.sizeDelta = new Vector2(1120f, 680f);

        var windowImage = _validationWindow.GetComponent<Image>();
        windowImage.color = new Color(0.035f, 0.045f, 0.065f, 0.98f);

        _validationWindowTitle = CreateWindowText("Title", _validationWindow.transform, 24f, TextAlignmentOptions.Left);
        var titleRect = _validationWindowTitle.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(28f, -64f);
        titleRect.offsetMax = new Vector2(-160f, -20f);

        var closeObject = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeObject.transform.SetParent(_validationWindow.transform, false);
        var closeRect = closeObject.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-24f, -22f);
        closeRect.sizeDelta = new Vector2(110f, 38f);
        closeObject.GetComponent<Image>().color = new Color(0.28f, 0.12f, 0.12f, 1f);
        var closeButton = closeObject.GetComponent<Button>();
        closeButton.targetGraphic = closeObject.GetComponent<Image>();
        closeButton.onClick.AddListener(() => _validationWindow.SetActive(false));
        var closeLabel = CreateWindowText("Label", closeObject.transform, 18f, TextAlignmentOptions.Center);
        closeLabel.text = "关闭";
        StretchToParent(closeLabel.rectTransform, 6f);

        var scrollObject = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollObject.transform.SetParent(_validationWindow.transform, false);
        var scrollRect = scrollObject.GetComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(28f, 28f);
        scrollRect.offsetMax = new Vector2(-28f, -82f);
        scrollObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.22f);

        var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportObject.transform.SetParent(scrollObject.transform, false);
        var viewportRect = viewportObject.GetComponent<RectTransform>();
        StretchToParent(viewportRect, 8f);

        var contentObject = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter), typeof(VerticalLayoutGroup));
        contentObject.transform.SetParent(viewportObject.transform, false);
        var contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);
        var fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(12, 12, 12, 12);

        _validationWindowText = CreateWindowText("Details", contentObject.transform, 20f, TextAlignmentOptions.TopLeft);
        _validationWindowText.enableWordWrapping = true;
        _validationWindowText.overflowMode = TextOverflowModes.Overflow;
        _validationWindowText.richText = false;
        var textRect = _validationWindowText.rectTransform;
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(-24f, 0f);

        var scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        _validationWindow.SetActive(false);
    }

    private TMP_Text CreateWindowText(string objectName, Transform parent, float fontSize, TextAlignmentOptions alignment)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        var label = textObject.GetComponent<TextMeshProUGUI>();
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = Color.white;
        label.richText = false;
        if (statusText != null) label.font = statusText.font;
        return label;
    }

    private static void StretchToParent(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    private void UpdateValidationDetails(DialogPreviewValidationResult validation, string filePath)
    {
        bool hasDetails = validation != null && (validation.Errors.Count > 0 || validation.Warnings.Count > 0);
        if (!hasDetails)
        {
            HideValidationDetails();
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine($"文件：{filePath}");
        builder.AppendLine();

        if (validation.Errors.Count > 0)
        {
            builder.AppendLine($"错误（{validation.Errors.Count}）");
            for (int i = 0; i < validation.Errors.Count; i++)
                builder.AppendLine($"[{i + 1}] {validation.Errors[i]}");
            builder.AppendLine();
        }

        if (validation.Warnings.Count > 0)
        {
            builder.AppendLine($"警告（{validation.Warnings.Count}）");
            for (int i = 0; i < validation.Warnings.Count; i++)
                builder.AppendLine($"[{i + 1}] {validation.Warnings[i]}");
        }

        SetValidationDetails(
            validation.Errors.Count > 0 ? "CSV 校验失败" : "CSV 校验提示",
            builder.ToString(),
            validation.Errors.Count > 0);
    }

    private void ShowLastValidation()
    {
        if (!_hasValidationDetails || _validationWindow == null) return;
        _validationWindow.SetActive(true);
    }

    private void SetValidationDetails(string title, string details, bool openWindow)
    {
        if (_validationDetailsButton == null || _validationWindow == null) return;

        _hasValidationDetails = true;
        _validationDetailsButton.gameObject.SetActive(true);
        _validationWindowTitle.text = title;
        _validationWindowText.text = details;
        _validationWindowText.ForceMeshUpdate();
        _validationWindow.SetActive(openWindow);
    }

    private void HideValidationDetails()
    {
        _hasValidationDetails = false;
        if (_validationDetailsButton != null)
            _validationDetailsButton.gameObject.SetActive(false);
        if (_validationWindow != null)
            _validationWindow.SetActive(false);
    }

    private static void AppendDetails(StringBuilder builder, System.Collections.Generic.List<string> details)
    {
        for (int i = 0; i < details.Count; i++)
            builder.AppendLine($"  - {details[i]}");
    }
}
