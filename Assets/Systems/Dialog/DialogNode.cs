using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DialogType
{
    Normal,
    CutScene,
    Choice,
    End,
}

public enum DiaLinkType
{
    Normal,
}

public class DialogLink
{
    public int targetIndex;
    public DiaLinkType type;
    public string Content;

    public DialogLink(int targetIndex, DiaLinkType type, string content)
    {
        this.targetIndex = targetIndex;
        this.type = type;
        Content = content;
    }
}

public class ElementState
{
    public bool visiblility;
    public bool position;
    public bool paraMoving;
}

public class DialogNode
{
    public int index;
    public DialogType type;
    public string content;
    public string logger_name;
    public string illustration;
    public string illustration_opponent;
    public string cg;
    public string scene;
    public string note;
    public string music;
    public string sfx;

    public List<string> StatMonitor;
    public List<int> MonitorVal;
    public bool monitorIsAnd = true;
    public List<string> StatChanger;
    public List<int> ChangerVal;

    public List<bool> ElementState; 

    public List<DialogLink> links;



    public DialogNode(int index, DialogType type, string content, string logger_name, string illustration, string illustration_opponent, string cg, string scene, string note, string music, string sfx)
    {
        this.index = index;
        this.type = type;
        this.content = content.Replace('^', '\n');
        this.logger_name = logger_name;
        this.illustration = illustration;
        this.illustration_opponent = illustration_opponent;
        this.cg = cg;
        links = new List<DialogLink>();
        StatMonitor = new List<string>();
        MonitorVal = new List<int>();
        StatChanger = new List<string>();
        ChangerVal = new List<int>();
        this.scene = scene;
        this.note = note;
        this.music = music;
        this.sfx = sfx;
    }

    public void AddMonitor(string name, int val)
    {
        StatMonitor.Add(name);
        MonitorVal.Add(val);
    }

    public void AddChanger(string name, int val)
    {
        StatChanger.Add(name);
        ChangerVal.Add(val);
        PlayerPrefs.DeleteKey(name);
    }

    public bool CheckAllRequireMents()
    {
        //if (StatMonitor.Count == 0) return true;
        bool res = false;
        if (monitorIsAnd)
        {
            res = true;
            for(int i = 0; i < StatMonitor.Count; i++)
            {
                res &= DialogList.Instance.CheckMonitor(StatMonitor[i], MonitorVal[i]);
            }
        }
        else
        {
            for (int i = 0; i < StatMonitor.Count; i++)
            {
                res |= DialogList.Instance.CheckMonitor(StatMonitor[i], MonitorVal[i]);
            }
        }
        
        return res;
    }

    public void ChangeAllValues()
    {
        for(int i = 0; i< StatChanger.Count; i++)
        {
            DialogList.Instance.ChangeValue(StatChanger[i], ChangerVal[i]);
        }
    }

    public void AddLink(DialogLink link)
    {
        if(!links.Exists(x => x.Equals(link))) links.Add(link);
    }
}
