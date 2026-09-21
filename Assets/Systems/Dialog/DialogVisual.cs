using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using Microsoft.International.Converters.TraditionalChineseToSimplifiedConverter;
using System.Runtime.InteropServices.WindowsRuntime;
using Object = UnityEngine.Object;

public class DialogVisual : SingleCase<DialogVisual>
{
    [SerializeField] private Ending ending;
    
    public Action<DialogNode> ContentTracker;

    private static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

    private void OnDisable()
    {
        CG.sprite = LoadSprite("黑屏");
        diaName.text = "";
        text.text = "";
    }

    public static Sprite LoadSprite(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (_spriteCache.TryGetValue(path, out var cached)) return cached;
        var sprite = Resources.Load<Sprite>(path);
        if (sprite != null) _spriteCache[path] = sprite;
        return sprite;
    }

    private IEnumerator LoadSpriteAsync(string path, Action<Sprite> onLoaded)
    {
        if (string.IsNullOrEmpty(path)) { onLoaded?.Invoke(null); yield break; }
        if (_spriteCache.TryGetValue(path, out var cached)) { onLoaded?.Invoke(cached); yield break; }
        var request = Resources.LoadAsync<Sprite>(path);
        yield return request;
        var sprite = request.asset as Sprite;
        if (sprite != null) _spriteCache[path] = sprite;
        onLoaded?.Invoke(sprite);
    }

    public static void ClearSpriteCache()
    {
        /*var usedSprites = new HashSet<Sprite>();
        foreach (var img in Object.FindObjectsOfType<Image>())
        {
            if (img.sprite != null) usedSprites.Add(img.sprite);
        }

        var keysToRemove = new List<string>();
        foreach (var kv in _spriteCache)
        {
            if (!usedSprites.Contains(kv.Value))
                keysToRemove.Add(kv.Key);
        }
        foreach (var key in keysToRemove)
            _spriteCache.Remove(key);*/
        
        _spriteCache.Clear();

        Resources.UnloadUnusedAssets();
    }

    private DialogTree tree = new DialogTree();

    public bool enableFill = false;
    
    public int startLine { get; private set; }

    #region Gets

    public DialogType GetType()
    {
        return tree.ShowDialog().links.Count > 1 ? DialogType.Choice : DialogType.Normal;
    } 
    
    public string GetCG()
    {
        return tree.ShowDialog().cg == "空"? "" : tree.ShowDialog().cg;
    }
    
    public string GetContent()
    {
        return tree.ShowDialog().content;
    }
    
    public string GetBackGround()
    {
        return GetScenePic(tree.ShowDialog().scene);
    }

    public string GetNextBackGround()
    {
        if (tree.ShowDialog().links.Count < 1)
        {
            return "对话背景-小平房间";
        }
        return GetScenePic(tree.ShowNextDialog().scene);
    }

    public string GetMusic()
    {
        return tree.ShowDialog().music;
    }
    
    public int currentIndex
    {
        get { return tree.ShowDialog().index; }

    }

    #endregion
    
    public Action OnTextDisplayEnd;
    public Action OnTextDisplayDelay;
    public Action OnTextDisplayStart;

    public TextAsset dialogFile;
    public string previewFileName;
    public int startIndex;
    public string ProtagonistName;

    private Coroutine TextPrintCor;
    private Coroutine _picProcessCor;
    private Coroutine _soundProcessCor;
    public float TextPrintInterval
    {
        get => SettingManager.Instance.settings.PrintSpeed;
    }
    public float TextPrintDuration = 0.05f;

    public bool PrintOnHold = false;

    private string unclosedTagCache = "";
    private readonly List<string> _tagStack = new List<string>();

    #region PrintTool

    private int CollectFullRichTag(string str, int index)
        {
            int tagStartIndex = index;
            while (index < str.Length && str[index] != '>')
            {
                index++;
                if (index >= str.Length) break;
            }
            
            if (index < str.Length && str[index] == '>')
            {
                string fullTag = str.Substring(tagStartIndex, index - tagStartIndex + 1);
                if (fullTag.Length > 1 && fullTag[1] != '/')
                {
                    _tagStack.Add(fullTag);
                }
                else if (_tagStack.Count > 0)
                {
                    _tagStack.RemoveAt(_tagStack.Count - 1);
                }
                unclosedTagCache = string.Concat(_tagStack);
            }

            if(index + 1 < str.Length && str[index + 1] == '<')
            {
                return CollectFullRichTag(str, index + 1);
            }
            
            return index;
        }
    
    public IEnumerator PrintText(TextMeshProUGUI text, string str, float interval)
        {
            OnTextDisplayStart.Invoke();
    
            int i = 0;
            //string currentString;
            bool isTyping;


            if (!str.StartsWith(text.text))
            {
                text.text = "";
                i = 0;
            }
            else
            {
                i = text.text.Length;
            }

            //Start
            //currentString = "";
            isTyping = true;
            //inRichTag = false;
            //unclosedTagCache = "";
    
            while (i < str.Length)
            {
                while (PrintOnHold)
                {
                    yield return null;
                }
    
                if (!isTyping)
                {
                    continue;
                }

                char currentChar = str[i]; //== '^'? '\n' : str[i];
                if (currentChar == '<')
                {
                    //inRichTag = true;
                    i = CollectFullRichTag(str, i) + 1;
                }
    
                text.text = str.Substring(0, i++);
                float alpha = 0f;
                SetCharacterAlpha(text, i - 2, alpha);
                if(tree.ShowDialog().logger_name == "旁白" && !ending.IsRunning()) SoundsManager.Instance.PlayTypingSfx("文字音效");
                while (alpha < 1f)
                {
                    alpha = Mathf.Clamp01(alpha + (1 / interval) * Time.deltaTime);
                    SetCharacterAlpha(text, i - 2, alpha);
                    yield return null;
                }
    
                //yield return new WaitForSeconds(interval);
            }
    
            text.text = str;
    
    
    
            TextPrintCor = null;
    
    
            OnTextDisplayEnd.Invoke();
            yield return new WaitForSeconds(SettingManager.Instance.settings.AutoPlaySpeed);
            OnTextDisplayDelay?.Invoke();
    
        }
    
    void SetCharacterAlpha(TextMeshProUGUI tmpText, int charIndex, float alpha)
       {
           // 安全校验：字符索引需在有效范围内
           if (charIndex < 0 || charIndex >= tmpText.textInfo.characterCount)
               return;
   
           TMP_TextInfo textInfo = tmpText.textInfo;
           TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
           int materialIndex = charInfo.materialReferenceIndex; // 字符对应的材质索引
           Color32[] vertexColors = textInfo.meshInfo[materialIndex].colors32; // 顶点颜色数组
   
           // 每个字符由4个顶点组成，需同时修改这4个顶点的Alpha值
           int vertexIndex = charInfo.vertexIndex;
           byte alphaByte = (byte)(alpha * 255); // 转换为0-255的字节值
           vertexColors[vertexIndex + 0].a = alphaByte;
           vertexColors[vertexIndex + 1].a = alphaByte;
           vertexColors[vertexIndex + 2].a = alphaByte;
           vertexColors[vertexIndex + 3].a = alphaByte;
   
           // 标记顶点数据需要更新，让TextMeshPro刷新显示
           tmpText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
       }

    public void ClearPrint()
    {
        if (TextPrintCor != null)
        {
            StopCoroutine(TextPrintCor);
            TextPrintCor = null;
            
            
            //OnTextDisplayStart.Invoke();
            
            OnTextDisplayDelay?.Invoke();
        }
        OnTextDisplayStart.Invoke();
        text.text = "";
    }
    #endregion    
    
    public float PicChangeInterval = 0.25f;
    public float PicRDoubleTime = 0.3f;
    public float PicRDoubleDistance = 200f;
    public Image PicRAlt { get; private set; }
    private Image _picRClone;
    private Vector3 picROriginalLocalPos;
    private Coroutine doublePicRCor;

    public Image Background;
    public Image Head;

    private Image PicL
    {
        get { return VisualLocator.Instance.foreGround.GetComponent<Image>(); }
    }
    private Image PicR
    {
        get { return VisualLocator.Instance.middleGround.GetComponent<Image>(); }
        set { VisualLocator.Instance.middleGround = value.GetComponent<RectTransform>(); }
    }

    public Image CG;
    
    public TextMeshProUGUI diaName;
    public TextMeshProUGUI text;
    public List<VerticalLayoutGroup> choiceGroup;
    public GameObject choicePrefab;

    public List<Button> choiceList = new List<Button>();

    public bool ForceNoClickSkip = false;
    public ReadDialogHistorySO ReadDialogHistory;
    //public DialogList.DialogAction endAction;

    protected override void Awake()
    {
        base.Awake();
        if (!IsPrimaryInstance) return;

        if (ReadDialogHistory == null)
        {
            ReadDialogHistory = ScriptableObject.CreateInstance<ReadDialogHistorySO>();
        }
    }

    public bool monitoringMouse;
    public DialogType currentType;
    public int clickDelay = 0;

    private bool Loaading = false;
    
    public void DialogStart()
    {
        StopAllCoroutines();
        skipRoutine = null;
        LoadRoutine = null;
        TextPrintCor = null;
        _picProcessCor = null;
        _soundProcessCor = null;
        _endingDialog = false;
        Loaading = true;
        monitoringMouse = false;
        previewFileName = null;
        currentType = DialogType.End;
        if (dialogFile != null)
        {
            tree.BuildTree(dialogFile);
        }


        StartDialog(startIndex);
    }

    public void DialogStart(int skipTo)
    {
        StopAllCoroutines();
        _endingDialog = false;
        Loaading = true;
        monitoringMouse = false;
        previewFileName = null;
        currentType = DialogType.End;
        if (dialogFile != null)
        {
            tree.BuildTree(dialogFile);
        }
        //Debug.LogWarning("V重载"+startIndex);
        StartDialog(startIndex);
        StartCoroutine(SkipUntil(skipTo));
    }

    private void SoundProcess()
    {
        DialogNode tmp = tree.ShowDialog();

        if (tmp.music == "空")
        {
            SoundsManager.Instance.PauseMusic();
            StartCoroutine(PlaySfxNextFrame(tmp.sfx));
            return;
        }

        if (tmp.music == "")
        {
            StartCoroutine(PlaySfxNextFrame(tmp.sfx));
            return;
        }

        var clip = Resources.Load<AudioClip>("Music/" + tmp.music);

        if (clip != null)
        {
            if (SoundsManager.Instance.IsFadingIn)
            {
                // 淡入中：只换 clip，保持淡入协程继续
                SoundsManager.Instance.SetMusicFile(clip);
            }
            else if (SoundsManager.Instance.IsFadingOut || SoundsManager.Instance.IsMusicFadedOut)
            {
                // 淡出进行中或已完成：不重启，只预设 clip 供下次主动播放使用
                SoundsManager.Instance.SetMusicFile(clip);
            }
            else
            {
                SoundsManager.Instance.PlayMusic(clip);
            }
            Loaading = false;
        }
        else
        {
            Debug.LogError("无法加载音乐" + tmp.music);
        }

        StartCoroutine(PlaySfxNextFrame(tmp.sfx));
        _soundProcessCor = null;
    }

    private IEnumerator PlaySfxNextFrame(string sfx)
    {
        yield return null;
        var cell = sfx.Split('+');
        if (cell.Length > 0) foreach (string s in cell)
        {
            SoundsManager.Instance.PlaySfx("SFX/" + s);
        }
    }

    private string GetScenePic(string name)
    {
        if (name.StartsWith("场景"))
        {
            return name;
        }
        if (name.StartsWith("对话背景"))
        {
            return name;
        }
        else
        {
            return "对话背景-" + name;
        }
    }
    
    private void UpdateHeadVisibility(DialogNode tmp)
    {
        if (tmp.illustration != "空")
        {
            if (Head.color.a == 0f) Head.GetComponent<PicSwaper>()?.PicShow();
        }
        else
        {
            if (Head.color.a == 1f) Head.GetComponent<PicSwaper>()?.PicHide();
        }
    }

    private void DoublePicR(bool enable, string sprite)
    {
        if (doublePicRCor != null) StopCoroutine(doublePicRCor);
        doublePicRCor = StartCoroutine(DoublePicRRoutine(enable, sprite));
    }

    private IEnumerator DoublePicRRoutine(bool enable, string sprite)
    {
        ForceNoClickSkip = true;
        var picR = PicR;
        if (enable)
        {
            picROriginalLocalPos = picR.transform.localPosition;
            var clone = Instantiate(picR.gameObject, picR.transform.parent);
            clone.transform.localPosition = picROriginalLocalPos;
            PicRAlt = clone.GetComponent<Image>();
            _picRClone = PicRAlt;
            
            var targetPos = picROriginalLocalPos + Vector3.right * PicRDoubleDistance;
            var targetPosAlt = picROriginalLocalPos + Vector3.left * PicRDoubleDistance;
            float t = 0f;
            string picRSpriteBaseName = picR.sprite != null ? picR.sprite.name.Split('-')[0] : "";
            if (!string.IsNullOrEmpty(picRSpriteBaseName) && sprite.Split("+")[1].Split('-')[0] == picRSpriteBaseName)
            {
                Color c = picR.color;
                c.a = 0;
                picR.color = c;
                picR.transform.localPosition = targetPos;
                while (t < 1f)
                {
                    t = Mathf.Clamp01(t + Time.deltaTime / PicRDoubleTime);
                    PicRAlt.transform.localPosition = Vector3.Lerp(picROriginalLocalPos, targetPosAlt, t);
                
                    yield return null;
                
                }
                yield return StartCoroutine(LoadSpriteAsync(sprite.Split("+")[0], s => { if (s != null) picR.sprite = s; }));
                picR.GetComponent<PicSwaper>()?.PicShow();
            }
            else
            {
                Color c = PicRAlt.color;
                c.a = 0;
                PicRAlt.color = c;
                PicRAlt.transform.localPosition = targetPosAlt;

                // 在移动动画期间并行预加载两张 sprite，动画结束后同步显示
                Sprite preloadedAlt = null;
                Sprite preloadedR = null;
                yield return StartCoroutine(LoadSpriteAsync(sprite.Split("+")[1], s => preloadedAlt = s));
                yield return StartCoroutine(LoadSpriteAsync(sprite.Split("+")[0], s => preloadedR = s));

                while (t < 1f)
                {
                    t = Mathf.Clamp01(t + Time.deltaTime / PicRDoubleTime);
                    picR.transform.localPosition = Vector3.Lerp(picROriginalLocalPos, targetPos, t);
                    yield return null;
                }

                if (preloadedAlt != null) PicRAlt.sprite = preloadedAlt;
                PicRAlt.GetComponent<PicSwaper>().PicShow();

                if (preloadedR != null)
                {
                    if (picR.color.a == 0f)
                    {
                        picR.sprite = preloadedR;
                        picR.GetComponent<PicSwaper>()?.PicShow();
                    }
                    else
                    {
                        picR.GetComponent<PicSwaper>()?.PicChange(preloadedR);
                    }
                }
            }

        }
        else
        {
            Image slideTarget = picR;
            if (PicRAlt.sprite != null) if (sprite.Split('-')[0] == PicRAlt.sprite.name.Split('-')[0])
            {
                slideTarget = PicRAlt;
            }

            if (picR == slideTarget)
            {
                _picRClone.GetComponent<PicSwaper>().PicHide();
            }else{
                picR.GetComponent<PicSwaper>().PicHide();
            }

            float t = 0f;
            var startPos = slideTarget.transform.localPosition;
            while (t < 1f)
            {
                t = Mathf.Clamp01(t + Time.deltaTime / PicRDoubleTime);
                slideTarget.transform.localPosition = Vector3.Lerp(startPos, picROriginalLocalPos, t);
                yield return null;
            }
            slideTarget.transform.localPosition = picROriginalLocalPos;

            if (slideTarget != picR)
            {
                picR.transform.localPosition = slideTarget.transform.localPosition;
                picR.sprite = slideTarget.sprite;
                picR.color = slideTarget.color;
            }

            Destroy(_picRClone.gameObject);
            _picRClone = null;
            PicRAlt = null;
        }
        ForceNoClickSkip = false;
        doublePicRCor = null;
    }

    private IEnumerator DoublePicRHideAllRoutine(bool instant = false)
    {
        var picR = PicR;
        var clone = _picRClone;
        var alt = PicRAlt;

        // 立即清空成员，后续帧不再误判为双图状态
        _picRClone = null;
        PicRAlt = null;

        alt.GetComponent<PicSwaper>()?.PicHide();
        if (picR.color.a > 0f) picR.GetComponent<PicSwaper>()?.PicHide();

        if (!instant)
        {
            ForceNoClickSkip = true;
            yield return new WaitForSeconds(PicChangeInterval);
            ForceNoClickSkip = false;
        }

        picR.transform.localPosition = picROriginalLocalPos;
        Destroy(clone.gameObject);
        doublePicRCor = null;
    }

private IEnumerator UpdateOpponentPicAsync(DialogNode tmp, bool instant = false)
    {
        if (tmp.illustration_opponent == "空" || tmp.illustration_opponent == "")
        {
            PicR.color = new Color(1f, 1f, 1f, PicR.color.a);
            if (PicRAlt != null)
            {
                if (doublePicRCor != null) { StopCoroutine(doublePicRCor); doublePicRCor = null; }
                doublePicRCor = StartCoroutine(DoublePicRHideAllRoutine(instant));
            }
            else if (PicR.color.a == 1f)
            {
                PicR.GetComponent<PicSwaper>()?.PicHide();
            }
            yield break;
        }

        if (tmp.illustration_opponent.Contains("+"))
        {
            string[] sprites = tmp.illustration_opponent.Split('+');
            if (PicRAlt == null)
            {
                DoublePicR(true, tmp.illustration_opponent);
                yield return doublePicRCor;
                yield break;
            }
            else
            {
                if (sprites[0].Contains(tmp.logger_name) && !sprites[1].Contains(tmp.logger_name))
                    PicRAlt.color = new Color(.75f, .75f, .75f, PicRAlt.color.a);
                else
                    PicRAlt.color = new Color(1f, 1f, 1f, PicRAlt.color.a);

                Sprite altSprite = null;
                yield return StartCoroutine(LoadSpriteAsync(sprites[1], s => altSprite = s));
                if (PicRAlt.color.a == 0f)
                {
                    if (altSprite != null) PicRAlt.sprite = altSprite;
                    PicRAlt.GetComponent<PicSwaper>()?.PicShow();
                }
                else
                    PicRAlt.GetComponent<PicSwaper>()?.PicChange(altSprite);
            }

            if (sprites[1].Contains(tmp.logger_name) && !sprites[0].Contains(tmp.logger_name))
                PicR.color = new Color(.75f, .75f, .75f, PicR.color.a);
            else
                PicR.color = new Color(1f, 1f, 1f, PicR.color.a);

            Sprite picRSprite = null;
            yield return StartCoroutine(LoadSpriteAsync(sprites[0], s => picRSprite = s));
            if (PicR.color.a == 0f)
            {
                if (picRSprite != null) PicR.sprite = picRSprite;
                PicR.GetComponent<PicSwaper>()?.PicShow();
            }
            else
                PicR.GetComponent<PicSwaper>()?.PicChange(picRSprite);
        }
        else
        {
            if (PicRAlt != null)
            {
                DoublePicR(false, tmp.illustration_opponent);
                yield return doublePicRCor;
                yield break;
            }
            PicR.color = new Color(1f, 1f, 1f, PicR.color.a);
            Sprite sprite = null;
            yield return StartCoroutine(LoadSpriteAsync(tmp.illustration_opponent, s => sprite = s));
            if (PicR.color.a == 0f)
            {
                if (sprite != null) PicR.sprite = sprite;
                PicR.GetComponent<PicSwaper>()?.PicShow();
            }
            else
            {
                if (sprite == null) yield break;
                if (instant)
                    PicR.sprite = sprite;
                else
                    PicR.GetComponent<PicSwaper>()?.PicChange(sprite);
            }
        }
    }

    private IEnumerator UpdateBackgroundAsync(DialogNode tmp, bool instantTransition = false)
    {
        var bgSwaper = Background.GetComponent<PicSwaper>();
        string scenePicPath = GetScenePic(tmp.scene);
        Sprite scenePic = null;
        yield return StartCoroutine(LoadSpriteAsync(scenePicPath, s => scenePic = s));

        bool spriteChanged = scenePic != null && Background.sprite != null && scenePic.name != Background.sprite.name;

        if (tmp.note.Contains("场景弹窗"))
        {
            bgSwaper?.PicChange(scenePic, 0.5f);
        }
        else if (scenePic != null)
        {
            if (instantTransition)
                bgSwaper?.PicChange(scenePic, 0f);
            else if (tmp.note.Contains("无刷屏转场"))
                bgSwaper?.PicChange(scenePic);
            else
                bgSwaper?.PicChangeWGradiant(scenePic);
        }

        if (tmp.scene.StartsWith("场景") && scenePic != null)
            StatEventCenter.Instance.PicRead(scenePicPath);

        if (spriteChanged)
        {
            _spriteCache.Remove(Background.sprite.name);
            Resources.UnloadUnusedAssets();
        }
    }

    private IEnumerator PicProcessAsync()
    {
        DialogNode tmp = tree.ShowDialog();
        bool isFirstLine = tmp.index == startLine;

        // CG: 只根据 cg 字段决定显示/隐藏
        bool hasCG = tmp.cg != "空" && tmp.cg != "转场" && tmp.cg != "";
        if (hasCG)
        {
            if (isFirstLine)
            {
               /* if (CG.color.a == 0f)
                {
                    Color c = CG.color;
                    c.a = 1f;
                    CG.color = c;
                }*/

                if (Head.color.a == 1f)
                {
                    Color c = Head.color;
                    c.a = 0f;
                    Head.color = c;
                }

                Sprite cgSprite = null;
                yield return StartCoroutine(LoadSpriteAsync(tmp.cg, s => cgSprite = s));
                if (cgSprite != null) CG.sprite = cgSprite;
            }

            if (CG.color.a == 0f)
            {
                CG.GetComponent<PicSwaper>()?.PicShow();
                ClearSpriteCache();
            }

            if (Resources.Load<VideoClip>(tmp.cg) is VideoClip videoClip)
            {
                CG.GetComponent<PicSwaper>().PicChange(videoClip);
                StatEventCenter.Instance.PicRead("VIDEO_" + tmp.cg);
            }
            else
            {
                Sprite cgSpr = null;
                yield return StartCoroutine(LoadSpriteAsync(tmp.cg, s => cgSpr = s));
                if (cgSpr != null)
                {
                    CG.GetComponent<PicSwaper>().PicChange(cgSpr);
                    StatEventCenter.Instance.PicRead(tmp.cg);
                    if (tmp.cg == "黑屏")
                    {
                        _spriteCache.Clear();
                        Resources.UnloadUnusedAssets();
                    }
                }
            }

        }
        else
        {
            if (CG.color.a == 1f)
            {
                CG.GetComponent<PicSwaper>()?.PicHide(0.25f);
                ClearSpriteCache();
            }
        }

        // PicR: 只根据 illustration_opponent 字段决定显示/隐藏
        bool hasPicR = tmp.illustration_opponent != "空" && tmp.illustration_opponent != "";

        UpdateHeadVisibility(tmp);
        yield return StartCoroutine(UpdateOpponentPicAsync(tmp, instant: isFirstLine));

        Sprite illustration = null;
        yield return StartCoroutine(LoadSpriteAsync("Head/" + tmp.illustration, s => illustration = s));
        if (illustration != null)
        {
            var headSwaper = Head.GetComponent<PicSwaper>();
            if (Head.color.a < 0.99f)
            {
                // 强制终止进行中的淡入动画，直接置为完全可见再切图
                Color c = Head.color;
                c.a = 1f;
                Head.color = c;
            }
            headSwaper.PicChange(illustration);
        }
        else if (tmp.illustration != "空" && tmp.illustration != "")
        {
            if (Head.color.a >= 0.99f) Head.GetComponent<PicSwaper>().PicHide();
        }

        yield return StartCoroutine(UpdateBackgroundAsync(tmp, instantTransition: isFirstLine));
        _picProcessCor = null;
    }

    private IEnumerator DelayAction(Action act, float time)
    {
        yield return new WaitForSeconds(time);
        act.Invoke();
    }
    
    private void Visualize()
    {

        DialogNode tmp = tree.ShowDialog();
        if (tmp.type == DialogType.End && tmp.content == "") { return; }
        if (!IsPreviewMode && _lastShownDialogIndex >= 0)
        {
            ReadDialogHistory?.MarkAsShown(DialogList.Instance.currentIndex, _lastShownDialogIndex);
        }
        _lastShownDialogIndex = tmp.index;
        //Debug.Log(tmp.cg);
        if (TextPrintCor != null) { StopCoroutine(TextPrintCor); TextPrintCor = null; }
        TextPrintCor = StartCoroutine(PrintText(text, tmp.content, TextPrintInterval));

        if (tmp.logger_name != "旁白" && tmp.logger_name != "空") diaName.text = tmp.logger_name;
        else diaName.text = "";
        
        // ContentTracker 先触发（让 FadeInMusic 等 effect 先设置 IsFadingIn），
        // SoundProcess 再执行，才能正确走 SetMusicFile 而非 PlayMusic 分支
        if(ContentTracker != null) ContentTracker.Invoke(tmp);

        if (_soundProcessCor != null) { StopCoroutine(_soundProcessCor); _soundProcessCor = null; }
        SoundProcess();
        
        if (_picProcessCor != null) { StopCoroutine(_picProcessCor); _picProcessCor = null; }
        _picProcessCor = StartCoroutine(PicProcessAsync());

        foreach (var ins in GameObject.FindObjectsOfType<ScrollviewAdder>())
        {
            ins.AddPrefab(tmp);
        }
        
    }

    private void Visualize_Choice()
    {
        Visualize();
        //StartCoroutine(DoChoice());
        StartCoroutine(DoChoiceWithPos());
    }

    private IEnumerator DoChoiceWithPos()
    {
        while (TextPrintCor != null)
        {
            yield return null;
        }
        choiceList.Clear();
        List<DialogNode> nodes = tree.ShowChoices();
        int positionIndex = nodes[0].note == "中" ? 1 : 0;
        bool waitChoice = true;
        for (int i = 0; i < nodes.Count; i++)
        {
            if (!nodes[i].CheckAllRequireMents()) continue;
            if (nodes[i].content == "")
            {
                OnButtonClick(i);
                waitChoice = false;
                i = nodes.Count;
                continue;
            }
        }
        
        if(waitChoice) for(int i = 0; i < nodes.Count; i++)
        {
            if (skipLoading)
            {
                OnButtonClick(0);
                i = nodes.Count;
                continue;
            }
            if (!nodes[i].CheckAllRequireMents()) continue;
            GameObject ins = Instantiate(choicePrefab, choiceGroup[positionIndex].transform);
            TextMeshProUGUI tx = ins.GetComponentInChildren<TextMeshProUGUI>();
            if (tx != null)
            {
                tx.text = nodes[i].content;
            }
            int index = i;
            choiceList.Add(ins.GetComponent<Button>());
            choiceList.Last().onClick.AddListener(() => OnButtonClick(index));

        }
    }

    void OnButtonClick(int index)
    {
        currentType = tree.Proceed(index);
        if (currentType != DialogType.Choice)
        {
            Visualize();
        }
        else
        {
            //Visualize();
            Visualize_Choice();
        }
        for (int i = 0; i < choiceList.Count; i++)
        {
            choiceList[i].onClick.RemoveAllListeners();
            Destroy(choiceList[i].gameObject);
        }
        clickDelay++;
    }

    public bool index_debug;

    public int debugstart;
    private int _lastShownDialogIndex = -1;

    public void StartDialog(int startIndex)
    {
        ClearSpriteCache();
        _lastShownDialogIndex = -1;
        currentType = DialogType.Normal;
        startLine = startIndex;
        for (int i = 0; i < choiceList.Count; i++)
        {
            if (choiceList[i] != null)
            {
                choiceList[i].onClick.RemoveAllListeners();
                Destroy(choiceList[i].gameObject);
            }
        }
        choiceList.Clear();
        foreach (var ins in GameObject.FindObjectsOfType<ScrollviewAdder>())
        {
            ins.Clear();
        }
        if (index_debug) tree.StartTree(debugstart);
        else tree.StartTree(startIndex);
        StartCoroutine(LateAction(Visualize));
        
        monitoringMouse = true;
    }

    private IEnumerator LateAction(Action act)
    {
        yield return null;
        act.Invoke();
    }
    
    private bool _endingDialog = false;

    private bool IsPreviewMode => !string.IsNullOrEmpty(previewFileName);

    private void EndDialog()
    {
        if (_endingDialog) return;
        _endingDialog = true;
        SoundsManager.Instance.ClearMusicClip();
        if (!IsPreviewMode && _lastShownDialogIndex >= 0)
        {
            ReadDialogHistory?.MarkAsShown(DialogList.Instance.currentIndex, _lastShownDialogIndex);
        }

        //Debug.LogWarning(tree.ShowDialog().index);
        monitoringMouse = false;
        if(skipRoutine != null)
        {
            StopCoroutine(skipRoutine);
            skipRoutine = null;
        }
        var x = GameObject.FindObjectOfType<DiaPlayer>();
        if (x != null) x.enabled = true;
        if (!IsPreviewMode)
            DialogList.Instance.InvokeEndAct(tree.ShowDialog().note == "" ? dialogFile.name : tree.ShowDialog().note);
    }

    /// <summary>
    /// 强制结束当前对话（供测试用，如 F2 跳过）
    /// </summary>
    public void ForceEndDialog()
    {
        EndDialog();
    }

    private IEnumerator WaitForNoClickSkip()
    {
        while (ForceNoClickSkip)
        {
            yield return null;
        }
        AutoProcceed();
    }
    
    /// <summary>
    /// 供 HotKeyManager onPressed 绑定：等价于鼠标左键点击前进，含跳过打印逻辑。
    /// </summary>
    public void ClickAdvance()
    {
        if (SaveSystem.Instance.saveRoutine != null) return;
        if (skipLoading) return;
        if (IsSkip) return;
        if (IsHistory) return;
        if (IsAuto) return;
        if (ForceNoClickSkip) return;
        if (!monitoringMouse) return;
        if (clickDelay > 0) { clickDelay--; return; }
        if (IsPause) return;
        if (IsUIHidden) return;

        if (TextPrintCor != null)
        {
            StopCoroutine(TextPrintCor);
            TextPrintCor = null;
            text.text = tree.ShowDialog().content;
            OnTextDisplayEnd.Invoke();
            OnTextDisplayDelay?.Invoke();
            if (!enableFill) return;
        }

        if (_picProcessCor != null) return;

        if (currentType == DialogType.Normal)
        {
            currentType = tree.Proceed();
            if (currentType != DialogType.Choice)
                Visualize();
            else
                Visualize_Choice();
        }
        else if (currentType == DialogType.Choice)
        {
        }
        else if (currentType == DialogType.End)
        {
            EndDialog();
        }
    }

    public void AutoProcceed()
    {
        if (_picProcessCor != null) return;
        if (ForceNoClickSkip)
        {
            StartCoroutine(WaitForNoClickSkip());
            return;
        }
        if (TextPrintCor != null)
        {
            StopCoroutine(TextPrintCor);
            
            TextPrintCor = null;
            DialogNode tmp = tree.ShowDialog();
            text.text = tmp.content;
        }
        //Debug.LogWarning(currentType);
        if (currentType == DialogType.Normal)
        {
            currentType = tree.Proceed();
            //Debug.LogWarning(currentType);
            if (currentType != DialogType.Choice)
            {
                
                Visualize();
            }
            else
            {
                //Visualize();
                Visualize_Choice();
            }
        }
        else if (currentType == DialogType.Choice)
        {

        }
        else if (currentType == DialogType.End)
        {
            EndDialog();
        }
    }

    public bool IsAuto = false;
    public bool IsSkip = false;
    public bool IsHistory = false;

    public bool IsPause = false;
    public bool IsUIHidden = false;

    private Coroutine skipRoutine;

    private bool skipLoading = false;
    private Coroutine LoadRoutine;
    public Action OnSkipLoadingEnds;

    public bool CanStartSkipAtCurrentLine()
    {
        if (SettingManager.Instance == null || SettingManager.Instance.settings == null)
        {
            return true;
        }

        if (!SettingManager.Instance.settings.OnlySkipReadDialog)
        {
            return true;
        }

        return HasShownCurrentDialogLine();
    }

    private bool HasShownCurrentDialogLine()
    {
        if (ReadDialogHistory == null)
        {
            return true;
        }

        return ReadDialogHistory.HasShown(DialogList.Instance.currentIndex, currentIndex);
    }

    private IEnumerator SkipUntil(int index)
    {
        skipLoading = true;
        ShowBigPic.Instance.Show("黑屏");
        
        while (tree.ShowDialog().index < index)
        {
            //Debug.LogWarning("跳转中"+tree.ShowDialog().index + "|" + index);
            yield return null;
        }

        ShowBigPic.Instance.Hide();
        skipLoading = false;
        OnSkipLoadingEnds?.Invoke();
    }
    
    private IEnumerator Skip()
    {
        while (true)
        {
            yield return new WaitUntil(() => _picProcessCor == null);

            if (!CanStartSkipAtCurrentLine())
            {
                IsSkip = false;
                skipRoutine = null;
                yield break;
            }

            if (!ForceNoClickSkip)
            {
                if (TextPrintCor != null)
                {
                    StopCoroutine(TextPrintCor);
                    TextPrintCor = null;
                    DialogNode tmp = tree.ShowDialog();
                    text.text = tmp.content;
                    OnTextDisplayEnd.Invoke();
                    OnTextDisplayDelay?.Invoke();
                }
                else if (currentType == DialogType.Normal)
                {
                    currentType = tree.Proceed();
                    if (currentType != DialogType.Choice)
                    {
                        Visualize();
                    }
                    else
                    {
                        //Visualize();
                        Visualize_Choice();
                    }
                }
                else if (currentType == DialogType.Choice)
                {

                }
                else if (currentType == DialogType.End)
                {
                    EndDialog();
                }
            }
            yield return new WaitForSeconds(0.1f);

        }
    }
    
    private bool CheckClick()
    {
        if (SaveSystem.Instance.saveRoutine != null)
        {
            return false;
        }
        
        
        if (skipLoading)
        {
            if (LoadRoutine == null)
            {
                LoadRoutine =  StartCoroutine(Skip());
            }

            return false;
        }
        else
        {
            if (LoadRoutine != null)
            {
                StopCoroutine(LoadRoutine);
                LoadRoutine = null;
            }
        }

        
        if (IsSkip)
        {
            if (skipRoutine == null)
            {
                skipRoutine =  StartCoroutine(Skip());
            }

            return false;
        }
        else
        {
            if (skipRoutine != null)
            {
                StopCoroutine(skipRoutine);
                skipRoutine = null;
            }
        }
        if (IsHistory) { return false; }
        if (IsAuto) return false;
        if (ForceNoClickSkip) return false;
        if (!monitoringMouse) return false;
        if (!Input.GetKeyUp(KeyCode.Mouse0)) return false;
        if (clickDelay > 0) { clickDelay--; return false; }
        if (IsPause) return false;
        if (IsUIHidden) return false;
        return true;
    }
    
    private void Update()
    {
        if (!CheckClick()) return;
        
        if (TextPrintCor != null)
        {
            StopCoroutine(TextPrintCor);
            TextPrintCor = null;
            DialogNode tmp = tree.ShowDialog();
            text.text = tmp.content;
            OnTextDisplayEnd.Invoke();
            OnTextDisplayDelay?.Invoke();
            if(!enableFill) return;
        }
        
        if (_picProcessCor != null) return;
        
        if (currentType == DialogType.Normal)
        {
            currentType = tree.Proceed();
            if (currentType != DialogType.Choice)
            {
                Visualize();
            }
            else
            {
                //Visualize();
                Visualize_Choice();
            }
        }
        else if (currentType == DialogType.Choice)
        {

        }
        else if (currentType == DialogType.End)
        {
            EndDialog();
        }
    }
    public void StartPreviewFromText(string csvText, bool isMiddle, string fileName)
    {
        StopAllCoroutines();
        skipRoutine = null;
        LoadRoutine = null;
        TextPrintCor = null;
        _picProcessCor = null;
        _soundProcessCor = null;
        _endingDialog = false;
        Loaading = true;
        monitoringMouse = false;
        previewFileName = fileName;
        currentType = DialogType.End;
        tree.BuildTreeFromText(csvText);
        PicRSetMiddleEffect.Instance.StartEffect(isMiddle);
        StartDialog(0);
    }

    /// <summary>
    /// 重置静态状态（由 StaticStateResetManager 在存读档时调用）
    /// </summary>
    public static void ResetStaticState()
    {
        _spriteCache.Clear();  // readonly 字段只能清空，不能赋值
        Debug.Log("[DialogVisual] 静态状态已重置: _spriteCache已清空");
    }
}
