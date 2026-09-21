using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PicSwaper))]
public class ST_Head : SmallTalkElements
{
    private PicSwaper swaper {get => GetComponent<PicSwaper>(); }

    protected override void OnMoveNext(DialogNode node)
    {
        var tmp = Resources.Load<Sprite>("Head/" + node.illustration);
        if(tmp !=null)
        {
            if(GetComponent<Image>().color.a < 1f){swaper.PicShow();}
            swaper.PicChange(tmp);
        }
        else if (node.illustration == "空")
        {
            swaper.PicHide();
        }
    }
}
