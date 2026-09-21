using UnityEngine;

/// <summary>
/// 已弃用：教学现在由 PrologueUIManager.CompletePrologue() 自动触发。
/// 保留脚本避免场景引用报错，可安全删除。
/// </summary>
public class TutorialAutoTrigger : MonoBehaviour
{
    private void Start()
    {
        // 不再自动触发，可从场景中移除此组件
        Destroy(this);
    }
}
