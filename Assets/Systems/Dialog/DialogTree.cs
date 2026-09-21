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
    backup = 10,
    statName = 11,
    statAmount = 12,
    eventName = 13,
    needName = 14,
    needValue = 15,
    music = 17,
}

public class DialogTree
{

    private DialogNode lastDia;
    private static string emptyChar = "空";
    List<DialogNode> tree = new List<DialogNode>();
    string[] dialogRows;
    int currentIndex = -1;

    //DialogNode currentNode;

    public void BuildTree(TextAsset Sheet)
    {
        tree.Clear();
        ReadText(Sheet.text);
    }

    public void BuildTreeFromText(string text)
    {
        tree.Clear();
        ReadText(text);
    }

    public DialogNode ShowDialog()
    {
        if (currentIndex == -1)
        {
            return lastDia == null
                ? new DialogNode(-1, DialogType.End, "", "END", "END", "END", "END", "END", "", "", ""): lastDia;
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

        currentIndex = tree[currentIndex].links[choice].targetIndex;
        currentIndex = tree[currentIndex].links[0].targetIndex;

        return tree[currentIndex].links.Count > 1 ? DialogType.Choice : DialogType.Normal;
    }


    private void ReadText(string txt)
    {
        int firstchoice = -1;
        List<int> fathercache = new List<int>();
        dialogRows = txt.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);
        string[] cell = dialogRows[1].Split(',');
        for (int i = 1; i < dialogRows.Length; i++)
        {
            //Debug.Log(dialogRows[i]);

            string[] newCell = dialogRows[i].Split(',');
            //Debug.Log(dialogRows[i]);
            if (newCell[(int)DialogColumn.ID] == "") break;
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
                        cell[(int)DialogColumn.pic], cell[(int)DialogColumn.potrait], cell[(int)DialogColumn.cg], cell[(int)DialogColumn.scene], cell[(int)DialogColumn.backup], cell[(int)DialogColumn.music], cell[(int)DialogColumn.sound]));
                    continue;
                }
                tree.Add(new DialogNode(int.Parse(cell[(int)DialogColumn.ID]), DialogType.Normal, cell[(int)DialogColumn.text], cell[(int)DialogColumn.name],
                    cell[(int)DialogColumn.pic], cell[(int)DialogColumn.potrait], cell[(int)DialogColumn.cg], cell[(int)DialogColumn.scene], cell[(int)DialogColumn.backup], cell[(int)DialogColumn.music], cell[(int)DialogColumn.sound]));

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
                    cell[(int)DialogColumn.pic], cell[(int)DialogColumn.potrait], cell[(int)DialogColumn.cg], cell[(int)DialogColumn.scene], cell[(int)DialogColumn.backup], cell[(int)DialogColumn.music], cell[(int)DialogColumn.sound]));
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
}
