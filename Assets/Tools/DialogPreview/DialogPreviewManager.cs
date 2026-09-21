using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public class DialogPreviewManager : MonoBehaviour
{
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
        [MarshalAs(UnmanagedType.LPTStr)] public string lpstrFile;
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

    // MultiByteToWideChar：直接调 Windows 编码转换，不依赖 Mono 编码体系
    [DllImport("kernel32.dll")]
    private static extern int MultiByteToWideChar(
        uint codePage, uint flags,
        byte[] lpMultiByteStr, int cbMultiByte,
        [Out] char[] lpWideCharStr, int cchWideChar);

    #endregion

    public string SelectCsvFile()
    {
        var ofn = new OpenFileName();
        ofn.lStructSize    = Marshal.SizeOf(ofn);
        ofn.lpstrFilter    = "CSV 文件\0*.csv\0所有文件\0*.*\0";
        ofn.lpstrFile      = new string('\0', 512);
        ofn.nMaxFile       = ofn.lpstrFile.Length;
        ofn.lpstrFileTitle = new string('\0', 64);
        ofn.nMaxFileTitle  = ofn.lpstrFileTitle.Length;
        ofn.lpstrTitle     = "选择对话 CSV 文件";
        // OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR
        ofn.Flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008;

        string savedDir = System.IO.Directory.GetCurrentDirectory();
        bool ok = GetOpenFileName(ref ofn);
        System.IO.Directory.SetCurrentDirectory(savedDir);

        return ok ? ofn.lpstrFile.TrimEnd('\0') : null;
    }

    public string LoadCsvAsUtf8(string filePath)
    {
        byte[] bytes = System.IO.File.ReadAllBytes(filePath);

        // UTF-8 BOM
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);

        // 无 BOM：检测是否为合法 UTF-8
        if (IsValidUtf8(bytes))
            return Encoding.UTF8.GetString(bytes);

        // 非 UTF-8：用 Windows API 按 GBK(936) 解码，完全不依赖 Mono 编码库
        return Win32GbkToString(bytes);
    }

    private static string Win32GbkToString(byte[] bytes)
    {
        const uint CP_GBK = 936;
        int len = MultiByteToWideChar(CP_GBK, 0, bytes, bytes.Length, null, 0);
        if (len <= 0) return Encoding.UTF8.GetString(bytes);
        char[] wide = new char[len];
        MultiByteToWideChar(CP_GBK, 0, bytes, bytes.Length, wide, len);
        return new string(wide);
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
        var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
        var visual = FindObjectOfType<DialogVisual>();
        visual.StartPreviewFromText(csvText, isMiddle, fileName);
    }
}
