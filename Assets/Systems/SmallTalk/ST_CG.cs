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
        if (node.cg == "空")
        {
            swaper.PicHide();
            return;
        }

        string cgPath = DialogCGResources.GetPath(node.cg);
        if (cgPath == null)
        {
            return;
        }

        Sprite cgSprite = Resources.Load<Sprite>(cgPath);
        if (cgSprite != null)
        {
            if (cgGroup.alpha < 1f)
            {
                CanvasGroupFader.FadeTo(cgGroup, 1f, 0.5f);
            }
            if(GetComponent<Image>().color.a < 1f){swaper.PicShow();}
            swaper.PicChange(cgSprite);
        }
    }
}
