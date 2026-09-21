using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ForcePlayEffect : MonoBehaviour, IEffect
{
    public CanvasGroup bracket;
    public CanvasGroup Play;
    public float IOTime;
    public float WaitTime;
    private Coroutine inplay;
    private Coroutine outplay;
    
    public bool IsRunning()
    {
        throw new System.NotImplementedException();
    }

    private IEnumerator InPlay()
    {
        DialogVisual.Instance.ForceNoClickSkip = true;
        yield return null;

        float elapsed = 0f;
        while (elapsed < IOTime)
        {
            DialogVisual.Instance.ClearPrint();
            elapsed += Time.deltaTime;
            Play.alpha = elapsed / IOTime;
            yield return null;
        }

        Play.alpha = 1;
        bracket.alpha = 0;

        if (WaitTime > 0)
        {
            float tt = WaitTime;
            WaitTime = 0;
            yield return new WaitForSeconds(tt);
        }

        DialogVisual.Instance.ForceNoClickSkip = false;
        inplay = null;
    }


    
    private IEnumerator OutPlay()
    {

        bool iswait = false;

        yield return null;

        DialogVisual.Instance.ForceNoClickSkip = true;
        if (WaitTime > 0)
        {
            iswait = true;
            float tt = WaitTime;
            WaitTime = 0;
            yield return new WaitForSeconds(tt);
        }
        bracket.alpha = 1;
        DialogVisual.Instance.ClearPrint();

        float elapsed = 0f;
        while (elapsed < IOTime)
        {
            elapsed += Time.deltaTime;
            Play.alpha = 1f - elapsed / IOTime;
            yield return null;
        }

        Play.alpha = 0;
        if (iswait)
        {
            DialogVisual.Instance.AutoProcceed();
        }
        DialogVisual.Instance.ForceNoClickSkip = false;
        GetComponentInChildren<TextMeshProUGUI>().text = "";
        outplay = null;
    }
    
    public void StartEffect(string param)
    {
        int t = 0;

        if(int.TryParse(param, out t))
        {
            Debug.LogWarning(param);
            WaitTime = t;
        }
        if (param == "渐入")
        {
            if (outplay != null) { StopCoroutine(outplay); outplay = null; }
            if (inplay != null) StopCoroutine(inplay);
            Play.alpha = 0;
            inplay = StartCoroutine(InPlay());
        }
        else if (param == "渐出")
        {
            if (inplay != null) { StopCoroutine(inplay); inplay = null; }
            if (outplay != null) StopCoroutine(outplay);
            Play.alpha = 1;
            outplay = StartCoroutine(OutPlay());
        }
    }

    public void PauseEffect()
    {
        throw new System.NotImplementedException();
    }

    public void ContinueEffect()
    {
        throw new System.NotImplementedException();
    }

    public void ResetEffect()
    {
        StopAllCoroutines();
        inplay = null;
        outplay = null;
        Play.alpha = 0;
        DialogVisual.Instance.ForceNoClickSkip = false;
    }
}
