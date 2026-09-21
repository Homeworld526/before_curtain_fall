using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 挂在与 DialogList 相同的 GameObject 上。
/// 语言切换时按 TextAsset.name 替换 DialogList.dialogs 中对应的条目。
/// </summary>
[RequireComponent(typeof(DialogList))]
public class DialogLocalizer : MonoBehaviour
{
    private DialogList _dialogList;

    // index → 原始中文 TextAsset
    private TextAsset[] _originals;

    private void Awake()
    {
        _dialogList = GetComponent<DialogList>();
    }

    private void Start()
    {
        CacheOriginals();
        ApplyLanguage(LocalizationManager.Instance.CurrentLanguage);
        LocalizationManager.OnLanguageChanged += ApplyLanguage;
    }

    private void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= ApplyLanguage;
    }

    private void CacheOriginals()
    {
        _originals = new TextAsset[_dialogList.dialogs.Count];
        for (int i = 0; i < _dialogList.dialogs.Count; i++)
            _originals[i] = _dialogList.dialogs[i];
    }

    private void ApplyLanguage(GameLanguage lang)
    {
        if (lang == GameLanguage.Chinese)
        {
            RestoreOriginals();
            return;
        }

        for (int i = 0; i < _originals.Length; i++)
        {
            var orig = _originals[i];
            if (orig == null) continue;
            var localized = LocalizationManager.Instance.GetDialogAsset(orig.name);
            _dialogList.dialogs[i] = localized != null ? localized : orig;
        }
    }

    private void RestoreOriginals()
    {
        for (int i = 0; i < _originals.Length && i < _dialogList.dialogs.Count; i++)
        {
            if (_originals[i] != null)
                _dialogList.dialogs[i] = _originals[i];
        }
    }
}
