using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.VisualBasic;
using Microsoft.International.Converters.TraditionalChineseToSimplifiedConverter;

[RequireComponent(typeof(Image))]
public class SceneOverviewEffect : MonoBehaviour, IEffect
{
    private Image _image { get => GetComponent<Image>(); }
    public Image BackGround;
    private RectTransform _rectTransform { get => GetComponent<RectTransform>(); }
    public float moveRange;
    public float moveTime;
    public float showTime;
    public AnimationCurve curve;
    public RectTransform Popup;
    public TextMeshProUGUI text;
    public string keyWord;

    public void Awake()
    {
        Popup.gameObject.SetActive(false);
        Color color = _image.color;
        color.a = 0f;
        _image.color = color;
        origin = _rectTransform.anchoredPosition;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        DialogVisual.Instance.ForceNoClickSkip = false;
        _rectTransform.anchoredPosition = origin;
        show = false;

        Color tmp = _image.color;
        tmp.a = 0f;
        _image.color = tmp;

        if (Popup != null)
        {
            var popupCanvasGroup = Popup.GetComponent<CanvasGroup>();
            if (popupCanvasGroup != null)
            {
                popupCanvasGroup.alpha = 0f;
            }

            Popup.gameObject.SetActive(false);
        }
    }
    
    private bool show = false;

    private IEnumerator ToggleImage(float showTime)
    {
        _image.sprite = Resources.Load<Sprite>(DialogVisual.Instance.GetBackGround());
        
        if (!show) {
            while(_image.color.a < 1f)
            {
                Color color = _image.color;
                float delt = color.a + Time.deltaTime / showTime;
                color.a = Mathf.Min(delt, 1f);
                _image.color = color;
                yield return null;
            }
        }
        else
        {
            BackGround.sprite = Resources.Load<Sprite>(DialogVisual.Instance.GetNextBackGround());
            while (_image.color.a > 0f)
            {
                Color color = _image.color;
                float delt = color.a - Time.deltaTime / showTime;
                color.a = Mathf.Max(delt, 0f);
                _image.color = color;
                yield return null;
            }
        }

        show = !show;
    }

    private Vector2 origin;
    
    private IEnumerator MoveScene(float moveRange, float moveTime, AnimationCurve curve)
    {
        float t = 0;
        Coroutine x = null;
        while (t < moveTime)
        {
            _rectTransform.anchoredPosition = origin + new Vector2(moveRange * curve.Evaluate(t / moveTime), 0);
            t += Time.deltaTime;
            yield return null;
            
            if (moveTime - t < showTime && x == null)
            {
                x = StartCoroutine(ToggleImage(showTime));
            }
        }
        DialogVisual.Instance.AutoProcceed();
        DialogVisual.Instance.ForceNoClickSkip = false;
    }

    private IEnumerator ShowPopup(float popTime)
    {
        text.text = _image.sprite.name.Split('-')[1];
        Popup.GetComponent<Popup>().Show();
        yield return new WaitForSeconds(popTime);
        Popup.GetComponent<Popup>().Disable();
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

    public void StartEffect(string param)
    {
        if(param == keyWord)
        {
            _rectTransform.anchoredPosition = origin;
            DialogVisual.Instance.ForceNoClickSkip = true;
            StartCoroutine(ToggleImage(showTime));
            StartCoroutine(MoveScene(moveRange,moveTime,curve));
            StartCoroutine(ShowPopup(moveTime));
        }
    }

    
}
