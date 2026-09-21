using System.Collections;
using TMPro;
using UnityEngine;

public class HotKeyRebinder : MonoBehaviour
{
    public HotKeyManager hotKeyManager;
    public string targetEntryName;
    public TextMeshProUGUI statusText;
    public string waitingText = "按任意键绑定";

    private void Start()
    {
        if (hotKeyManager == null)
            hotKeyManager = FindObjectOfType<HotKeyManager>();
        RefreshText();
    }

    public void StartRebind()
    {
        if (hotKeyManager == null || hotKeyManager.IsRebinding) return;

        if (statusText != null) statusText.text = waitingText;

        hotKeyManager.BeginRebind(targetEntryName, OnRebindComplete);
    }

    private void OnRebindComplete(KeyCode kc)
    {
        RefreshText();
    }

    private void RefreshText()
    {
        if (statusText == null || hotKeyManager == null) return;

        foreach (var entry in hotKeyManager.HotKeys)
        {
            if (entry.name != targetEntryName) continue;
            statusText.text = entry.keys != null && entry.keys.Count > 0
                ? entry.keys[0].ToString()
                : "未绑定";
            return;
        }

        statusText.text = "未绑定";
    }
}
