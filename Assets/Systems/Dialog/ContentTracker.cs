using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContentTracker : MonoBehaviour
{
    public DialogColumn type;
    public string target;
    public string offtarget;
    public Action act;
    public Action deact;
    public bool on;
    public bool oneshot;

    public void OnEnable()
    {
        DialogVisual.Instance.ContentTracker += TrackContent;
        on = false;
    }
    public void OnDisable()
    {
        if (DialogVisual.Instance != null)
            DialogVisual.Instance.ContentTracker -= TrackContent;
    }

    protected virtual void TrackContent(DialogNode xx)
    {
        if(type == DialogColumn.backup)
        {
            string[] x = xx.note.Split('+');
            for(int i = 0; i < x.Length; i++)
            {
                if (x[i] == target && !on)
                {
                    act.Invoke();
                    on = true;
                }
                else if (x[i] == offtarget && on && !oneshot)
                {
                    deact.Invoke();
                    on = false;
                }
            }
            
        }
    }
}
