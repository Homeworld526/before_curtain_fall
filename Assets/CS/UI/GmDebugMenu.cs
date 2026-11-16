#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 仅在编辑器与 Development Build 中创建的调试菜单。
/// </summary>
public sealed class GmDebugMenu : MonoBehaviour
{
    private const float Margin = 28f;
    private const float GmButtonWidth = 84f;
    private const float GmButtonHeight = 38f;
    private const float MenuWidth = 250f;
    private const float MenuItemHeight = 42f;
    private const int JobUnlockInitializationFrameLimit = 10;

    private static bool _prologueSkipPending;
    private static string _prologueSkipTargetScene;
    private static bool _allJobsUnlockPending;
    private static string _allJobsUnlockTargetScene;
    private static bool _sceneLoadHandlerRegistered;

    private AutoButtonBinder _menuOwner;
    private GameObject _menuPanel;
    private Button _gmButton;
    private Button _skipPrologueButton;
    private Button _unlockAllJobsButton;
    private Button _adjustResourcesButton;
    private Button _toggleBackboneDisplayButton;

    private GameObject _resourceAdjustmentPanel;
    private GameObject _resourceTypeOptionsPanel;
    private Button _resourceTypeButton;
    private TMP_InputField _resourceDeltaInput;
    private TextMeshProUGUI _resourceCurrentValueText;
    private TextMeshProUGUI _resourceFeedbackText;
    private GameResourceType _selectedResourceType;

    public static void Create(RectTransform parent, AutoButtonBinder menuOwner)
    {
        if (parent.GetComponent<GmDebugMenu>() != null)
            throw new InvalidOperationException("[GM] Start 场景已存在 GM 调试菜单。");

        parent.gameObject.AddComponent<GmDebugMenu>().Initialize(menuOwner);
    }

    public static void CreateForGameScene()
    {
        if (FindObjectOfType<GmDebugMenu>() != null)
            throw new InvalidOperationException("[GM] 主场景已存在 GM 调试菜单。");

        EnsureEventSystem();

        GameObject root = new GameObject(
            "GM Debug Overlay",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 5001;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GmDebugMenu>().Initialize(null);
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        new GameObject(
            "GM EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));
        Debug.Log("[GM] 主场景缺少 EventSystem，已创建 GM 输入事件系统。");
    }

    public static void RequestPrologueSkip(string targetScene)
    {
        if (_prologueSkipPending)
            throw new InvalidOperationException("[GM] 序章跳转已经在进行中。");

        _prologueSkipPending = true;
        _prologueSkipTargetScene = targetScene;
        RegisterSceneLoadHandler();
    }

    public static void RequestAllJobsUnlock(string targetScene)
    {
        if (_allJobsUnlockPending)
            throw new InvalidOperationException("[GM] 全部打工解锁已经在进行中。");

        _allJobsUnlockPending = true;
        _allJobsUnlockTargetScene = targetScene;
        RegisterSceneLoadHandler();
    }

    private static void RegisterSceneLoadHandler()
    {
        if (_sceneLoadHandlerRegistered)
            return;

        SceneManager.sceneLoaded += OnSceneLoaded;
        _sceneLoadHandlerRegistered = true;
    }

    public static void ReapplyPendingDialogSuppression()
    {
        if (_prologueSkipPending)
            DialogList.SuppressAutoDialog = true;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool shouldSkipPrologue = _prologueSkipPending && scene.name == _prologueSkipTargetScene;
        bool shouldUnlockAllJobs = _allJobsUnlockPending && scene.name == _allJobsUnlockTargetScene;
        if (!shouldSkipPrologue && !shouldUnlockAllJobs)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        _sceneLoadHandlerRegistered = false;

        if (shouldSkipPrologue)
            SceneCenter.Instance.StartCoroutine(CompletePrologueAfterSceneLoad());

        if (shouldUnlockAllJobs)
            SceneCenter.Instance.StartCoroutine(UnlockAllJobsAfterSceneLoad());
    }

    private static IEnumerator CompletePrologueAfterSceneLoad()
    {
        // sceneLoaded 在 Start 之前触发；第二帧确保新游戏流程的场景后置重置已完成。
        yield return null;
        yield return null;

        var prologueEndEvent = FindObjectOfType<BackToMainMenuEvent>();
        if (prologueEndEvent == null || ShowDialog.Instance == null ||
            PrologueUIManager.Instance == null || TutorialManager.Instance == null)
        {
            throw new InvalidOperationException("[GM] 无法跳过序章：SampleScene 缺少序章收尾事件、主 UI、序章或教学管理器配置。");
        }

        // 复用剧情正常结束时的收尾链路：它会切换至 home 背景并恢复主场景状态。
        prologueEndEvent.SkipPrologue();
        // GM 没有实际播放序章末句，补齐 ShowDialog 依赖的完成标记。
        ShowDialog.Instance.SetPrologueCompleted();
        DialogList.SuppressAutoDialog = false;
        _prologueSkipPending = false;
        _prologueSkipTargetScene = null;

        Debug.Log("[GM] 已跳过序章并进入序章结束后的主场景状态。");
    }

    private static IEnumerator UnlockAllJobsAfterSceneLoad()
    {
        for (int frame = 0; frame < JobUnlockInitializationFrameLimit; frame++)
        {
            if (ConfigManager.Instance != null &&
                DialogKeywordDetector.Instance != null &&
                DialogKeywordDetector.Instance.keywordUnlocks != null &&
                JobManager.Instance != null &&
                JobListManager.Instance != null)
            {
                break;
            }

            yield return null;
        }

        UnlockAllJobsInActiveScene();

        _allJobsUnlockPending = false;
        _allJobsUnlockTargetScene = null;
    }

    private static void UnlockAllJobsInActiveScene()
    {
        if (ConfigManager.Instance == null ||
            DialogKeywordDetector.Instance == null ||
            DialogKeywordDetector.Instance.keywordUnlocks == null ||
            JobManager.Instance == null ||
            JobListManager.Instance == null)
        {
            throw new InvalidOperationException(
                "[GM] 无法解锁全部打工：主场景未完成打工配置、关键词检测器或列表管理器初始化。");
        }

        int normalJobCount = UnlockJobs(ConfigManager.Instance.normalJobs);
        int characterJobCount = UnlockJobs(ConfigManager.Instance.characterJobs);

        foreach (DialogKeywordDetector.KeywordUnlock unlock in DialogKeywordDetector.Instance.keywordUnlocks)
            unlock.hasTriggered = DialogKeywordDetector.IsKeywordUnlocked(unlock.unlockKey);

        JobListManager.Instance.ClearJobUnlockedStateCache();
        JobManager.Instance.RefreshCurrentJobList();
        Debug.Log($"[GM] 已解锁全部打工：普通 {normalJobCount} 个，角色 {characterJobCount} 个。");
    }

    private static int UnlockJobs(System.Collections.Generic.IEnumerable<GameSystem.JobData> jobs)
    {
        int count = 0;
        foreach (GameSystem.JobData job in jobs)
        {
            if (string.IsNullOrEmpty(job.unlockDialogKeyword))
                continue;

            DialogKeywordDetector.SetKeywordUnlocked(job.unlockDialogKeyword, true);
            count++;
        }

        return count;
    }

    private void Initialize(AutoButtonBinder menuOwner)
    {
        _menuOwner = menuOwner;

        RectTransform menuRoot = CreateRectTransform("GM Debug Menu", transform);
        menuRoot.anchorMin = Vector2.one;
        menuRoot.anchorMax = Vector2.one;
        menuRoot.pivot = Vector2.one;
        menuRoot.anchoredPosition = new Vector2(-Margin, -Margin);

        _gmButton = CreateButton(menuRoot, "GM", new Vector2(GmButtonWidth, GmButtonHeight), new Color(0.14f, 0.18f, 0.22f, 0.96f));
        _gmButton.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        _gmButton.onClick.AddListener(ToggleMenu);

        _menuPanel = CreatePanel(menuRoot);
        List<Button> menuItems = new List<Button>();
        if (_menuOwner != null)
        {
            _skipPrologueButton = CreateButton(_menuPanel.GetComponent<RectTransform>(), "快速跳转序章结束", new Vector2(MenuWidth, MenuItemHeight), new Color(0.2f, 0.29f, 0.32f, 0.98f));
            _skipPrologueButton.onClick.AddListener(SkipToPrologueEnd);
            menuItems.Add(_skipPrologueButton);

            _unlockAllJobsButton = CreateButton(_menuPanel.GetComponent<RectTransform>(), "解锁全部打工", new Vector2(MenuWidth, MenuItemHeight), new Color(0.2f, 0.29f, 0.32f, 0.98f));
            _unlockAllJobsButton.onClick.AddListener(UnlockAllJobs);
            menuItems.Add(_unlockAllJobsButton);
        }
        else
        {
            _unlockAllJobsButton = CreateButton(_menuPanel.GetComponent<RectTransform>(), "解锁全部打工", new Vector2(MenuWidth, MenuItemHeight), new Color(0.2f, 0.29f, 0.32f, 0.98f));
            _unlockAllJobsButton.onClick.AddListener(UnlockAllJobs);
            menuItems.Add(_unlockAllJobsButton);

            _adjustResourcesButton = CreateButton(_menuPanel.GetComponent<RectTransform>(), "数值增减", new Vector2(MenuWidth, MenuItemHeight), new Color(0.2f, 0.29f, 0.32f, 0.98f));
            _adjustResourcesButton.onClick.AddListener(ToggleResourceAdjustmentPanel);
            menuItems.Add(_adjustResourcesButton);
        }

        _toggleBackboneDisplayButton = CreateButton(_menuPanel.GetComponent<RectTransform>(), string.Empty, new Vector2(MenuWidth, MenuItemHeight), new Color(0.2f, 0.29f, 0.32f, 0.98f));
        _toggleBackboneDisplayButton.onClick.AddListener(ToggleBackboneDisplay);
        RefreshBackboneDisplayButtonLabel();
        menuItems.Add(_toggleBackboneDisplayButton);
        LayoutMenuItems(menuItems);
        _menuPanel.SetActive(false);
    }

    private void LayoutMenuItems(List<Button> menuItems)
    {
        RectTransform panelRect = _menuPanel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(MenuWidth, MenuItemHeight * menuItems.Count + 20f);

        float firstItemY = (menuItems.Count - 1) * MenuItemHeight * 0.5f;
        for (int index = 0; index < menuItems.Count; index++)
        {
            menuItems[index].GetComponent<RectTransform>().anchoredPosition =
                new Vector2(0f, firstItemY - index * MenuItemHeight);
        }
    }

    private static RectTransform CreateRectTransform(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject.GetComponent<RectTransform>();
    }

    private static GameObject CreatePanel(RectTransform parent)
    {
        var panel = new GameObject("GM Menu Items", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);

        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(0f, -GmButtonHeight - 8f);
        rect.sizeDelta = new Vector2(MenuWidth, MenuItemHeight + 20f);

        var image = panel.GetComponent<Image>();
        image.color = new Color(0.035f, 0.05f, 0.065f, 0.98f);
        return panel;
    }

    private static Button CreateButton(RectTransform parent, string label, Vector2 size, Color backgroundColor)
    {
        var buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        var buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = size;

        var image = buttonObject.GetComponent<Image>();
        image.color = backgroundColor;

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        RectTransform labelRect = CreateRectTransform("Label", buttonObject.transform);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 2f);
        labelRect.offsetMax = new Vector2(-8f, -2f);

        var text = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 18f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.text = label;
        text.raycastTarget = false;

        return button;
    }

    private void ToggleMenu()
    {
        _menuPanel.SetActive(!_menuPanel.activeSelf);
        if (_menuPanel.activeSelf)
            RefreshBackboneDisplayButtonLabel();
    }

    private void ToggleBackboneDisplay()
    {
        BackboneDebugValueDisplay.ToggleVisibility();
        RefreshBackboneDisplayButtonLabel();
    }

    private void RefreshBackboneDisplayButtonLabel()
    {
        TextMeshProUGUI label = _toggleBackboneDisplayButton.GetComponentInChildren<TextMeshProUGUI>(true);
        label.text = BackboneDebugValueDisplay.IsVisible ? "隐藏骨气值" : "显示骨气值";
    }

    private void SkipToPrologueEnd()
    {
        _gmButton.interactable = false;
        _skipPrologueButton.interactable = false;
        _menuPanel.SetActive(false);
        _menuOwner.StartGameAfterPrologueForDebug();
    }

    private void UnlockAllJobs()
    {
        if (_menuOwner != null)
        {
            _gmButton.interactable = false;
            _unlockAllJobsButton.interactable = false;
            _menuPanel.SetActive(false);
            _menuOwner.StartGameWithAllJobsUnlockedForDebug();
            return;
        }

        UnlockAllJobsInActiveScene();
        _menuPanel.SetActive(false);
    }

    private void ToggleResourceAdjustmentPanel()
    {
        if (_resourceAdjustmentPanel == null)
            CreateResourceAdjustmentPanel();

        bool shouldShow = !_resourceAdjustmentPanel.activeSelf;
        _resourceAdjustmentPanel.SetActive(shouldShow);
        if (!shouldShow)
            return;

        _resourceTypeOptionsPanel.SetActive(false);
        _resourceDeltaInput.text = string.Empty;
        _resourceFeedbackText.text = string.Empty;
        RefreshResourceDisplay();
        _resourceDeltaInput.ActivateInputField();
    }

    private void CreateResourceAdjustmentPanel()
    {
        _selectedResourceType = GameResourceType.Money;
        _resourceAdjustmentPanel = CreatePanel("GM Resource Adjustment", transform, new Vector2(560f, 300f));
        RectTransform panelRect = _resourceAdjustmentPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI title = CreateText("Title", _resourceAdjustmentPanel.transform, 24f, TextAlignmentOptions.Center);
        ConfigureRect(title.rectTransform, new Vector2(0f, 104f), new Vector2(460f, 36f));
        title.text = "数值增减";

        Button closeButton = CreateButton(_resourceAdjustmentPanel.GetComponent<RectTransform>(), "X", new Vector2(36f, 36f), new Color(0.3f, 0.12f, 0.12f, 0.98f));
        closeButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(242f, 112f);
        closeButton.onClick.AddListener(() => _resourceAdjustmentPanel.SetActive(false));

        _resourceTypeButton = CreateButton(_resourceAdjustmentPanel.GetComponent<RectTransform>(), string.Empty, new Vector2(340f, 42f), new Color(0.16f, 0.22f, 0.26f, 1f));
        _resourceTypeButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 50f);
        _resourceTypeButton.onClick.AddListener(ToggleResourceTypeOptions);

        _resourceCurrentValueText = CreateText("Current Value", _resourceAdjustmentPanel.transform, 17f, TextAlignmentOptions.Center);
        ConfigureRect(_resourceCurrentValueText.rectTransform, new Vector2(0f, 5f), new Vector2(460f, 28f));
        _resourceCurrentValueText.color = new Color(0.8f, 0.88f, 0.9f, 1f);

        _resourceDeltaInput = CreateIntegerInput(_resourceAdjustmentPanel.transform, new Vector2(290f, 42f));
        _resourceDeltaInput.GetComponent<RectTransform>().anchoredPosition = new Vector2(-72f, -45f);
        _resourceDeltaInput.onValueChanged.AddListener(_ => _resourceFeedbackText.text = string.Empty);

        Button confirmButton = CreateButton(_resourceAdjustmentPanel.GetComponent<RectTransform>(), "确认", new Vector2(110f, 42f), new Color(0.18f, 0.4f, 0.27f, 1f));
        confirmButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(145f, -45f);
        confirmButton.onClick.AddListener(ApplyResourceAdjustment);

        _resourceFeedbackText = CreateText("Feedback", _resourceAdjustmentPanel.transform, 16f, TextAlignmentOptions.Center);
        ConfigureRect(_resourceFeedbackText.rectTransform, new Vector2(0f, -108f), new Vector2(480f, 28f));

        _resourceTypeOptionsPanel = CreatePanel("Resource Type Options", _resourceAdjustmentPanel.transform, new Vector2(340f, Enum.GetValues(typeof(GameResourceType)).Length * 36f));
        RectTransform optionsRect = _resourceTypeOptionsPanel.GetComponent<RectTransform>();
        optionsRect.anchorMin = new Vector2(0.5f, 0.5f);
        optionsRect.anchorMax = new Vector2(0.5f, 0.5f);
        optionsRect.pivot = new Vector2(0.5f, 1f);
        optionsRect.anchoredPosition = new Vector2(0f, 29f);

        Array resourceTypes = Enum.GetValues(typeof(GameResourceType));
        for (int index = 0; index < resourceTypes.Length; index++)
        {
            GameResourceType resourceType = (GameResourceType)resourceTypes.GetValue(index);
            Button optionButton = CreateButton(optionsRect, GetResourceDisplayName(resourceType), new Vector2(332f, 34f), new Color(0.15f, 0.2f, 0.24f, 1f));
            optionButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -18f - index * 36f);
            optionButton.onClick.AddListener(() => SelectResourceType(resourceType));
        }

        _resourceTypeOptionsPanel.SetActive(false);
        _resourceAdjustmentPanel.SetActive(false);
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 size)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<RectTransform>().sizeDelta = size;
        panel.GetComponent<Image>().color = new Color(0.035f, 0.05f, 0.065f, 0.98f);
        return panel;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static TMP_InputField CreateIntegerInput(Transform parent, Vector2 size)
    {
        GameObject inputObject = new GameObject("Delta Input", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        inputObject.transform.SetParent(parent, false);
        inputObject.GetComponent<RectTransform>().sizeDelta = size;
        inputObject.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.19f, 1f);

        TMP_InputField inputField = inputObject.GetComponent<TMP_InputField>();
        inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
        inputField.characterLimit = 19;

        RectTransform textArea = CreateRectTransform("Text Area", inputObject.transform);
        textArea.anchorMin = Vector2.zero;
        textArea.anchorMax = Vector2.one;
        textArea.offsetMin = new Vector2(12f, 4f);
        textArea.offsetMax = new Vector2(-12f, -4f);

        TextMeshProUGUI text = CreateText("Text", textArea, 19f, TextAlignmentOptions.MidlineLeft);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        text.enableWordWrapping = false;

        TextMeshProUGUI placeholder = CreateText("Placeholder", textArea, 17f, TextAlignmentOptions.MidlineLeft);
        placeholder.rectTransform.anchorMin = Vector2.zero;
        placeholder.rectTransform.anchorMax = Vector2.one;
        placeholder.rectTransform.offsetMin = Vector2.zero;
        placeholder.rectTransform.offsetMax = Vector2.zero;
        placeholder.color = new Color(0.58f, 0.64f, 0.67f, 1f);
        placeholder.text = "输入正负整数";

        inputField.textViewport = textArea;
        inputField.textComponent = text;
        inputField.placeholder = placeholder;
        return inputField;
    }

    private static void ConfigureRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private void ToggleResourceTypeOptions()
    {
        _resourceTypeOptionsPanel.SetActive(!_resourceTypeOptionsPanel.activeSelf);
    }

    private void SelectResourceType(GameResourceType resourceType)
    {
        _selectedResourceType = resourceType;
        _resourceTypeOptionsPanel.SetActive(false);
        _resourceFeedbackText.text = string.Empty;
        RefreshResourceDisplay();
    }

    private void RefreshResourceDisplay()
    {
        _resourceTypeButton.GetComponentInChildren<TextMeshProUGUI>(true).text = GetResourceDisplayName(_selectedResourceType);
        _resourceCurrentValueText.text = $"当前数值：{_selectedResourceType.GetCurrentValue():N0}";
    }

    private void ApplyResourceAdjustment()
    {
        if (!long.TryParse(_resourceDeltaInput.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long delta))
        {
            _resourceFeedbackText.color = new Color(1f, 0.58f, 0.58f, 1f);
            _resourceFeedbackText.text = "请输入有效的正负整数。";
            return;
        }

        try
        {
            long currentValue = _selectedResourceType.Adjust(delta, "GM 数值增减");
            _resourceCurrentValueText.text = $"当前数值：{currentValue:N0}";
            _resourceFeedbackText.color = new Color(0.6f, 1f, 0.7f, 1f);
            _resourceFeedbackText.text = $"{GetResourceDisplayName(_selectedResourceType)}已调整 {delta:+#;-#;0}";
            _resourceDeltaInput.text = string.Empty;
            _resourceDeltaInput.ActivateInputField();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            _resourceFeedbackText.color = new Color(1f, 0.58f, 0.58f, 1f);
            _resourceFeedbackText.text = exception.Message;
        }
    }

    private static string GetResourceDisplayName(GameResourceType resourceType)
    {
        switch (resourceType)
        {
            case GameResourceType.Money:
                return "现金";
            case GameResourceType.Backbone:
                return "骨气";
            case GameResourceType.XiaLunAffection:
                return "夏伦好感度";
            case GameResourceType.ZhangHaixinAffection:
                return "张海心好感度";
            case GameResourceType.ChenYushuAffection:
                return "陈玉澍好感度";
            default:
                throw new ArgumentOutOfRangeException(nameof(resourceType), resourceType, "未配置 GM 显示名称的资源类型。");
        }
    }
}

/// <summary>
/// GM 专用的骨气调试读数。仅保存当前会话开关状态，不参与游戏存档。
/// </summary>
internal sealed class BackboneDebugValueDisplay : MonoBehaviour
{
    private const float Margin = 28f;

    private static BackboneDebugValueDisplay _instance;
    private static bool _isVisible;

    private GameObject _panel;
    private TextMeshProUGUI _valueText;
    private bool _isSubscribed;
    private Coroutine _refreshCoroutine;

    public static bool IsVisible => _isVisible;

    public static void ToggleVisibility()
    {
        SetVisibility(!_isVisible);
    }

    private static void SetVisibility(bool visible)
    {
        EnsureInstance();
        _isVisible = visible;
        _instance.ApplyVisibility();
    }

    private static void EnsureInstance()
    {
        if (_instance != null)
            return;

        GameObject displayRoot = new GameObject(
            "GM Backbone Value Display",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));
        UnityEngine.Object.DontDestroyOnLoad(displayRoot);
        _instance = displayRoot.AddComponent<BackboneDebugValueDisplay>();
        _instance.Initialize();
    }

    private void Initialize()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 5000;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _panel = new GameObject("Backbone Value", typeof(RectTransform), typeof(Image));
        _panel.transform.SetParent(transform, false);
        RectTransform panelRect = _panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-Margin, Margin);
        panelRect.sizeDelta = new Vector2(240f, 48f);

        Image panelImage = _panel.GetComponent<Image>();
        panelImage.color = new Color(0.035f, 0.05f, 0.065f, 0.9f);
        panelImage.raycastTarget = false;

        GameObject textObject = new GameObject("Value", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(_panel.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(14f, 4f);
        textRect.offsetMax = new Vector2(-14f, -4f);

        _valueText = textObject.GetComponent<TextMeshProUGUI>();
        _valueText.font = TMP_Settings.defaultFontAsset;
        _valueText.fontSize = 24f;
        _valueText.enableAutoSizing = true;
        _valueText.fontSizeMin = 14f;
        _valueText.fontSizeMax = 24f;
        _valueText.alignment = TextAlignmentOptions.MidlineRight;
        _valueText.color = Color.white;
        _valueText.raycastTarget = false;

        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyVisibility();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnsubscribeFromBackboneChanges();
        if (_instance == this)
            _instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UnsubscribeFromBackboneChanges();
        if (!_isVisible)
            return;

        if (_refreshCoroutine != null)
            StopCoroutine(_refreshCoroutine);
        _refreshCoroutine = StartCoroutine(RefreshAfterSceneInitialization());
    }

    private void ApplyVisibility()
    {
        _panel.SetActive(_isVisible);
        if (!_isVisible)
        {
            UnsubscribeFromBackboneChanges();
            return;
        }

        RefreshDisplayedValue();
        if (_refreshCoroutine != null)
            StopCoroutine(_refreshCoroutine);
        _refreshCoroutine = StartCoroutine(RefreshAfterSceneInitialization());
    }

    private System.Collections.IEnumerator RefreshAfterSceneInitialization()
    {
        // DialogList 在场景 Start 中初始化骨气；延后两帧后再读取和订阅。
        yield return null;
        yield return null;
        RefreshDisplayedValue();
        _refreshCoroutine = null;
    }

    private void RefreshDisplayedValue()
    {
        if (DialogList.Instance == null || DialogList.Instance.statData == null)
        {
            _valueText.text = "骨气：等待游戏开始";
            return;
        }

        if (!DialogList.Instance.statData.TryGetInt(BackboneSystem.StatKey, out int value))
        {
            _valueText.text = "骨气：初始化中";
            return;
        }

        SubscribeToBackboneChanges();
        SetDisplayedValue(value);
    }

    private void SubscribeToBackboneChanges()
    {
        if (_isSubscribed)
            return;

        BackboneSystem.ValueChanged += OnBackboneValueChanged;
        _isSubscribed = true;
    }

    private void UnsubscribeFromBackboneChanges()
    {
        if (!_isSubscribed)
            return;

        BackboneSystem.ValueChanged -= OnBackboneValueChanged;
        _isSubscribed = false;
    }

    private void OnBackboneValueChanged(int previousValue, int currentValue)
    {
        SetDisplayedValue(currentValue);
    }

    private void SetDisplayedValue(int value)
    {
        _valueText.text = $"骨气：{value}";
    }
}
#endif
