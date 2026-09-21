using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualLocator : SingleCase<VisualLocator>
{
    public enum VisualState
    {
        L,
        R,
        Closeup
    }

    private void Start()
    {
        ChangeVisual(VisualState.L);
    }

    public RectTransform foreGround { get; private set; }
    public RectTransform backGround;
    public RectTransform middleGround { get; set; }


    public Transform LVisual;
    public Transform RVisual;
    public Transform Closeup;
    public Transform CG;
    public Transform CG_old;

    public VisualState currentState { get; private set; }



    private static void Hide(PicSwaper pic)
    {
        if (pic.gameObject.activeInHierarchy) pic.PicHide();
        else pic.PicHideImmediate();
    }

    private static void Show(PicSwaper pic)
    {
        if (pic.gameObject.activeInHierarchy) pic.PicShow();
        else pic.PicShowImmediate();
    }

    public void LoadState(VisualState s)
    {
        currentState = s;
    }
    
    public void ChangeVisual(VisualState s)
    {
        currentState = s;
        if(foreGround!=null)foreGround.GetComponent<PicSwaper>().CleanClones();
        if(middleGround !=null)middleGround.GetComponent<PicSwaper>().CleanClones();
        //backGround.GetComponent<PicSwaper>().CleanClones();
        if (s == VisualState.L)
        {
            foreach( PicSwaper pic in RVisual.GetComponentsInChildren<PicSwaper>(true)){
                Hide(pic);
            }
            foreach (PicSwaper pic in Closeup.GetComponentsInChildren<PicSwaper>(true))
            {
                Hide(pic);
            }
            foreach (PicSwaper pic in LVisual.GetComponentsInChildren<PicSwaper>(true))
            {
                Show(pic);
            }
            foreGround = LVisual.GetChild(1).GetComponent<RectTransform>();
            middleGround = LVisual.GetChild(0).GetComponent<RectTransform>();
        }
        else if (s == VisualState.R)
        {
            foreach (PicSwaper pic in RVisual.GetComponentsInChildren<PicSwaper>(true))
            {
                Show(pic);
            }
            foreach (PicSwaper pic in Closeup.GetComponentsInChildren<PicSwaper>(true))
            {
                Hide(pic);
            }
            foreach (PicSwaper pic in LVisual.GetComponentsInChildren<PicSwaper>(true))
            {
                Hide(pic);
            }
            foreGround = RVisual.GetChild(1).GetComponent<RectTransform>();
            middleGround = RVisual.GetChild(0).GetComponent<RectTransform>();
        }
        else
        {
            foreach (PicSwaper pic in RVisual.GetComponentsInChildren<PicSwaper>(true))
            {
                Hide(pic);
            }
            foreach (PicSwaper pic in Closeup.GetComponentsInChildren<PicSwaper>(true))
            {
                Show(pic);
            }
            foreach (PicSwaper pic in LVisual.GetComponentsInChildren<PicSwaper>(true))
            {
                Hide(pic);
            }
            foreGround = null;
            middleGround = Closeup.GetChild(0).GetComponent<RectTransform>();
        }
        UITimedParallax.Instance.SetInit();
    }

}
