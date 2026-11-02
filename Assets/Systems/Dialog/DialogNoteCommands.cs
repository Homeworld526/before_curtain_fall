/// <summary>
/// 剧情 CSV“备注”列中可由对话系统识别的通用指令。
/// 指令以 + 分隔，运行时按原始文本精确匹配，因此不要在指令两侧添加空格。
/// </summary>
public static class DialogNoteCommands
{
    public const string MaskOn = "蒙版渐入";
    public const string MaskOff = "蒙版渐出";
    public const string AutoPlay = "自动播放";
    public const string ScreenWipeTransition = "刷屏转场";
    private const string MaskPrefix = "蒙版";
    private const string AutoPlayPrefix = "自动播放";

    public const string AuthoringGuide =
        "填写位置：剧情 CSV 的第 11 列“备注”。多个指令使用 + 连接，指令两侧不能有空格。\n\n" +
        "蒙版渐入\n" +
        "在 0.5 秒内对场景和角色立绘渐入现有色卡的正片叠底效果；文本框和其后的 UI 不受影响。\n\n" +
        "蒙版渐出\n" +
        "在 0.5 秒内渐出并关闭蒙版色卡效果。\n\n" +
        "自动播放\n" +
        "进入本行时屏蔽点击；等待 DialogVisual 的 Auto Play Delay Seconds 后自动进入下一行。默认等待 1 秒。\n\n" +
        "刷屏转场\n" +
        "场景切换默认使用无刷屏转场；仅本行备注包含该指令时使用刷屏转场。\n\n" +
        "示例\n" +
        "第 20 行备注：蒙版渐入\n" +
        "第 24 行备注：蒙版渐出\n" +
        "第 28 行备注：自动播放\n" +
        "第 32 行备注：刷屏转场\n" +
        "可与既有指令组合，例如：蒙版渐入+自动播放";

    public static bool IsMaskCommand(string token)
    {
        return token == MaskOn || token == MaskOff;
    }

    public static bool LooksLikeMaskCommand(string token)
    {
        return token.Contains(MaskPrefix);
    }

    public static bool HasAutoPlayCommand(string note)
    {
        string[] tokens = note.Split('+');
        for (int i = 0; i < tokens.Length; i++)
        {
            if (tokens[i] == AutoPlay)
                return true;
        }

        return false;
    }

    public static bool LooksLikeAutoPlayCommand(string token)
    {
        return token.Contains(AutoPlayPrefix);
    }
}
