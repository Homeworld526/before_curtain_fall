using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class AttachToTextEnd : MonoBehaviour
{
    public ST_TextPrinter smalltalk;
    
    private RectTransform targetImage { get => GetComponent<RectTransform>(); }

    [Header("偏移调整")]
    [Tooltip("Sprite 相对于文本末端的偏移量 (避免贴太紧)")]
    public Vector2 offset = new Vector2(0.1f, 0);

    [Tooltip("是否每帧更新 (动态文本必开)")]
    public bool updateEveryFrame = false;

    // 缓存 TMP 文本组件
    public TMP_Text _tmpText;
    private RectTransform _textRect { get => _tmpText.GetComponent<RectTransform>(); }

    public void Hide()
    {
        Color color = GetComponent<Image>().color;
        color.a = 0;
        GetComponent<Image>().color = color;
    }

    public void Show()
    {
        Color color = GetComponent<Image>().color;
        color.a = 1;
        GetComponent<Image>().color = color;
    }

    public void OnEnable()
    {
        DialogVisual.Instance.OnTextDisplayEnd += UpdateSpritePosition;
        DialogVisual.Instance.OnTextDisplayStart += Hide;
        smalltalk.OnTextDisplayStart += Hide;
        smalltalk.OnTextDisplayEnd += UpdateSpritePosition_ST;
    }

    public void Disable()
    {
        DialogVisual.Instance.OnTextDisplayEnd -= UpdateSpritePosition;
        DialogVisual.Instance.OnTextDisplayStart -= Hide;
        smalltalk.OnTextDisplayStart -= Hide;
        smalltalk.OnTextDisplayEnd -= UpdateSpritePosition_ST;
    }

    private void LateUpdate()
    {
        // 动态文本：每帧更新位置
        if (updateEveryFrame)
        {
            UpdateSpritePosition();
        }
    }

    public void UpdateSpritePosition_ST()
    {
        UpdateSpritePosition(smalltalk.GetComponent<TextMeshProUGUI>());
    }

    public void UpdateSpritePosition(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;
        
        TextMeshProUGUI tmpText = tmp.GetComponent<TextMeshProUGUI>();
        RectTransform textRect = tmp.GetComponent<RectTransform>();
        
        // 强制刷新文本网格，获取最新字符位置
        tmpText.ForceMeshUpdate();

        if (tmpText == null || targetImage == null) return;
        if (tmpText.textInfo.characterCount == 0) return;

        // 获取最后一个可见字符
        //Debug.Log(_tmpText.textInfo.characterCount - 1);
        TMP_CharacterInfo lastChar = tmpText.textInfo.characterInfo[tmpText.textInfo.characterCount - 1];

        float height = lastChar.baseLine;

        // 字符右侧中心点（解决不同字符错位问题）
        Vector3 charCenterRight = new Vector3(lastChar.xAdvance, height);

        // 转世界坐标
        Vector3 worldPos = textRect.TransformPoint(charCenterRight);

        // 转回UI局部坐标
        Vector3 anchoredPos = targetImage.parent.InverseTransformPoint(worldPos);

        // 应用偏移
        anchoredPos.x += offset.x;
        anchoredPos.y += offset.y;

        // 赋值给图标
        targetImage.anchoredPosition = anchoredPos;
        if (GetComponent<UISineWaveMovement>() != null)
        {
            GetComponent<UISineWaveMovement>()._originalAnchoredPos = anchoredPos;
        }
        
        Show();
    }

    public void UpdateSpritePosition()
    {

        // 强制刷新文本网格，获取最新字符位置
        _tmpText.ForceMeshUpdate();

        if (_tmpText == null || targetImage == null) return;
        if (_tmpText.textInfo.characterCount == 0) return;

        // 获取最后一个可见字符
        //Debug.Log(_tmpText.textInfo.characterCount - 1);
        TMP_CharacterInfo lastChar = _tmpText.textInfo.characterInfo[_tmpText.textInfo.characterCount - 1];

        float height = lastChar.baseLine;

        // 字符右侧中心点（解决不同字符错位问题）
        Vector3 charCenterRight = new Vector3(lastChar.xAdvance, height);

        // 转世界坐标
        Vector3 worldPos = _textRect.TransformPoint(charCenterRight);

        // 转回UI局部坐标
        Vector3 anchoredPos = targetImage.parent.InverseTransformPoint(worldPos);

        // 应用偏移
        anchoredPos.x += offset.x;
        anchoredPos.y += offset.y;

        // 赋值给图标
        targetImage.anchoredPosition = anchoredPos;
        if (GetComponent<UISineWaveMovement>() != null)
        {
            GetComponent<UISineWaveMovement>()._originalAnchoredPos = anchoredPos;
        }
        
        Show();
    }
}
