using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnfoldButton : MonoBehaviour
{
    public RectTransform targ;
    private bool state = true;
    public RectTransform arrow;
    public DropGroup group;
    
    private void Start()
    {
        targ.gameObject.SetActive(false);
    }

    public void Unfold()
    {
        
        targ.gameObject.SetActive(state);
        state = !state;
        arrow.localScale = new Vector3(1, state ? -1 : 1, 1);
        
        group.ArrangeElements();
    }
}
