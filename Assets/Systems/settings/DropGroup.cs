using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DelayMoves : MonoBehaviour
{
    protected void DoOneFrameDelay(Action action)
    {
        StartCoroutine(DelayOneFrame(action));
    }
    
    protected IEnumerator DelayOneFrame(Action action)
    {
        yield return null;
        action.Invoke();
    }
}

[RequireComponent(typeof(RectTransform))]
public class DropGroup : DelayMoves
{
    public List<RectTransform> elements;

    /// <summary>
    /// 将elements中的元素从矩形顶部开始纵向紧贴按顺序排列
    /// 若元素被SetActive为false，则跳过并移动其他元素继续紧贴排列
    /// </summary>
    public void ArrangeElements()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        float currentY = 0f;

        foreach (RectTransform element in elements)
        {
            if (element == null || !element.gameObject.activeSelf)
            {
                continue;
            }

            // 设置元素的锚点为顶部中心
            element.anchorMin = new Vector2(0.5f, 1f);
            element.anchorMax = new Vector2(0.5f, 1f);
            element.pivot = new Vector2(0.5f, 1f);

            // 设置位置，从顶部开始纵向排列，保持原有x位置
            element.anchoredPosition = new Vector2(element.anchoredPosition.x, -currentY);

            // 累加当前元素的高度作为下一个元素的起始位置
            currentY += element.rect.height;
        }
    }

    private void OnEnable()
    {
        DoOneFrameDelay(ArrangeElements); 
    }
}
