using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static UnityEngine.UI.Image;

[RequireComponent(typeof(RectTransform))]
public class CameraScaleEffect : MonoBehaviour, IEffect
{
    public float minv;
    public float maxv;
    private Coroutine co = null;
    public AnimationCurve curve;
    public float time;
    public bool isCG = false;
    
    public float scale
    {
        get { return m_RectTransform.localScale.x; }
    }

    private RectTransform m_RectTransform {
        get { return GetComponent<RectTransform>(); }
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
        throw new System.NotImplementedException();
    }

    public GameObject tmpPic;
    
    private IEnumerator ScaleI(float scale)
    {
        //yield return null;
        if (isCG && DialogVisual.Instance.GetCG() == "")
        {
            
            float t = 0f;
            while (t < 1f)
            {
                t+= Time.deltaTime;
                yield return null;
            }
            m_RectTransform.localScale = new Vector3(scale, scale, scale);
            co = null;
        }
        else
        {
            float originalscale = m_RectTransform.localScale.x;
        
            float t = 0f;
            if (Mathf.Abs(scale - originalscale) < float.Epsilon) t = time + 1;
            tmpPic = Instantiate(this.gameObject, this.transform.parent);
            tmpPic.transform.SetSiblingIndex(this.transform.GetSiblingIndex());
            foreach(var et in tmpPic.GetComponents<EffectTracker>()){
                et.enabled = false;
            }
            tmpPic.GetComponent<CanvasGroup>().alpha = this.transform.GetComponentInChildren<Image>().color.a;
            GetComponent<CanvasGroup>().alpha = 0;
        
            m_RectTransform.localScale = new Vector3(scale, scale, scale);
            while (t < time)
            {
                t += Time.deltaTime;
                if (GetComponent<CameraPosEffect>() == null) tmpPic.GetComponent<CanvasGroup>().alpha = 1 - t / time;
                GetComponent<CanvasGroup>().alpha = t / time;
                //SetAllImagesAlpha(tmpPic.transform, 1 - t / time);
                yield return null;
            }
            GetComponent<CanvasGroup>().alpha = 1;
            Destroy(tmpPic);
            tmpPic = null;
            //Debug.LogWarning("finish");
            co = null;
        }
        
    }

    private IEnumerator ScaleS(float scale)
    {
        float originalscale = m_RectTransform.localScale.x;

        float t = 0f;
        //m_RectTransform.localScale = new Vector3(scale, scale, scale);
        while (t < time)
        {
            t += Time.deltaTime;
            m_RectTransform.localScale =Vector3.one * ( originalscale + (scale - originalscale) * curve.Evaluate(t / time));
            yield return null;
        }

        co = null;
    }

    private IEnumerator ScaleS(float scale, float duration)
    {
        float originalscale = m_RectTransform.localScale.x;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            m_RectTransform.localScale = Vector3.one * (originalscale + (scale - originalscale) * curve.Evaluate(t / duration));
            yield return null;
        }

        co = null;
    }

    private void StopCo()
    {
        if (co == null) return;
        StopCoroutine(co);
        co = null;
        if (tmpPic != null)
        {
            Destroy(tmpPic);
            tmpPic = null;
            
            GetComponent<CanvasGroup>().alpha = 1;
        }
    }

    public void StartEffect(string param)
    {
        if (param == "缓慢放大")
        {
            StopCo();
            co = StartCoroutine(ScaleS(1.15f, 10f));
            return;
        }

        if (!param.Contains("特写")) return;
        string pattern = $"(\\d+)";
        Match match = Regex.Match(param, pattern);
        if (match.Success)
        {

            //Debug.LogWarning("start");
            int num = int.Parse(match.Groups[0].Value);
            Debug.LogWarning("特写" + num);
            float res = minv + (maxv - minv) * (num / 100f);
            if (param.Contains("滑动"))
            {
                StopCo();
                co = StartCoroutine(ScaleS(res));
            }
            else if (param.Contains("缓动"))
            {
                float time = 5f;
                StopCo();
                co = StartCoroutine(ScaleS(res, time));
            }
            else
            {
                if (!CheckAllImageAlpha(this.transform))
                {
                    //Debug.LogWarning("Transparent");
                    m_RectTransform.localScale = new Vector3(res, res, res);
                    return;
                }
                StopCo();
                co = StartCoroutine(ScaleI(res));
            }
            
            
            //m_RectTransform.localScale = new Vector3(res, res, res);
        }
    }


    public bool CheckAllImageAlpha(Transform t)
    {
        Image[] allImages = t.GetComponentsInChildren<Image>(true);

        bool res = true;
        
        // 遍历并修改每个Image的透明度
        foreach (Image image in allImages)
        {

            res &= image.color.a < 1f;

        }
        return !res;
    }

    private void SetAllImagesAlpha(Transform t ,float alpha)
    {
        // 校验透明度值范围（确保在0-1之间）
        alpha = Mathf.Clamp01(alpha);

        // 获取父物体下所有的Image组件（包括所有层级的子物体）
        Image[] allImages = t.GetComponentsInChildren<Image>(true);

        // 遍历并修改每个Image的透明度
        foreach (Image image in allImages)
        {
            if (image != null)
            {
                // 保留原有颜色的RGB值，只修改A通道（透明度）
                Color newColor = image.color;
                newColor.a = alpha;
                image.color = newColor;
            }
        }

        //Debug.Log($"已将{uiParent.name}下{allImages.Length}个Image的透明度设置为：{alpha}");
    }
}
