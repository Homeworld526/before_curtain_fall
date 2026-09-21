using UnityEngine;
using UnityEngine.EventSystems;

public class MouseHoverInteract : MonoBehaviour
{
    private AutoInteractable currentInteractable; // 当前悬停的物体

    // 公开这个属性，让相机脚本可以读取，防止拖拽冲突
    public bool IsHoveringInteractable => currentInteractable != null;

    void Update()
    {
        // 1. 如果鼠标在 UI 上（例如对话框），不进行射线检测
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // 地图正在占用输入时，跳过场景交互
        if (ShowMapButton.IsMapConsumingInput)
            return;

        // 2. 发射射线检测
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        if (hit.collider != null)
        {
            // 尝试获取物体上的交互组件
            AutoInteractable hitInteractable = hit.collider.GetComponent<AutoInteractable>();

            if (hitInteractable != null)
            {
                // === 悬停逻辑 ===
                // 如果指到了一个新的物体
                if (currentInteractable != hitInteractable)
                {
                    // 离开旧的
                    if (currentInteractable != null)
                        currentInteractable.OnPlayerExit();

                    // 进入新的
                    currentInteractable = hitInteractable;
                    currentInteractable.OnPlayerEnter(); // 这里会调用 InteractionUIManager 显示 UI
                }

                // === 点击逻辑 ===
                if (Input.GetMouseButtonDown(0)) // 鼠标左键点击
                {
                    Debug.Log($"点击了: {hit.collider.name}");
                    currentInteractable.OnInteract();
                }
            }
            else
            {
                // 指到了有碰撞体但不可交互的东西（比如墙壁）
                ClearSelection();
            }
        }
        else
        {
            // 鼠标指在空地上
            ClearSelection();
        }
    }

    // 清理状态：隐藏 UI 并清空记录
    private void ClearSelection()
    {
        if (currentInteractable != null)
        {
            currentInteractable.OnPlayerExit(); // 隐藏 UI
            currentInteractable = null;
        }
    }
}