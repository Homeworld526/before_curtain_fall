using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Poster : AutoInteractable
{
    public GameObject posterUI;

    protected override void Reset()
    {
        base.Reset();
        promptText = "查看海报";
    }

    public override void OnInteract()
    {
        if (posterUI != null)
        {
            posterUI.SetActive(true);
            Debug.Log("海报已查看");
        }
        else
        {
            Debug.LogWarning("poster UI is null");
        }
    }
}
