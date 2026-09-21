using UnityEngine;
using TMPro;
using System.IO;
using System.Linq;

public class TextAssetFontUpdater : MonoBehaviour
{
    [Header("配置")]
    public TMP_FontAsset targetFont; // 目标动态字体资产
    public TextAsset characterTextAsset; // 包含新字符的TextAsset
    public bool autoRunOnStart = true; // 启动时自动执行
    public bool logDebugInfo = true; // 输出调试信息

    private void Start()
    {
        if (autoRunOnStart)
        {
            AddCharactersFromTextAsset();
        }
    }

    /// <summary>
    /// 从TextAsset读取字符并增量添加到字体（保留现有字符）
    /// </summary>
    public void AddCharactersFromTextAsset()
    {
        // 校验参数
        if (targetFont == null || characterTextAsset == null)
        {
            Debug.LogError("目标字体或TextAsset未赋值！");
            return;
        }



        // 读取TextAsset内容并去重（避免重复添加）
        string textContent = characterTextAsset.text;
        string uniqueCharacters = new string(textContent.Distinct().ToArray());

        if (logDebugInfo)
        {
            Debug.Log($"从TextAsset读取到 {textContent.Length} 个字符，去重后 {uniqueCharacters.Length} 个");
        }

        // 增量添加字符（仅添加缺失的）
        bool hasNewCharactersAdded = targetFont.TryAddCharacters(uniqueCharacters);

        if (logDebugInfo)
        {
            if (hasNewCharactersAdded)
            {
                Debug.Log("成功添加新字符到动态字体！");
                // 可选：输出新增字符
                foreach (char c in uniqueCharacters)
                {
                    if (!targetFont.characterLookupTable.ContainsKey(c))
                    {
                        Debug.Log($"字符 '{c}' (Unicode: {(int)c}) 添加失败");
                    }
                }
            }
            else
            {
                Debug.Log("所有字符已存在于字体中，无需添加");
            }

            Debug.Log($"当前字体字符总数：{targetFont.characterLookupTable.Count}");
        }
    }

    /// <summary>
    /// 手动触发添加（可通过按钮调用）
    /// </summary>
    [ContextMenu("手动添加字符")]
    public void ManualAddCharacters()
    {
        AddCharactersFromTextAsset();
    }
}
