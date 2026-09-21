using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PicRSetMiddleEffect : MonoBehaviour
{
    public static PicRSetMiddleEffect Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }


    [SerializeField] private Vector3 posr;
    [SerializeField] private Vector3 posm;
    [SerializeField] private Vector3 picLDefaultPos;


    public float setmiddleDis = 200f;
    public float setmiddleHeight = 45f;
    public float setmiddleTime = 0.5f;
    public Image PicL;
    public Image PicR;

    public bool IsRunning()
    {
        throw new System.NotImplementedException();
    }

    public void StartEffect(bool param)
    {
        Debug.LogWarning(param);
        if (param)
        {
            PicL.color = new Color(PicL.color.r, PicL.color.g, PicL.color.b, 0f);
            PicR.GetComponent<RectTransform>().anchoredPosition = posm;
            PicL.GetComponent<RectTransform>().anchoredPosition = picLDefaultPos;
        }
        else
        {
            PicL.color = new Color(PicL.color.r, PicL.color.g, PicL.color.b,  VisualLocator.Instance.currentState == VisualLocator.VisualState.L ? 1f : 0f);
            PicL.GetComponent<RectTransform>().anchoredPosition = picLDefaultPos;
            PicR.GetComponent<RectTransform>().anchoredPosition = posr;
        }
    }

    
}
