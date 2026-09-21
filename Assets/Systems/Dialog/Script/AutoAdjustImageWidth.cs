using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class AutoAdjustImageWidth : MonoBehaviour
{
    //[Header("固定高度（像素）")]
    //[Tooltip("设置Image要保持的固定高度")]
    private float fixedHeight {
        get
        {
            return GetComponent<RectTransform>().rect.height;
        }
    }

    private Image _image;
    private RectTransform _rectTransform;

    // 初始化
    private void Awake()
    {
        _image = GetComponent<Image>();
        _rectTransform = GetComponent<RectTransform>();
        // 初始化时调整尺寸
        AdjustWidthAccordingToSprite();
    }

    // 编辑器模式下实时预览
    private void OnValidate()
    {
        _image = GetComponent<Image>();
        _rectTransform = GetComponent<RectTransform>();
        AdjustWidthAccordingToSprite();
    }

    /// <summary>
    /// 适配任意锚点：固定高度，按Sprite比例自动调整宽度
    /// 保留原有锚点、轴心、位置不变
    /// </summary>
    public void AdjustWidthAccordingToSprite()
    {
        // 校验组件和Sprite有效性
        if (_image == null || _image.sprite == null)
        {
            Debug.LogWarning("Image组件或Sprite为空，无法调整尺寸", this);
            return;
        }

        // 获取Sprite原始宽高（像素）
        float spriteWidth = _image.sprite.rect.width;
        float spriteHeight = _image.sprite.rect.height;

        // 防止除零异常
        if (spriteHeight <= 0)
        {
            Debug.LogError("Sprite原始高度为0，无法计算宽高比", this);
            return;
        }

        // 计算Sprite原始宽高比
        float aspectRatio = spriteWidth / spriteHeight;
        // 根据固定高度计算目标宽度
        float targetWidth = fixedHeight * aspectRatio;

        // 关键：使用SetSizeWithCurrentAnchors适配任意锚点
        // 第一步：先固定高度（不修改锚点/位置）
        _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fixedHeight);
        // 第二步：按比例设置宽度（自动适配锚点规则）
        _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
    }

    // 可选：提供外部调用的方法，方便动态更换Sprite后调整
    public void UpdateImageWithNewSprite(Sprite newSprite)
    {
        if (newSprite == null)
        {
            Debug.LogWarning("传入的新Sprite为空", this);
            return;
        }
        _image.sprite = newSprite;
        AdjustWidthAccordingToSprite();
    }
}
