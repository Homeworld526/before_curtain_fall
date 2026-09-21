using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CGColumns
{
    index = 0,
    name = 1,
    description = 2,
}

public class CGUnlockSystem : SingleCase<CGUnlockSystem>
{
    public List<TextAsset> galleryOrders;
    public CGUnlockDataSO unlockData;
    //public Dictionary<string, int> Map;
    public List<List<string>> filename;
    public List<string> description;
    public bool debug;

    private void OnEnable()
    {
        /*if (PlayerPrefs.GetInt("FirstGame", 0) < 1)
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.SetInt("FirstGame", 1);
        }*/
        StatEventCenter.Instance.onPicRead += UnlockPic;
    }

    private void OnDisable()
    {
        StatEventCenter.Instance.onPicRead -= UnlockPic;
    }

    private void Start()
    {
        BuildFromTextAsset(galleryOrders[0]);
        if(debug) UnlockPic(filename[0][0]);
    }

    public void SetGalleryOrder(int index)
    {
        BuildFromTextAsset(galleryOrders[index]);
    }

    private void BuildFromTextAsset(TextAsset asset)
    {
        filename = new List<List<string>>();
        description = new List<string>();
        string[] dialogRows = asset.text.Split('\n');

        for (int i = 1; i < dialogRows.Length - 1; i++)
        {
            string[] cell = dialogRows[i].Split(',');
            Debug.Log(dialogRows[i]);
            int idx = int.Parse(cell[(int)CGColumns.index]);
            while (filename.Count <= idx)
            {
                filename.Add(new List<string>());
                description.Add("");
            }
            description[idx] = cell[(int)CGColumns.description];
            filename[idx].Add(cell[(int)CGColumns.name]);
        }
    }

    public void UnlockPic(string name)
    {
        //Debug.LogWarning(name);
        unlockData.Unlock(name);
    }

    public bool IsUnlock(int index)
    {
        foreach (string name in filename[index])
            if (unlockData.IsUnlocked(name)) return true;
        return false;
    }

    public string GetFile(int index)
    {
        return filename[index][0];
    }
    
    public string GetFile(int index,int variant)
    {
        return filename[index][variant];
    }

    public string GetDes(int index)
    {
        return description[index];
    }

    public static bool IsVideo(string name) => name.StartsWith("VIDEO_");

    public Sprite FindCoverSprite(int index)
    {
        foreach (string v in filename[index])
        {
            if (!IsVideo(v))
            {
                var s = Resources.Load<Sprite>(v);
                if (s != null) return s;
            }
        }
        return null;
    }
}
