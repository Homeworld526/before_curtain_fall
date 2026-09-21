using UnityEngine;
using UnityEngine.EventSystems;

public class MapUIController : MonoBehaviour
{
    public RectTransform mapRoot; // 地图内容
    public RectTransform viewport; // 显示区域（Mask）

    [Header("缩放")]
    public float zoomSpeed = 0.2f;
    public float minScale = 1f;
    public float maxScale = 2.5f;

    [Header("拖拽")]
    public float dragSpeed = 1f;

    private Vector2 lastMousePos;
    private bool isDragging;

    void Update()
    {
        HandleZoom();
        HandleDrag();
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll == 0) return;

        float scale = mapRoot.localScale.x;
        scale += scroll * zoomSpeed;
        scale = Mathf.Clamp(scale, minScale, maxScale);

        mapRoot.localScale = Vector3.one * scale;
        ClampPosition();
    }

    void HandleDrag()
    {
        if (EventSystem.current.IsPointerOverGameObject() == false) return;

        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            lastMousePos = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
            isDragging = false;

        if (!isDragging) return;

        Vector2 delta = (Vector2)Input.mousePosition - lastMousePos;
        mapRoot.anchoredPosition += delta * dragSpeed;

        lastMousePos = Input.mousePosition;

        ClampPosition();
    }

    void ClampPosition()
    {
        Vector2 size = mapRoot.rect.size * mapRoot.localScale.x;
        Vector2 viewSize = viewport.rect.size;

        float limitX = (size.x - viewSize.x) / 2f;
        float limitY = (size.y - viewSize.y) / 2f;

        Vector2 pos = mapRoot.anchoredPosition;
        pos.x = Mathf.Clamp(pos.x, -limitX, limitX);
        pos.y = Mathf.Clamp(pos.y, -limitY, limitY);

        mapRoot.anchoredPosition = pos;
    }
}