using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SaveVisual: MonoBehaviour
{
    [SerializeField] private Image _image;
    public int index;
    private GameObject textPrefab;
    public Vector2 textOffset;
    private GameObject inst;
    private string _loadedPath;

    public bool isRead;
    
    private void OnEnable()
    {
        textPrefab = SaveManager.Instance.textPrefab;
        if(!isRead)GetComponent<Button>().onClick.AddListener(() => SaveManager.Instance.AddSave(index));
        else GetComponent<Button>().onClick.AddListener(() => SaveManager.Instance.LoadSave(index));
        UpdateVisual();
    }

    private void OnDisable()
    {
        GetComponent<Button>().onClick.RemoveAllListeners();
    }

    public void SetPage(int pageOffset)
    {
        textPrefab = SaveManager.Instance.textPrefab;
        GetComponent<Button>().onClick.RemoveAllListeners();
        index = pageOffset;
        if(!isRead)GetComponent<Button>().onClick.AddListener(() => SaveManager.Instance.AddSave(index));
        else GetComponent<Button>().onClick.AddListener(() => SaveManager.Instance.LoadSave(index));
        UpdateVisual();
    }

    public void UpdateVisual(string screenshotPath, SaveInfo info)
    {
        Debug.Log(screenshotPath);
        if (File.Exists(screenshotPath)) {
            if(inst == null && textPrefab != null) inst = Instantiate(textPrefab, transform);
            if(inst != null) inst.GetComponent<SaveDataText>().Distribute(info.currentWeek, info.SaveTime, info.dialogIndex);

            if (screenshotPath != _loadedPath) {
                _image.color = Color.white;
                byte[] fileData = File.ReadAllBytes(screenshotPath);
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(fileData);
                _image.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                _loadedPath = screenshotPath;
            }
        }
        else
        {
            if(inst != null) Destroy(inst);
            _image.color = Color.clear;
            _loadedPath = null;
        }
    }
    public void UpdateVisual()
    {
        string screenshotPath = SaveSystem.Instance.GetAllSaveInfos()[index].Pic;
        SaveInfo info = SaveSystem.Instance.GetAllSaveInfos()[index];
        Debug.Log(screenshotPath);
        if (File.Exists(screenshotPath)) {
            if(inst == null && textPrefab != null) inst = Instantiate(textPrefab, transform);
            if(inst != null) inst.GetComponent<SaveDataText>().Distribute(info.currentWeek, info.SaveTime, info.dialogIndex);

            if (screenshotPath != _loadedPath) {
                _image.color = Color.white;
                byte[] fileData = File.ReadAllBytes(screenshotPath);
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(fileData);
                _image.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                _loadedPath = screenshotPath;
            }
        }
        else
        {
            if(inst != null) Destroy(inst);
            _image.color = Color.clear;
            _loadedPath = null;
        }
    }


}
