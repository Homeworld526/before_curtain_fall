using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ReadDialogHistorySO))]
public class ReadDialogHistorySOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        if (GUILayout.Button("清空已读对话记录"))
        {
            ReadDialogHistorySO history = (ReadDialogHistorySO)target;
            history.ClearHistory();
            EditorUtility.SetDirty(history);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("提示", "已清空已读对话记录。", "确定");
        }
    }
}
