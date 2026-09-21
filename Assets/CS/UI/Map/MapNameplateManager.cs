using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 管理所有地图名牌，确保同一时间只有一个名牌详情显示
/// </summary>
public class MapNameplateManager : MonoBehaviour
{
    public static MapNameplateManager Instance;

    [Header("所有名牌列表（自动收集或手动拖入）")]
    public List<MapNameplate> nameplates = new List<MapNameplate>();

    private MapNameplate currentShowing;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 自动收集子物体中的所有名牌
            CollectNameplates();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 自动收集名牌
    /// </summary>
    private void CollectNameplates()
    {
        if (nameplates.Count == 0)
        {
            MapNameplate[] found = GetComponentsInChildren<MapNameplate>(true);
            nameplates.AddRange(found);
        }
    }

    /// <summary>
    /// 注册名牌（由名牌自己调用）
    /// </summary>
    public void Register(MapNameplate nameplate)
    {
        if (!nameplates.Contains(nameplate))
        {
            nameplates.Add(nameplate);
        }
    }

    /// <summary>
    /// 通知其他名牌关闭详情
    /// </summary>
    public void NotifyShowing(MapNameplate showing)
    {
        currentShowing = showing;

        // 关闭其他名牌的详情
        foreach (var np in nameplates)
        {
            if (np != showing)
            {
                np.HideDetail();
            }
        }
    }

    /// <summary>
    /// 通知名牌详情已关闭
    /// </summary>
    public void NotifyHidden(MapNameplate hidden)
    {
        if (currentShowing == hidden)
        {
            currentShowing = null;
        }
    }

    /// <summary>
    /// 隐藏所有名牌详情
    /// </summary>
    public void HideAll()
    {
        foreach (var np in nameplates)
        {
            if (np != null)
                np.HideDetail();
        }
        currentShowing = null;
    }
}
