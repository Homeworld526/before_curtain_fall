using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public static class TPSheetImporter
{
    [MenuItem("Assets/TPSheet/Auto Split Sprites")]
    public static void AutoSplit()
    {
        Texture2D tex = Selection.activeObject as Texture2D;
        if (tex == null)
        {
            EditorUtility.DisplayDialog("提示", "请选中图集PNG", "OK");
            return;
        }

        string texPath = AssetDatabase.GetAssetPath(tex);
        string tpsPath = Path.ChangeExtension(texPath, ".tpsheet");
        if (!File.Exists(tpsPath))
        {
            EditorUtility.DisplayDialog("错误", "找不到同名 .tpsheet", "OK");
            return;
        }

        // 先获取贴图真实尺寸
        TextureImporter tempImp = AssetImporter.GetAtPath(texPath) as TextureImporter;
        int atlasW = tempImp.maxTextureSize;
        int atlasH = tempImp.maxTextureSize;

        List<SpriteMetaData> spriteList = new List<SpriteMetaData>();
        string[] lines = File.ReadAllLines(tpsPath);

        foreach (string line in lines)
        {
            string s = line.Trim();
            if (string.IsNullOrEmpty(s) || s.StartsWith("#") || s.StartsWith(":"))
                continue;

            string[] parts = s.Split(';');
            if (parts.Length < 5) continue;

            string name = parts[0].Trim();
            int x = int.Parse(parts[1].Trim());
            int y = int.Parse(parts[2].Trim());
            int w = int.Parse(parts[3].Trim());
            int h = int.Parse(parts[4].Trim());

            // 终极修正：完全不翻转Y轴，直接使用原始坐标
            // 之前的公式是 int unityY = atlasH - y - h;
            // 现在改成：
            int unityY = y;

            SpriteMetaData md = new SpriteMetaData
            {
                name = name,
                rect = new Rect(x, unityY, w, h),
                pivot = new Vector2(0.5f, 0.5f),
                alignment = 9
            };
            spriteList.Add(md);
        }

        tempImp.textureType = TextureImporterType.Sprite;
        tempImp.spriteImportMode = SpriteImportMode.Multiple;
        tempImp.spritesheet = spriteList.ToArray();
        tempImp.SaveAndReimport();

        EditorUtility.DisplayDialog("完成", $"成功切分 {spriteList.Count} 个精灵，已尝试终极修正", "OK");
    }
}