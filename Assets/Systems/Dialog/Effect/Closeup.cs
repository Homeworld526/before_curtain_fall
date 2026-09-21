using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ContentTracker))]
public class Closeup : MonoBehaviour
{
    private void OnEnable()
    {
        GetComponent<ContentTracker>().act += MBOn;
        GetComponent<ContentTracker>().deact += MBOff;
    }

    private void OnDisable()
    {
        GetComponent<ContentTracker>().act -= MBOn;
        GetComponent<ContentTracker>().deact -= MBOff;
    }
    
    protected void MBOn()
    {
        VisualLocator.Instance.ChangeVisual(VisualLocator.VisualState.Closeup);
    }

    protected void MBOff()
    {
        VisualLocator.Instance.ChangeVisual(VisualLocator.VisualState.L);
    }
}
