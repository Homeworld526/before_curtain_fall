#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 仅在编辑器与 Development Build 中创建的 Start 场景调试菜单。
/// </summary>
public sealed class GmDebugMenu : MonoBehaviour
{
    private const float Margin = 28f;
    private const float GmButtonWidth = 84f;
    private const float GmButtonHeight = 38f;
    private const float MenuWidth = 250f;
    private const float MenuItemHeight = 42f;

    private static bool _prologueSkipPending;
    private static string _prologueSkipTargetScene;

    private AutoButtonBinder _menuOwner;
    private GameObject _menuPanel;
    private Button _gmButton;
    private Button _skipPrologueButton;
    private Button _toggleBackboneDisplayButton;

    public static void Create(RectTransform parent, AutoButtonBinder menuOwner)
    {
        if (parent.GetComponent<GmDebugMenu>() != null)
            throw new InvalidOperationException("[GM] Start 场景已存在 GM 调试菜单。");

        parent.gameObject.AddComponent<GmDebugMenu>().Initialize(menuOwner);
    }

    public static void RequestPrologueSkip(string targetScene)
    {
        if (_prologueSkipPending)
            throw new InvalidOperationException("[GM] 序章跳转已经在进行中。");

        _prologueSkipPending = true;
        _prologueSkipTargetScene = targetScene;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public static void ReapplyPendingDialogSuppression()
    {
        if (_prologueSkipPending)
            DialogList.SuppressAutoDialog = true;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!_prologueSkipPending || scene.name != _prologueSkipTargetScene)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneCenter.Instance.StartCoroutine(CompletePrologueAfterSceneLoad());
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
        _skipPrologueButton = CreateButton(_menuPanel.GetComponent<RectTransform>(), "快速跳转序章结束", new Vector2(MenuWidth, MenuItemHeight), new Color(0.2f, 0.29f, 0.32f, 0.98f));
        _skipPrologueButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 25f);
        _skipPrologueButton.onClick.AddListener(SkipToPrologueEnd);

        _toggleBackboneDisplayButton = CreateButton(_menuPanel.GetComponent<RectTransform>(), string.Empty, new Vector2(MenuWidth, MenuItemHeight), new Color(0.2f, 0.29f, 0.32f, 0.98f));
        _toggleBackboneDisplayButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -25f);
        _toggleBackboneDisplayButton.onClick.AddListener(ToggleBackboneDisplay);
        RefreshBackboneDisplayButtonLabel();
        _menuPanel.SetActive(false);
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
        rect.sizeDelta = new Vector2(MenuWidth, MenuItemHeight * 2f + 20f);

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
