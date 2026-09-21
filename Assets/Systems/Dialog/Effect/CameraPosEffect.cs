using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class CameraPosEffect : MonoBehaviour, IEffect
{
    public Vector2 minv;
    public Vector2 maxv;
    public AnimationCurve curve;
    public float time;
    public Coroutine co;
    private Vector2 original;
    
    public AnimationCurve blurCurve;

    public float pos
    {
        get { return m_RectTransform.anchoredPosition.x; }
    }

    private RectTransform m_RectTransform
    {
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

    private IEnumerator ScaleI(Vector2 scale, Action onScaleEnd = null)
    {
        yield return ScaleI(scale, time, onScaleEnd);
    }

    private IEnumerator ScaleI(Vector2 scale, float duration, Action onScaleEnd = null)
    {
        yield return null;
        if (GetComponent<CameraScaleEffect>().tmpPic != null || GetComponentInChildren<Image>().color.a < 1f)
        {
            m_RectTransform.anchoredPosition = scale;
        }
        else
        {
            Vector2 originalPos = m_RectTransform.anchoredPosition;
            float t = 0f;
            var blur = GetComponent<ImageBlurController>();
            while (t < duration)
            {
                t += Time.deltaTime;

                m_RectTransform.anchoredPosition = originalPos + (scale - originalPos) * curve.Evaluate(t / duration);

                if (blur != null && duration < 10f)
                {
                    blur.blurRadius = 0.1f * blurCurve.Evaluate(t / duration);
                }

                yield return null;
            }
        }
        onScaleEnd?.Invoke();
        //Debug.LogWarning("finish");
        co = null;
    }

    public float slowMoveSpeed = 10f;
    private Coroutine slowMovec;
    public float boundaryx;
    

    private void StopCo()
    {
        if (co == null) return;
        StopCoroutine(co);
        co = null;
        var blur = GetComponent<ImageBlurController>();
        if (blur != null) blur.blurRadius = 0f;
    }

    private void StopSlowMove()
    {
        if (slowMovec != null) StopCoroutine(slowMovec);
        slowMovec = null;
    }

    private IEnumerator SlowMoveLeftI(float speed)
    {
        Debug.LogWarning("开始缓动");
        Vector2 currentPos = m_RectTransform.anchoredPosition;
        float targetRightBoundary = original.x + m_RectTransform.sizeDelta.x / 2f;

        while (true)
        {
            float currentRightBoundary = m_RectTransform.localScale.x * (currentPos.x + m_RectTransform.sizeDelta.x) / 2f;

            if (currentRightBoundary <= targetRightBoundary)
                break;

            currentPos.x -= speed * Time.deltaTime;
            m_RectTransform.anchoredPosition = currentPos;

            yield return null;
        }

        slowMovec = null;
    }

    private void Start()
    {
        original = new Vector2(864,274);
    }

    public void StartEffect(string param)
    {
        if (!param.Contains("偏移")) return;

        string pattern = $"(\\d+)";
        Match match = Regex.Match(param, pattern);
        
        if (match.Success)
        {
            int num = int.Parse(match.Value);
            Debug.LogWarning(param);

            if (param.Contains("焦点"))
            {
                Vector2 focus = GetComponentInChildren<FocusPoint>().GetFocus(num);
                Debug.LogWarning(focus);
                if (param.Contains("缓动"))
                {
                    if (co == null) co = StartCoroutine(ScaleI(focus, () =>
                    {
                        if(slowMovec!=null) StopSlowMove();
                        slowMovec = StartCoroutine(SlowMoveLeftI(slowMoveSpeed));
                    }));
                    else
                    {
                        StopCo();
                        co = StartCoroutine(ScaleI(focus, () =>
                        {
                            if(slowMovec!=null) StopSlowMove();
                            slowMovec = StartCoroutine(SlowMoveLeftI(slowMoveSpeed));
                        }));
                    }
                }
                else if (param.Contains("缓慢"))
                {
                    Debug.Log("man");
                    StopCo();
                    co = StartCoroutine(ScaleI(focus, 10f));
                }
                else
                {
                    if (co == null) co = StartCoroutine(ScaleI(focus));
                    else
                    {
                        StopCo();
                        co = StartCoroutine(ScaleI(focus));
                    }
                }

                return;
            }

            if (num == 50)
            {
                if (slowMovec != null)
                {
                    StopCoroutine(slowMovec);
                    slowMovec = null;
                }
                if (co == null) co = StartCoroutine(ScaleI(original));
                else
                {
                    StopCo();
                    co = StartCoroutine(ScaleI(original));
                }
                return;
            }
            Vector2 res = original;
            if (num > 50)
            {
                res = original + (maxv - original) * ((num-50) / 100f);
            }
            else
            {
                res = original + (minv - original) * ((50-num) / 100f);
            }

            if (co == null) co = StartCoroutine(ScaleI(res));
            else
            {
                StopCo();
                co = StartCoroutine(ScaleI(res));
            }
            //m_RectTransform.anchoredPosition = new Vector2(res, m_RectTransform.anchoredPosition.y);
        }
    }

}
