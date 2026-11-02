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

        if (DialogLocator.Instance == null || DialogLocator.Instance.Dialog == null ||
            ShowDialog.Instance == null || PrologueUIManager.Instance == null || TutorialManager.Instance == null)
        {
            throw new InvalidOperationException("[GM] 无法跳过序章：SampleScene 缺少对话、主 UI、序章或教学管理器配置。");
        }

        DialogLocator.Instance.Dialog.SetActive(false);
        PrologueUIManager.Instance.CompletePrologue();
        ShowDialog.Instance.SetPrologueCompleted();
        ShowDialog.Instance.Hide2();
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
        _skipPrologueButton.onClick.AddListener(SkipToPrologueEnd);
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
        rect.sizeDelta = new Vector2(MenuWidth, MenuItemHeight + 12f);

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
    }

    private void SkipToPrologueEnd()
    {
        _gmButton.interactable = false;
        _skipPrologueButton.interactable = false;
        _menuPanel.SetActive(false);
        _menuOwner.StartGameAfterPrologueForDebug();
    }
}
#endif
