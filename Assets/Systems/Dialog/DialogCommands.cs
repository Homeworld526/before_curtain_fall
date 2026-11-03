using System.Globalization;

/// <summary>
/// 剧情 CSV“指令”列中可由对话系统识别的通用指令。
/// 指令以 + 分隔，运行时按原始文本精确匹配，因此不要在指令两侧添加空格。
/// </summary>
public static class DialogCommands
{
    public const string MaskOn = "蒙版渐入";
    public const string MaskOff = "蒙版渐出";
    public const string AutoPlay = "自动播放";
    public const string ScreenWipeTransition = "刷屏转场";
    private const string MaskPrefix = "蒙版";
    private const string AutoPlayPrefix = "自动播放";

    public const string AuthoringGuide =
        "填写位置：剧情 CSV 的第 11 列“指令”和第 12 列“指令参数”。指令用 + 连接，参数用 | 连接，二者按位置对应。\n" +
        "不需要参数的指令保留空位，例如：蒙版渐入+自动播放 对应 |1。\n\n" +
        "蒙版渐入\n" +
        "在 0.5 秒内对场景和角色立绘渐入现有色卡的正片叠底效果；文本框和其后的 UI 不受影响。\n\n" +
        "蒙版渐出\n" +
        "在 0.5 秒内渐出并关闭蒙版色卡效果。\n\n" +
        "自动播放\n" +
        "进入本行时屏蔽点击；参数填写等待秒数，未填写时默认等待 1 秒。\n\n" +
        "头像框抖动/头像抖动、正立绘抖动、左立绘抖动、右立绘抖动\n" +
        "参数填写“持续秒数,强度系数”，例如 1.5,2 表示抖动 1.5 秒且强度为基准的 2 倍。时长未填写时默认 0.5 秒；强度未填写时立绘默认为 1、头像默认为 0.5。\n\n" +
        "刷屏转场\n" +
        "场景切换默认使用无刷屏转场；仅本行指令包含该指令时使用刷屏转场。\n\n" +
        "示例\n" +
        "第 20 行指令：蒙版渐入\n" +
        "第 24 行指令：蒙版渐出\n" +
        "第 28 行指令：自动播放；指令参数：1\n" +
        "第 30 行指令：头像抖动；指令参数：1.5,0.75\n" +
        "第 32 行指令：刷屏转场\n" +
        "组合示例：指令填蒙版渐入+自动播放，指令参数填|1";

    public static bool IsMaskCommand(string token)
    {
        return token == MaskOn || token == MaskOff;
    }

    public static bool LooksLikeMaskCommand(string token)
    {
        return token.Contains(MaskPrefix);
    }

    public static bool HasAutoPlayCommand(string command)
    {
        string[] tokens = command.Split('+');
        for (int i = 0; i < tokens.Length; i++)
        {
            if (tokens[i] == AutoPlay)
                return true;
        }

        return false;
    }

    public static bool TryGetCommandParameter(DialogNode dialog, int commandIndex, out string parameter)
    {
        if (commandIndex >= dialog.commandParameters.Length || dialog.commandParameters[commandIndex].Length == 0)
        {
            parameter = string.Empty;
            return false;
        }

        parameter = dialog.commandParameters[commandIndex];
        return true;
    }

    public static bool TryGetCommandParameter(DialogNode dialog, string targetCommand, out string parameter)
    {
        string[] commands = dialog.command.Split('+');
        for (int i = 0; i < commands.Length; i++)
        {
            if (commands[i] == targetCommand)
                return TryGetCommandParameter(dialog, i, out parameter);
        }

        parameter = string.Empty;
        return false;
    }

    public static bool TryParseNonNegativeFloat(string parameter, out float value)
    {
        return float.TryParse(parameter, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value >= 0f;
    }

    public static bool TryParseShakeParameter(
        string parameter,
        float defaultDuration,
        float defaultIntensityScale,
        out float duration,
        out float intensityScale)
    {
        string[] values = parameter.Split(',');
        if (values.Length > 2)
        {
            duration = 0f;
            intensityScale = 0f;
            return false;
        }

        duration = defaultDuration;
        intensityScale = defaultIntensityScale;
        if (values[0].Length > 0 && !TryParseNonNegativeFloat(values[0], out duration))
            return false;

        return values.Length == 1
            || values[1].Length == 0
            || TryParseNonNegativeFloat(values[1], out intensityScale);
    }

    public static bool LooksLikeAutoPlayCommand(string token)
    {
        return token.Contains(AutoPlayPrefix);
    }
}

