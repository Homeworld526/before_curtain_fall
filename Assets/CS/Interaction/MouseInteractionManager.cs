using UnityEngine;
using UnityEngine.EventSystems;

public class MouseInteractionManager : MonoBehaviour
{
    private AutoInteractable currentInteractable; // 当前悬停的物体

    void Update()
    {
        // 如果鼠标在UI上，不进行场景交互检测（防止穿透UI）
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // 地图正在占用输入时，跳过场景交互
        if (ShowMapButton.IsMapConsumingInput)
            return;

        // 1. 发射 2D 射线检测鼠标指向的物体
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        if (hit.collider != null)
        {
            // 尝试获取可交互组件
            AutoInteractable hitInteractable = hit.collider.GetComponent<AutoInteractable>();

            if (hitInteractable != null)
            {
                // 如果指到了一个新的物体
                if (currentInteractable != hitInteractable)
                {
                    // 如果之前有指着别的，先隐藏之前的UI
                    if (currentInteractable != null)
                        currentInteractable.OnPlayerExit();

                    // 记录新物体并显示它的UI
                    currentInteractable = hitInteractable;
                    currentInteractable.OnPlayerEnter(); 
                }
                
                if (Input.GetMouseButtonDown(0)) 
                {
                    currentInteractable.OnInteract();
                }
            }
            else
            {
                ClearSelection();
            }
        }
        else
        {
            // 没指到任何东西，隐藏UI
            ClearSelection();
        }
    }

    // 清除当前选择并隐藏UI
    private void ClearSelection()
    {
        if (currentInteractable != null)
        {
            currentInteractable.OnPlayerExit(); // 复用你原本的代码隐藏UI
            currentInteractable = null;
        }
    }
}