using UnityEditor;
using UnityEngine;

public class JobDataEditor : EditorWindow
{
    [MenuItem("Tools/打工配置编辑器")]
    public static void ShowWindow()
    {
        GetWindow<JobDataEditor>("打工配置编辑器");
    }

    private void OnGUI()
    {
        GUILayout.Label("打工配置说明", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("普通打工 (NormalJobs.csv)：");
        GUILayout.Label("第16列：备注");
        GUILayout.Label("第17列：unlockDialogKeyword (剧情解锁关键词)");
        GUILayout.Space(10);
        
        GUILayout.Label("角色打工 (CharacterJobs.csv)：");
        GUILayout.Label("第18列：备注");
        GUILayout.Label("第21列：unlockDialogKeyword (剧情解锁关键词)");
        GUILayout.Space(20);
        
        if (GUILayout.Button("打开 NormalJobs.csv"))
        {
            string path = "Assets/Resources/Configs/NormalJobs.csv";
            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(path, 1);
        }
        
        if (GUILayout.Button("打开 CharacterJobs.csv"))
        {
            string path = "Assets/Resources/Configs/CharacterJobs.csv";
            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(path, 1);
        }
        
        GUILayout.Space(20);
        
        if (GUILayout.Button("重置所有剧情解锁状态"))
        {
            PlayerPrefs.DeleteAll();
            Debug.Log("已重置所有 PlayerPrefs！");
            EditorUtility.DisplayDialog("提示", "已重置所有剧情解锁状态！", "确定");
        }
    }
}
