using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AutoButton : MonoBehaviour, IEffect
{
    private float originalInterval;
    public Sprite sprite;
    //private bool state = false;
    public bool skip = false;
    private bool force = false;
    
    public void FlipToggle()
    {
        GetComponent<Toggle>().isOn = !GetComponent<Toggle>().isOn;
    }
    
    public void StartAuto()
    {
        bool state = GetComponent<Toggle>().isOn;
        Debug.LogWarning(state);
        if (!skip)
        {
            if (state)
            {
                DialogVisual.Instance.OnTextDisplayDelay += DialogVisual.Instance.AutoProcceed;
                DialogVisual.Instance.AutoProcceed();

                DialogVisual.Instance.IsAuto = true;
                //state = true;
            }
            else
            {
                //if (force) return;
                GetComponent<Image>().sprite = sprite;
                EndAuto();
                //state = false;
            }
        }
        if (skip)
        {
            if (state)
            {
                if (!DialogVisual.Instance.CanStartSkipAtCurrentLine())
                {
                    GetComponent<Toggle>().isOn = false;
                    return;
                }

                DialogVisual.Instance.IsSkip = true;
                originalInterval = DialogVisual.Instance.PicChangeInterval;
                DialogVisual.Instance.PicChangeInterval = 0;
                //state = true;

            }
            else
            {
                EndAuto();
                //state= false;
            }

        }
    }
    public void EndAuto()
    {
        if(!skip) DialogVisual.Instance.OnTextDisplayDelay -= DialogVisual.Instance.AutoProcceed;
        else
        {
            DialogVisual.Instance.IsSkip = false;
            DialogVisual.Instance.PicChangeInterval = originalInterval;
        }
        DialogVisual.Instance.IsAuto = false;
        
    }

    public bool IsRunning()
    {
        throw new System.NotImplementedException();
    }

    public void StartEffect(string param)
    {
        bool state = GetComponent<Toggle>().isOn;
        if (param == "自动")
        {
            if(!state)
            {
                //DialogVisual.Instance.OnTextDisplayDelay += DialogVisual.Instance.AutoProcceed;
                //DialogVisual.Instance.IsAuto = true;
                force = true;
                GetComponent<Toggle>().isOn = true;
            }
        }

        if (param == "恢复")
        {
            if (state)
            {
                //DialogVisual.Instance.OnTextDisplayDelay -= DialogVisual.Instance.AutoProcceed;
                //DialogVisual.Instance.IsAuto = false;
                force = false;
                GetComponent<Toggle>().isOn = false;
                //state = !state;
            }
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
