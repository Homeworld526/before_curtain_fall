using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks.Sources;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(RectTransform))]
public class SpinEffect : MonoBehaviour, IEffect
{
    public float spinTime;
    public float maxSpin;
    public float maxScale;
    public AnimationCurve curve;
    public float minScale = 1.05f;

    private GameObject _blackBg;

    private void CreateBlackBg()
    {
        RectTransform rect = GetComponent<RectTransform>();
        Transform parent = rect.parent;
        
        DestroyBlackBg();
        
        _blackBg = new GameObject("_SpinBlackBg", typeof(RectTransform), typeof(Image));
        RectTransform bgRect = _blackBg.GetComponent<RectTransform>();
        bgRect.SetParent(parent, false);

        // 插入到本 Image 的下方、父级其他子元素的上方
        int siblingIndex = rect.GetSiblingIndex();
        bgRect.SetSiblingIndex(siblingIndex);

        // 复制变换
        bgRect.localPosition = rect.localPosition;
        bgRect.localRotation = rect.localRotation;
        bgRect.localScale = rect.localScale;
        bgRect.anchorMin = rect.anchorMin;
        bgRect.anchorMax = rect.anchorMax;
        bgRect.anchoredPosition = rect.anchoredPosition;
        bgRect.sizeDelta = rect.sizeDelta;
        bgRect.pivot = rect.pivot;

        _blackBg.GetComponent<Image>().color = Color.black;
    }

    private void DestroyBlackBg()
    {
        if (_blackBg != null)
        {
            Destroy(_blackBg);
            _blackBg = null;
        }
    }

    private IEnumerator Spin()
    {
        CreateBlackBg();
        RectTransform rect = GetComponent<RectTransform>();
        float elapsedTime = 0f;
        while(elapsedTime < spinTime)
        {
            elapsedTime += Time.deltaTime;

            rect.localRotation = Quaternion.Euler(0f,0f,Mathf.Lerp(0,1,(elapsedTime/spinTime)) * maxSpin);
            float s = curve.Evaluate(elapsedTime /spinTime) * (maxScale-minScale);
            rect.localScale = (s + minScale) * Vector3.one;
            yield return null;
        }
        DestroyBlackBg();
    }


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
        Debug.Log("deleted");

        StopAllCoroutines();
        DestroyBlackBg();
        RectTransform rect = GetComponent<RectTransform>();
        rect.localRotation = Quaternion.Euler(0f, 0f, 0f);
        rect.localScale = Vector3.one;
    }

    private void OnEnable()
    {
        ResetEffect();
    }

    private void OnDisable()
    {
        DestroyBlackBg();
    }

    public void StartEffect(string param)
    {
        if(param == "螺旋放大")
        {
            StartCoroutine(Spin());
        }
        if(param == "重置")
        {
            Debug.LogWarning("Spin重置");
            ResetEffect();
        }
    }


}
