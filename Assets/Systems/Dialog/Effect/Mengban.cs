using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ContentTracker))]
public class Mengban : MonoBehaviour
{
    public ScreenMultiplyEffect_Builtin image; 
    


    private void OnEnable()
    {
        if(image == null) image = Camera.main.GetComponent<ScreenMultiplyEffect_Builtin>();
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
        image.enabled = true;
    }

    protected void MBOff()
    {
        image.enabled = false;
    }
}
