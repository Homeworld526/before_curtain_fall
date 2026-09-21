using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : AutoInteractable
{
    public GameObject doorUI;

    protected override void Reset()
    {
        base.Reset();
        promptText = "打开门";
    }

    public override void OnInteract()
    {
        if (doorUI != null)
        {
            doorUI.SetActive(true);
            Debug.Log("门已打开");
        }
        else
        {
            Debug.LogWarning("poster UI is null");
        }
    }
}
