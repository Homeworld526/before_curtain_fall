using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// 对话预览加载前的校验结果。错误会阻止预览启动；警告仅用于提示策划。
/// </summary>
public sealed class DialogPreviewValidationResult
{
    public readonly List<string> Errors = new List<string>();
    public readonly List<string> Warnings = new List<string>();

    public bool IsValid => Errors.Count == 0;

    public string ToDisplayText(int maxDetails = 3)
    {
        if (IsValid)
        {
            return Warnings.Count == 0
                ? "校验通过，可以开始预览"
                : $"校验通过（{Warnings.Count} 条提示，详见 Console）";
        }

        var builder = new StringBuilder();
        builder.Append($"校验失败：{Errors.Count} 项");
        int count = Math.Min(maxDetails, Errors.Count);
        for (int i = 0; i < count; i++)
        {
            builder.Append('\n');
            builder.Append(Errors[i]);
        }

        if (Errors.Count > count)
        {
            builder.Append($"\n另有 {Errors.Count - count} 项，详见 Console");
        }

        return builder.ToString();
    }
}

public class DialogPreviewManager : MonoBehaviour
{
    private const int RequiredColumnCount = 18;

    private struct JumpReference
    {
        public readonly int LineNumber;
        public readonly int SourceId;
        public readonly int TargetId;

        public JumpReference(int lineNumber, int sourceId, int targetId)
        {
            LineNumber = lineNumber;
            SourceId = sourceId;
            TargetId = targetId;
        }
    }

    private enum ResourceKind
    {
        Sprite,
        Audio,
        SpriteOrVideo
    }

    private sealed class ResourceReference
    {
        public readonly string Label;
        public readonly string Path;
        public readonly ResourceKind Kind;
        public readonly HashSet<int> Lines = new HashSet<int>();

        public ResourceReference(string label, string path, ResourceKind kind, int lineNumber)
        {
            Label = label;
            Path = path;
            Kind = kind;
            Lines.Add(lineNumber);
        }
    }

    #region Windows 原生文件选择框

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct OpenFileName
    {
        public int           lStructSize;
        public System.IntPtr hwndOwner;
        public System.IntPtr hInstance;
        [MarshalAs(UnmanagedType.LPTStr)] public string lpstrFilter;
        [MarshalAs(UnmanagedType.LPTStr)] public string lpstrCustomFilter;
        public int           nMaxCustFilter;
        public int           nFilterIndex;
        public System.IntPtr lpstrFile;
        public int           nMaxFile;
        [MarshalAs(UnmanagedType.LPTStr)] public string lpstrFileTitle;
        public int           nMaxFileTitle;
        [MarshalAs(UnmanagedType.LPTStr)] public string lpstrInitialDir;
        [MarshalAs(UnmanagedType.LPTStr)] public string lpstrTitle;
        public int           Flags;
        public short         nFileOffset;
        public short         nFileExtension;
        [MarshalAs(UnmanagedType.LPTStr)] public string lpstrDefExt;
        public System.IntPtr lCustData;
        public System.IntPtr lpfnHook;
        [MarshalAs(UnmanagedType.LPTStr)] public string lpTemplateName;
    }

    [DllImport("Comdlg32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GetOpenFileName(ref OpenFileName ofn);

    // MultiByteToWideChar：直接调 Windows 编码转换，不依赖 Mono 编码体系。
    // 使用原生 UTF-16 缓冲区，避免 Mono 将 char[] 输出封送为全零数组。
    [DllImport("kernel32.dll", EntryPoint = "MultiByteToWideChar", SetLastError = true)]
    private static extern int MultiByteToWideChar(
        uint codePage, uint flags,
        byte[] lpMultiByteStr, int cbMultiByte,
        System.IntPtr lpWideCharStr, int cchWideChar);

    #endregion

    public string SelectCsvFile()
    {
        string[] files = SelectCsvFilesInternal(false);
        return files != null && files.Length > 0 ? files[0] : null;
    }

    /// <summary>
    /// 打开支持多选的 CSV 文件选择框。Windows 原生对话框在多选时返回
    /// “目录\0文件名1\0文件名2\0\0”，这里统一转换为完整路径列表。
    /// </summary>
    public string[] SelectCsvFiles()
    {
        return SelectCsvFilesInternal(true);
    }

    private string[] SelectCsvFilesInternal(bool allowMultiple)
    {
        int bufferCharacters = allowMultiple ? 32768 : 512;
        int bufferBytes = bufferCharacters * sizeof(char);
        System.IntPtr fileBuffer = Marshal.AllocHGlobal(bufferBytes);
        Marshal.Copy(new byte[bufferBytes], 0, fileBuffer, bufferBytes);

        var ofn = new OpenFileName();
        ofn.lStructSize    = Marshal.SizeOf(ofn);
        ofn.lpstrFilter    = "CSV 文件\0*.csv\0所有文件\0*.*\0";
        // 多选时缓冲区需要容纳目录和所有文件名；过小会导致原生对话框失败。
        ofn.lpstrFile      = fileBuffer;
        ofn.nMaxFile       = bufferCharacters;
        ofn.lpstrFileTitle = new string('\0', allowMultiple ? 512 : 64);
        ofn.nMaxFileTitle  = ofn.lpstrFileTitle.Length;
        ofn.lpstrTitle     = allowMultiple ? "选择要批量校验的对话 CSV 文件" : "选择对话 CSV 文件";
        // OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR
        // OFN_ALLOWMULTISELECT
        ofn.Flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008;
        if (allowMultiple)
            ofn.Flags |= 0x00000200;

        try
        {
            string savedDir = System.IO.Directory.GetCurrentDirectory();
            bool ok;
            try
            {
                ok = GetOpenFileName(ref ofn);
            }
            finally
            {
                // 即使原生调用抛异常，也不能把 Unity 的工作目录留在用户选择的目录。
                System.IO.Directory.SetCurrentDirectory(savedDir);
            }

            if (!ok)
                return null;

            string[] parts = ReadSelectedPaths(fileBuffer, bufferCharacters);
            if (parts.Length == 0)
                return null;

            if (!allowMultiple || parts.Length == 1)
                return new[] { parts[0] };

            string directory = parts[0];
            var files = new List<string>(parts.Length - 1);
            for (int i = 1; i < parts.Length; i++)
            {
                string file = parts[i];
                files.Add(System.IO.Path.IsPathRooted(file)
                    ? file
                    : System.IO.Path.Combine(directory, file));
            }

            return files.ToArray();
        }
        finally
        {
            Marshal.FreeHGlobal(fileBuffer);
        }
    }

    private static string[] ReadSelectedPaths(System.IntPtr buffer, int bufferCharacters)
    {
        var paths = new List<string>();
        int start = 0;
        for (int index = 0; index < bufferCharacters; index++)
        {
            if (Marshal.ReadInt16(buffer, index * sizeof(char)) != 0)
                continue;

            int length = index - start;
            if (length == 0)
                break;

            paths.Add(Marshal.PtrToStringUni(System.IntPtr.Add(buffer, start * sizeof(char)), length));
            start = index + 1;
        }

        return paths.ToArray();
    }

    public string LoadCsvAsUtf8(string filePath)
    {
        byte[] bytes = System.IO.File.ReadAllBytes(filePath);

        // UTF-16 BOM 必须优先处理，否则每个 ASCII 字节后面的 0 会被误判为内容。
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);

        // UTF-8 BOM
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);

        // 无 BOM：检测是否为合法 UTF-8
        if (IsValidUtf8(bytes))
            return Encoding.UTF8.GetString(bytes);

        // 非 UTF-8：用 Windows API 按 GBK(936) 解码，完全不依赖 Mono 编码库
        return Win32GbkToString(bytes);
    }

    /// <summary>
    /// 按当前 DialogTree 的运行规则预检 CSV。这里刻意不构建 DialogTree，
    /// 以避免预检阶段写入任何剧情状态。
    /// </summary>
    public DialogPreviewValidationResult ValidateCsv(string csvText)
    {
        var result = new DialogPreviewValidationResult();
        if (string.IsNullOrWhiteSpace(csvText))
        {
            result.Errors.Add("文件为空，至少需要表头和一行对话数据。");
            return result;
        }

        if (csvText.IndexOf('\0') >= 0)
        {
            result.Errors.Add("解码后的文本包含空字符；文件可能是无 BOM 的 UTF-16 或包含二进制内容。");
            return result;
        }

        string[] rows = csvText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        if (rows.Length < 2 || string.IsNullOrWhiteSpace(rows[0]))
        {
            result.Errors.Add("缺少表头或对话数据行。");
            return result;
        }

        string[] header = rows[0].Split(',');
        if (header.Length < RequiredColumnCount)
        {
            result.Errors.Add($"表头只有 {header.Length} 列；当前对话格式至少需要 {RequiredColumnCount} 列。");
            return result;
        }

        if (header.Length > RequiredColumnCount)
        {
            rows[0] = NormalizeCsvRow(rows[0]);
        }

        var inheritedCells = new string[RequiredColumnCount];
        for (int column = 0; column < inheritedCells.Length; column++)
            inheritedCells[column] = string.Empty;
        var nodeIds = new HashSet<int>();
        var jumps = new List<JumpReference>();
        var resourceReferences = new Dictionary<string, ResourceReference>();
        int expectedId = 1;
        int nodeCount = 0;

        for (int rowIndex = 1; rowIndex < rows.Length; rowIndex++)
        {
            int lineNumber = rowIndex + 1;
            string row = rows[rowIndex];
            if (string.IsNullOrWhiteSpace(row))
            {
                if (HasNonEmptyRowAfter(rows, rowIndex))
                    result.Errors.Add($"第 {lineNumber} 行为空。空行会使运行时提前停止读取，不能出现在数据中间。");
                break;
            }

            if (row.IndexOf('"') >= 0)
            {
                result.Errors.Add($"第 {lineNumber} 行包含双引号字段。当前运行时不支持带引号、逗号或换行的 CSV 字段。");
                continue;
            }

            int columnCount = CountCsvColumns(row);
            if (columnCount > RequiredColumnCount)
            {
                if (HasNonEmptyExtraColumns(row))
                {
                    result.Errors.Add($"第 {lineNumber} 行包含第 {RequiredColumnCount + 1} 列以后的非空内容；当前对话格式最多支持 {RequiredColumnCount} 列。");
                    continue;
                }

                rows[rowIndex] = NormalizeCsvRow(row);
                row = rows[rowIndex];
            }
            else if (columnCount < RequiredColumnCount)
            {
                rows[rowIndex] = NormalizeCsvRow(row);
                row = rows[rowIndex];
            }

            string[] cells = row.Split(',');

            string idText = cells[(int)DialogColumn.ID].Trim();
            // DialogTree 会在空 ID 处结束读取；允许它作为文件末尾的填充行，
            // 但不能允许其把后面的有效剧情静默截断。
            if (idText.Length == 0)
            {
                if (HasNonEmptyRowAfter(rows, rowIndex))
                    result.Errors.Add($"第 {lineNumber} 行的 ID 为空。运行时会在此停止读取，后续剧情不会进入预览。");
                break;
            }

            if (!int.TryParse(idText, out int id) || id <= 0)
            {
                result.Errors.Add($"第 {lineNumber} 行的 ID“{idText}”无效；ID 必须是从 1 开始的正整数。");
                continue;
            }

            if (!nodeIds.Add(id))
                result.Errors.Add($"第 {lineNumber} 行的 ID {id} 重复。");
            if (id != expectedId)
                result.Errors.Add($"第 {lineNumber} 行的 ID 为 {id}，应为连续编号 {expectedId}。当前运行时按 ID-1 定位跳转目标。");
            expectedId++;
            nodeCount++;

            // 与 DialogTree.ReadText 保持一致：0-6 列允许沿用上一行，7-16 列每行重置，音乐列允许沿用。
            for (int column = 0; column <= (int)DialogColumn.cg; column++)
            {
                if (!string.IsNullOrEmpty(cells[column]))
                    inheritedCells[column] = cells[column];
            }
            for (int column = (int)DialogColumn.text; column < (int)DialogColumn.music; column++)
                inheritedCells[column] = cells[column];
            if (!string.IsNullOrEmpty(cells[(int)DialogColumn.music]))
                inheritedCells[(int)DialogColumn.music] = cells[(int)DialogColumn.music];

            string type = inheritedCells[(int)DialogColumn.type].Trim();
            if (type != "#" && type != "&")
            {
                result.Errors.Add($"第 {lineNumber} 行的标志“{type}”无效；只能使用 #、& 或留空以沿用上一行标志。");
                continue;
            }

            ValidateNumberPairs(result, lineNumber, inheritedCells[(int)DialogColumn.statName], inheritedCells[(int)DialogColumn.statAmount], "影响数值", "影响量");
            ValidateNumberPairs(result, lineNumber, inheritedCells[(int)DialogColumn.needName], inheritedCells[(int)DialogColumn.needValue], "需要数值", "需求量");
            ValidateJump(result, jumps, lineNumber, id, type, inheritedCells[(int)DialogColumn.next]);
            ValidateResources(result, lineNumber, inheritedCells, resourceReferences);
        }

        if (nodeCount == 0)
            result.Errors.Add("未找到可运行的对话节点。");

        foreach (JumpReference jump in jumps)
        {
            if (!nodeIds.Contains(jump.TargetId))
                result.Errors.Add($"第 {jump.LineNumber} 行（ID {jump.SourceId}）跳转到 ID {jump.TargetId}，但目标不存在。");
        }

        ValidateResourceReferences(result, resourceReferences);

        if (result.IsValid && !HasEndNode(rows))
            result.Warnings.Add("未找到跳转值为 END 的结束节点；预览可能无法自然结束。");

        if (!result.IsValid)
        {
            Debug.LogError("[DialogPreview] CSV 校验失败：\n" + result.ToDisplayText(int.MaxValue));
        }
        else if (result.Warnings.Count > 0)
        {
            Debug.LogWarning("[DialogPreview] CSV 校验提示：\n" + string.Join("\n", result.Warnings));
        }

        return result;
    }

    public void StopPreview()
    {
        var visual = FindObjectOfType<DialogVisual>();
        if (visual != null)
            visual.StopPreview();
    }

    private static bool HasNonEmptyRowAfter(string[] rows, int rowIndex)
    {
        for (int i = rowIndex + 1; i < rows.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(rows[i]))
                return true;
        }

        return false;
    }

    private static void ValidateNumberPairs(DialogPreviewValidationResult result, int lineNumber, string names, string values, string namesColumn, string valuesColumn)
    {
        bool hasNames = !string.IsNullOrWhiteSpace(names);
        bool hasValues = !string.IsNullOrWhiteSpace(values);
        if (hasNames != hasValues)
        {
            result.Errors.Add($"第 {lineNumber} 行的“{namesColumn}”与“{valuesColumn}”必须同时填写。");
            return;
        }

        if (!hasNames)
            return;

        string[] nameParts = names.Split('+');
        string[] valueParts = values.Split('+');
        if (nameParts.Length != valueParts.Length)
        {
            result.Errors.Add($"第 {lineNumber} 行的“{namesColumn}”与“{valuesColumn}”数量不一致。");
            return;
        }

        for (int i = 0; i < valueParts.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(nameParts[i]) || !int.TryParse(valueParts[i].Trim(), out _))
                result.Errors.Add($"第 {lineNumber} 行的“{namesColumn}/{valuesColumn}”第 {i + 1} 组不是“名称 + 整数”的有效组合。");
        }
    }

    private static void ValidateJump(DialogPreviewValidationResult result, List<JumpReference> jumps, int lineNumber, int id, string type, string next)
    {
        string target = next.Trim();
        if (type == "#")
        {
            if (target == "END")
                return;

            if (target.Length == 0)
            {
                jumps.Add(new JumpReference(lineNumber, id, id + 1));
                return;
            }
        }
        else if (target.Length == 0)
        {
            result.Errors.Add($"第 {lineNumber} 行的选项（&）缺少跳转目标。");
            return;
        }

        if (!int.TryParse(target, out int targetId) || targetId <= 0)
        {
            result.Errors.Add($"第 {lineNumber} 行的跳转值“{target}”无效；普通文本可填写正整数或 END，选项必须填写正整数。");
            return;
        }

        jumps.Add(new JumpReference(lineNumber, id, targetId));
    }

    private static void ValidateResources(DialogPreviewValidationResult result, int lineNumber, string[] cells, Dictionary<string, ResourceReference> resourceReferences)
    {
        string scene = cells[(int)DialogColumn.scene].Trim();
        if (!IsEmptyResourceValue(scene))
        {
            string scenePath = scene.StartsWith("场景") || scene.StartsWith("对话背景") ? scene : "对话背景-" + scene;
            RegisterResource(resourceReferences, lineNumber, "场景", scenePath, ResourceKind.Sprite);
        }

        string portrait = cells[(int)DialogColumn.pic].Trim();
        if (!IsEmptyResourceValue(portrait))
            RegisterResource(resourceReferences, lineNumber, "头像", "Head/" + portrait, ResourceKind.Sprite);

        string opponent = cells[(int)DialogColumn.potrait].Trim();
        if (!IsEmptyResourceValue(opponent))
        {
            string[] opponents = opponent.Split('+');
            if (opponents.Length > 2 || opponents.Length == 0)
            {
                result.Errors.Add($"第 {lineNumber} 行的对手立绘“{opponent}”格式无效；最多支持两张立绘，以 + 分隔。");
            }

            for (int i = 0; i < opponents.Length; i++)
            {
                string path = opponents[i].Trim();
                if (string.IsNullOrEmpty(path))
                    result.Errors.Add($"第 {lineNumber} 行的对手立绘存在空名称。");
                else
                    RegisterResource(resourceReferences, lineNumber, "对手立绘", path, ResourceKind.Sprite);
            }
        }

        string cg = cells[(int)DialogColumn.cg].Trim();
        if (!IsEmptyResourceValue(cg) && cg != "转场")
        {
            RegisterResource(resourceReferences, lineNumber, "CG/视频", cg, ResourceKind.SpriteOrVideo);
        }

        string music = cells[(int)DialogColumn.music].Trim();
        if (!IsEmptyResourceValue(music))
            RegisterResource(resourceReferences, lineNumber, "音乐", "Music/" + music, ResourceKind.Audio);

        string sfx = cells[(int)DialogColumn.sound].Trim();
        if (!IsEmptyResourceValue(sfx))
        {
            foreach (string rawPath in sfx.Split('+'))
            {
                string path = rawPath.Trim();
                if (string.IsNullOrEmpty(path))
                    result.Errors.Add($"第 {lineNumber} 行的音效字段存在空名称。");
                else
                    RegisterResource(resourceReferences, lineNumber, "音效", "SFX/" + path, ResourceKind.Audio);
            }
        }
    }

    private static bool IsEmptyResourceValue(string value)
    {
        return string.IsNullOrEmpty(value) || value == "空";
    }

    private static void RegisterResource(Dictionary<string, ResourceReference> resourceReferences, int lineNumber, string label, string path, ResourceKind kind)
    {
        path = NormalizeResourcePath(path);
        if (string.IsNullOrEmpty(path))
            return;

        string resourceKey = kind + "|" + path;
        if (resourceReferences.TryGetValue(resourceKey, out ResourceReference reference))
        {
            reference.Lines.Add(lineNumber);
            return;
        }

        resourceReferences.Add(resourceKey, new ResourceReference(label, path, kind, lineNumber));
    }

    private static void ValidateResourceReferences(DialogPreviewValidationResult result, Dictionary<string, ResourceReference> resourceReferences)
    {
        foreach (ResourceReference reference in resourceReferences.Values)
        {
            UnityEngine.Object loaded = LoadExpectedResource(reference.Path, reference.Kind);
            string lineText = FormatLineNumbers(reference.Lines);
            if (loaded != null)
                continue;

            UnityEngine.Object actual = Resources.Load(reference.Path);
            if (actual != null)
            {
                result.Errors.Add($"{lineText} 的{reference.Label}资源“{reference.Path}”类型为 {GetResourceTypeName(actual)}，但当前字段需要 {GetExpectedTypeName(reference.Kind)}。");
                continue;
            }

            string suggestion = FindResourcePathSuggestion(reference.Path, reference.Kind);
            if (!string.IsNullOrEmpty(suggestion))
            {
                string csvValue = GetCsvValueSuggestion(reference.Label, suggestion);
                result.Errors.Add($"{lineText} 找不到{reference.Label}资源“{reference.Path}”；检测到可用资源“{suggestion}”，CSV 字段应填写“{csvValue}”。");
            }
            else
            {
                result.Errors.Add($"{lineText} 找不到{reference.Label}资源“{reference.Path}”（期望类型：{GetExpectedTypeName(reference.Kind)}）。");
            }
        }
    }

    private static UnityEngine.Object LoadExpectedResource(string path, ResourceKind kind)
    {
        switch (kind)
        {
            case ResourceKind.Sprite:
                return Resources.Load<Sprite>(path);
            case ResourceKind.Audio:
                return Resources.Load<AudioClip>(path);
            case ResourceKind.SpriteOrVideo:
                return Resources.Load<Sprite>(path) ?? (UnityEngine.Object)Resources.Load<VideoClip>(path);
            default:
                return null;
        }
    }

    private static string FindResourcePathSuggestion(string path, ResourceKind kind)
    {
        foreach (string candidate in GetResourcePathCandidates(path))
        {
            if (LoadExpectedResource(candidate, kind) != null)
                return candidate;
        }

        return null;
    }

    private static string GetCsvValueSuggestion(string label, string resourcePath)
    {
        string automaticPrefix = label == "头像" ? "Head/"
            : label == "音乐" ? "Music/"
            : label == "音效" ? "SFX/"
            : string.Empty;

        return !string.IsNullOrEmpty(automaticPrefix) && resourcePath.StartsWith(automaticPrefix, StringComparison.OrdinalIgnoreCase)
            ? resourcePath.Substring(automaticPrefix.Length)
            : resourcePath;
    }

    private static IEnumerable<string> GetResourcePathCandidates(string path)
    {
        string normalized = NormalizeResourcePath(path);
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (normalized.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase))
            candidates.Add(normalized.Substring("Resources/".Length));

        AddDuplicatePrefixCandidate(candidates, normalized, "Head/");
        AddDuplicatePrefixCandidate(candidates, normalized, "Music/");
        AddDuplicatePrefixCandidate(candidates, normalized, "SFX/");

        string extension = System.IO.Path.GetExtension(normalized);
        if (!string.IsNullOrEmpty(extension))
            candidates.Add(normalized.Substring(0, normalized.Length - extension.Length));

        if (normalized.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(extension))
        {
            string withoutRoot = normalized.Substring("Resources/".Length);
            candidates.Add(withoutRoot.Substring(0, withoutRoot.Length - extension.Length));
        }

        candidates.Remove(normalized);
        return candidates;
    }

    private static void AddDuplicatePrefixCandidate(HashSet<string> candidates, string path, string prefix)
    {
        string duplicatePrefix = prefix + prefix;
        if (path.StartsWith(duplicatePrefix, StringComparison.OrdinalIgnoreCase))
            candidates.Add(path.Substring(prefix.Length));
    }

    private static string NormalizeResourcePath(string path)
    {
        return string.IsNullOrEmpty(path)
            ? string.Empty
            : path.Trim().Replace('\\', '/').Trim('/');
    }

    private static string FormatLineNumbers(HashSet<int> lines)
    {
        var ordered = new List<int>(lines);
        ordered.Sort();
        var values = new List<string>(ordered.Count);
        for (int i = 0; i < ordered.Count; i++)
            values.Add(ordered[i].ToString());
        return ordered.Count == 1 ? $"第 {values[0]} 行" : $"第 {string.Join("、", values)} 行";
    }

    private static string GetExpectedTypeName(ResourceKind kind)
    {
        switch (kind)
        {
            case ResourceKind.Sprite:
                return "Sprite 图片";
            case ResourceKind.Audio:
                return "AudioClip 音频";
            case ResourceKind.SpriteOrVideo:
                return "Sprite 图片或 VideoClip 视频";
            default:
                return "Unity 资源";
        }
    }

    private static string GetResourceTypeName(UnityEngine.Object resource)
    {
        return resource == null ? "未知" : resource.GetType().Name;
    }

    private static bool HasEndNode(string[] rows)
    {
        for (int i = 1; i < rows.Length; i++)
        {
            string[] cells = rows[i].Split(',');
            if (cells.Length > (int)DialogColumn.next && cells[(int)DialogColumn.next].Trim() == "END")
                return true;
        }

        return false;
    }

    private static string Win32GbkToString(byte[] bytes)
    {
        const uint CP_GBK = 936;
        int len = MultiByteToWideChar(CP_GBK, 0, bytes, bytes.Length, System.IntPtr.Zero, 0);
        if (len <= 0)
            throw new InvalidDataException("文件不是有效的 UTF-8 或 GBK 编码。Windows 无法完成 GBK 解码。");

        System.IntPtr buffer = Marshal.AllocHGlobal(checked(len * sizeof(char)));
        try
        {
            int written = MultiByteToWideChar(CP_GBK, 0, bytes, bytes.Length, buffer, len);
            if (written <= 0)
                throw new InvalidDataException("文件不是有效的 GBK 编码。Windows 无法完成 GBK 解码。");
            return Marshal.PtrToStringUni(buffer, written);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static int CountCsvColumns(string row)
    {
        int columns = 1;
        for (int i = 0; i < row.Length; i++)
            if (row[i] == ',') columns++;
        return columns;
    }

    private static bool HasNonEmptyExtraColumns(string row)
    {
        int commaCount = 0;
        for (int i = 0; i < row.Length; i++)
        {
            if (row[i] != ',') continue;
            commaCount++;
            if (commaCount == RequiredColumnCount)
                return row.Substring(i + 1).Trim(',').Trim().Length > 0;
        }

        return false;
    }

    private static string NormalizeCsvRow(string row)
    {
        int columns = CountCsvColumns(row);
        if (columns == RequiredColumnCount)
            return row;

        if (columns > RequiredColumnCount)
        {
            int commaCount = 0;
            for (int i = 0; i < row.Length; i++)
            {
                if (row[i] != ',') continue;
                commaCount++;
                if (commaCount == RequiredColumnCount)
                    return row.Substring(0, i);
            }
        }

        return row + new string(',', RequiredColumnCount - columns);
    }

    private static bool IsValidUtf8(byte[] bytes)
    {
        int i = 0;
        while (i < bytes.Length)
        {
            byte b = bytes[i];
            int extra;
            if      (b < 0x80) { i++; continue; }
            else if (b < 0xC2) return false;
            else if (b < 0xE0) extra = 1;
            else if (b < 0xF0) extra = 2;
            else if (b < 0xF5) extra = 3;
            else return false;

            for (int j = 1; j <= extra; j++)
                if (i + j >= bytes.Length || (bytes[i + j] & 0xC0) != 0x80) return false;

            i += 1 + extra;
        }
        return true;
    }

    public void StartPreview(string csvText, bool isMiddle, string filePath)
    {
        DialogPreviewValidationResult validation = ValidateCsv(csvText);
        if (!validation.IsValid)
            throw new InvalidOperationException(validation.ToDisplayText());

        // 预览使用与校验相同的 18 列规范，避免运行时再次解析 Excel 导出的空列。
        csvText = NormalizeCsvText(csvText);

        var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
        var visual = FindObjectOfType<DialogVisual>();
        if (visual == null)
            throw new InvalidOperationException("场景中找不到 DialogVisual，无法启动预览。");
        visual.StartPreviewFromText(csvText, isMiddle, fileName);
    }

    private static string NormalizeCsvText(string csvText)
    {
        string[] rows = csvText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        for (int i = 0; i < rows.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(rows[i]))
                rows[i] = NormalizeCsvRow(rows[i]);
        }

        return string.Join("\n", rows);
    }
}
