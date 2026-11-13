using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using GameSystem;

public class ConfigManager : MonoBehaviour
{
    private const string NormalJobsResourcePath = "SchemaTable/Configs/NormalJobs";
    private const string CharacterJobsResourcePath = "SchemaTable/Configs/CharacterJobs";
    private const string WorkEventsResourcePath = "SchemaTable/Configs/WorkEvents";
    private const string CharacterWorkEventsResourcePath = "SchemaTable/Configs/CharacterWorkEvents";
    private const int WorkEventColumnCount = 12;
    private const int CharacterWorkEventColumnCount = 13;
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
    private static readonly Encoding Gb18030 = Encoding.GetEncoding(936);

    public static ConfigManager Instance { get; private set; }

    public List<JobData> normalJobs = new List<JobData>();
    public List<JobData> characterJobs = new List<JobData>();
    public Dictionary<string, WorkEventData> workEvents = new Dictionary<string, WorkEventData>();
    public Dictionary<string, WorkEventData> characterWorkEvents = new Dictionary<string, WorkEventData>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        LoadAllConfigs();
    }

    private void OnDestroy()
    {
        // 清空静态变量，避免指向已销毁的对象
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void LoadAllConfigs()
    {
        // 清空现有列表，避免重复加载
        normalJobs.Clear();
        characterJobs.Clear();
        workEvents.Clear();
        characterWorkEvents.Clear();

        LoadWorkEvents();
        Debug.Log($"打工事件加载完成: {workEvents.Count}个");

        LoadCharacterWorkEvents();
        Debug.Log($"角色打工事件加载完成: {characterWorkEvents.Count}个");
        
        LoadCsv(NormalJobsResourcePath, normalJobs, false);
        Debug.Log($"普通工作加载完成: {normalJobs.Count}个");
        
        LoadCsv(CharacterJobsResourcePath, characterJobs, true);
        Debug.Log($"角色工作加载完成: {characterJobs.Count}个");
        
        Debug.Log($"配置加载完成: 普通工作{normalJobs.Count}个, 角色工作{characterJobs.Count}个");
    }

    void LoadCsv(string resourceName, List<JobData> list, bool isCharacter)
    {
        TextAsset text = Resources.Load<TextAsset>(resourceName);
        if (text == null)
        {
            Debug.LogError($"未找到文件: {resourceName}");
            return;
        }

        string[] lines = DecodeCsvText(text).Split('\n');
        Debug.Log($"[LoadCsv] {resourceName} 共有 {lines.Length} 行");
        
        if (lines.Length < 2) 
        {
            Debug.LogWarning($"[LoadCsv] {resourceName} 行数不足，只有 {lines.Length} 行");
            return;
        }

        // 跳过表头 (i=1)
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            // 处理可能的回车符
            string line = lines[i].Replace("\r", "");
            string[] cols = line.Split(','); // 假设逗号分隔，如果是分号请修改
            
            Debug.Log($"[LoadCsv] {resourceName} 第 {i} 行: {cols.Length} 列, 内容: {line.Substring(0, Mathf.Min(50, line.Length))}...");
            
            // 角色打工第20列为可选的角色打工事件 ID 组，以兼容已有配置。
            int requiredCols = isCharacter ? 19 : 16;
            if (cols.Length < requiredCols)
            {
                Debug.LogWarning($"[LoadCsv] {resourceName} 第 {i} 行列数不足，需要 {requiredCols} 列，实际 {cols.Length} 列");
                continue;
            }
            
            JobData job = new JobData();
            int colIndex = 0;

            job.id = $"{(isCharacter ? "CHAR_" : "NORM_")}{i}";
            job.name = cols[colIndex++];
            job.apCost = int.Parse(cols[colIndex++]);

            string timeStr = cols[colIndex++];
            job.timeType = ParseTimeType(timeStr, isCharacter);

            if (!isCharacter)
            {
                job.cdVal = int.Parse(cols[colIndex++]);
                job.rewards = new int[3];
                for (int k = 0; k < 3; k++) job.rewards[k] = int.Parse(cols[colIndex++]);
                job.Backbone = int.Parse(cols[colIndex++]);
                colIndex++;
                job.imageName = cols[colIndex++];
                job.unlockWeek = 1;
                job.favorIncrement = 0;
                job.Refs = new string[3];
                for (int k = 0; k < 3; k++) job.Refs[k] = cols[colIndex++];
                job.Description = colIndex < cols.Length ? cols[colIndex++] : "";
                colIndex++; // 跳过备注列
                
                // 加载剧情解锁关键词 (第16列，索引15)
                if (cols.Length > colIndex && !string.IsNullOrEmpty(cols[colIndex]))
                {
                    job.unlockDialogKeyword = cols[colIndex].Trim();
                }
                colIndex++;

                job.workEventIds = cols.Length > colIndex
                    ? ParseWorkEventIds(cols[colIndex], resourceName, i)
                    : new string[0];
            }
            else
            {
                colIndex++;
                job.ischar = true;
                job.rewards = new int[4];
                for (int k = 0; k < 4 && colIndex < cols.Length; k++) 
                {
                    job.rewards[k] = int.Parse(cols[colIndex++]);
                }
                job.cdVal = 0;
                job.unlockWeek = colIndex < cols.Length ? int.Parse(cols[colIndex++]) : 1;
                job.lockChapter = colIndex < cols.Length ? int.Parse(cols[colIndex++]) : 1;
                job.imageName = colIndex < cols.Length ? cols[colIndex++] : "";
                job.favorIncrement = colIndex < cols.Length ? int.Parse(cols[colIndex++]) : 0;
                job.head = colIndex < cols.Length ? cols[colIndex++] : "";
                job.BG = colIndex < cols.Length ? cols[colIndex++] : "";
                job.BG_1 = colIndex < cols.Length ? cols[colIndex++] : "";
                job.BG_2 = colIndex < cols.Length ? cols[colIndex++] : "";
                job.npcName = colIndex < cols.Length ? cols[colIndex++] : "";
                job.Refs = new string[2];
                job.Refs[0] = colIndex < cols.Length ? cols[colIndex++] : "";
                job.Refs[1] = colIndex < cols.Length ? cols[colIndex++] : "";
                colIndex++; // 跳过备注列
                
                // 加载剧情解锁关键词 (第19列，索引18)
                if (cols.Length > 18 && !string.IsNullOrEmpty(cols[18]))
                {
                    job.unlockDialogKeyword = cols[18].Trim();
                }

                job.workEventIds = cols.Length > 19
                    ? ParseWorkEventIds(cols[19], characterWorkEvents, resourceName, i)
                    : new string[0];
            }

            // 初始化运行时数据
            job.currentCd = 0;
            job.currentLevel = 0;
            job.performedCountThisWeek = 0;
            list.Add(job);
            
            Debug.Log($"[LoadCsv] 加载打工: {job.name}, 解锁关键词: {job.unlockDialogKeyword}");
        }
    }

    private void LoadWorkEvents()
    {
        TextAsset text = Resources.Load<TextAsset>(WorkEventsResourcePath);
        if (text == null)
            throw new FileNotFoundException($"未找到打工事件配置: {WorkEventsResourcePath}");

        string[] lines = DecodeCsvText(text).Split('\n');
        if (lines.Length < 2)
        {
            throw new InvalidDataException($"[LoadWorkEvents] {WorkEventsResourcePath} 至少需要表头和一条事件配置。");
        }

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string[] cols = lines[i].Replace("\r", "").Split(',');
            if (cols.Length != WorkEventColumnCount)
            {
                throw new InvalidDataException(
                    $"[LoadWorkEvents] {WorkEventsResourcePath} 第 {i} 行列数错误，需要 {WorkEventColumnCount} 列，实际 {cols.Length} 列。");
            }

            WorkEventData workEvent = new WorkEventData
            {
                id = cols[0].Trim(),
                description = cols[1].Trim(),
                thirdOptionRequiredLevel = ParseThirdChoiceRequiredLevel(cols[2], WorkEventsResourcePath, i),
                option1 = CreateWorkEventOption(cols, 3, WorkEventsResourcePath, i, 1),
                option2 = CreateWorkEventOption(cols, 6, WorkEventsResourcePath, i, 2),
                option3 = CreateWorkEventOption(cols, 9, WorkEventsResourcePath, i, 3)
            };

            if (string.IsNullOrWhiteSpace(workEvent.id) || string.IsNullOrWhiteSpace(workEvent.description))
            {
                throw new InvalidDataException(
                    $"[LoadWorkEvents] {WorkEventsResourcePath} 第 {i} 行缺少事件 ID 或事件描述。");
            }

            if (workEvents.ContainsKey(workEvent.id))
            {
                throw new InvalidDataException(
                    $"[LoadWorkEvents] {WorkEventsResourcePath} 第 {i} 行的事件 ID “{workEvent.id}”重复。");
            }

            workEvents.Add(workEvent.id, workEvent);
        }
    }

    private void LoadCharacterWorkEvents()
    {
        TextAsset text = Resources.Load<TextAsset>(CharacterWorkEventsResourcePath);
        if (text == null)
            throw new FileNotFoundException($"未找到角色打工事件配置: {CharacterWorkEventsResourcePath}");

        string[] lines = DecodeCsvText(text).Split('\n');
        if (lines.Length == 0 || string.IsNullOrWhiteSpace(lines[0]))
        {
            throw new InvalidDataException(
                $"[LoadCharacterWorkEvents] {CharacterWorkEventsResourcePath} 缺少表头。");
        }

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string[] cols = lines[i].Replace("\r", "").Split(',');
            if (cols.Length != CharacterWorkEventColumnCount)
            {
                throw new InvalidDataException(
                    $"[LoadCharacterWorkEvents] {CharacterWorkEventsResourcePath} 第 {i} 行列数错误，需要 {CharacterWorkEventColumnCount} 列，实际 {cols.Length} 列。");
            }

            WorkEventData workEvent = new WorkEventData
            {
                id = cols[0].Trim(),
                description = cols[1].Trim(),
                requiredAffectionLevel = ParseNonNegativeInt(
                    cols[2], CharacterWorkEventsResourcePath, i, "事件解锁好感度要求"),
                thirdOptionRequiredAffectionLevel = ParseNonNegativeInt(
                    cols[3], CharacterWorkEventsResourcePath, i, "第三选项解锁好感度要求"),
                option1 = CreateCharacterWorkEventOption(cols, 4, CharacterWorkEventsResourcePath, i, 1),
                option2 = CreateCharacterWorkEventOption(cols, 7, CharacterWorkEventsResourcePath, i, 2),
                option3 = CreateCharacterWorkEventOption(cols, 10, CharacterWorkEventsResourcePath, i, 3)
            };

            if (string.IsNullOrWhiteSpace(workEvent.id) || string.IsNullOrWhiteSpace(workEvent.description))
            {
                throw new InvalidDataException(
                    $"[LoadCharacterWorkEvents] {CharacterWorkEventsResourcePath} 第 {i} 行缺少事件 ID 或事件描述。");
            }

            if (characterWorkEvents.ContainsKey(workEvent.id))
            {
                throw new InvalidDataException(
                    $"[LoadCharacterWorkEvents] {CharacterWorkEventsResourcePath} 第 {i} 行的事件 ID “{workEvent.id}”重复。");
            }

            characterWorkEvents.Add(workEvent.id, workEvent);
        }
    }

    private static WorkEventOption CreateWorkEventOption(
        string[] cols,
        int startIndex,
        string resourceName,
        int rowIndex,
        int optionIndex)
    {
        WorkEventOption option = new WorkEventOption
        {
            text = cols[startIndex].Trim(),
            resultDescription = cols[startIndex + 2].Trim()
        };
        int[] deltas = ParseOptionDeltas(
            cols[startIndex + 1], 2, resourceName, rowIndex, optionIndex, "钱|骨气");
        option.cashDelta = deltas[0];
        option.backboneDelta = deltas[1];

        if (string.IsNullOrWhiteSpace(option.text) || string.IsNullOrWhiteSpace(option.resultDescription))
        {
            throw new InvalidDataException(
                $"[LoadWorkEvents] {resourceName} 第 {rowIndex} 行的选项 {optionIndex} 缺少文本或结果说明。");
        }

        return option;
    }

    private static WorkEventOption CreateCharacterWorkEventOption(
        string[] cols,
        int startIndex,
        string resourceName,
        int rowIndex,
        int optionIndex)
    {
        WorkEventOption option = new WorkEventOption
        {
            text = cols[startIndex].Trim(),
            resultDescription = cols[startIndex + 2].Trim()
        };
        int[] deltas = ParseOptionDeltas(
            cols[startIndex + 1], 3, resourceName, rowIndex, optionIndex, "钱|骨气|好感度");
        option.cashDelta = deltas[0];
        option.backboneDelta = deltas[1];
        option.affectionDelta = deltas[2];

        if (string.IsNullOrWhiteSpace(option.text) || string.IsNullOrWhiteSpace(option.resultDescription))
        {
            throw new InvalidDataException(
                $"[LoadCharacterWorkEvents] {resourceName} 第 {rowIndex} 行的选项 {optionIndex} 缺少文本或结果说明。");
        }

        return option;
    }

    private static int[] ParseOptionDeltas(
        string value,
        int expectedCount,
        string resourceName,
        int rowIndex,
        int optionIndex,
        string valueFormat)
    {
        string[] values = value.Split('|');
        if (values.Length != expectedCount)
        {
            throw new InvalidDataException(
                $"[LoadCsv] {resourceName} 第 {rowIndex} 行选项 {optionIndex} 的影响数值必须为“{valueFormat}”格式，实际为“{value}”。");
        }

        int[] deltas = new int[expectedCount];
        for (int i = 0; i < values.Length; i++)
        {
            if (!int.TryParse(values[i].Trim(), out deltas[i]))
            {
                throw new InvalidDataException(
                    $"[LoadCsv] {resourceName} 第 {rowIndex} 行选项 {optionIndex} 的影响数值“{values[i]}”不是有效整数。");
            }
        }

        return deltas;
    }

    private string[] ParseWorkEventIds(string value, string resourceName, int rowIndex)
    {
        return ParseWorkEventIds(value, workEvents, resourceName, rowIndex);
    }

    private string[] ParseWorkEventIds(
        string value,
        Dictionary<string, WorkEventData> availableEvents,
        string resourceName,
        int rowIndex)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new string[0];

        string[] rawIds = value.Split(new[] { '|', ';', '、' }, System.StringSplitOptions.RemoveEmptyEntries);
        List<string> ids = new List<string>();
        HashSet<string> uniqueIds = new HashSet<string>();
        foreach (string rawId in rawIds)
        {
            string eventId = rawId.Trim();
            if (!uniqueIds.Add(eventId))
            {
                throw new InvalidDataException(
                    $"[LoadCsv] {resourceName} 第 {rowIndex} 行的打工事件 ID 组包含重复 ID “{eventId}”。");
            }

            if (!availableEvents.ContainsKey(eventId))
            {
                throw new InvalidDataException(
                    $"[LoadCsv] {resourceName} 第 {rowIndex} 行引用了不存在的打工事件 ID “{eventId}”。");
            }

            ids.Add(eventId);
        }

        return ids.ToArray();
    }

    public WorkEventData GetWorkEvent(string eventId)
    {
        if (!workEvents.TryGetValue(eventId, out WorkEventData workEvent))
            throw new KeyNotFoundException($"未找到打工事件 ID “{eventId}”。");

        return workEvent;
    }

    public WorkEventData GetCharacterWorkEvent(string eventId)
    {
        if (!characterWorkEvents.TryGetValue(eventId, out WorkEventData workEvent))
            throw new KeyNotFoundException($"未找到角色打工事件 ID “{eventId}”。");

        return workEvent;
    }

    private static string DecodeCsvText(TextAsset text)
    {
        try
        {
            return StrictUtf8.GetString(text.bytes);
        }
        catch (DecoderFallbackException)
        {
            return Gb18030.GetString(text.bytes);
        }
    }

    private int ParseThirdChoiceRequiredLevel(string value, string resourceName, int rowIndex)
    {
        return value.Trim() switch
        {
            "熟手" => 1,
            "老手" => 2,
            _ => throw new InvalidDataException(
                $"[LoadCsv] {resourceName} 第 {rowIndex} 行第三个打工选项的熟练度条件必须为“熟手”或“老手”，实际为“{value}”。")
        };
    }

    private static int ParseNonNegativeInt(string value, string resourceName, int rowIndex, string fieldName)
    {
        if (!int.TryParse(value.Trim(), out int parsedValue) || parsedValue < 0)
        {
            throw new InvalidDataException(
                $"[LoadCsv] {resourceName} 第 {rowIndex} 行“{fieldName}”必须为非负整数，实际为“{value}”。");
        }

        return parsedValue;
    }

    TimeType ParseTimeType(string str, bool isChar)
    {
        if (isChar) return TimeType.CharOnly;
        str = str.Trim();

        // 兼容中文和英文
        switch (str)
        {
            case "AllDay":
            case "全天": return TimeType.AllDay;
            case "DayOnly":
            case "白天": return TimeType.DayOnly;
            case "NightOnly":
            case "晚上": return TimeType.NightOnly;
            case "SundayOnly":
            case "周日": return TimeType.SundayOnly;
            case "MorningOnly":
            case "上午": return TimeType.MorningOnly;
            case "AfternoonOnly":
            case "下午": return TimeType.AfternoonOnly;
            default:
                Debug.LogWarning($"未知的时间类型: {str}, 默认为全天");
                return TimeType.AllDay;
        }
    }
}
