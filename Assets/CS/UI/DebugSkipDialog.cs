using UnityEngine;

/// <summary>
/// F2 极速跳过当前对话（每帧模拟两次点击：第一次完成文字，第二次推进下一句）
/// 挂到场景中任意物体上即可。
/// </summary>
public class DebugSkipDialog : MonoBehaviour
{
    private bool _fastForward = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            _fastForward = !_fastForward;
            Debug.Log($"[DebugSkipDialog] F2 极速快进: {(_fastForward ? "开启" : "关闭")}");
        }

        if (!_fastForward) return;

        var visual = FindObjectOfType<DialogVisual>();
        if (visual == null || visual.currentType == DialogType.End)
        {
            _fastForward = false;
            return;
        }

        // 遇到选项停住
        if (visual.currentType == DialogType.Choice)
        {
            _fastForward = false;
            return;
        }

        // 每帧调两次：第一次完成当前文字打印，第二次推进到下一句
        visual.ClickAdvance();
        visual.ClickAdvance();
    }
}
