using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 挂载在名牌详情预制体上，管理标题和内容的显示
/// </summary>
public class MapNameplateDetail : MonoBehaviour
{
    [Header("UI 引用")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI contentText;
    public Image backgroundImage;
    public Image borderImage;

    /// <summary>
    /// 设置标题
    /// </summary>
    public void SetTitle(string title)
    {
        if (titleText != null)
        {
            titleText.text = title;
        }
    }

    /// <summary>
    /// 设置内容
    /// </summary>
    public void SetContent(string content)
    {
        if (contentText != null)
        {
            contentText.text = content;
        }
    }

    /// <summary>
    /// 设置底图
    /// </summary>
    public void SetBackground(Sprite sprite)
    {
        if (backgroundImage != null)
        {
            backgroundImage.sprite = sprite;
        }
    }

    /// <summary>
    /// 设置边框
    /// </summary>
    public void SetBorder(Sprite sprite)
    {
        if (borderImage != null)
        {
            borderImage.sprite = sprite;
        }
    }

    /// <summary>
    /// 一次性设置所有内容
    /// </summary>
    public void Setup(string title, string content, Sprite background = null, Sprite border = null)
    {
        SetTitle(title);
        SetContent(content);

        if (background != null) SetBackground(background);
        if (border != null) SetBorder(border);
    }
}
