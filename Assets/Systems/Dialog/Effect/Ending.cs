using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Ending : MonoBehaviour, IEffect
{
    public CanvasGroup ending;
    public ScrollviewAdder endingScroll;
    public GameObject EndIcon;
    [SerializeField] private Image backGround;
    
    public bool IsRunning()
    {
        return ending.gameObject.activeInHierarchy;
    }

    public void StartEffect(string param)
    {
        if (param == "开始")
        {
            ending.gameObject.SetActive(true);
            EndIcon.SetActive(false);
            StartCoroutine(FadeInEnding());
            DialogVisual.Instance.enableFill = true;
        }
        else if (param == "结束")
        {
            ending.gameObject.SetActive(false);
            EndIcon.SetActive(true);
            backGround.sprite = Resources.Load<Sprite>("黑屏");
            backGround.color = new Color(1f, 1f, 1f, 1f);
            DialogVisual.Instance.enableFill = false;
        }
        else if (param == "翻页")
        {
            endingScroll.preClear = true;
        }
    }

    /// <summary>
    /// 协程：Ending渐入效果
    /// </summary>
    private IEnumerator FadeInEnding(float duration = 0.5f)
    {
        endingScroll.Clear();
        ending.alpha = 0f;
        float elapsed = 0f;
        DialogVisual.Instance.ClearPrint();
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            ending.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }

        ending.alpha = 1f;
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
