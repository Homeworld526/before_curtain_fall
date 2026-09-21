using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenShakeEffect : MonoBehaviour, IEffect
{
    [SerializeField] private float intensity = 8f;

    private RectTransform _rect;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    
    public void Shake()
    {
        StopAllCoroutines();
        StartCoroutine(ShakeRoutine());
    }
    
    private IEnumerator ShakeRoutine()
    {
        Vector2 origin = _rect.anchoredPosition;
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            float dampedIntensity = intensity * (1f - progress);
            _rect.anchoredPosition = origin + Random.insideUnitCircle * dampedIntensity;
            elapsed += Time.deltaTime;
            yield return null;
        }

        _rect.anchoredPosition = origin;
    }

    public bool IsRunning()
    {
        throw new System.NotImplementedException();
    }

    public void StartEffect(string param)
    {
        if (param == "抖动")
        {
            Shake();
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
}
