using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class Popup : MonoBehaviour
{
    public float popTime;


    private IEnumerator Pop()
    {
        float t = 0;
        while (t < popTime)
        {
            GetComponent<CanvasGroup>().alpha = t / popTime;
            t += Time.deltaTime;
            yield return null;
        }
        GetComponent<CanvasGroup>().alpha = 1;
    }

    private IEnumerator Popoff()
    {
        float t = 0;
        while (t < popTime)
        {
            GetComponent<CanvasGroup>().alpha =1 - t / popTime;
            t += Time.deltaTime;
            yield return null;
        }
        GetComponent<CanvasGroup>().alpha = 0;
        gameObject.SetActive(false);
    }

    public void Show()
    {
        StopAllCoroutines();
        GetComponent<CanvasGroup>().alpha = 0f;
        gameObject.SetActive(true);
        StartCoroutine(Pop());
    }

    public void Disable()
    {
        StartCoroutine(Popoff());
    }
}
