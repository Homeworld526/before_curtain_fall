using UnityEngine;
using UnityEngine.Video;

public class VideoLoopSwitcher : MonoBehaviour
{
    public VideoPlayer playerA;
    public VideoPlayer playerB;
    public VideoClip mainVideo;
    public VideoClip loopVideo;

    void Awake()
    {
        if (playerA == null)
            playerA = GetComponent<VideoPlayer>();

        mainVideo = playerA.clip;
        loopVideo = playerB.clip;

        if (playerA.targetTexture != null)
            playerB.targetTexture = playerA.targetTexture;

        playerB.Prepare();
        playerB.prepareCompleted += OnBPrepared;
    }

    void Start()
    {
        playerA.loopPointReached += OnMainVideoFinished;

        if (!playerA.isPlaying)
            playerA.Play();
    }

    private void OnBPrepared(VideoPlayer vp)
    {
        Debug.Log("循环视频预加载完成，随时可以无缝切换");
        playerB.prepareCompleted -= OnBPrepared;
    }

    private void OnMainVideoFinished(VideoPlayer vp)
    {
        playerA.Stop();
        playerB.Play();

        playerA.loopPointReached -= OnMainVideoFinished;

        Debug.Log("开场视频播放完毕，无缝切换到循环视频");
    }

    void OnDestroy()
    {
        if (playerA != null)
            playerA.loopPointReached -= OnMainVideoFinished;
        if (playerB != null)
            playerB.prepareCompleted -= OnBPrepared;
    }
}