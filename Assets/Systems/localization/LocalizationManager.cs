using System;
using System.Collections.Generic;
using UnityEngine;

public class LocalizationManager : SingleCase<LocalizationManager>
{
    [SerializeField] private LocalizationConfig config;

    public static event Action<GameLanguage> OnLanguageChanged;

    public GameLanguage CurrentLanguage { get; private set; }

    private readonly Dictionary<string, string> _uiTexts = new Dictionary<string, string>();

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        if (config == null)
        {
            Debug.LogError("[LocalizationManager] config 未赋值");
            return;
        }
        CurrentLanguage = config.LoadSettings();
        LoadUITexts(CurrentLanguage);
    }

    /// <summary>
    /// 重置静态状态（由 StaticStateResetManager 在场景切换时调用）
    /// 用于 DontDestroyOnLoad 单例，确保场景切换后重新初始化
    /// </summary>
    public static void ResetStaticState()
    {
        // SingleCase 的 Instance 会自动处理"假 null"情况
        // 这里只需要确保清空即可
        Debug.Log("[LocalizationManager] 静态状态已重置");
    }

    public void SetLanguage(GameLanguage lang)
    {
        if (CurrentLanguage == lang) return;
        CurrentLanguage = lang;
        config.currentLanguage = lang;
        config.SaveSettings();
        LoadUITexts(lang);
        OnLanguageChanged?.Invoke(lang);
    }

    public TextAsset GetDialogAsset(string key)
    {
        if (config == null) return null;
        return config.GetDialogAsset(key, CurrentLanguage);
    }

    public string GetUIText(string key)
    {
        _uiTexts.TryGetValue(key, out var value);
        return value;
    }

    private void LoadUITexts(GameLanguage lang)
    {
        _uiTexts.Clear();
        string filename = lang == GameLanguage.Chinese ? "Localization/ui_text_zh" : "Localization/ui_text_en";
        var asset = Resources.Load<TextAsset>(filename);
        if (asset == null)
        {
            Debug.LogWarning($"[LocalizationManager] 找不到 UI 文本资源：{filename}");
            return;
        }

        foreach (var rawLine in asset.text.Split('\n'))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            int comma = line.IndexOf(',');
            if (comma < 0) continue;
            var key = line.Substring(0, comma).Trim();
            var value = line.Substring(comma + 1).Trim();
            if (key == "Key") continue; // 跳过表头
            _uiTexts[key] = value;
        }
    }
}
