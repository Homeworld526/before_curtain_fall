using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 简单前置对话框，支持多句对话，点击任意位置进入下一句。
/// </summary>
public class SimpleDialog : MonoBehaviour
{
    [Header("UI 引用")]
    public TextMeshProUGUI contentText;
    public Image tailImage;
    [SerializeField] private TextMeshProUGUI name;
    
    [Header("设置")]
    [TextArea(2, 5)]
    public string[] dialogLines;

    public Sprite tailSprite;
    public float fadeInDuration = 0.3f;

    private CanvasGroup canvasGroup;
    private int currentLine = 0;
    private Action onComplete;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 设置尾巴图片
        if (tailImage != null && tailSprite != null)
        {
            tailImage.sprite = tailSprite;
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 显示对话框
    /// </summary>
    public void Show(string[] lines, Action onCompleteCallback)
    {
        dialogLines = lines;
        onComplete = onCompleteCallback;
        currentLine = 0;
        if (lines.Length < 3)
        {
            name.text = "";
        }
        
        gameObject.SetActive(true);
        canvasGroup.alpha = 0f;
        StartCoroutine(FadeIn());

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (dialogLines == null || currentLine >= dialogLines.Length)
        {
            // 对话结束
            Hide();
            onComplete?.Invoke();
            return;
        }

        if (contentText != null)
        {
            contentText.text = dialogLines[currentLine];
            if (tailImage != null)
            {
                tailImage.GetComponent<AttachToTextEnd>().UpdateSpritePosition(contentText);
            }
        }


    }

    private System.Collections.IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

    }

    private void Update()
    {
        if (!gameObject.activeSelf) return;

        // 点击任意位置进入下一句
        if (Input.GetMouseButtonDown(0))
        {
            currentLine++;
            ShowCurrentLine();
        }
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
