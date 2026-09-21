using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(AudioSource))]
public class SoundsManager : SingleCase<SoundsManager>
{
    /// <summary>
    /// 背景音乐 AudioSource
    /// </summary>
    private AudioSource _musicSource;

    /// <summary>
    /// 音效 AudioSource 池（支持多个音效同时播放）
    /// </summary>
    private List<AudioSource> _sfxSources;

    /// <summary>
    /// UI 音效专用 AudioSource（独立通道，不被对话音效挤占）
    /// </summary>
    private AudioSource _uiSfxSource;

    /// <summary>
    /// 打字音效专用 AudioSource（避免频繁播放占满 SFX 池）
    /// </summary>
    private AudioSource _typingSfxSource;

    /// <summary>
    /// 音效 AudioSource 最大数量
    /// </summary>
    [SerializeField]
    private int _maxSfxSources = 10;

    /// <summary>
    /// 当前背景音乐文件路径
    /// </summary>
    private string _currentMusicPath;

    /// <summary>
    /// 当前音效文件路径
    /// </summary>
    private string _currentSfxPath;

    [Header("UI 音效配置")]
    [SerializeField] private AudioClip _clickSfxClip;
    [SerializeField] private string _clickSfxPath = "SFX/关按键";

    [SerializeField] private AudioClip _confirmSfxClip;
    [SerializeField] private string _confirmSfxPath = "";

    [SerializeField] private AudioClip _mapExpandSfxClip;
    [SerializeField] private string _mapExpandSfxPath = "";

    [SerializeField] private AudioClip _hoverSfxClip;
    [SerializeField] private string _hoverSfxPath = "";

    [SerializeField] private bool _uiSfxEnabled = true;

    /// <summary>
    /// 是否正在播放背景音乐
    /// </summary>
    public bool IsMusicPlaying => _musicSource != null && _musicSource.isPlaying;

    public bool IsFadingIn => _fadeInCoroutine != null;
    public bool IsFadingOut => _fadeOutCoroutine != null;
    public bool IsMusicFadedOut => _fadedOut;

    // 音乐被有意淡出/停止后置 true，阻止 SoundProcess 以同一 clip 自动重启
    private bool _fadedOut = false;

    /// <summary>
    /// 是否有音效正在播放
    /// </summary>
    public bool IsSfxPlaying => _sfxSources != null && _sfxSources.Exists(s => s != null && s.isPlaying);

    /// <summary>
    /// 渐弱默认时长（秒）
    /// </summary>
    [SerializeField]
    private float _fadeDuration = 1f;

    /// <summary>
    /// 当前音乐音量
    /// </summary>
    private float _musicVolume = 1f;

    /// <summary>
    /// 当前音效音量
    /// </summary>
    private float _sfxVolume = 1f;

    /// <summary>
    /// 主音量
    /// </summary>
    private float _masterVolume = 1f;

    /// <summary>
    /// 是否静音
    /// </summary>
    private bool _isMuted = false;

    /// <summary>
    /// 静音前的音量(用于恢复)
    /// </summary>
    private float _volumeBeforeMute = 1f;

    /// <summary>
    /// 获取当前音乐音量
    /// </summary>
    public float MusicVolume => _musicVolume;

    /// <summary>
    /// 获取当前音效音量
    /// </summary>
    public float SfxVolume => _sfxVolume;

    /// <summary>
    /// 获取主音量
    /// </summary>
    public float MasterVolume => _masterVolume;

    /// <summary>
    /// 是否处于静音状态
    /// </summary>
    public bool IsMuted => _isMuted;

    /// <summary>
    /// 当前播放的音乐路径
    /// </summary>
    public string CurrentMusicPath => _currentMusicPath;

    /// <summary>
    /// 当前加载的音乐 clip 名（用于存档还原）
    /// </summary>
    public string CurrentMusicClipName => _musicSource?.clip?.name;

    /// <summary>
    /// 当前正在进行的淡入协程
    /// </summary>
    private Coroutine _fadeInCoroutine;

    /// <summary>
    /// 当前正在进行的淡出协程
    /// </summary>
    private Coroutine _fadeOutCoroutine;

    /// <summary>
    /// 当前正在进行的音效渐出协程
    /// </summary>
    private Coroutine _sfxFadeOutCoroutine;

    /// <summary>
    /// 当前正在进行的主音量渐变协程
    /// </summary>
    private Coroutine _masterVolumeFadeCoroutine;

    protected override void Awake()
    {
        base.Awake();
        if (!IsPrimaryInstance) return;

        init();
    }

    private bool isInit = false;
    
    public override void init()
    {
        base.init();

        // 获取或添加 AudioSource 组件
        _musicSource = GetComponent<AudioSource>();
        _musicSource.playOnAwake = false;
        _musicSource.loop = true;
        _musicSource.volume = _musicVolume;

        // 为音效创建多个独立的 AudioSource（支持同时播放）
        _sfxSources = new List<AudioSource>();
        for (int i = 0; i < _maxSfxSources; i++)
        {
            GameObject sfxObj = new GameObject($"SFX_Source_{i}");
            sfxObj.transform.SetParent(transform);
            AudioSource sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.volume = _sfxVolume;
            _sfxSources.Add(sfxSource);
        }

        // UI 音效专用 AudioSource
        GameObject uiObj = new GameObject("UI_SFX_Source");
        uiObj.transform.SetParent(transform);
        _uiSfxSource = uiObj.AddComponent<AudioSource>();
        _uiSfxSource.playOnAwake = false;
        _uiSfxSource.loop = false;
        _uiSfxSource.volume = _sfxVolume;

        // 打字音效专用 AudioSource
        GameObject typingObj = new GameObject("Typing_SFX_Source");
        typingObj.transform.SetParent(transform);
        _typingSfxSource = typingObj.AddComponent<AudioSource>();
        _typingSfxSource.playOnAwake = false;
        _typingSfxSource.loop = false;
        _typingSfxSource.volume = _sfxVolume;

        isInit = true;
    }

    #region 音乐控制

    /// <summary>
    /// 设置背景音乐文件路径（Resources 文件夹下的相对路径，不含扩展名）
    /// </summary>
    /// <param name="path">如 "Music/BGM"，会自动尝试加载 .mp3/.wav/.ogg 等格式</param>
    public void SetMusicFile(string path)
    {
        // 如果设置的是相同的音乐文件路径，不重复设置
        if (!string.IsNullOrEmpty(path) && path == _currentMusicPath)
        {
            //Debug.Log($"[SoundsManager] 音乐文件路径相同，不重复设置: {path}");
            return;
        }
        _currentMusicPath = path;
    }

    public void ClearMusicClip()
    {
        _currentMusicPath = null;
        if (_musicSource != null) _musicSource.clip = null;
    }

    /// <summary>
    /// 设置背景音乐文件（使用 AudioClip）
    /// </summary>
    /// <param name="clip">音乐片段</param>
    public void SetMusicFile(AudioClip clip)
    {
        if (_musicSource != null && clip != null)
        {
            if (_musicSource.clip == clip)
            {
                return;
            }
            _musicSource.clip = clip;
            // 仅在淡入中才重新 Play（让新 clip 立即接管），淡出中或停止时不重启
            if (_fadeInCoroutine != null)
            {
                _musicSource.Play();
            }
        }
    }

    /// <summary>
    /// 播放背景音乐
    /// </summary>
    /// <param name="path">可选：音乐文件路径。如果未提前设置，需传入此参数</param>
    public void PlayMusic(string path = null)
    {
        string musicPath = path ?? _currentMusicPath;
        if (string.IsNullOrEmpty(musicPath))
        {
            Debug.LogWarning("[SoundsManager] 未设置音乐文件路径");
            return;
        }

        LoadAndPlayMusic(musicPath);
    }

    /// <summary>
    /// 播放背景音乐（使用已设置的 AudioClip）
    /// </summary>
    public void PlayMusic()
    {
        if (_musicSource != null && _musicSource.clip != null)
        {
            _musicSource.Play();
        }
        else
        {
            Debug.LogWarning("[SoundsManager] 未设置音乐 AudioClip");
        }
    }

    /// <summary>
    /// 播放背景音乐（使用 AudioClip）
    /// </summary>
    /// <param name="clip">音乐片段</param>
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[SoundsManager] 音乐 AudioClip 为空");
            return;
        }

        // 如果加载的是相同的 AudioClip 且正在播放，不打断播放
        if (_musicSource != null && _musicSource.clip == clip && _musicSource.isPlaying)
        {
            //Debug.Log($"[SoundsManager] AudioClip 相同，不打断播放: {clip.name}");
            return;
        }

        LoadAndPlayMusic(clip);
    }

    private void LoadAndPlayMusic(AudioClip clip)
    {
        _fadedOut = false;
        // 停止正在进行的淡入淡出协程
        StopAllFadeCoroutines();

        // 如果正在播放，先停止
        if (_musicSource.isPlaying)
        {
            _musicSource.Stop();
        }

        if (clip != null)
        {
            _musicSource.clip = clip;
            _musicSource.Play();
        }
        else
        {
            Debug.LogError("[SoundsManager] 音乐加载失败：AudioClip 为空");
        }
    }

    private void LoadAndPlayMusic(string path)
    {
        // 如果加载的是相同的音乐文件，不打断播放
        if (!string.IsNullOrEmpty(_currentMusicPath) && _currentMusicPath == path && _musicSource.isPlaying)
        {
            //Debug.Log($"[SoundsManager] 音乐文件相同，不打断播放: {path}");
            return;
        }

        // 停止正在进行的淡入淡出协程
        StopAllFadeCoroutines();

        // 如果正在播放，先停止
        if (_musicSource.isPlaying)
        {
            _musicSource.Stop();
        }

        // 从 Resources 加载音频
        AudioClip clip = LoadAudioClip(path);
        if (clip != null)
        {
            _musicSource.clip = clip;
            _musicSource.Play();
            // 设置当前音乐路径（修复：之前遗漏了这一步）
            _currentMusicPath = path;
            Debug.Log($"[SoundsManager] 成功播放音乐: {path}");
        }
        else
        {
            Debug.LogError($"[SoundsManager] 音乐加载失败：{path}");
        }
    }

    /// <summary>
    /// 暂停背景音乐
    /// </summary>
    public void PauseMusic()
    {
        if (_musicSource != null && _musicSource.isPlaying)
        {
            // 停止正在进行的淡入淡出协程
            StopAllFadeCoroutines();
            _musicSource.Pause();
        }
    }

    /// <summary>
    /// 恢复播放背景音乐
    /// </summary>
    public void ResumeMusic()
    {
        if (_musicSource != null && !_musicSource.isPlaying)
        {
            _musicSource.Play();
        }
    }

    /// <summary>
    /// 重置背景音乐到开头并暂停
    /// </summary>
    public void ResetMusic()
    {
        if (_musicSource != null)
        {
            // 停止正在进行的淡入淡出协程
            StopAllFadeCoroutines();
            _musicSource.Stop();
            _musicSource.time = 0f;
        }
    }

    /// <summary>
    /// 背景音乐渐弱并停止
    /// </summary>
    /// <param name="duration">渐弱时长（秒），默认使用设置的渐弱时长</param>
    public void FadeOutMusic(float? duration = null)
    {
        float fadeTime = duration ?? _fadeDuration;
        if (_musicSource == null || !_musicSource.isPlaying)
        {
            return;
        }

        _fadedOut = true;

        // 停止正在进行的淡入协程，避免冲突
        if (_fadeInCoroutine != null)
        {
            StopCoroutine(_fadeInCoroutine);
            _fadeInCoroutine = null;
        }

        // 停止正在进行的淡出协程（如果有）
        if (_fadeOutCoroutine != null)
        {
            StopCoroutine(_fadeOutCoroutine);
        }

        // 启动新的淡出协程
        _fadeOutCoroutine = StartCoroutine(FadeOutCoroutine(_musicSource, fadeTime));
    }

    /// <summary>
    /// 背景音乐渐强
    /// </summary>
    /// <param name="duration">渐强时长（秒）</param>
    public void FadeInMusic(float duration , float startVolume)
    {
        if (_musicSource == null)
        {
            return;
        }

        _fadedOut = false;
        if (_fadeOutCoroutine != null)
        {
            StopCoroutine(_fadeOutCoroutine);
            _fadeOutCoroutine = null;
        }

        // 停止正在进行的淡入协程（如果有）
        if (_fadeInCoroutine != null)
        {
            StopCoroutine(_fadeInCoroutine);
        }

        // 先把音量归零，确保 Play() 时不会先以满音量播一帧
        _musicSource.volume = startVolume;

        // 如果未播放，先开始播放
        if (!_musicSource.isPlaying)
        {
            _musicSource.Play();
        }

        // 启动新的淡入协程
        _fadeInCoroutine = StartCoroutine(FadeInCoroutine(_musicSource, duration, startVolume));
    }

    /// <summary>
    /// 设置音乐音量
    /// </summary>
    /// <param name="volume">音量值（0-1）</param>
    public void SetMusicVolume(float volume)
    {
        _musicVolume = Mathf.Clamp01(volume) * SettingManager.Instance.settings.MasterVolume;
        if (_musicSource != null)
        {
            ApplyVolumeToMusicSource();
        }
    }

    /// <summary>
    /// 调整音乐音量（相对增减）
    /// </summary>
    /// <param name="delta">音量变化量（正数增加，负数减少）</param>
    public void AdjustMusicVolume(float delta)
    {
        SetMusicVolume(_musicVolume + delta);
    }

    /// <summary>
    /// 应用音量到 AudioSource（考虑主音量和静音状态）
    /// </summary>
    private void ApplyVolumeToMusicSource()
    {
        if (_musicSource != null)
        {
            _musicSource.volume = _isMuted ? 0f : _musicVolume * _masterVolume;
        }
    }

    #endregion

    #region 音效控制

    /// <summary>
    /// 设置音效文件路径（Resources 文件夹下的相对路径，不含扩展名）
    /// </summary>
    /// <param name="path">如 "SFX/Click"，会自动尝试加载 .mp3/.wav/.ogg 等格式</param>
    public void SetSfxFile(string path)
    {
        _currentSfxPath = path;
    }

    /// <summary>
    /// 获取一个空闲的音效 AudioSource
    /// </summary>
    /// <returns>空闲的 AudioSource，如果没有则返回 null</returns>
    private AudioSource GetFreeSfxSource()
    {
        // 先查找空闲的 AudioSource
        foreach (var sfxSource in _sfxSources)
        {
            if (sfxSource != null && !sfxSource.isPlaying)
            {
                return sfxSource;
            }
        }
        
        // 如果没有空闲的，返回第一个（将会打断当前播放的音效）
        Debug.LogWarning("[SoundsManager] 所有音效源都在使用中，将重用第一个 AudioSource");
        return _sfxSources[0];
    }

    /// <summary>
    /// 播放音效（支持多个音效同时播放）
    /// </summary>
    /// <param name="path">可选：音效文件路径。如果未提前设置，需传入此参数</param>
    public void PlaySfx(string path = null)
    {
        if (!isInit) return;
        string sfxPath = path ?? _currentSfxPath;
        if (string.IsNullOrEmpty(sfxPath))
        {
            Debug.LogWarning("[SoundsManager] 未设置音效文件路径");
            return;
        }

        LoadAndPlaySfx(sfxPath);
    }

    private void LoadAndPlaySfx(string path)
    {
        // 获取一个空闲的音效源
        AudioSource sfxSource = GetFreeSfxSource();
        if (sfxSource == null)
        {
            Debug.LogError("[SoundsManager] 无法获取音效源");
            return;
        }

        // 从 Resources 加载音频
        AudioClip clip = LoadAudioClip(path);
        if (clip != null)
        {
            sfxSource.clip = clip;
            sfxSource.Play();
            // 启动协程，播放完毕后清空 clip 释放资源
            StartCoroutine(ReleaseSfxSourceAfterPlay(sfxSource));
        }
        else
        {
            ///Debug.LogError($"[SoundsManager] 音效加载失败：{path}");
        }
    }

    /// <summary>
    /// 音效播放完毕后自动清空 clip，释放回 SFX 池
    /// </summary>
    private IEnumerator ReleaseSfxSourceAfterPlay(AudioSource source)
    {
        if (source == null || source.clip == null) yield break;

        float clipLength = source.clip.length;
        yield return new WaitForSeconds(clipLength);

        // 确保仍在播放且 clip 未被清空
        if (source != null && source.clip != null)
        {
            source.clip = null;
        }
    }

    /// <summary>
    /// 停止所有音效播放
    /// </summary>
    public void StopSfx()
    {
        if (_sfxSources != null)
        {
            foreach (var sfxSource in _sfxSources)
            {
                if (sfxSource != null && sfxSource.isPlaying)
                {
                    sfxSource.Stop();
                }
            }
        }
    }

    /// <summary>
    /// 所有音效渐出并停止（含 SFX 池、UI 音效、打字音效）
    /// </summary>
    /// <param name="duration">渐出时长（秒），默认使用 _fadeDuration</param>
    public void FadeOutSfx(float? duration = null)
    {
        float fadeTime = duration ?? _fadeDuration;
        if (_sfxFadeOutCoroutine != null)
            StopCoroutine(_sfxFadeOutCoroutine);
        _sfxFadeOutCoroutine = StartCoroutine(FadeOutSfxCoroutine(fadeTime));
    }

    private IEnumerator FadeOutSfxCoroutine(float duration)
    {
        float startVolume = _isMuted ? 0f : _sfxVolume * _masterVolume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float vol = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            if (_sfxSources != null)
                foreach (var s in _sfxSources)
                    if (s != null) s.volume = vol;
            if (_uiSfxSource != null) _uiSfxSource.volume = vol;
            if (_typingSfxSource != null) _typingSfxSource.volume = vol;
            yield return null;
        }

        StopSfx();
        if (_uiSfxSource != null) _uiSfxSource.Stop();
        if (_typingSfxSource != null) _typingSfxSource.Stop();

        // 恢复音量，为下次播放做准备
        ApplyVolumeToSfxSources();
        _sfxFadeOutCoroutine = null;
    }

    /// <summary>
    /// 设置所有音效音量
    /// </summary>
    /// <param name="volume">音量值（0-1）</param>
    public void SetSfxVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
        if (_sfxSources != null)
        {
            ApplyVolumeToSfxSources();
        }
    }

    /// <summary>
    /// 调整音效音量（相对增减）
    /// </summary>
    /// <param name="delta">音量变化量（正数增加，负数减少）</param>
    public void AdjustSfxVolume(float delta)
    {
        SetSfxVolume(_sfxVolume + delta);
    }

    /// <summary>
    /// 应用音量到所有音效 AudioSource（考虑主音量和静音状态）
    /// </summary>
    private void ApplyVolumeToSfxSources()
    {
        if (_sfxSources != null)
        {
            float finalVolume = _isMuted ? 0f : _sfxVolume * _masterVolume;
            foreach (var sfxSource in _sfxSources)
            {
                if (sfxSource != null)
                {
                    sfxSource.volume = finalVolume;
                }
            }
        }
        if (_uiSfxSource != null)
        {
            _uiSfxSource.volume = _isMuted ? 0f : _sfxVolume * _masterVolume;
        }
        if (_typingSfxSource != null)
        {
            _typingSfxSource.volume = _isMuted ? 0f : _sfxVolume * _masterVolume;
        }
    }

    #endregion

    #region UI 音效控制

    /// <summary>
    /// 播放 UI 音效（使用独立的 UI 通道，不被对话音效挤占）
    /// </summary>
    private void PlayUiSfx(AudioClip clip, string path)
    {
        if (!_uiSfxEnabled || _uiSfxSource == null) return;
        AudioClip target = clip;
        if (target == null && !string.IsNullOrEmpty(path))
            target = LoadAudioClip(path);
        if (target == null)
        {
            Debug.LogWarning($"[SoundsManager] UI 音效未配置：clip 为空且 path \"{path}\" 无法加载。请在 SoundsManager 的 Inspector 中配置对应音效。");
            return;
        }
        _uiSfxSource.clip = target;
        _uiSfxSource.Play();
    }

    public void PlayClickSfx() => PlayUiSfx(_clickSfxClip, _clickSfxPath);
    public void PlayClickSfx(AudioClip clip) => PlayUiSfx(clip, _clickSfxPath);
    public void PlayConfirmSfx() => PlayUiSfx(_confirmSfxClip, _confirmSfxPath);
    public void PlayConfirmSfx(AudioClip clip) => PlayUiSfx(clip, _confirmSfxPath);
    public void PlayMapExpandSfx() => PlayUiSfx(_mapExpandSfxClip, _mapExpandSfxPath);
    public void PlayMapExpandSfx(AudioClip clip) => PlayUiSfx(clip, _mapExpandSfxPath);
    public void PlayHoverSfx() => PlayUiSfx(_hoverSfxClip, _hoverSfxPath);
    public void PlayHoverSfx(AudioClip clip) => PlayUiSfx(clip, _hoverSfxPath);

    /// <summary>
    /// 播放打字音效（专用通道，不占 SFX 池）
    /// </summary>
    public void PlayTypingSfx(string path)
    {
        if (!isInit || _typingSfxSource == null) return;
        _typingSfxSource.clip = LoadAudioClip(path);
        if (_typingSfxSource.clip != null) _typingSfxSource.Play();
    }

    /// <summary>
    /// 直接用 AudioClip 播放音效（复用 SFX 池）
    /// </summary>
    private void PlayClipSfx(AudioClip clip)
    {
        if (!isInit || clip == null) return;
        AudioSource sfxSource = GetFreeSfxSource();
        if (sfxSource == null) return;
        sfxSource.clip = clip;
        sfxSource.Play();
        StartCoroutine(ReleaseSfxSourceAfterPlay(sfxSource));
    }

    public void SetClickSfx(AudioClip clip) => _clickSfxClip = clip;
    public void SetConfirmSfx(AudioClip clip) => _confirmSfxClip = clip;
    public void SetMapExpandSfx(AudioClip clip) => _mapExpandSfxClip = clip;
    public void SetHoverSfx(AudioClip clip) => _hoverSfxClip = clip;

    public void SetClickSfxPath(string path) => _clickSfxPath = path;
    public void SetConfirmSfxPath(string path) => _confirmSfxPath = path;
    public void SetMapExpandSfxPath(string path) => _mapExpandSfxPath = path;
    public void SetHoverSfxPath(string path) => _hoverSfxPath = path;

    public void SetUiSfxEnabled(bool enabled) => _uiSfxEnabled = enabled;
    public bool IsUiSfxEnabled => _uiSfxEnabled;

    #endregion

    #region 主音量与静音控制

    /// <summary>
    /// 设置主音量
    /// </summary>
    /// <param name="volume">主音量值（0-1）</param>
    public void SetMasterVolume(float volume)
    {
        _masterVolume = Mathf.Clamp01(volume) * SettingManager.Instance.settings.MasterVolume;
        ApplyVolumeToMusicSource();
        ApplyVolumeToSfxSources();
    }

    private IEnumerator UpdateVo()
    {
        yield return null;
        _masterVolume = SettingManager.Instance.settings.MasterVolume;
        _musicVolume = SettingManager.Instance.settings.MusicVolume;
        _sfxVolume = SettingManager.Instance.settings.SFXVolume;
        ApplyVolumeToMusicSource();
        ApplyVolumeToSfxSources();
    }
    
    public void UpdateVolumes(float newValue)
    {
        StartCoroutine(UpdateVo());
    }
    
    /// <summary>
    /// 设置主音量（带渐变效果）
    /// </summary>
    /// <param name="volume">目标主音量值（0-1）</param>
    /// <param name="duration">渐变时长（秒）</param>
    public void SetMasterVolume(float volume, float duration)
    {
        // 停止正在进行的渐变协程
        if (_masterVolumeFadeCoroutine != null)
        {
            StopCoroutine(_masterVolumeFadeCoroutine);
        }

        // 启动新的渐变协程
        _masterVolumeFadeCoroutine = StartCoroutine(MasterVolumeFadeCoroutine(Mathf.Clamp01(volume), duration));
    }

    /// <summary>
    /// 调整主音量（相对增减）
    /// </summary>
    /// <param name="delta">音量变化量（正数增加，负数减少）</param>
    public void AdjustMasterVolume(float delta)
    {
        SetMasterVolume(_masterVolume + delta);
    }

    /// <summary>
    /// 静音/取消静音
    /// </summary>
    /// <param name="muted">true为静音，false为取消静音</param>
    public void SetMute(bool muted)
    {
        if (_isMuted == muted) return; // 状态未改变

        _isMuted = muted;
        
        if (_isMuted)
        {
            // 静音时保存当前主音量
            _volumeBeforeMute = _masterVolume;
        }
        else
        {
            // 取消静音时恢复主音量
            _masterVolume = _volumeBeforeMute;
        }

        ApplyVolumeToMusicSource();
        ApplyVolumeToSfxSources();
    }

    /// <summary>
    /// 切换静音状态
    /// </summary>
    public void ToggleMute()
    {
        SetMute(!_isMuted);
    }

    /// <summary>
    /// 重置所有音量为默认值
    /// </summary>
    public void ResetAllVolumes()
    {
        _masterVolume = 1f;
        _musicVolume = 1f;
        _sfxVolume = 1f;
        _isMuted = false;
        
        ApplyVolumeToMusicSource();
        ApplyVolumeToSfxSources();
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 停止所有正在进行的淡入淡出协程
    /// </summary>
    private void StopAllFadeCoroutines()
    {
        if (_fadeInCoroutine != null)
        {
            StopCoroutine(_fadeInCoroutine);
            _fadeInCoroutine = null;
        }

        if (_fadeOutCoroutine != null)
        {
            StopCoroutine(_fadeOutCoroutine);
            _fadeOutCoroutine = null;
        }
    }

    /// <summary>
    /// 从 Resources 加载音频文件（自动尝试多种扩展名）
    /// </summary>
    /// <param name="path">Resources 文件夹下的相对路径（不含扩展名）</param>
    /// <returns>AudioClip，加载失败返回 null</returns>
    private AudioClip LoadAudioClip(string path)
    {
        // 尝试直接加载（可能已包含扩展名或无扩展名）
        AudioClip clip = Resources.Load<AudioClip>(path);
        if (clip != null)
        {
            return clip;
        }

        // 尝试常见音频扩展名
        string[] extensions = { ".mp3", ".wav", ".ogg", ".aiff" };
        foreach (string ext in extensions)
        {
            string pathWithExt = path.EndsWith(ext) ? path : path + ext;
            // 去掉扩展名再加载（Resources.Load 不需要扩展名）
            string pathWithoutExt = pathWithExt.Substring(0, pathWithExt.LastIndexOf('.'));
            clip = Resources.Load<AudioClip>(pathWithoutExt);
            if (clip != null)
            {
                return clip;
            }
        }

        return null;
    }

    private IEnumerator FadeOutCoroutine(AudioSource source, float duration)
    {
        // 从当前实际音量开始渐弱（考虑静音状态）
        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            source.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        // 确保最终音量为0并停止
        source.volume = 0f;
        source.Stop();
        
        // 清空协程引用
        _fadeOutCoroutine = null;
        
        // 恢复音量设置（为下次播放做准备）
        ApplyVolumeToMusicSource();
    }
    
    private IEnumerator FadeOutCoroutine(AudioSource source, float duration, float endVolume)
    {
        // 从当前实际音量开始渐弱（考虑静音状态）
        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            source.volume = Mathf.Lerp(startVolume, endVolume, t);
            yield return null;
        }

        // 确保最终音量为0并停止
        source.volume = endVolume;
        source.Stop();
        
        // 清空协程引用
        _fadeOutCoroutine = null;
        
        // 恢复音量设置（为下次播放做准备）
        ApplyVolumeToMusicSource();
    }

    private IEnumerator FadeInCoroutine(AudioSource source, float duration, float startVolume)
    {
        // 从当前音量（通常为0）渐强到目标音量
        //float startVolume = source.volume;
        float targetVolume = _isMuted ? 0f : _musicVolume * _masterVolume;
        float elapsed = 0f;

        // 如果目标音量为0（静音状态），不执行渐强
        if (targetVolume <= 0.001f)
        {
            _fadeInCoroutine = null;
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            source.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        // 确保最终音量准确
        source.volume = targetVolume;

        // 清空协程引用
        _fadeInCoroutine = null;
    }

    /// <summary>
    /// 主音量渐变协程
    /// </summary>
    /// <param name="targetVolume">目标音量</param>
    /// <param name="duration">渐变时长</param>
    private IEnumerator MasterVolumeFadeCoroutine(float targetVolume, float duration)
    {
        float startVolume = _masterVolume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            _masterVolume = Mathf.Lerp(startVolume, targetVolume, t);
            ApplyVolumeToMusicSource();
            ApplyVolumeToSfxSources();
            yield return null;
        }

        // 确保最终音量准确
        _masterVolume = targetVolume;
        ApplyVolumeToMusicSource();
        ApplyVolumeToSfxSources();

        // 清空协程引用
        _masterVolumeFadeCoroutine = null;
    }

    #endregion
}
