using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PageSwitch : DelayMoves
{
    public List<RectTransform> elements;

    private void Awake()
    {
        elements[0].gameObject.SetActive(true);
    }

    public void Switch(int index)
    {
        for (int i = 0; i < elements.Count; i++)
        {
            elements[i].gameObject.SetActive(i == index);
            
        }
    }
}
