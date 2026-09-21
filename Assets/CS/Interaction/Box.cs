using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Box : AutoInteractable
{
    public GameObject boxUI;

    protected override void Reset()
    {
        base.Reset();
        promptText = "打开箱子";
    }

    public override void OnInteract()
    {
        if (boxUI != null)
        {
            boxUI.SetActive(true);
            
            Debug.Log("箱子已打开");
        }
        else
        {
            Debug.LogWarning("poster UI is null");
        }
    }
}
