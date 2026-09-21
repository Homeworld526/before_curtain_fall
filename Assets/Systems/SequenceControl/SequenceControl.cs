using System;
using System.Collections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DialogUpdateSequence
{
    StartPrint,
    InPrint,
    EndPrint,
    WaitForClick,
    StartTransition,
    InTransition,
    EndTransition,
}

public struct ActionSequence
{
    public Action StartPrintAction;
    public Action InPrintAction;
    public Action EndPrintAction;
    public Action StartTransitionAction;
    public Action EndTransitionAction;
    public Action WaitForClickAction;
    public Action InTransitionAction;

    public void Add(Action act, DialogUpdateSequence seq)
    {
        switch (seq)
        {
            case DialogUpdateSequence.StartPrint:
                StartPrintAction += act;
                break;
            case DialogUpdateSequence.InPrint:
                InPrintAction += act;
                break;
            case DialogUpdateSequence.EndPrint:
                EndPrintAction += act;
                break;
            case DialogUpdateSequence.WaitForClick:
                WaitForClickAction += act;
                break;
            case DialogUpdateSequence.StartTransition:
                StartTransitionAction += act;
                break;
            case DialogUpdateSequence.InTransition:
                InTransitionAction += act;
                break;
            case DialogUpdateSequence.EndTransition:
                EndTransitionAction += act;
                break;
        }
    }

    public void Remove(Action act, DialogUpdateSequence seq)
    {
        switch (seq)
        {
            case DialogUpdateSequence.StartPrint:
                StartPrintAction -= act;
                break;
            case DialogUpdateSequence.InPrint:
                InPrintAction -= act;
                break;
            case DialogUpdateSequence.EndPrint:
                EndPrintAction -= act;
                break;
            case DialogUpdateSequence.WaitForClick:
                WaitForClickAction -= act;
                break;
            case DialogUpdateSequence.StartTransition:
                StartTransitionAction -= act;
                break;
            case DialogUpdateSequence.InTransition:
                InTransitionAction -= act;
                break;
            case DialogUpdateSequence.EndTransition:
                EndTransitionAction -= act;
                break;
        }
    }
}
public interface ISequenceControl
{
    ActionSequence RegisterPermaAction();
    ActionSequence RegisterTempAction();
    
}


public class SequenceControl : SingleCase<SequenceControl>
{
    private event Action StartPrintAction;
    private event Action InPrintAction;
    private event Action EndPrintAction;
    private event Action StartTransitionAction;
    private event Action EndTransitionAction;
    private event Action WaitForClickAction;
    private event Action InTransitionAction;
    

    public void AddAction(ActionSequence x)
    {
        StartPrintAction += x.StartPrintAction;
        InPrintAction += x.InPrintAction;
        EndPrintAction += x.EndPrintAction;
        StartTransitionAction += x.StartTransitionAction;
        EndTransitionAction += x.EndTransitionAction;
        WaitForClickAction += x.WaitForClickAction;
        InTransitionAction += x.InTransitionAction;
    }

    public void RemoveAction(ActionSequence x)
    {
        StartPrintAction -= x.StartPrintAction;
        InPrintAction -= x.InPrintAction;
        EndPrintAction -= x.EndPrintAction;
        StartTransitionAction -= x.StartTransitionAction;
        EndTransitionAction -= x.EndTransitionAction;
        WaitForClickAction -= x.WaitForClickAction;
        InTransitionAction -= x.InTransitionAction;
    }


    private bool IsInit = false;
    private IEnumerator Init()
    {
        newAct = new List<ActionSequence>(); 
        yield return null;
        foreach (var host in GetComponents<ISequenceControl>())
        {
            AddAction(host.RegisterPermaAction());
        }
        IsInit = true;
    }
    
    void Start()
    {
        StartCoroutine(Init());
    }

    private List<ActionSequence> newAct;

    private Coroutine _sequence;

    private IEnumerator WaitForEndOfPrint()
    {
        //while(DialogVisual.Instance.IsPrinting())
        while(true)
        {
            InPrintAction.Invoke();
            yield return null;
        }
    }

    private IEnumerator WaitForClick()
    {
        //while(!DialogVisual.Instance.Mousedown)
        while(true)
        {
            yield return null;
        }
    }
    
    private IEnumerator WaitForTrans()
    {
        //while(transitionTime/2)
        while(true)
        {
            WaitForClickAction.Invoke();
            yield return null;
        }
    }
    
    
    private IEnumerator Sequence()
    {
        newAct.Clear();
        foreach (var host in GetComponents<ISequenceControl>())
        {
            newAct.Add(host.RegisterTempAction());
        }
        float waitTime = 0.1f;
        //Visualize
        StartPrintAction.Invoke();
        yield return StartCoroutine(WaitForEndOfPrint());
        EndPrintAction.Invoke();
        yield return StartCoroutine(WaitForClick());
        StartTransitionAction.Invoke();
        yield return StartCoroutine(WaitForTrans());
        InTransitionAction.Invoke();
        yield return StartCoroutine(WaitForTrans());
        EndTransitionAction.Invoke();
        foreach (var host in newAct)
        {
            RemoveAction(host);
        }
        _sequence = null;
    }
    
    
    // Update is called once per frame
    void Update()
    {
        if (!IsInit) return;
        if(_sequence == null) _sequence = StartCoroutine(Sequence());
        
    }
    
    
}
