using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnOffEffect : MonoBehaviour, IEffect
{
    public Transform target;

    private void OnEnable()
    {
        target.gameObject.SetActive(false);
    }
    public void ContinueEffect()
    {
        //throw new System.NotImplementedException();
    }

    public bool IsRunning()
    {
        return false;
        //throw new System.NotImplementedException();
    }

    public void PauseEffect()
    {
        //throw new System.NotImplementedException();
    }

    public void ResetEffect()
    {
        //throw new System.NotImplementedException();
    }

    public void StartEffect(string param)
    {
        if (param == "关")
        {
            target.gameObject.SetActive(false);
        }
        if (param == "开")
        {
            target.gameObject.SetActive(true);
        }
    }
}
