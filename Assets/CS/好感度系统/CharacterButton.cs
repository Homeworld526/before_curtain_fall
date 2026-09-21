using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// 角色按钮组件
/// 简单的UI点击处理
/// </summary>
[RequireComponent(typeof(Image))]
public class CharacterButton : MonoBehaviour, IPointerClickHandler
{
    [Header("事件")]
    [Tooltip("按钮点击回调")]
    public UnityEvent OnClick;

    /// <summary>
    /// 简化的点击回调（用于代码绑定）
    /// </summary>
    public System.Action OnButtonClicked;

    /// <summary>
    /// 实现IPointerClickHandler接口，处理UI点击
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[CharacterButton] 点击角色按钮: {gameObject.name}");

        // 触发 UnityEvent
        OnClick?.Invoke();

        // 触发简化回调
        OnButtonClicked?.Invoke();
    }
}
