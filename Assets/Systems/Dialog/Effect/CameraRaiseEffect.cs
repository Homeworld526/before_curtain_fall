using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRaiseEffect : MonoBehaviour, IEffect
{
    private RectTransform _rect { get{return GetComponent<RectTransform>();} }
    public float moveSpeed = 10f;
    public float scaleMultiplier = 1.1f;

    private Coroutine _slideCoroutine;
    private static Vector3 _originalScale;
    private static Vector2 _originalAnchoredPos;
    private static bool initialized = false;
    private bool _isRunning;

    [SerializeField] private RectTransform Pic;
    

    private void OnEnable()
    {
        if (initialized) return;
        _originalScale = _rect.localScale;
        _originalAnchoredPos = _rect.anchoredPosition;
        initialized = true;
    }

    public bool IsRunning()
    {
        throw new System.NotImplementedException();
    }

    public void StartEffect(string param)
    {
        if (param.Contains("上移"))
        {
            StartSlide(param.Contains("放大"));
        }

        if (param == "重置")
        {
            PauseSlide();
            ResetSlide();
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
        throw new System.NotImplementedException();
    }

    private IEnumerator SlideCoroutine(bool big)
    {
        //scaleMultiplier = _rect.localScale.x;
        yield return new WaitForSeconds(0.6f);
        _isRunning = true;
        Vector3 targetScale;
        if (big)
        {
             targetScale = _originalScale * scaleMultiplier;
        }
        else
        {
            targetScale = _originalScale * _rect.localScale.x;
        }
        _rect.localScale = targetScale;

        float originalHeight = Screen.height;
        
        Debug.Log("最高点： "+ Pic.rect.yMax);
        float scaledHeight = Pic.TransformPoint(new Vector3(0, Pic.rect.yMax, 0)).y;
        Debug.Log("屏幕高度： "+originalHeight);
        Debug.Log("图片高度： "+scaledHeight);
        float heightDifference = scaledHeight - originalHeight;
        Debug.Log("位移： "+heightDifference);
        Vector2 startPos = _rect.transform.position;
        Vector2 endPos = startPos + new Vector2(0, -heightDifference);

        float distance = Vector2.Distance(startPos, endPos);
        float duration = moveSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            _rect.transform.position = Vector2.Lerp(startPos, endPos, progress);
            yield return null;
        }

        _rect.transform.position = endPos;
        _isRunning = false;
    }

    public void StartSlide(bool big)
    {
        if (_slideCoroutine != null)
            StopCoroutine(_slideCoroutine);
        _slideCoroutine = StartCoroutine(SlideCoroutine(big));
    }

    public void PauseSlide()
    {
        if (_slideCoroutine != null)
        {
            StopCoroutine(_slideCoroutine);
            _slideCoroutine = null;
            _isRunning = false;
        }
    }

    public void ResetSlide()
    {
        if (_slideCoroutine != null)
        {
            StopCoroutine(_slideCoroutine);
            _slideCoroutine = null;
        }
        _rect.localScale = _originalScale;
        _rect.anchoredPosition = _originalAnchoredPos;
        _isRunning = false;
    }
}
