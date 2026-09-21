using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TextBackGround : MonoBehaviour
{
    public TextMeshProUGUI textComponent;
    public Vector2 padding = new Vector2(10f, 10f);

    private RectTransform _rectTransform;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        UpdateBackgroundSize();
    }

    void OnDestroy()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
    }

    private void OnTextChanged(Object obj)
    {
        if (obj == textComponent)
            UpdateBackgroundSize();
    }

    void UpdateBackgroundSize()
    {
        if (textComponent == null)
        {
            Debug.Log("图片或者Text为空");
            return;
        }

        float textWidth = textComponent.preferredWidth;
        float textHeight = textComponent.preferredHeight;

        _rectTransform.sizeDelta = new Vector2(textWidth + padding.x * 2, textHeight + padding.y * 2);

        if (transform.parent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.GetComponent<RectTransform>());
    }
}
