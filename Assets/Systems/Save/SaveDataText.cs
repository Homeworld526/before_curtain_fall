using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public static class IntegerToChineseConverter
{
    // 数字对应的汉字映射
    private static readonly string[] _digitChars = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
    // 中文数字单位（按四位分级：个、十、百、千 | 万、十、百、千 | 亿、十、百、千）
    private static readonly string[] _unitChars = { "", "十", "百", "千", "万", "十", "百", "千", "亿" };

    /// <summary>
    /// 将int整数转换为中文汉字
    /// </summary>
    /// <param name="number">整数</param>
    /// <returns>中文数字字符串</returns>
    public static string ConvertToChinese(int number)
    {
        // 处理零值
        if (number == 0)
            return _digitChars[0];

        // 处理负数：int.MinValue绝对值超出int范围，统一转为long处理
        long num = number;
        StringBuilder result = new StringBuilder();
        
        // 追加负号
        if (num < 0)
        {
            result.Append("负");
            num = -num;
        }

        // 转换非负数字
        result.Append(ConvertNonNegativeToChinese(num));
        return result.ToString();
    }

    /// <summary>
    /// 非负长整数转换为中文（核心逻辑）
    /// </summary>
    private static string ConvertNonNegativeToChinese(long num)
    {
        StringBuilder sb = new StringBuilder();
        // 数字位数计数器
        int digitIndex = 0;
        // 标记上一位是否为零（用于合并连续零）
        bool lastIsZero = true;

        // 从低位向高位拆解数字
        while (num > 0)
        {
            // 取最后一位数字
            int digit = (int)(num % 10);
            num /= 10;

            if (digit == 0)
            {
                // 当前位是0：仅当上一位非零时，追加一个零（合并连续零）
                if (!lastIsZero)
                {
                    sb.Insert(0, _digitChars[0]);
                    lastIsZero = true;
                }
            }
            else
            {
                // 当前位非零：拼接 数字 + 单位
                string current = $"{_digitChars[digit]}{_unitChars[digitIndex]}";
                sb.Insert(0, current);
                lastIsZero = false;
            }

            digitIndex++;
        }

        string chinese = sb.ToString();
        // 特殊处理：10→十、15→十五（去掉开头的"一"）
        if (chinese.StartsWith("一十"))
        {
            chinese = chinese[1..];
        }

        return chinese;
    }
}




public class SaveDataText: MonoBehaviour
{
    public TextMeshProUGUI weekText;
    public TextMeshProUGUI timeText;

    public static readonly Dictionary<int, string> DialogLabels = new Dictionary<int, string>
    {
        // 在此按 dialogIndex 填入对应显示文本
         { 0, "序章" },
         { 1, "序章" },
         { 2, "夏伦·其二" },
         { 9, "张海心·其一" },
         { 10, "陈玉澍·其一" },
         { 11, "夏伦·其一" },
    };

    public void Distribute(int week, string time, int dialogIndex = -1)
    {
        if (dialogIndex >= 0 && DialogLabels.TryGetValue(dialogIndex, out string label))
            weekText.text = label;
        else
            weekText.text = "第" + IntegerToChineseConverter.ConvertToChinese(week) + "周";

        timeText.text = time;
    }
}
