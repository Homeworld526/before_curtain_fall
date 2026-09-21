using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class ZoomEffect : MonoBehaviour,IEffect
{
    public float startingPos = 1.5f;
    public float zoomTime = 2f;
    public AnimationCurve curve;

    public void ContinueEffect()
    {
        throw new System.NotImplementedException();
    }

    public bool IsRunning()
    {
        throw new System.NotImplementedException();
    }

    public void PauseEffect()
    {
        throw new System.NotImplementedException();
    }

    public void ResetEffect()
    {
        StopAllCoroutines();
    }

    private IEnumerator ZoomOut()
    {
        float elapsedTime = 0f;
        //DialogVisual.Instance.PrintOnHold = true;
        //DialogVisual.Instance.ForceNoClickSkip = true;
        while(elapsedTime < zoomTime)
        {
            elapsedTime += Time.deltaTime;
            float y = curve.Evaluate(elapsedTime / zoomTime);
            GetComponent<RectTransform>().localScale = (1f + (startingPos - 1) * (1-y)) * Vector3.one;
            yield return null;
        }
        GetComponent<RectTransform>().localScale = Vector3.one;
        //DialogVisual.Instance.ForceNoClickSkip = false;
        //DialogVisual.Instance.PrintOnHold = false;
    }

    public void StartEffect(string param)
    {
        if(param == "缩小")
        {
            StartCoroutine(ZoomOut());
        }
        if(param == "重置")
        {
            ResetEffect();
        }
    }
}
