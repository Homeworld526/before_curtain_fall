using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class MainMenuVideoControl : MonoBehaviour
{
    [Tooltip("循环起始时间点")]
    public float loopTime = 0f;

    private VideoPlayer videoPlayer;
    private bool hasReachedLoopPoint = false;

    void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        
        if (videoPlayer == null)
        {
            Debug.LogError("MainMenuVideoControl: 未找到 VideoPlayer 组件!");
            return;
        }

        // 确保视频设置为播放模式
        videoPlayer.playOnAwake = true;
        
        // 监听播放完成事件
        videoPlayer.loopPointReached += OnLoopPointReached;
    }

    void Update()
    {
        if (videoPlayer == null || !videoPlayer.isPlaying)
            return;

        // 检测是否已经播放到 loopTime
        if (!hasReachedLoopPoint && videoPlayer.time >= loopTime)
        {
            hasReachedLoopPoint = true;
        }

        // 如果已经进入循环模式，检测是否到达视频结尾
        if (hasReachedLoopPoint && videoPlayer.time >= videoPlayer.length - 0.1f)
        {
            // 跳转回 loopTime 继续播放
            //Debug.Log(1);
            videoPlayer.time = loopTime;
            videoPlayer.Play();
        }
    }

    private void OnLoopPointReached(VideoPlayer vp)
    {
        // 如果启用了循环模式，重置状态
        hasReachedLoopPoint = false;
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnLoopPointReached;
        }
    }
}
