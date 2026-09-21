using UnityEngine;

public class PreviewDialogEventHost : DialogEventHost
{
    protected override void DialogEndEvent(string param)
    {
        Debug.Log($"[DialogPreview] 对话结束，note={param}");
    }
}
