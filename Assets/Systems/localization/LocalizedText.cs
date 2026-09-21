using TMPro;
using UnityEngine;

/// <summary>
/// 挂到带 TextMeshProUGUI 的 GameObject 上，填写 localizationKey。
/// 语言切换时自动从 LocalizationManager 查询对应文本并更新。
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    [Tooltip("与 ui_text_*.csv 第一列 Key 对应")]
    public string localizationKey;

    private TextMeshProUGUI _text;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        RefreshText(LocalizationManager.Instance.CurrentLanguage);
        LocalizationManager.OnLanguageChanged += RefreshText;
    }

    private void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= RefreshText;
    }

    private void RefreshText(GameLanguage lang)
    {
        if (string.IsNullOrEmpty(localizationKey)) return;
        var value = LocalizationManager.Instance.GetUIText(localizationKey);
        if (value != null) _text.text = value;
    }
}
