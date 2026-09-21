using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReSetDia : MonoBehaviour
{
    Button button;

    private void Start()
    {
        // 存档加载期间不重置对话位置，由 SaveSystem 负责恢复
        if (!DialogList.SuppressAutoDialog)
        {
            ReSet();
        }
    }
    public void ReSet()
    {
        PlayerPrefs.SetInt("dialogIndex", 0);
        PlayerPrefs.SetInt("dialogLine", 7);
        DialogLocator.Instance.Dialog.SetActive(true);
    }
}
