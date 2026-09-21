using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PageAnim : MonoBehaviour
{
    public float distance;
    private Vector3 originalPos;

    private void Start()
    {
        originalPos = transform.position;
    }

    private void OnDisable()
    {
        ResetAnim();
    }

    public void PlayAnim()
    {
        transform.position = originalPos + Vector3.right * distance;
    }

    public void ResetAnim()
    {
        transform.position = originalPos;
    }
}
