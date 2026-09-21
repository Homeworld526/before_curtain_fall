using UnityEngine;

public class UIRotate : MonoBehaviour
{
    [Header("旋转速度 (度/秒)")]
    public float speed = 90f;

    [Header("旋转轴")]
    public Vector3 axis = Vector3.forward;

    [Header("设置")]
    public bool clockwise = true;
    public bool unscaledTime = false;

    void Update()
    {
        float dt = unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float dir = clockwise ? -1f : 1f;
        transform.Rotate(axis, speed * dir * dt, Space.Self);
    }
}
