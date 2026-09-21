using System.Collections.Generic;
using System.IO;
using UnityEngine;
using GameSystem;

public class ConfigManager : MonoBehaviour
{
    public static ConfigManager Instance { get; private set; }

    public List<JobData> normalJobs = new List<JobData>();
    public List<JobData> characterJobs = new List<JobData>();

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
        
        LoadCsv("Configs/NormalJobs", normalJobs, false);
        Debug.Log($"普通工作加载完成: {normalJobs.Count}个");
        
        LoadCsv("Configs/CharacterJobs", characterJobs, true);
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

        string[] lines = text.text.Split('\n');
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
            
            // 普通打工需要16列，角色打工需要19列（现在都有 unlockDialogKeyword 列）
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
                if (cols.Length > 15 && !string.IsNullOrEmpty(cols[15]))
                {
                    job.unlockDialogKeyword = cols[15].Trim();
                }
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
            }

            // 初始化运行时数据
            job.currentCd = 0;
            job.currentLevel = 0;
            job.performedCountThisWeek = 0;
            list.Add(job);
            
            Debug.Log($"[LoadCsv] 加载打工: {job.name}, 解锁关键词: {job.unlockDialogKeyword}");
        }
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