using UnityEngine;

/// <summary>
/// 挂在详情面板预制体上，拖入箭头即可保持其相对位置不变。
/// </summary>
public class FixedRelativePosition : MonoBehaviour
{
    [Tooltip("需要固定相对位置的对象（如箭头）")]
    public Transform target;

    private Vector3 savedLocalPos;

    private void Awake()
    {
        if (target != null)
            savedLocalPos = target.localPosition;
    }

    private void LateUpdate()
    {
        if (target != null)
            target.localPosition = savedLocalPos;
    }
}
