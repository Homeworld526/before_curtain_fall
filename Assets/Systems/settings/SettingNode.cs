using UnityEngine.UI;

public class SettingNode : BaseNode
{
    /// <summary>
    /// 只在内存中分配一次
    /// </summary>
    private static Text text;
    public override void Start()
    {
        
        base.Start();
        GetComponent<Button>()?.onClick.AddListener(() =>
        {
            SwitchNode();
        });
    }
}