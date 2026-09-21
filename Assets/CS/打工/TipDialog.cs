using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TipDialog : MonoBehaviour
{
    public static TipDialog Instance { get; private set; }

    [Header("UI控件")]
    public GameObject panel;

    [Header("自动关闭设置")]
    public float autoCloseDelay = 1f;

    private Coroutine closeCoroutine;

    public static void EnsureInstance()
    {
        if (Instance == null)
        {
            GameObject tipDialogObj = new GameObject("TipDialog");
            TipDialog tipDialog = tipDialogObj.AddComponent<TipDialog>();
            
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                tipDialogObj.transform.SetParent(canvas.transform, false);
            }
            
            RectTransform rect = tipDialogObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            GameObject panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(tipDialogObj.transform, false);
            
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;

            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.7f);

            tipDialog.panel = panelObj;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (panel == null)
        {
            Transform panelTrans = transform.Find("Panel");
            if (panelTrans != null)
            {
                panel = panelTrans.gameObject;
            }
        }
    }

    private void Start()
    {
        Hide();
    }

    public void Show(string message = "")
    {
        if (Instance == null)
        {
            EnsureInstance();
        }

        if (panel != null)
        {
            panel.SetActive(true);
        }

        if (closeCoroutine != null)
        {
            StopCoroutine(closeCoroutine);
        }

        closeCoroutine = StartCoroutine(AutoCloseCoroutine());
    }

    private IEnumerator AutoCloseCoroutine()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        Hide();
    }

    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // 清空静态变量，避免指向已销毁的对象
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
