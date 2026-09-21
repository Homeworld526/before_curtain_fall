using System;
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

    private void Start()
    {
        selectBtn.onClick.AddListener(OnSelectFile);
        startBtn.onClick.AddListener(OnStart);
        resetBtn.onClick.AddListener(OnReset);
        middleToggle.onValueChanged.AddListener(OnMiddleToggleChanged);
        startBtn.interactable = false;
    }

    private void OnMiddleToggleChanged(bool value)
    {
        PicRSetMiddleEffect.Instance.StartEffect(value);
    }

    private void OnSelectFile()
    {
        var path = manager.SelectCsvFile();
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            _loadedCsvText = manager.LoadCsvAsUtf8(path);
            _loadedFilePath = path;
            filePathText.text = path;
            statusText.text = "加载成功";
            startBtn.interactable = true;
        }
        catch (Exception e)
        {
            statusText.text = $"加载失败：{e.Message}";
            startBtn.interactable = false;
        }
    }

    private void OnStart()
    {
        if (_loadedCsvText == null) return;
        statusText.text = "预览中...";
        manager.StartPreview(_loadedCsvText, middleToggle.isOn, _loadedFilePath);
    }

    private void OnReset()
    {
        _loadedCsvText = null;
        _loadedFilePath = null;
        filePathText.text = "未选择文件";
        statusText.text = "";
        startBtn.interactable = false;
    }
}
