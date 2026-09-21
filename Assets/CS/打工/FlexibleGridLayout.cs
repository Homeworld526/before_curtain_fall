using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[ExecuteInEditMode]
public class FlexibleGridLayout : MonoBehaviour
{
    [Header("基础格子设置")]
    public float cellWidth = 30;       // 格子宽度（固定）
    public float cellHeight = 65;      // 1个格子的高度
    public float spacingX = 40;        // 列与列之间的水平间距
    public float spacingY = 0;         // 格子之间的垂直间距

    [Header("排列规则")]
    public int maxRowsPerColumn = 3;   // 一列最多能放多少「格」（超过自动换列）
    public float leftPadding = 0;     // 左边距（防贴边）
    public float topPadding = 0;      // 上边距（防贴边）

    private List<RectTransform> children = new List<RectTransform>();

    void Update()
    {
        RefreshLayout();
    }

    [ContextMenu("刷新布局")]
    public void RefreshLayout()
    {
        children.Clear();
        foreach (RectTransform child in transform)
        {
            if (child.gameObject.activeSelf)
                children.Add(child);
        }

        int currentColumn = 0;
        int currentUsedRows = 0; // 当前列已经用了多少「格」高度

        foreach (var child in children)
        {
            // ==========================================
            // 关键：按「高度」判断物体占几格
            // ==========================================
            int heightCells = 1;
            if (child.sizeDelta.y >= cellHeight * 3 - 1)
                heightCells = 3; // 占3格高度
            else if (child.sizeDelta.y >= cellHeight * 2 - 1)
                heightCells = 2; // 占2格高度（你的130就是这个！）
            else
                heightCells = 1; // 占1格高度

            // ==========================================
            // 如果当前列放不下这个物体，就换列
            // ==========================================
            if (currentUsedRows + heightCells > maxRowsPerColumn)
            {
                currentColumn++;
                currentUsedRows = 0;
            }

            // ==========================================
            // 计算正确位置（不会重叠、不会超出）
            // ==========================================
            float posX = leftPadding + currentColumn * (cellWidth + spacingX);
            float posY = -topPadding - currentUsedRows * (cellHeight + spacingY);

            child.anchoredPosition = new Vector2(posX, posY);

            // 关键：当前列已占用的行数 += 这个物体占的格数
            currentUsedRows += heightCells;
        }
    }
}