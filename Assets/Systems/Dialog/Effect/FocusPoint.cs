using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FocusPoint : MonoBehaviour
{
    public List<string> fileName;
    public List<Focus> focusPoints;
    public Focus focusInstance;

    public bool hasFocus(string name)
    {
        
        if (fileName.Contains(name))
        {
            return true;
        }
        return false;
    }

    public void loadFocus(string name)
    {
       
        if (!fileName.Contains(name))
        {
            Debug.LogWarning("未找到焦点数据");
            return;
        }
        else
        {
            Debug.LogWarning(focusPoints[fileName.FindIndex(x => x == name)]);
            focusInstance = focusPoints[fileName.FindIndex(x => x == name)];
        }
    }

    public Vector2 GetFocus(int num)
    {
        var visual = DialogVisual.Instance;
        string name = visual.dialogFile != null ? visual.dialogFile.name : visual.previewFileName;
        loadFocus(name);
        if (focusInstance == null) { return focusPoints[0].point[0]; }
        return focusInstance.point[num];
    }
}
