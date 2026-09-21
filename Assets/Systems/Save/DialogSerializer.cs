using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class RectTransformData
{
    public string hierarchyPath;
    public float[] anchoredPosition;
    public float[] sizeDelta;
    public float[] anchorMin;
    public float[] anchorMax;
    public float[] pivot;
    public float[] localScale;
    public float[] localRotation;
    public float canvasGroupAlpha;
    public bool hasCanvasGroup;
    public float imageAlpha;
    public bool hasImage;

    public RectTransformData() { }

    public RectTransformData(RectTransform rt)
    {
        hierarchyPath = GetHierarchyPath(rt);
        anchoredPosition = new[] { rt.anchoredPosition.x, rt.anchoredPosition.y };
        sizeDelta = new[] { rt.sizeDelta.x, rt.sizeDelta.y };
        anchorMin = new[] { rt.anchorMin.x, rt.anchorMin.y };
        anchorMax = new[] { rt.anchorMax.x, rt.anchorMax.y };
        pivot = new[] { rt.pivot.x, rt.pivot.y };
        localScale = new[] { rt.localScale.x, rt.localScale.y, rt.localScale.z };
        localRotation = new[] { rt.localEulerAngles.x, rt.localEulerAngles.y, rt.localEulerAngles.z };

        var cg = rt.GetComponent<CanvasGroup>();
        hasCanvasGroup = cg != null;
        canvasGroupAlpha = hasCanvasGroup ? cg.alpha : 1f;

        var image = rt.GetComponent<Image>();
        hasImage = image != null;
        imageAlpha = hasImage ? image.color.a : 1f;
    }

    public void Apply(RectTransform rt)
    {
        rt.anchoredPosition = new Vector2(anchoredPosition[0], anchoredPosition[1]);
        rt.sizeDelta = new Vector2(sizeDelta[0], sizeDelta[1]);
        rt.anchorMin = new Vector2(anchorMin[0], anchorMin[1]);
        rt.anchorMax = new Vector2(anchorMax[0], anchorMax[1]);
        rt.pivot = new Vector2(pivot[0], pivot[1]);
        rt.localScale = new Vector3(localScale[0], localScale[1], localScale[2]);
        rt.localEulerAngles = new Vector3(localRotation[0], localRotation[1], localRotation[2]);

        if (hasCanvasGroup)
        {
            var cg = rt.GetComponent<CanvasGroup>();
            if (cg != null)
                cg.alpha = canvasGroupAlpha;
        }

        if (hasImage)
        {
            var image = rt.GetComponent<Image>();
            if (image != null)
            {
                var c = image.color;
                c.a = imageAlpha;
                image.color = c;
            }
        }
    }

    private static string GetHierarchyPath(Transform t)
    {
        var parts = new List<string>();
        var current = t;
        while (current != null)
        {
            parts.Add(current.name);
            current = current.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }
}

[System.Serializable]
public class DialogSceneData
{
    public List<RectTransformData> entries = new List<RectTransformData>();
    public int visualLocatorState;
    public bool enableFill;
    public bool endingActive;
    public string musicPath;
    public bool musicPlaying;
}

public static class DialogSerializer
{
    public static void SaveRuntimeScene(string savePath)
    {
        var recorded = new HashSet<GameObject>();
        var sceneData = new DialogSceneData();

        foreach (var ps in Object.FindObjectsOfType<PicSwaper>(true))
        {
            var rt = ps.GetComponent<RectTransform>();
            if (rt == null) continue;
            if (ps.GetComponent<CanvasGroup>() != null) continue;
            recorded.Add(ps.gameObject);
            sceneData.entries.Add(new RectTransformData(rt));
        }

        foreach (var cg in Object.FindObjectsOfType<CanvasGroup>(true))
        {
            if (recorded.Contains(cg.gameObject)) continue;
            var rt = cg.GetComponent<RectTransform>();
            if (rt == null) continue;
            if (cg.GetComponent<Image>() != null) continue;
            recorded.Add(cg.gameObject);
            sceneData.entries.Add(new RectTransformData(rt));
        }

        if (VisualLocator.Instance != null)
            sceneData.visualLocatorState = (int)VisualLocator.Instance.currentState;

        if (DialogVisual.Instance != null)
            sceneData.enableFill = DialogVisual.Instance.enableFill;

        var ending = Object.FindObjectOfType<Ending>(true);
        if (ending != null)
            sceneData.endingActive = ending.ending.gameObject.activeSelf;

        if (SoundsManager.Instance != null)
        {
            sceneData.musicPath = !string.IsNullOrEmpty(SoundsManager.Instance.CurrentMusicPath)
                ? SoundsManager.Instance.CurrentMusicPath
                : (!string.IsNullOrEmpty(SoundsManager.Instance.CurrentMusicClipName)
                    ? "Music/" + SoundsManager.Instance.CurrentMusicClipName
                    : "");
            sceneData.musicPlaying = SoundsManager.Instance.IsMusicPlaying;
        }

        string json = JsonUtility.ToJson(sceneData, true);
        File.WriteAllText(savePath, json);
        
    }

    public static void LoadRuntimeScene(string loadPath)
    {
        if (!File.Exists(loadPath))
        {
            Debug.LogWarning($"[DialogSerializer] 存档文件不存在: {loadPath}");
            return;
        }
        //DebugPrintSave(loadPath);
        string json = File.ReadAllText(loadPath);
        var sceneData = JsonUtility.FromJson<DialogSceneData>(json);
        if (sceneData == null || sceneData.entries == null) return;

        foreach (var ps in Object.FindObjectsOfType<PicSwaper>(true))
            ps.StopAllCoroutines();

        var pathMap = new Dictionary<string, RectTransform>();
        foreach (var rt in Object.FindObjectsOfType<RectTransform>(true))
            pathMap[GetHierarchyPath(rt)] = rt;

        foreach (var entry in sceneData.entries)
        {
            if (!pathMap.TryGetValue(entry.hierarchyPath, out var rt)) continue;
            entry.Apply(rt);
        }

        if (VisualLocator.Instance != null)
            VisualLocator.Instance.LoadState((VisualLocator.VisualState)sceneData.visualLocatorState);

        if (DialogVisual.Instance != null)
            DialogVisual.Instance.enableFill = sceneData.enableFill;

        var ending = Object.FindObjectOfType<Ending>(true);
        if (ending != null)
        {
            //Debug.Log("ending状态"+ ending.ending.gameObject.activeInHierarchy + " "+sceneData.endingActive);
            if (sceneData.endingActive)
            {
                ending.StartEffect("开始");
            }
            else if (!sceneData.endingActive)
            {
                ending.StartEffect("结束");
            }
        }

        if (SoundsManager.Instance != null && !string.IsNullOrEmpty(sceneData.musicPath))
        {
            if (sceneData.musicPlaying)
                SoundsManager.Instance.PlayMusic(sceneData.musicPath);
            else
            {
                var clip = Resources.Load<AudioClip>(sceneData.musicPath);
                if (clip != null) SoundsManager.Instance.SetMusicFile(clip);
                SoundsManager.Instance.PauseMusic();
            }
        }
            
    }
    

    private static string GetHierarchyPath(Transform t)
    {
        var parts = new List<string>();
        var current = t;
        while (current != null)
        {
            parts.Add(current.name);
            current = current.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }
}
