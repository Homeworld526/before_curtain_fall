using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Security.Cryptography;

public enum DialogColumn
{
    type = 0,
    ID = 1,
    scene = 2,
    name = 3,
    pic = 4,
    potrait = 5,
    cg = 6,
    text = 7,
    next = 8,
    sound = 9,
    command = 10,
    commandParameter = 11,
    statName = 12,
    statAmount = 13,
    eventName = 14,
    needName = 15,
    needValue = 16,
    music = 18,
    requirementBehavior = 19,
    lockedNext = 20,
}

public class DialogTree
{

    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
    private static readonly Encoding Gbk = Encoding.GetEncoding(936);
    private const int ColumnCount = (int)DialogColumn.lockedNext + 1;

    private DialogNode lastDia;
    private static string emptyChar = "空";
    List<DialogNode> tree = new List<DialogNode>();
    string[] dialogRows;
    int currentIndex = -1;

    //DialogNode currentNode;

    public void BuildTree(TextAsset Sheet)
    {
        tree.Clear();
        lastDia = null;
        currentIndex = -1;
        ReadText(DecodeTextAsset(Sheet));
    }

    private static string DecodeTextAsset(TextAsset sheet)
    {
        try
        {
            return StrictUtf8.GetString(sheet.bytes);
        }
        catch (DecoderFallbackException)
        {
            // 旧剧情 CSV 以 GBK 保存；TextAsset.text 会将其中的字节替换为 U+FFFD。
            return Gbk.GetString(sheet.bytes);
        }
    }

    public void BuildTreeFromText(string text)
    {
        tree.Clear();
        lastDia = null;
        currentIndex = -1;
        ReadText(text);
    }

    public DialogNode ShowDialog()
    {
        if (currentIndex == -1)
        {
            return lastDia == null
                ? new DialogNode(-1, DialogType.End, "", "END", "END", "END", "END", "END", "", "", "", ""): lastDia;
        }
        //Debug.Log(currentIndex);
        
        
        return tree[currentIndex];
    }

    public DialogNode ShowNextDialog()
    {
        if (currentIndex == -1)
        {
            return tree[0];
        }

        if (tree[currentIndex].links[0].targetIndex == -1)
        {
            return tree[currentIndex];
        }
        return tree[tree[currentIndex].links[0].targetIndex];
    }

    /// <summary>
    /// 沿确定的后续路径查找下一张背景，遇到尚未选择的分支时停止。
    /// 对话表允许场景列留空并继承上一行，因此自动转场不能只看紧邻节点。
    /// </summary>
    public DialogNode ShowNextBackgroundDialog(string currentScene, out List<DialogNode> transitionPath)
    {
        transitionPath = new List<DialogNode>();
        if (currentIndex < 0 || currentIndex >= tree.Count)
            return null;

        HashSet<int> visited = new HashSet<int> { currentIndex };
        DialogNode cursor = tree[currentIndex];
        bool currentSceneIsEmpty = string.IsNullOrEmpty(currentScene) || currentScene == emptyChar;
        transitionPath.Add(cursor);

        while (cursor.links.Count == 1)
        {
            int targetIndex = cursor.links[0].targetIndex;
            if (targetIndex < 0 || targetIndex >= tree.Count || !visited.Add(targetIndex))
                return null;

            cursor = tree[targetIndex];
            bool hasBackground = !string.IsNullOrEmpty(cursor.scene) && cursor.scene != emptyChar;
            if (hasBackground && (currentSceneIsEmpty || cursor.scene != currentScene))
                return cursor;
            transitionPath.Add(cursor);
        }

        return null;
    }
    
    public List<DialogNode> ShowChoices()
    {
        List<DialogNode> res = new List<DialogNode>();
        //Debug.Log("kk "+ tree[currentIndex].index);
        for (int i = 0; i < tree[currentIndex].links.Count; i++)
        {
            DialogLink link = tree[currentIndex].links[i];
            //Debug.Log("type"+ tree[link.targetIndex].type);
            //Debug.Log("id" + tree[link.targetIndex].index);
            if (tree[link.targetIndex].type == DialogType.Choice)
            {
                res.Add(tree[link.targetIndex]);
            }
        }
        return res;
    }

    public void StartTree(int startIndex)
    {
        currentIndex = startIndex;
    }

    /// <summary>
    /// 返回当前节点的第一个子节点
    /// </summary>
    /// <returns>当前节点的第一个子节点，如果没有子节点则返回 null</returns>
    public DialogNode GetFirstChildNode()
    {
        if (currentIndex == -1 || currentIndex >= tree.Count)
        {
            return null;
        }

        if (tree[currentIndex].links == null || tree[currentIndex].links.Count == 0)
        {
            return null;
        }

        int firstChildIndex = tree[currentIndex].links[0].targetIndex;
        
        if (firstChildIndex < 0 || firstChildIndex >= tree.Count)
        {
            return null;
        }

        return tree[firstChildIndex];
    }


    
    public DialogType Proceed()
    {
        if (currentIndex == -1)
        {
            //Debug.Log("Dialog Not Initialized!");
            return DialogType.End;
        }

        if (tree[currentIndex].StatChanger.Count != 0)
        {
            tree[currentIndex].ChangeAllValues();
        }

        if (tree[currentIndex].links.Count > 1)
        {
            return DialogType.Choice;
        }

        if (tree[currentIndex].type == DialogType.End)
        {
            lastDia = tree[currentIndex];
            lastDia.content = "";
            currentIndex = -1;
            return DialogType.End;
        }
        currentIndex = tree[currentIndex].links[0].targetIndex;
        //Debug.Log(currentIndex + " " + tree[currentIndex].content);
        return tree[currentIndex].links.Count > 1 ? DialogType.Choice : DialogType.Normal;
    }

    public DialogType Proceed(int choice)
    {
        if (currentIndex == -1)
        {
            Debug.Log("Dialog Not Initialized!");
            return DialogType.End;
        }

        if (tree[currentIndex].StatChanger.Count != 0)
        {
            tree[currentIndex].ChangeAllValues();
        }

        if (tree[currentIndex].links.Count <= 1)
        {
            return DialogType.End;
        }

        if (choice < 0 || choice >= tree[currentIndex].links.Count)
            throw new ArgumentOutOfRangeException(nameof(choice), "选项索引超出当前对话选项范围。");

        DialogNode selectedChoice = tree[tree[currentIndex].links[choice].targetIndex];
        if (!selectedChoice.CheckAllRequireMents())
            throw new InvalidOperationException($"剧情节点 {selectedChoice.index} 的条件不满足，不能进入该选项。");

        if (selectedChoice.StatChanger.Count != 0)
            selectedChoice.ChangeAllValues();

        currentIndex = selectedChoice.links[0].targetIndex;

        return tree[currentIndex].links.Count > 1 ? DialogType.Choice : DialogType.Normal;
    }

    public DialogType ProceedLockedChoice(int choice)
    {
        if (currentIndex == -1)
        {
            Debug.Log("Dialog Not Initialized!");
            return DialogType.End;
        }

        if (choice < 0 || choice >= tree[currentIndex].links.Count)
            throw new ArgumentOutOfRangeException(nameof(choice), "锁定选项索引超出当前对话选项范围。");

        DialogNode lockedChoice = tree[tree[currentIndex].links[choice].targetIndex];
        if (!lockedChoice.IsLockedByRequirements() || lockedChoice.lockedTargetIndex < 0)
        {
            throw new InvalidOperationException(
                $"剧情节点 {lockedChoice.index} 不是带有效失败跳转的锁定选项。");
        }

        if (lockedChoice.lockedTargetIndex >= tree.Count)
            throw new InvalidOperationException($"剧情节点 {lockedChoice.index} 的锁定跳转目标不存在。");

        currentIndex = lockedChoice.lockedTargetIndex;
        return tree[currentIndex].links.Count > 1 ? DialogType.Choice : DialogType.Normal;
    }


    private void ReadText(string txt)
    {
        int firstchoice = -1;
        List<int> fathercache = new List<int>();
        dialogRows = txt.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);
        string[] cell = NormalizeCells(dialogRows[1].Split(','));
        for (int i = 1; i < dialogRows.Length; i++)
        {
            // CSV 末尾常见的换行会产生空行；它不属于对话数据，直接结束读取。
            if (string.IsNullOrWhiteSpace(dialogRows[i])) break;

            //Debug.Log(dialogRows[i]);

            string[] newCell = NormalizeCells(dialogRows[i].Split(','));
            //Debug.Log(dialogRows[i]);
            if (newCell.Length <= (int)DialogColumn.ID || newCell[(int)DialogColumn.ID] == "") break;
            // 转场只在配置行执行；其后的空白演出列应保持 CG 隐藏。
            if (DialogCGResources.IsAutoTransitionCommand(cell[(int)DialogColumn.cg])
                || DialogCGResources.IsAutoTransitionCommand(cell[(int)DialogColumn.command]))
                cell[(int)DialogColumn.cg] = emptyChar;
            for (int j = 0; j <= (int)DialogColumn.cg; j++)
            {
                if (newCell[j] != "")
                {
                    cell[j] = newCell[j];
                }
            }
            for (int j = (int)DialogColumn.cg + 1; j < (int)DialogColumn.music; j++)
            {
                cell[j] = newCell[j];
            }
            if (newCell[(int)DialogColumn.music] != "")
            {
                cell[(int)DialogColumn.music] = newCell[(int)DialogColumn.music];
            }
            for (int j = (int)DialogColumn.requirementBehavior; j <= (int)DialogColumn.lockedNext; j++)
            {
                cell[j] = newCell[j];
            }
            if (cell[(int)DialogColumn.name] == emptyChar) cell[(int)DialogColumn.name] = "";
            
            //for (int j = 0; j < cell.Length; j++) Debug.Log(cell[j]);
            
            if (cell[(int)DialogColumn.type] != "#" && cell[(int)DialogColumn.type] != "&") continue;
            if (cell[(int)DialogColumn.type] == "#")
            {
                if (cell[(int)DialogColumn.name] == "旁白") cell[(int)DialogColumn.text] = "<color=#BEBEBE>"+cell[(int)DialogColumn.text];
                firstchoice = -1;
                fathercache.Clear();
                if (cell[(int)DialogColumn.next] == "END")
                {
                    tree.Add(new DialogNode(int.Parse(cell[(int)DialogColumn.ID]), DialogType.End, cell[(int)DialogColumn.text], cell[(int)DialogColumn.name],
                        cell[(int)DialogColumn.pic], cell[(int)DialogColumn.potrait], cell[(int)DialogColumn.cg], cell[(int)DialogColumn.scene], cell[(int)DialogColumn.command], cell[(int)DialogColumn.commandParameter], cell[(int)DialogColumn.music], cell[(int)DialogColumn.sound]));
                    continue;
                }
                tree.Add(new DialogNode(int.Parse(cell[(int)DialogColumn.ID]), DialogType.Normal, cell[(int)DialogColumn.text], cell[(int)DialogColumn.name],
                    cell[(int)DialogColumn.pic], cell[(int)DialogColumn.potrait], cell[(int)DialogColumn.cg], cell[(int)DialogColumn.scene], cell[(int)DialogColumn.command], cell[(int)DialogColumn.commandParameter], cell[(int)DialogColumn.music], cell[(int)DialogColumn.sound]));

                if (cell[(int)DialogColumn.next] != "")
                {
                    Debug.Log(cell[(int)DialogColumn.next]);
                    tree.Last().AddLink(new DialogLink(int.Parse(cell[(int)DialogColumn.next]) - 1, DiaLinkType.Normal,
                        ""));
                }
                else
                {
                    //if(tree.Last().index == 133) Debug.Log("Add"+(int.Parse(cell[(int)DialogColumn.ID]) + 1));
                    tree.Last().AddLink(new DialogLink(int.Parse(cell[(int)DialogColumn.ID]), DiaLinkType.Normal, ""));
                }

            }
            else if (cell[(int)DialogColumn.type] == "&")
            {
                //Debug.Log(cell[(int)DialogColumn.ID]);
                if (firstchoice == -1)
                {
                    firstchoice = int.Parse(cell[(int)DialogColumn.ID]) - 1;
                    //Debug.Log(tree.Count);
                    for (int j = tree.Count - 1; j >= 0; j--)
                    {
                        //Debug.Log(j);
                        //Debug.Log(tree[j].links[0].targetIndex);
                        if (tree[j].links.Count == 1)
                        {
                            if (tree[j].links[0].targetIndex == firstchoice)
                            {
                                fathercache.Add(j);
                                //break;
                            }
                        }
                    }
                }
                //Debug.Log(firstchoice + " + " + tree[fathercache].content);
                if (firstchoice + 1 != int.Parse(cell[(int)DialogColumn.ID]))
                {
                    //Debug.Log(tree[fathercache].index + " " + cell[(int)DialogColumn.ID]);
                    foreach (var VARIABLE in fathercache)
                    {
                        Debug.Log("Link " + VARIABLE + " " + cell[(int)DialogColumn.ID]);
                        tree[VARIABLE].AddLink(new DialogLink(int.Parse(cell[(int)DialogColumn.ID]) - 1,
                            DiaLinkType.Normal, ""));

                    }
                }

                tree.Add(new DialogNode(int.Parse(cell[(int)DialogColumn.ID]), DialogType.Choice, cell[(int)DialogColumn.text], cell[(int)DialogColumn.name],
                    cell[(int)DialogColumn.pic], cell[(int)DialogColumn.potrait], cell[(int)DialogColumn.cg], cell[(int)DialogColumn.scene], cell[(int)DialogColumn.command], cell[(int)DialogColumn.commandParameter], cell[(int)DialogColumn.music], cell[(int)DialogColumn.sound]));
                Debug.Log(dialogRows[i]);
                tree.Last().AddLink(new DialogLink(int.Parse(cell[(int)DialogColumn.next]) - 1, DiaLinkType.Normal, ""));
                //for(int j = tree[fathercache].links.Count - 1; j >= 0 ; j--)
                //{
                //Debug.Log(tree[fathercache].links[j].targetIndex);
                //}
                if (cell[(int)DialogColumn.needName] != "")
                {
                    string[] Changers = cell[(int)DialogColumn.needName].Split('+');
                    string[] Vals = cell[(int)DialogColumn.needValue].Split('+');
                    for (int j = 0; j < Changers.Length; j++)
                    {
                        //Debug.Log(Changers[j]);
                        //Debug.Log(Vals[j]);
                        tree.Last().AddMonitor(Changers[j], int.Parse(Vals[j]));
                    }
                }

                ConfigureRequirementBehavior(tree.Last(), cell);
            }

            if (cell[(int)DialogColumn.statName] != "")
            {
                string[] Changers = cell[(int)DialogColumn.statName].Split('+');
                string[] Vals = cell[(int)DialogColumn.statAmount].Split('+');
                for (int j = 0; j < Changers.Length; j++)
                {
                    //Debug.Log(Changers[j]);
                    tree.Last().AddChanger(Changers[j], int.Parse(Vals[j]));
                }
            }
        }
        Debug.Log("readFinish");
    }

    private static string[] NormalizeCells(string[] source)
    {
        var cells = new string[ColumnCount];
        Array.Copy(source, cells, Math.Min(source.Length, cells.Length));
        for (int i = 0; i < cells.Length; i++)
            cells[i] ??= string.Empty;
        return cells;
    }

    private static void ConfigureRequirementBehavior(DialogNode node, string[] cells)
    {
        string behavior = cells[(int)DialogColumn.requirementBehavior].Trim();
        string lockedNext = cells[(int)DialogColumn.lockedNext].Trim();

        if (string.IsNullOrEmpty(behavior))
        {
            if (!string.IsNullOrEmpty(lockedNext))
                throw new FormatException($"剧情节点 {node.index} 填写了锁定跳转，但未将条件表现设为“锁定”。");
            return;
        }

        if (behavior != "锁定")
        {
            if (behavior == "隐藏")
                return;
            throw new FormatException($"剧情节点 {node.index} 的条件表现“{behavior}”无效；只支持“隐藏”或“锁定”。");
        }

        if (node.StatMonitor.Count == 0)
            throw new FormatException($"剧情节点 {node.index} 使用锁定条件表现，但没有配置需求数值。");

        if (!int.TryParse(lockedNext, out int targetId) || targetId <= 0)
        {
            throw new FormatException(
                $"剧情节点 {node.index} 使用锁定条件表现时，锁定跳转必须是有效的正整数 ID。");
        }

        node.ConfigureLockedRequirement(targetId - 1);
    }
}
