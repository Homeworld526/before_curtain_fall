using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LittleCGItem : MonoBehaviour
{
    [Header("组件引用")]
    public Image iconImage;      // 进度条格子的图标
    public TextMeshProUGUI rewardText; // 报酬文本
    public Image coinIcon;       // 金币图标（显示报酬时启用）
    public Image Head;           // 角色打工头像（角色打工时显示）

    private float[] childMaskOriginalAlphas; // 存储子物体蒙版图片的原始透明度

    // 初始化状态
    public void Initialize()
    {
        // 先保存所有子物体蒙版图片的原始透明度
        SaveChildMaskAlphas();
        
        // 图标图片
        if (iconImage != null && !IsMaskImage(iconImage))
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
        }
        
        // 报酬文本
        if (rewardText != null)
        {
            rewardText.gameObject.SetActive(false);
            rewardText.text = "";
        }
        
        // 金币图标
        if (coinIcon != null)
        {
            coinIcon.enabled = false;
        }

        // 角色打工头像
        if (Head != null)
        {
            Head.enabled = false;
            Head.sprite = null;
        }

        // 禁用"Null"子对象（空打工显示的对象）
        Transform nullTransform = transform.Find("Null");
        if (nullTransform != null)
        {
            nullTransform.gameObject.SetActive(false);
        }
        
        // 恢复子物体蒙版图片的原始透明度
        RestoreChildMaskAlphas();
    }
    
    // 保存所有子物体蒙版图片的原始透明度
    private void SaveChildMaskAlphas()
    {
        Image[] allImages = GetComponentsInChildren<Image>(true);
        childMaskOriginalAlphas = new float[allImages.Length];
        
        for (int i = 0; i < allImages.Length; i++)
        {
            if (IsMaskImage(allImages[i]))
            {
                childMaskOriginalAlphas[i] = allImages[i].color.a;
            }
            else
            {
                childMaskOriginalAlphas[i] = -1f; // 标记为非蒙版图片
            }
        }
    }
    
    // 恢复子物体蒙版图片的原始透明度
    private void RestoreChildMaskAlphas()
    {
        if (childMaskOriginalAlphas == null) return;
        
        Image[] allImages = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < allImages.Length && i < childMaskOriginalAlphas.Length; i++)
        {
            if (childMaskOriginalAlphas[i] >= 0f) // 只恢复蒙版图片
            {
                Color color = allImages[i].color;
                color.a = childMaskOriginalAlphas[i];
                allImages[i].color = color;
            }
        }
    }

    // 设置图标
    public void SetIcon(Sprite sprite)
    {
        if (iconImage != null && !IsMaskImage(iconImage))
        {
            iconImage.sprite = sprite;
            iconImage.enabled = sprite != null;
        }
    }
    
    // 检查是否是蒙版图片
    private bool IsMaskImage(Image image)
    {
        if (image == null) return false;
        
        // 检查是否有 Mask 组件
        if (image.GetComponent<Mask>() != null)
            return true;
        
        // 检查是否有 MaskableGraphic 组件且是遮罩目标
        MaskableGraphic maskable = image.GetComponent<MaskableGraphic>();
        if (maskable != null && !maskable.maskable)
            return true;
        
        // 检查名称是否包含蒙版相关的关键词
        if (image.name.Contains("Mask") || image.name.Contains("mask") || 
            image.name.Contains("遮罩") || image.name.Contains("MaskImage"))
            return true;
        
        return false;
    }

    // 显示报酬文本和金币图标
    public void ShowReward(int reward)
    {
        if (rewardText != null)
        {
            rewardText.text = $"+{reward}";
            rewardText.gameObject.SetActive(true);
            
            // 设置完全不透明
            Color color = rewardText.color;
            color.a = 1f;
            rewardText.color = color;
        }
        
        // 启用金币图标
        if (coinIcon != null)
        {
            coinIcon.enabled = true;
        }
    }

    // 降低透明度
    public void FadeOut(float alpha)
    {
        if (rewardText != null && rewardText.gameObject.activeSelf)
        {
            Color color = rewardText.color;
            color.a = alpha;
            rewardText.color = color;
        }
        
        // 金币图标也降低透明度
        if (coinIcon != null && coinIcon.enabled)
        {
            Color color = coinIcon.color;
            color.a = alpha;
            coinIcon.color = color;
        }
    }

    // 设置图标可见
    public void ShowIcon()
    {
        if (iconImage != null && !IsMaskImage(iconImage))
        {
            iconImage.enabled = true;
        }
    }

    // 设置角色打工头像
    public void SetCharacterHead(Sprite headSprite)
    {
        if (Head != null)
        {
            Head.sprite = headSprite;
            Head.enabled = headSprite != null;
        }

        // 禁用iconImage
        if (iconImage != null )
        {
            Debug.Log("[LittleCGItem] 禁用iconImage");
            iconImage.enabled = false;
        }
        else
        {
            Debug.LogWarning("[LittleCGItem] iconImage 未配置或为蒙版图片");
        }
    }
}