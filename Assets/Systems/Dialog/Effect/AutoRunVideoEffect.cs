using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(VideoSource))]
public class AutoRunVideoEffect : MonoBehaviour, IEffect
{
    private VideoPlayer _videoSource;
    public PicSwaper swaper;
    public Toggle skipButton;
    
    
    public void ContinueEffect()
    {
        throw new System.NotImplementedException();
    }

    public bool IsRunning()
    {
        throw new System.NotImplementedException();
    }

    public void PauseEffect()
    {
        throw new System.NotImplementedException();
    }

    public void ResetEffect()
    {
        throw new System.NotImplementedException();
    }

    public void StartEffect(string param)
    {
        if(param == "CG")
        {
            _videoSource = GetComponent<VideoPlayer>();
            if (skipButton.isOn)
            {
                skipButton.isOn = false;
            }
            StartCoroutine(AutoRun());
        }

        if (param == "非循环CG")
        {
            _videoSource = GetComponent<VideoPlayer>();
            if (skipButton.isOn)
            {
                skipButton.isOn = false;
            }
            StartCoroutine(AutoRunNoLoop());
        }
    }

    private IEnumerator AutoRun()
    {
        yield return null;
        if (_videoSource.clip == null) StopCoroutine(AutoRun());
        DialogVisual.Instance.monitoringMouse = false;
        double time = _videoSource.clip.length;
        while (time > 0)
        {
            time -= Time.deltaTime;
            yield return null;
        }
        //Debug.LogWarning("auto");
        DialogVisual.Instance.AutoProcceed();
        DialogVisual.Instance.monitoringMouse = true;
    }
    private IEnumerator AutoRunNoLoop()
    {
        yield return null;
        if (_videoSource.clip == null) StopCoroutine(AutoRunNoLoop());
        DialogVisual.Instance.monitoringMouse = false;
        double time = _videoSource.clip.length;
        while (time > 0.05)
        {
            time -= Time.deltaTime;
            yield return null;
        }
        swaper?.PicChange(Resources.Load<Sprite>(_videoSource.clip.name + "-静态"));
        DialogVisual.Instance.monitoringMouse = true;
    }
}
