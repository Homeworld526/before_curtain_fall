using UnityEngine;


public class VolumeControlEffect : MonoBehaviour, IEffect
{
    public bool IsRunning()
    {
        throw new System.NotImplementedException();
    }

    public void StartEffect(string param)
    {
        if (param == "减小")
        {
            SoundsManager.Instance.SetMusicVolume(0.25f);
        }

        if (param == "恢复")
        {
            SoundsManager.Instance.SetMusicVolume(1f);
        }

        if (param == "停止" || param == "中止")
        {
            SoundsManager.Instance.StopSfx();
        }

        if (param == "淡出")
        {
            SoundsManager.Instance.FadeOutSfx();
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
