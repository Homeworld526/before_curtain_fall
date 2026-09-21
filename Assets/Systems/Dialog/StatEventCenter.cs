using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatEventCenter : MonoBehaviour
{
    public static StatEventCenter Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            onStatChange += AddMoney;
        }
    }

    private void AddMoney(string statName, int value)
    {
        if (statName == "现金") JobManager.Instance.AddMoney(value);
    }
    
    public event Action<string, int> onStatChange;

    public void ChangeStat(string statName, int value)
    {
        onStatChange?.Invoke(statName, value);
    }

    public delegate int StatEvent(string statName);
    
    public event StatEvent onGetStat;

    public void GetStat(string statName)
    {
        onGetStat?.Invoke(statName);
    }

    public Action<string> onPicRead;

    public void PicRead(string name)
    {
        onPicRead?.Invoke(name);
    }

    public Action<string> onDialogEnd;

    public void OnDialogEnd(string index)
    {
        onDialogEnd?.Invoke(index);
    }

    private void OnDestroy()
    {
        // 清空静态变量，避免指向已销毁的对象
        if (Instance == this)
        {
            Instance = null;
        }
    }

}
