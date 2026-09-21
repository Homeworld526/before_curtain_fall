using DG.Tweening;
using UnityEngine;

public class UIFloat : MonoBehaviour
{
    public float distance = 0.5f; // 上下距离
    public float duration = 1f;   // 单程时间

    void Start()
    {
        transform.DOMoveY(transform.position.y + distance, duration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }
}