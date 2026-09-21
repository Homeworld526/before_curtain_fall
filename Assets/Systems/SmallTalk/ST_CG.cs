using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ST_CG : SmallTalkElements
{
    public CanvasGroup cgGroup;
    
    private PicSwaper swaper {get => GetComponent<PicSwaper>(); }

    protected override void OnMoveNext(DialogNode node)
    {
        if(Resources.Load<Sprite>(node.cg)!=null)
        {
            if (cgGroup.alpha < 1f)
            {
                CanvasGroupFader.FadeTo(cgGroup, 1f, 0.5f);
            }
            if(GetComponent<Image>().color.a < 1f){swaper.PicShow();}
            swaper.PicChange(Resources.Load<Sprite>(node.cg));
        }
        else if (node.cg == "空")
        {
            swaper.PicHide();
        }
    }
}
