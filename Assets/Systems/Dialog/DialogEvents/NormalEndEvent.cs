using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class NormalEndEvent : DialogEventHost
{
    // 定义一个事件
    public event Action DialogEnded;

    public GameObject Home;
    public GameObject PrepRoom;
    public GameObject Dialog;
    public string transitionVideoName;
    public GameObject skipButtonPrefab;
    private bool showPV = true;
    private VideoPlayer _activeVp;

    public void SetShowPV(bool show)
    {
        showPV = !show;
    }

    /// <summary>
    /// 在过场视频播放期间调用，立即跳过视频并进入下一步。
    /// </summary>
    public void SkipVideo()
    {
        if (_activeVp != null && _activeVp.isPlaying)
            _activeVp.Stop();
    }
    
    
    protected override void DialogEndEvent(string param)
    {
        // 设置对话框为不激活状态
        Dialog.SetActive(false);

        // 触发事件
        DialogEnded?.Invoke();
        
        PrepRoom.SetActive(false);
        Home.SetActive(true);

        if (param == "加载下半")
        {
            StartCoroutine(PlayVideoThenNext(1));
            return;
        }

        if (param == "至夏伦二章")
        {
            PlayerPrefs.SetInt("dialogIndex", 2);
            PlayerPrefs.SetInt("dialogLine", 0);

            var dialog = DialogLocator.Instance.Dialog;
            dialog.SetActive(false);
            dialog.SetActive(true);
            return;
        }
        
        if (param != "无刷屏转场")
        {
            Debug.LogWarning("endend");
            //GetComponent<ShowDialog>().Hide(); 
            JobManager.Instance.AdvanceOneDayAndSetEmpty();
            SoundsManager.Instance.PlayMusic("小平房间循环曲");
        }
    }

    private IEnumerator PlayVideoThenNext(int index)
    {
        var req = Resources.LoadAsync<VideoClip>(transitionVideoName);
        yield return req;

        var clip = req.asset as VideoClip;
        if (clip != null && showPV)
        {
            var videoGo = new GameObject("TransitionVideo");
            videoGo.transform.SetParent(ShowBigPic.Instance.Element.parent, false);

            var rt = videoGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(1920, 1080);

            var renderTex = new RenderTexture(1920, 1080, 0);
            videoGo.AddComponent<RawImage>().texture = renderTex;

            var vp = videoGo.AddComponent<VideoPlayer>();
            vp.clip = clip;
            vp.targetTexture = renderTex;
            vp.isLooping = false;
            vp.playOnAwake = false;
            vp.audioOutputMode = VideoAudioOutputMode.Direct;
            vp.Prepare();

            float elapsed = 0f;
            while (!vp.isPrepared && elapsed < 10f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!vp.isPrepared)
            {
                Debug.LogWarning("VideoPlayer prepare timed out, skipping video.");
                vp.clip = null;
                renderTex.Release();
                Destroy(renderTex);
                Destroy(videoGo);
                Resources.UnloadAsset(clip);
                yield return StartCoroutine(Next(index));
                yield break;
            }

            vp.Play();
            _activeVp = vp;

            GameObject skipBtn = null;
            if (skipButtonPrefab != null)
            {
                skipBtn = Instantiate(skipButtonPrefab, videoGo.transform.parent);
                // 确保按钮渲染在视频上层
                skipBtn.transform.SetAsLastSibling();
                var btn = skipBtn.GetComponentInChildren<UnityEngine.UI.Button>();
                if (btn != null)
                    btn.onClick.AddListener(SkipVideo);
            }

            yield return new WaitUntil(() => !vp.isPlaying);
            _activeVp = null;

            if (skipBtn != null)
                Destroy(skipBtn);

            vp.Stop();
            vp.clip = null;
            renderTex.Release();
            Destroy(renderTex);
            Destroy(videoGo);
            Resources.UnloadAsset(clip);
        }

        yield return StartCoroutine(Next(index));
    }

    private IEnumerator Next(int index)
    {
        //ShowBigPic.Instance.Show("黑屏");
        yield return null;
        FindObjectOfType<ShowDialog>().Show(index, 0);
        yield return null;
        //ShowBigPic.Instance.Hide();
    }
}

