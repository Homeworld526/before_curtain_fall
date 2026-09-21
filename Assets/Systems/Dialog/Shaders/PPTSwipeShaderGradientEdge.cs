using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PPTSwipeShaderGradientEdge : SingleCase<PPTSwipeShaderGradientEdge>
{
    //public RectTransform targetPage; // 新页面（PageB）
    public float swipeTime = 0.5f;   // 刷屏时长
    public bool isHorizontal = true; // 横向/纵向刷屏
    public bool isReverse = false;   // 是否反向（横：右→左 | 纵：下→上）
    private RectTransform maskRect;
    public Image image;

    private void OnEnable()
    {
        image.enabled = false;
    }


    public void StartSwipe(Image targetPage, Sprite newSprite)
    {
        image.enabled = true;
        maskRect = GetComponent<RectTransform>();
        maskRect.anchoredPosition = new Vector3(-(Screen.width / 2 + maskRect.sizeDelta.x / 2), maskRect.anchoredPosition.y, 0);
        StartCoroutine(SwipeCoroutine(targetPage, newSprite));
    }

    IEnumerator SwipeCoroutine(Image targetPage, Sprite newSprite)
    {
        DialogVisual.Instance.ForceNoClickSkip = true;
        float length = Screen.width + maskRect.sizeDelta.x;
        float speed = length / swipeTime;
        float elapsedTime = 0f;
        while (elapsedTime < swipeTime)
        {
            elapsedTime += Time.deltaTime;
            maskRect.anchoredPosition = new Vector3(maskRect.anchoredPosition.x + speed * Time.deltaTime, maskRect.anchoredPosition.y, 0);
            if (elapsedTime > swipeTime / 2)
            {
                if (targetPage.sprite.name != newSprite.name)
                {
                    targetPage.sprite = newSprite;
                }
            }
            yield return null;
        }
        DialogVisual.Instance.ForceNoClickSkip = false;
        image.enabled = false;
    }
}
