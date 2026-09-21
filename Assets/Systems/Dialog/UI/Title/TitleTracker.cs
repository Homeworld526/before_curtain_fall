using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TitleTracker : MonoBehaviour
{
    public CanvasGroup group;
    public TextMeshProUGUI text;
    public float gradientTime;

    private Coroutine swapr;
    
    private void Update()
    {
        if (group.alpha > 0f)
        {
            string newText = DialogVisual.Instance.GetContent();
            newText = newText.Replace('^', '\n');
            if (newText != text.text && swapr == null)
            {
                StopAllCoroutines();
                swapr = StartCoroutine(SwaptextCoroutine(newText));
                
            }
        }
    }


    private IEnumerator SwaptextCoroutine(string newText)
    {
        Debug.LogWarning(newText);
        
        float elapsedTime = 0f;
        float originalAlpha = 1f;
        if (text.text != "")
        {
            while (elapsedTime < gradientTime)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / gradientTime);
                text.alpha = originalAlpha * (1f - progress);
                yield return null;
            }
        }
        text.alpha = 0;
        
        text.text = newText;

        elapsedTime = 0f;
        while (elapsedTime < gradientTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / gradientTime);
            text.alpha = originalAlpha * progress;
            yield return null;
        }

        text.alpha = originalAlpha;
        swapr = null;
    }
    
}
