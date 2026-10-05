using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatEventCenter : SingleCase<StatEventCenter>
{
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

}
