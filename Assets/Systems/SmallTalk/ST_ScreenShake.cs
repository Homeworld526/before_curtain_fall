using System.Collections;
using UnityEngine;

public class ST_ScreenShake : SmallTalkElements
{
    [SerializeField] private float intensity = 8f;

    private RectTransform _rect;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    protected override void OnMoveNext(DialogNode node)
    {
        foreach (var VARIABLE in node.note.Split('+'))
        {
            if (VARIABLE == "屏幕抖动")
            {
                Shake();
                return;
            }
        }
        
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
}
