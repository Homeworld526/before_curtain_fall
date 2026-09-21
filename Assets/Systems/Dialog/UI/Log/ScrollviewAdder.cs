using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Text;
using TMPro;

public static class KeyWords
{
    public static string protagonist = "姜韶平";
    public static string pb = "旁白";
    public static string HistoryFacePrefix = "角色图标-";
}


public class ScrollviewAdder : MonoBehaviour
{
    
    
    public List<Transform> contents = new List<Transform>();
    public ScrollRect scrollRect;
    private float[] rateArr;
    //获取Content的RectTransform
    private RectTransform contentTransform;
    //设置添加的预制体
    public RectTransform itemTransform;

    public float rctscale;
    // Use this for initialization
    void Awake()
    {
        //获取自身的ScrollRect组件
        //scrollRect = GetComponent<ScrollRect>();
        //contentTransform = transform.Find("content_new").GetComponent<RectTransform>();
    }

    public void Clear()
    {
        lastContent = null;
        foreach(var tmp in contents)
        {
            Destroy(tmp.gameObject);
        }
        contents.Clear();
    }

    public void ScrollToBottomView()
    {
        if (scrollRect == null || scrollRect.content == null || scrollRect.viewport == null)
        {
            Debug.LogWarning("ScrollRect 或 Content 或 Viewport 未赋值！");
            return;
        }

        // 1. 获取 Content 的高度（包含所有子物体的总高度）
        float contentHeight = scrollRect.content.rect.height;

        // 2. 获取 Viewport 的高度（即 ScrollView 可视区域的高度）
        float viewportHeight = scrollRect.viewport.rect.height;

        // 3. 计算 Content 需要移动的目标位置
        // 只有当内容高度大于视口高度时才需要滚动，否则保持在顶部
        if (contentHeight > viewportHeight)
        {
            // anchoredPosition 的 Y 轴为负值，因为 UI 的原点在左下角
            scrollRect.content.anchoredPosition = new Vector2(0, (contentHeight - viewportHeight));
        }
        else
        {
            // 内容不够长，直接回到顶部
            scrollRect.content.anchoredPosition = Vector2.zero;
        }

        // 强制更新布局，确保位置生效
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
    }

    private string lastName = "*";

    public bool IsLog = true;
    public bool preClear = false;
    private DialogNode lastContent;

    public float fadeTime = 0.25f;


    public void AddPrefab(DialogNode content)
    {
        if(IsLog) while(contents.Count > 50)
        {
            Destroy(contents.First().gameObject);
            contents.RemoveAt(0);
        }
        
        if (preClear)
        {
            Clear();
            preClear = false;
        }

        
        
        
        if (lastContent != null) if (content.content.StartsWith(lastContent.content))
        {
            int l = lastContent.content.Length;
            string addcont = content.content.Substring(lastContent.content.Length);

            contents.Last().GetComponent<HistoryBlockDistributer>().Addtext(addcont, fadeTime);
            lastContent = content;
            return;
        }

        Transform temp = Instantiate(itemTransform).transform;
        temp.SetParent(transform);
        temp.localPosition = Vector3.zero;
        temp.localRotation = Quaternion.identity;
        temp.localScale = Vector3.one;

        string cont;
        if (lastContent != null) cont = content.content;
        else cont = content.content;

        var tmp = temp.GetComponent<HistoryBlockDistributer>();
        
        if (content.logger_name == KeyWords.pb)
        {
            tmp.Distribute(content.logger_name, cont);
        }
        else if (content.logger_name == lastName)
        {
            tmp.Distribute(content.logger_name, cont);
        }
        else
        {
            //Debug.Log(KeyWords.HistoryFacePrefix + content.logger_name);
            tmp.Distribute(
                Resources.Load<Sprite>(KeyWords.HistoryFacePrefix + content.logger_name), content.logger_name,
                content.content);
        }

        if (contents.Count != 0) contents[contents.Count - 1].GetComponent<HistoryBlockDistributer>().HideIcon();
        lastName = content.logger_name;
        contents.Add(temp);
        lastContent = content;


    }
}

