using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(Image))]
public class PicSwaper : MonoBehaviour, IEffect
{
    public Vector3 InitPos { get; private set; }

    public bool hasVideo = false;
    public VideoPlayer videoSource;
    public RawImage videoHolder;
    private bool displayingVideo;

    private Image _image;
    private Coroutine _coroutine;
    private Coroutine _fadeCoroutine; // 专门用于渐入渐出协程
    private bool _skipToFinal = false; // 更换图片协程跳过标志
    private bool _skipFadeToFinal = false; // 渐入渐出协程跳过标志
    public bool IOonEnable = false;
    private bool IsTransParent = false;
    private bool resetNextPic;
    private GameObject _clone;
    
    private void Start()
    {
        if(_image == null)_image = GetComponent<Image>();
        InitPos = transform.localPosition;
    }

    private void OnEnable()
    {
        CleanClones();
        if(IOonEnable) PicShow();
        displayingVideo = false;
    }

    private Coroutine videoCoroutine;

    public void PicChange(VideoClip newVideo)
    {
        Debug.LogWarning("��Ƶ" + newVideo.name);
        Debug.LogWarning(displayingVideo);
        if (!hasVideo) return;
        if (_image == null) _image = GetComponent<Image>();
        var sp = DialogVisual.LoadSprite(newVideo.name.Replace("-动态", ""));
        if (sp != null)
        {
            _image.sprite = sp;
        }
        if(videoSource.clip != null)if (newVideo.name == videoSource.clip.name) { return; }
        if (IsTransParent) return;
        //if (_coroutine != null) { videoSource.clip =newVideo; return; }
        if (!displayingVideo)
        {
            displayingVideo = true;
            videoSource.clip = newVideo;
            //StartCoroutine(PicIO(_image, PicChangeInterval, false));
            if(videoCoroutine != null) StopCoroutine(videoCoroutine);
            videoCoroutine = StartCoroutine(PicIO(videoHolder, PicChangeInterval, true, newVideo));
        }
        else
        {
            _coroutine = StartCoroutine(PicChange(videoHolder, newVideo, PicChangeInterval));
        }
    }

    public void PicChange(Sprite newPic)
    {
        if (hasVideo && displayingVideo) {
            Debug.LogWarning("reset");
            if (videoCoroutine != null) StopCoroutine(videoCoroutine);
            videoCoroutine = StartCoroutine(PicIO(videoHolder, PicChangeInterval, false));
            displayingVideo = false;
        }
        if (_image == null) _image = GetComponent<Image>();
        if (_image.sprite != null && newPic.name == _image.sprite.name) { /*Debug.Log("Same");*/ return; }
        if (IsTransParent) return;

        // 场景1：渐入渐出协程正在执行，直接替换图片并退出（不停止渐入渐出协程）
        if (_fadeCoroutine != null)
        {
            _image.sprite = newPic;
            return;
        }

        // 场景2：更换图片协程正在执行，跳过到末状态后启动新协程
        if (_coroutine != null)
        {
            _skipToFinal = true;
            // 等待一帧让协程检测到标志并跳到末状态
            StartCoroutine(WaitAndApplyFinalState(() => {
                _coroutine = StartCoroutine(PicChange(_image, newPic, PicChangeInterval));
            }));
            return;
        }

        _coroutine = StartCoroutine(PicChange(_image, newPic, PicChangeInterval));
    }

    public void PicChange(float delay, Sprite newPic)
    {
        if (hasVideo && displayingVideo) {
            Debug.LogWarning("reset");
            if (videoCoroutine != null) StopCoroutine(videoCoroutine);
            videoCoroutine = StartCoroutine(PicIO(videoHolder, PicChangeInterval, false));
            displayingVideo = false;
        }
        if (_image == null) _image = GetComponent<Image>();
        if (_image.sprite != null && newPic.name == _image.sprite.name) { Debug.Log("Same"); return; }
        if (IsTransParent) return;

        // 场景1：渐入渐出协程正在执行，直接替换图片并退出（不停止渐入渐出协程）
        if (_fadeCoroutine != null)
        {
            _image.sprite = newPic;
            return;
        }

        // 场景2：更换图片协程正在执行，跳过到末状态后启动新协程
        if (_coroutine != null)
        {
            _skipToFinal = true;
            // 等待一帧让协程检测到标志并跳到末状态
            StartCoroutine(WaitAndApplyFinalState(() => {
                _coroutine = StartCoroutine(PicChange(_image, newPic, PicChangeInterval, delay));
            }));
            return;
        }

        _coroutine = StartCoroutine(PicChange(_image, newPic, PicChangeInterval, delay));
    }
    
    public void PicChange(Sprite newPic, float interval)
    {
        if (hasVideo && displayingVideo) {
            Debug.LogWarning("reset");
            if (videoCoroutine != null) StopCoroutine(videoCoroutine);
            videoCoroutine = StartCoroutine(PicIO(videoHolder, interval, false));
            displayingVideo = false;
        }
        if (_image == null) _image = GetComponent<Image>();
        if (_image.sprite != null && newPic.name == _image.sprite.name) { Debug.Log("Same"); return; }
        if (IsTransParent) return;

        // 场景1：渐入渐出协程正在执行，直接替换图片并退出（不停止渐入渐出协程）
        if (_fadeCoroutine != null)
        {
            _image.sprite = newPic;
            return;
        }

        // 场景2：更换图片协程正在执行，跳过到末状态后启动新协程
        if (_coroutine != null)
        {
            _skipToFinal = true;
            StartCoroutine(WaitAndApplyFinalState(() => {
                _coroutine = StartCoroutine(PicChange(_image, newPic, interval));
            }));
            return;
        }

        _coroutine = StartCoroutine(PicChange(_image, newPic, interval));
    }

    public void PicChangeWGradiant(Sprite newPic)
    {
        if (_image == null) _image = GetComponent<Image>();
        //Debug.Log(_image.sprite.name + newPic.name);
        if (_image.sprite != null && newPic.name == _image.sprite.name) return;
        
        PPTSwipeShaderGradientEdge.Instance.StartSwipe(_image, newPic);
    }

    public void PicHideImmediate()
    {
        if (_image == null) _image = GetComponent<Image>();
        if (_coroutine != null) { StopCoroutine(_coroutine); _coroutine = null; }
        if (_fadeCoroutine != null) { StopCoroutine(_fadeCoroutine); _fadeCoroutine = null; }
        var c = _image.color; c.a = 0; _image.color = c;
        IsTransParent = true;
    }

    public void PicShowImmediate()
    {
        if (_image == null) _image = GetComponent<Image>();
        if (_coroutine != null) { StopCoroutine(_coroutine); _coroutine = null; }
        if (_fadeCoroutine != null) { StopCoroutine(_fadeCoroutine); _fadeCoroutine = null; }
        var c = _image.color; c.a = 1; _image.color = c;
        IsTransParent = false;
    }

    public void PicHide()
    {
        if(_clone != null) Destroy(_clone);
        if (_image == null) _image = GetComponent<Image>();
        
        
        // 更换图片协程正在执行，跳过到末状态
        if (_coroutine != null)
        {
            _skipToFinal = true;
            StartCoroutine(WaitAndApplyFinalState(() => {
                _fadeCoroutine = StartCoroutine(PicIO(_image, PicChangeInterval, false));
            }));
            return;
        }

        // 渐入渐出协程正在执行，跳过到末状态
        if (_fadeCoroutine != null)
        {
            _skipFadeToFinal = true;
            StartCoroutine(WaitAndApplyFadeFinalState(() => {
                _fadeCoroutine = StartCoroutine(PicIO(_image, PicChangeInterval, false));
            }));
            return;
        }

        _fadeCoroutine = StartCoroutine(PicIO(_image, PicChangeInterval, false));
    }
    
    public void PicHide(float delay)
    {
        if(_clone != null) Destroy(_clone);
        if (_image == null) _image = GetComponent<Image>();

        // 更换图片协程正在执行，跳过到末状态
        if (_coroutine != null)
        {
            _skipToFinal = true;
            StartCoroutine(WaitAndApplyFinalState(() => {
                _fadeCoroutine = StartCoroutine(PicIO(_image, PicChangeInterval, false, delay));
            }));
            return;
        }

        // 渐入渐出协程正在执行，跳过到末状态
        if (_fadeCoroutine != null)
        {
            _skipFadeToFinal = true;
            StartCoroutine(WaitAndApplyFadeFinalState(() => {
                _fadeCoroutine = StartCoroutine(PicIO(_image, PicChangeInterval, false, delay));
            }));
            return;
        }

        _fadeCoroutine = StartCoroutine(PicIO(_image, PicChangeInterval, false, delay));
    }

    public void PicShow()
    {
        if (_image == null) _image = GetComponent<Image>();

        // 更换图片协程正在执行，跳过到末状态
        if (_coroutine != null)
        {
            _skipToFinal = true;
            StartCoroutine(WaitAndApplyFinalState(() => {
                _fadeCoroutine = StartCoroutine(PicIO(_image, PicChangeInterval, true));
            }));
            return;
        }

        // 渐入渐出协程正在执行，跳过到末状态
        if (_fadeCoroutine != null)
        {
            _skipFadeToFinal = true;
            StartCoroutine(WaitAndApplyFadeFinalState(() => {
                _fadeCoroutine = StartCoroutine(PicIO(_image, PicChangeInterval, true));
            }));
            return;
        }

        _fadeCoroutine = StartCoroutine(PicIO(_image, PicChangeInterval, true));
    }

    public void PicShow(bool reset)
    {
        if (_image == null) _image = GetComponent<Image>();

        // 更换图片协程正在执行，跳过到末状态
        if (_coroutine != null)
        {
            _skipToFinal = true;
            StartCoroutine(WaitAndApplyFinalState(() => {
                _fadeCoroutine = StartCoroutine(PicIO(_image, PicChangeInterval, true, reset));
            }));
            return;
        }

        // 渐入渐出协程正在执行，跳过到末状态
        if (_fadeCoroutine != null)
        {
            _skipFadeToFinal = true;
            StartCoroutine(WaitAndApplyFadeFinalState(() => {
                _fadeCoroutine = StartCoroutine(PicIO(_image, PicChangeInterval, true, reset));
            }));
            return;
        }

        _fadeCoroutine = StartCoroutine(PicIO(_image, PicChangeInterval, true, reset));
    }

    public float PicChangeInterval = 0.25f;

    /// <summary>
    /// 等待一帧让跳过标志生效，然后停止旧协程并执行回调
    /// </summary>
    private IEnumerator WaitAndApplyFinalState(System.Action onComplete)
    {
        yield return null; // 等待一帧让协程跳到末状态
        if(_coroutine != null) StopCoroutine(_coroutine);
        onComplete?.Invoke();
    }

    /// <summary>
    /// 等待一帧让跳过标志生效，然后停止渐入渐出协程并执行回调
    /// </summary>
    private IEnumerator WaitAndApplyFadeFinalState(System.Action onComplete)
    {
        yield return null; // 等待一帧让协程跳到末状态
        if(_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        onComplete?.Invoke();
    }

    public void CleanClones()
    {
        if(_clone != null) Destroy(_clone);
    }
    
    private IEnumerator PicChange(Image Pic, Sprite newPic, float interval)
    {
        if(_clone != null) Destroy(_clone);
        _clone = Instantiate(Pic.gameObject, this.transform.parent);
        _clone.GetComponent<PicSwaper>().enabled = false;
        if( _clone.GetComponent<CGTracker>() != null) _clone.GetComponent<CGTracker>().enabled = false;
        _clone.transform.SetSiblingIndex(Pic.transform.GetSiblingIndex());
        if (resetNextPic)
        {
            Pic.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0, 0, 0);
            Pic.GetComponent<RectTransform>().localScale = Vector3.one;
            resetNextPic = false;
        }
        Color tmp1 = Pic.color;
        Pic.sprite = newPic;
        tmp1.a = 0;
        Pic.color = tmp1;
        while (Pic.color.a < 1f)
        {
            if (_skipToFinal) break;
            tmp1.a = Mathf.Min(tmp1.a + (1 / interval) * Time.deltaTime, 1);
            Pic.color = tmp1;
            _clone.transform.position = Pic.transform.position;
            yield return null;
        }
        if (_skipToFinal)
        {
            tmp1.a = 1;
            Pic.color = tmp1;
            _skipToFinal = false;
        }
        Destroy(_clone);
        _coroutine = null;
    }
    private IEnumerator PicChange(Image Pic, Sprite newPic, float interval, float delay)
    {
        yield return new WaitForSeconds(delay);
        if(_clone != null) Destroy(_clone);
        _clone = Instantiate(Pic.gameObject, this.transform.parent);
        _clone.GetComponent<PicSwaper>().enabled = false;
        _clone.transform.SetSiblingIndex(Pic.transform.GetSiblingIndex());
        
        if (resetNextPic)
        {
            Pic.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0, 0, 0);
            Pic.GetComponent<RectTransform>().localScale = Vector3.one;
            resetNextPic = false;
        }
        Color tmp1 = Pic.color;
        Pic.sprite = newPic;
        tmp1.a = 0;
        Pic.color = tmp1;
        while (Pic.color.a < 1f)
        {
            if (_skipToFinal) break;
            tmp1.a = Mathf.Min(tmp1.a + (1 / interval) * Time.deltaTime, 1);
            Pic.color = tmp1;
            _clone.transform.position = Pic.transform.position;
            yield return null;
        }
        if (_skipToFinal)
        {
            tmp1.a = 1;
            Pic.color = tmp1;
            _skipToFinal = false;
        }
        Destroy(_clone);
        _coroutine = null;
    }
    private IEnumerator PicChange(RawImage Pic, VideoClip newPic, float interval)
    {
        //Debug.Log(newPic.name);
        if(_clone != null) Destroy(_clone);
        _clone = Instantiate(Pic.gameObject, this.transform.parent);
        _clone.transform.SetSiblingIndex(Pic.transform.GetSiblingIndex());
        if (resetNextPic)
        {
            Pic.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0, 0, 0);
            Pic.GetComponent<RectTransform>().localScale = Vector3.one;
            resetNextPic = false;
        }
        Color tmp1 = Pic.color;
        videoSource.clip = newPic;
        tmp1.a = 0;
        Pic.color = tmp1;
        // 等待新视频准备完毕，避免旧视频帧闪烁
        //videoSource.Prepare();
        //yield return new WaitUntil(() => videoSource.isPrepared);
        //videoSource.Play();
        yield return null;
        while (Pic.color.a < 1f)
        {
            tmp1.a = Mathf.Min(tmp1.a + (1 / interval) * Time.deltaTime, 1);
            Pic.color = tmp1;
            yield return null;
        }
        Destroy(_clone);
        _coroutine = null;
    }

    private IEnumerator PicIO(RawImage Pic, float interval, bool In)
    {

        Color tmp = Pic.color;
        if (In)
        {
            tmp.a = 0;
            while (Pic.color.a < 1f)
            {
                tmp.a = Mathf.Min(tmp.a + (1 / interval) * Time.deltaTime, 1);
                Pic.color = tmp;
                yield return null;
            }
        }
        else
        {
            tmp.a = 1;
            while (Pic.color.a > 0f)
            {
                tmp.a = Mathf.Max(tmp.a - (1 / interval) * Time.deltaTime, 0);
                Pic.color = tmp;
                yield return null;
            }
        }

        videoCoroutine = null;
    }
    private IEnumerator PicIO(RawImage Pic, float interval, bool In, VideoClip newVid)
    {

        Color tmp = Pic.color;
        if (In)
        {
            tmp.a = 0;
            Pic.color = tmp;
            // 等待视频准备完毕，避免旧帧闪烁
            videoSource.Prepare();
            yield return new WaitUntil(() => videoSource.isPrepared);
            videoSource.Play();
            yield return null;
            while (Pic.color.a < 1f)
            {
                tmp.a = Mathf.Min(tmp.a + (1 / interval) * Time.deltaTime, 1);
                Pic.color = tmp;
                yield return null;
            }
        }
        else
        {
            tmp.a = 1;
            while (Pic.color.a > 0f)
            {
                tmp.a = Mathf.Max(tmp.a - (1 / interval) * Time.deltaTime, 0);
                Pic.color = tmp;
                yield return null;
            }
        }
        if(DialogVisual.LoadSprite(newVid.name) != null)
        {
            _image.sprite = DialogVisual.LoadSprite(newVid.name);
        }
        videoCoroutine = null;
    }

    private IEnumerator PicIO(Image Pic, float interval, bool In)
    {
        Color tmp = Pic.color;
        if (In)
        {
            tmp.a = 0;
            Pic.color = tmp;
            while (Pic.color.a < 1f)
            {
                if (_skipFadeToFinal) break;
                tmp.a = Mathf.Min(tmp.a + (1 / interval) * Time.deltaTime, 1);
                Pic.color = tmp;
                yield return null;
            }
            if (_skipFadeToFinal)
            {
                tmp.a = 1;
                Pic.color = tmp;
                _skipFadeToFinal = false;
            }
        }
        else
        {
            if (tmp.a != 0f)
            {
                tmp.a = 1;
                Pic.color = tmp;
                while (Pic.color.a > 0f)
                {
                    if (_skipFadeToFinal) break;
                    
                    tmp.a = Mathf.Max(tmp.a - (1 / interval) * Time.deltaTime, 0);
                    Pic.color = tmp;
                    yield return null;
                }

                if (_skipFadeToFinal)
                {
                    tmp.a = 0;
                    Pic.color = tmp;
                    _skipFadeToFinal = false;
                }
            }

        }
        _fadeCoroutine = null;
    }

    private IEnumerator PicIO(Image Pic, float interval, bool In, float delay)
    {
        Color tmp = Pic.color;
        float t = 0f;
        if (In)
        {
            tmp.a = 0;
            Pic.color = tmp;
            
            while (Pic.color.a < 1f)
            {
                if (_skipFadeToFinal) break;
                if (t < delay)
                {
                    
                    t += Time.deltaTime;
                }
                else
                {
                    tmp.a = Mathf.Min(tmp.a + (1 / interval) * Time.deltaTime, 1);
                    Pic.color = tmp;
                }
                yield return null;
            }
            if (_skipFadeToFinal)
            {
                tmp.a = 1;
                Pic.color = tmp;
                _skipFadeToFinal = false;
            }
        }
        else
        {
            if (tmp.a != 0f)
            {
                tmp.a = 1;
                Pic.color = tmp;
                while (Pic.color.a > 0f)
                {
                    if (_skipFadeToFinal) break;
                    if (t < delay)
                    {
                        
                        t += Time.deltaTime;
                    }
                    else
                    {
                        tmp.a = Mathf.Max(tmp.a - (1 / interval) * Time.deltaTime, 0);
                        Pic.color = tmp;
                    }
                    yield return null;
                }

                if (_skipFadeToFinal)
                {
                    tmp.a = 0;
                    Pic.color = tmp;
                    _skipFadeToFinal = false;
                }
            }

        }
        _fadeCoroutine = null;
    }
    
    private IEnumerator PicIO(Image Pic, float interval, bool In, bool reset)
    {
        Color tmp = Pic.color;
        if (In)
        {
            tmp.a = 0;
            Pic.color = tmp;
            while (Pic.color.a < 1f)
            {
                if (_skipFadeToFinal) break;
                tmp.a = Mathf.Min(tmp.a + (1 / interval) * Time.deltaTime, 1);
                Pic.color = tmp;
                yield return null;
            }
            if (_skipFadeToFinal)
            {
                tmp.a = 1;
                Pic.color = tmp;
                _skipFadeToFinal = false;
            }
            if(reset) UITimedParallax.Instance.ResetEffect();
        }
        else
        {
            tmp.a = 1;
            Pic.color = tmp;
            while (Pic.color.a > 0f)
            {
                if (_skipFadeToFinal) break;
                tmp.a = Mathf.Max(tmp.a - (1 / interval) * Time.deltaTime, 0);
                Pic.color = tmp;
                yield return null;
            }
            if (_skipFadeToFinal)
            {
                tmp.a = 0;
                Pic.color = tmp;
                _skipFadeToFinal = false;
            }
        }
        _fadeCoroutine = null;
    }

    public bool IsRunning()
    {
        return _coroutine == null;
    }

    public bool IsAnyCoroutineRunning()
    {
        return _coroutine != null || _fadeCoroutine != null || videoCoroutine != null;
    }

    public void StartEffect(string param)
    {
        if (this._image == null)
        {
            this._image = this.GetComponent<Image>();
        }
        if (this._image == null)
        {
            return;
        }
        if (param == "渐入")
        {
            // 更换图片协程正在执行，跳过到末状态
            if (_coroutine != null)
            {
                _skipToFinal = true;
                StartCoroutine(WaitAndApplyFinalState(() => {
                    _fadeCoroutine = StartCoroutine(PicIO(_image, PicChangeInterval, true));
                }));
                IsTransParent = false;
                return;
            }
            
            // 渐入渐出协程正在执行，跳过到末状态
            if (_fadeCoroutine != null)
            {
                _skipFadeToFinal = true;
                StartCoroutine(WaitAndApplyFadeFinalState(() => {
                    _fadeCoroutine = StartCoroutine(PicIO(_image, PicChangeInterval, true));
                }));
                IsTransParent = false;
                return;
            }
            
            _fadeCoroutine = StartCoroutine(PicIO(_image, PicChangeInterval, true));
            IsTransParent = false;
        }
        if (param == "渐出")
        {
            
            // 更换图片协程正在执行，跳过到末状态
            if (_coroutine != null)
            {
                _skipToFinal = true;
                StartCoroutine(WaitAndApplyFinalState(() => {
                    _fadeCoroutine = StartCoroutine(PicIO(_image, PicChangeInterval, false));
                }));
                IsTransParent = true;
                return;
            }
            
            // 渐入渐出协程正在执行，跳过到末状态
            if (_fadeCoroutine != null)
            {
                _skipFadeToFinal = true;
                StartCoroutine(WaitAndApplyFadeFinalState(() => {
                    _fadeCoroutine = StartCoroutine(PicIO(_image, PicChangeInterval, false));
                }));
                IsTransParent = true;
                return;
            }
            
            _fadeCoroutine = StartCoroutine(PicIO(_image, PicChangeInterval, false));
            IsTransParent = true;
        }
        if (param == "透明")
        {
            if(_coroutine != null) StopCoroutine(_coroutine);
            if(_fadeCoroutine != null)
            {
                _skipFadeToFinal = true;
                StopCoroutine(_fadeCoroutine);
            }
            _image.color = new Color(_image.color.r, _image.color.g, _image.color.b, 0);
            IsTransParent = true;
        }
        if(param == "重置")
        {
            ResetEffect();
        }
    }

    private IEnumerator HoldPrint(float time)
    {
        DialogVisual.Instance.ForceNoClickSkip = true;
        yield return new WaitForSeconds(time);
        DialogVisual.Instance.ForceNoClickSkip = false;
    }

    public void PauseEffect()
    {
        
    }

    public void ContinueEffect()
    {
        
    }

    public void ResetEffect()
    {
        resetNextPic = true;
    }
}
