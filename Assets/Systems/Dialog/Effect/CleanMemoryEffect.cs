using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CleanMemoryEffect : MonoBehaviour, IEffect
{


    public bool IsRunning()
    {
        throw new System.NotImplementedException();
    }

    public void StartEffect(string param)
    {
        if (param == "渐出")
        {
            Resources.UnloadUnusedAssets();
        }
    }

    public void PauseEffect()
    {
        throw new System.NotImplementedException();
    }

    public void ContinueEffect()
    {
        throw new System.NotImplementedException();
    }

    public void ResetEffect()
    {
        throw new System.NotImplementedException();
    }
}
