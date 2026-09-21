using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class ShowBigPic : SingleCase<ShowBigPic>
{
    public Transform Element;
    public Image image;
    public RawImage videoDisplay;
    public VideoPlayer videoPlayer;
    public GameObject Quit;

    private int _currentIndex = -1;
    private int _currentVariant = -1;

    public void Show(string name)
    {
        _currentIndex = -1;
        _currentVariant = -1;
        if (CGUnlockSystem.IsVideo(name))
        {
            StartCoroutine(ShowVideoRoutine(name));
        }
        else
        {
            videoPlayer.Stop();
            videoDisplay.gameObject.SetActive(false);
            image.gameObject.SetActive(true);
            image.sprite = Resources.Load<Sprite>(name);
            if (name == "黑屏") Quit.SetActive(false);
            Element.gameObject.SetActive(true);
        }
    }

    public void Show(string name, int index, int variant)
    {
        _currentIndex = index;
        _currentVariant = variant;
        if (CGUnlockSystem.IsVideo(name))
        {
            StartCoroutine(ShowVideoRoutine(name));
        }
        else
        {
            videoPlayer.Stop();
            videoDisplay.gameObject.SetActive(false);
            image.gameObject.SetActive(true);
            image.sprite = Resources.Load<Sprite>(name);
            if (name == "黑屏") Quit.SetActive(false);
            Element.gameObject.SetActive(true);
        }
    }

    private IEnumerator ShowVideoRoutine(string name)
    {
        string clipName = name.Substring("VIDEO_".Length);
        var clip = Resources.Load<VideoClip>(clipName);
        if (clip == null)
        {
            Debug.LogWarning($"[ShowBigPic] VideoClip 未找到: {clipName}");
            yield break;
        }

        videoPlayer.Stop();
        videoPlayer.clip = clip;

        videoDisplay.gameObject.SetActive(false);
        Element.gameObject.SetActive(true);

        // 系列第一张：加载期间不显示任何图片；否则显示前一张图片作为过渡
        bool isFirstInSeries = _currentIndex < 0 || _currentVariant <= 0;
        if (!isFirstInSeries)
        {
            string prevFile = CGUnlockSystem.Instance.GetFile(_currentIndex, _currentVariant - 1);
            if (!string.IsNullOrEmpty(prevFile) && !CGUnlockSystem.IsVideo(prevFile))
            {
                image.sprite = Resources.Load<Sprite>(prevFile);
                image.gameObject.SetActive(true);
            }
            else
            {
                image.gameObject.SetActive(false);
            }
        }
        else
        {
            image.gameObject.SetActive(false);
        }

        videoPlayer.Prepare();
        yield return new WaitUntil(() => videoPlayer.isPrepared);

        image.gameObject.SetActive(false);
        videoDisplay.gameObject.SetActive(true);
        videoPlayer.Play();
    }

    public void ShowNext()
    {
        if (_currentIndex < 0) return;
        int next = _currentVariant + 1;
        if (next >= CGUnlockSystem.Instance.filename[_currentIndex].Count) { Hide(); return; }
        string file = CGUnlockSystem.Instance.GetFile(_currentIndex, next);
        string resourceName = CGUnlockSystem.IsVideo(file) ? file.Substring("VIDEO_".Length) : file;
        if (!string.IsNullOrEmpty(file) && Resources.Load(resourceName) != null)
            Show(file, _currentIndex, next);
    }

    public void ShowPrev()
    {
        if (_currentIndex < 0 || _currentVariant <= 0) return;
        int prev = _currentVariant - 1;
        string file = CGUnlockSystem.Instance.GetFile(_currentIndex, prev);
        string resourceName = CGUnlockSystem.IsVideo(file) ? file.Substring("VIDEO_".Length) : file;
        if (!string.IsNullOrEmpty(file) && Resources.Load(resourceName) != null)
            Show(file, _currentIndex, prev);
    }

    public void Hide()
    {
        videoPlayer.Stop();
        Element.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Element.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Hide();
    }
}
