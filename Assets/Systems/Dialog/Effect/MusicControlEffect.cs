using UnityEngine;
using System.Collections;
    
public class MusicControlEffect: MonoBehaviour, IEffect
{
    public bool IsRunning()
    {
        throw new System.NotImplementedException();
    }

    public void StartEffect(string param)
    {
        if (param == "渐入")
        {
            SoundsManager.Instance.FadeInMusic(2f, 0f);
        }else if (param == "淡出")
        {
            SoundsManager.Instance.FadeOutMusic(2f);
        }else if (param == "暂停")
        {
            SoundsManager.Instance.PauseMusic();
        }else if (param == "恢复")
        {
            SoundsManager.Instance.ResumeMusic();
        }
        else if (param == "结束")
        {
            SoundsManager.Instance.FadeOutMusic();
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
