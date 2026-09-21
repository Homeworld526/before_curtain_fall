using UnityEngine;
using System.Collections;

public interface IEffect
{
    bool IsRunning();
    void StartEffect(string param);
    void PauseEffect();
    void ContinueEffect();
    void ResetEffect();
}

public class UITimedParallax : SingleCase<UITimedParallax>, IEffect
{
    private RectTransform foreground
    {
        get
        {
            return VisualLocator.Instance.foreGround;
        }
    }       // 前景（可空）
    private RectTransform middleGround
    {
        get { return VisualLocator.Instance.middleGround; }
    } // 中景父物体（可空）
    private RectTransform background
    {
        get { return VisualLocator.Instance.backGround; }
    }   // 背景父物体（可空）
    public Vector3 MidOffSet;
    public Vector3 ForeOffSet;
    [Header("视差比例")]
    public float bgRatio = 2f;
    public float midRatio = 1f;
    public float frontRatio = 1f;
    Vector3 midInitPos = new Vector3();
    Vector3 bgInitPos = new Vector3();
    Vector3 frInitPos = new Vector3();

    public bool EnableEndScaling;
    public bool debug;

    private Coroutine _coroutine;
    private bool _isCoroutinePaused;


    public void SetInit()
    {
        // 【修改1】判空后再赋值初始位置
        if (middleGround != null) if (middleGround.GetComponent<PicSwaper>() != null)
            midInitPos = middleGround.GetComponent<PicSwaper>().InitPos;
        if (background != null) if (background.GetComponent<PicSwaper>() != null)
            bgInitPos = background.GetComponent<PicSwaper>().InitPos;
        if (foreground != null) if (foreground.GetComponent<PicSwaper>() != null)
            frInitPos = foreground.GetComponent<PicSwaper>().InitPos;
        Debug.Log(middleGround.name + "初始位置" + midInitPos);
    }

    private void Update()
    {
        if (debug)
        {
            ParallaxMove(-1080f, 7f);
            debug = false;
        }
    }

    public IEnumerator ResetTransform(float delay)
    {
        yield return new WaitForSeconds(delay);
        // 【修改2】重置位置和缩放时判空
        if (middleGround != null)
        {
            middleGround.localPosition = midInitPos;
            middleGround.localScale = Vector3.one;
        }
        if (background != null)
        {
            background.localPosition = bgInitPos;
            background.localScale = Vector3.one;
        }
        if (foreground != null)
        {
            foreground.localPosition = frInitPos;
            foreground.localScale = Vector3.one;
        }
    }

    // 核心：指定时间+位移 视差移动（自动限幅）
    public void ParallaxMove(float targetMoveX, float duration)
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
            _coroutine = null;
            StartCoroutine(ResetTransform(0f));
        }
        _coroutine = StartCoroutine(DoTimedParallax(targetMoveX, duration));
    }

    // 循环往返移动（自动限幅，持续镜头转动）
    public void ParallaxLoop(float targetMoveX, float duration)
    {
        StopCoroutine(LoopTimedParallax(targetMoveX, duration));
        StartCoroutine(LoopTimedParallax(targetMoveX, duration));
    }

    // 协程：定时移动+缓动+边界限制核心逻辑
    private IEnumerator DoTimedParallax(float targetMoveX, float duration)
    {
        yield return new WaitForSeconds(0.1f);
        
        float elapsedTime = 0f;
        // 【修改3】协程内初始化位置时判空
        Vector3 midCurrentInit = middleGround != null ? middleGround.localPosition : Vector3.zero;
        Vector3 bgCurrentInit = background != null ? background.localPosition : Vector3.zero;
        Vector3 frCurrentInit = foreground != null ? foreground.localPosition : Vector3.zero;

        // 1. 计算中景/背景 最大可移动幅度（根据RectTransform尺寸）
        float bgMaxMove = GetMaxMoveX(background);
        float midMaxMove = middleGround != null ? (GetMaxMoveX(middleGround) + MidOffSet.x / 2) : 0;
        float frMaxMove = foreground != null ? (GetMaxMoveX(foreground) + ForeOffSet.x / 2) : 0;
        //Debug.Log("计算位移" + GetMaxMoveX(background)+" " + GetMaxMoveX(middleGround)+" " + GetMaxMoveX(foreground));
        // 2. 按视差比例算目标位移，再钳位（不超最大幅度）
        float midTargetMove = -64f;//Mathf.Clamp(targetMoveX * midRatio, -midMaxMove, midMaxMove);
        float bgTargetMove = -50f;//Mathf.Clamp(targetMoveX * bgRatio, -bgMaxMove, bgMaxMove);
        float frTargetMove = -104.96f;//Mathf.Clamp(targetMoveX * frontRatio, -frMaxMove, frMaxMove);
        Debug.Log("实际位移" + bgTargetMove.ToString() + midTargetMove.ToString() + frTargetMove.ToString());

        // 3. 缓动平移（启停平滑，模拟镜头转动）
        while (elapsedTime < duration)
        {
            while (_isCoroutinePaused)
            {
                yield return null;
            }

            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsedTime / duration);

            // 【修改4】更新位置时判空，避免空引用
            if (middleGround != null)
                middleGround.localPosition = new Vector3(midCurrentInit.x + midTargetMove * t, middleGround.localPosition.y, 0);
            if (background != null)
                background.localPosition = new Vector3(bgCurrentInit.x + bgTargetMove * t, background.localPosition.y, 0);
            if (foreground != null)
                foreground.localPosition = new Vector3(frCurrentInit.x + frTargetMove * t, foreground.localPosition.y, 0);
            yield return null;
        }

        // 最终精准到位（强制贴边界内）
        if (middleGround != null)
            middleGround.localPosition = new Vector3(midCurrentInit.x + midTargetMove, middleGround.localPosition.y, 0);
        if (background != null)
            background.localPosition = new Vector3(bgCurrentInit.x + bgTargetMove, background.localPosition.y, 0);
        if (foreground != null)
            foreground.localPosition = new Vector3(frCurrentInit.x + frTargetMove, foreground.localPosition.y, 0);

        if (EnableEndScaling)
            yield return EndScale(duration * 3, 1.01f);
    }

    private IEnumerator EndScale(float time, float targetScale)
    {
        float elapsedTime = 0;
        float scalingSpeed = targetScale;
        float foreSpeed = scalingSpeed * frontRatio - 1;
        float midSpeed = scalingSpeed * midRatio - 1;
        float bgSpeed = scalingSpeed * bgRatio - 1;

        while (elapsedTime < time)
        {
            while (_isCoroutinePaused)
            {
                yield return null;
            }

            float t = Mathf.SmoothStep(0, 1, elapsedTime / time);

            // 【修改5】缩放时判空
            if (foreground != null)
                foreground.localScale = new Vector3(1 + foreSpeed * t, 1 + foreSpeed * t, 1 + foreSpeed * t);
            if (middleGround != null)
                middleGround.localScale = new Vector3(1 + midSpeed * t, 1 + midSpeed * t, 1 + midSpeed * t);
            if (background != null)
                background.localScale = new Vector3(1 + bgSpeed * t, 1 + bgSpeed * t, 1 + bgSpeed * t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    // 循环逻辑（往返移动，始终不超界）
    private IEnumerator LoopTimedParallax(float targetMoveX, float duration)
    {
        while (true)
        {
            yield return StartCoroutine(DoTimedParallax(targetMoveX, duration));
            yield return StartCoroutine(DoTimedParallax(-targetMoveX, duration));
        }
    }

    // 【修改6】增强GetMaxMoveX方法，增加RectTransform判空
    private float GetMaxMoveX(RectTransform rt)
    {
        // 如果rt为空，直接返回0，避免空引用
        if (rt == null)
            return 0;

        // 计算逻辑：(层级宽度 - 屏幕宽度)/2 → 左右各能移一半，刚好不露空
        float rtWidth = rt.rect.width * rt.localScale.x;
        float screenWidth = Screen.width;
        return Mathf.Max(0, (rtWidth - screenWidth) / 2);
    }

    // 可选：编辑器实时看最大可移动幅度（不用运行）
    /*private void OnValidate()
    {
        // 【修改7】打印日志时判空
        if (middleGround != null)
            Debug.Log("中景最大移幅：" + GetMaxMoveX(middleGround));
        if (background != null)
            Debug.Log("背景最大移幅：" + GetMaxMoveX(background));
    }*/

    public void ContinueEffect()
    {
        _isCoroutinePaused = false;
    }
    public void ResetEffect()
    {
        Debug.Log("ResetEffect");
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = null;
        StartCoroutine(ResetTransform(0.5f));
    }
    
    public void StartEffect(string param)
    {
        if (param == "右移")
        {
            Debug.Log("右移");
            if (_coroutine == null) SetInit();
            ParallaxMove(-1080f, 7f);
        }
        if (param == "左移")
        {
            ParallaxMove(1080f, 7f);
        }
        if (param == "暂停")
        {
            //Debug.Log("暂停");
            //Debug.Log("暂停");
            PauseEffect();
        }
        if (param == "重开")
        {
            ContinueEffect();
        }
        if (param == "重置")
        {
            ResetEffect();
        }
    }
    public void PauseEffect()
    {
        if (IsRunning())
        {
            _isCoroutinePaused = true;
        }
    }

    public bool IsRunning()
    {
        return _coroutine != null;
    }
}
