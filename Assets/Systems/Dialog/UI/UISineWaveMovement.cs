using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UISineWaveMovement : MonoBehaviour
{
    [Header("波动设置")]
    [Tooltip("上下移动的幅度（像素）")]
    public float amplitude = 10f;

    [Tooltip("移动速度")]
    public float speed = 2f;

    [Tooltip("是否自动开始波动")]
    public bool autoPlay = true;

    // 缓存
    private RectTransform _rect;
    public Vector2 _originalAnchoredPos;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        // 记录初始位置（保证永远不漂移）
        _originalAnchoredPos = _rect.anchoredPosition;
    }

    private void Update()
    {
        if (!autoPlay) return;
        DoWave();
    }

    /// <summary>
    /// 执行正弦波动
    /// </summary>
    public void DoWave()
    {
        // 计算正弦波动的 Y 偏移
        float offsetY = Mathf.Sin(Time.time * speed) * amplitude;

        // 赋值新位置（基于原始位置，不会漂移）
        _rect.anchoredPosition = new Vector2(
            _originalAnchoredPos.x,
            _originalAnchoredPos.y + offsetY
        );
    }

    /// <summary>
    /// 重置回原始位置
    /// </summary>
    public void ResetPosition()
    {
        _rect.anchoredPosition = _originalAnchoredPos;
    }
}
