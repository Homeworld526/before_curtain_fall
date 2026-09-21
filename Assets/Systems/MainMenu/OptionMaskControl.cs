using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectMask2D))]
public class OptionMaskControl : MonoBehaviour
{
    public float startTime;
    public float interval;
    public float endPos;

    private RectMask2D rectMask;

    void Start()
    {
        rectMask = GetComponent<RectMask2D>();
        StartCoroutine(AnimatePaddingRight());
    }

    private IEnumerator AnimatePaddingRight()
    {
        // 等待 startTime
        yield return new WaitForSeconds(startTime);

        // 记录起始位置
        Vector4 startPadding = rectMask.padding;
        float startPos = startPadding.z;

        // 在 interval 时间内匀速移动到 endPos
        float elapsed = 0f;
        while (elapsed < interval)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / interval);
            
            Vector4 currentPadding = rectMask.padding;
            currentPadding.z = Mathf.Lerp(startPos, endPos, t);
            rectMask.padding = currentPadding;
            
            yield return null;
        }

        // 确保最终位置准确
        Vector4 finalPadding = rectMask.padding;
        finalPadding.z = endPos;
        rectMask.padding = finalPadding;
    }
}
