using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ContentTracker))]
public class Mengban : MonoBehaviour
{
    [Tooltip("提供现有蒙版色卡和 _Intensity 配置的相机组件。")]
    public ScreenMultiplyEffect_Builtin image;

    private ContentTracker _contentTracker;
    private DialogMaskOverlay _overlay;

    private void OnEnable()
    {
        if (image == null)
            image = Camera.main.GetComponent<ScreenMultiplyEffect_Builtin>();

        if (image == null)
            throw new InvalidOperationException("剧情蒙版缺少 ScreenMultiplyEffect_Builtin 配置，无法读取色卡和强度。");

        // 旧实现是相机后处理，会重绘整张 Canvas，因而无法将文本框排除在外。
        // 保留它作为现有材质/色卡的配置来源，但不再启用该后处理。
        image.enabled = false;

        _overlay = GetComponent<DialogMaskOverlay>();
        if (_overlay == null)
            _overlay = gameObject.AddComponent<DialogMaskOverlay>();
        _overlay.Initialize(image);

        _contentTracker = GetComponent<ContentTracker>();
        _contentTracker.act += MBOn;
        _contentTracker.deact += MBOff;
    }

    private void OnDisable()
    {
        _contentTracker.act -= MBOn;
        _contentTracker.deact -= MBOff;
        _overlay.Hide();
    }

    protected void MBOn()
    {
        _overlay.FadeIn();
    }

    protected void MBOff()
    {
        _overlay.FadeOut();
    }
}

/// <summary>
/// 在剧情 Canvas 中放置正片叠底色卡。它位于 CGParent 后、文本框 ContentLining 前，
/// 所以仅影响已经绘制的场景和角色立绘，不会覆盖文本框或其后的 UI。
/// </summary>
public sealed class DialogMaskOverlay : MonoBehaviour
{
    private const string ShaderName = "Custom/UI/DialogMultiplyMask";
    private const float FadeDuration = 0.5f;
    private static readonly int MaskTextureId = Shader.PropertyToID("_MaskTex");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

    private RawImage _graphic;
    private Material _material;
    private Coroutine _fadeRoutine;
    private float _configuredIntensity;

    public void Initialize(ScreenMultiplyEffect_Builtin settings)
    {
        if (settings.multiplyMaterial == null)
            throw new InvalidOperationException("剧情蒙版材质未配置，无法读取 _Intensity。");
        if (settings.maskTexture == null)
            throw new InvalidOperationException("剧情蒙版色卡未配置，无法创建场景与角色立绘蒙版。");

        if (_graphic == null)
            CreateGraphic();

        _material.SetTexture(MaskTextureId, settings.maskTexture);
        _configuredIntensity = settings.multiplyMaterial.GetFloat(IntensityId);
        Hide();
    }

    public void FadeIn()
    {
        FadeTo(_configuredIntensity);
    }

    public void FadeOut()
    {
        FadeTo(0f);
    }

    /// <summary>
    /// 在停用剧情系统时立即清除色卡，避免下一段剧情继承未完成的渐变状态。
    /// </summary>
    public void Hide()
    {
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        _material.SetFloat(IntensityId, 0f);
        _graphic.enabled = false;
    }

    private void FadeTo(float targetIntensity)
    {
        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _graphic.enabled = true;
        _fadeRoutine = StartCoroutine(FadeIntensity(targetIntensity));
    }

    private IEnumerator FadeIntensity(float targetIntensity)
    {
        float startIntensity = _material.GetFloat(IntensityId);
        float elapsed = 0f;

        while (elapsed < FadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / FadeDuration);
            _material.SetFloat(IntensityId, Mathf.Lerp(startIntensity, targetIntensity, progress));
            yield return null;
        }

        _material.SetFloat(IntensityId, targetIntensity);
        if (Mathf.Approximately(targetIntensity, 0f))
            _graphic.enabled = false;

        _fadeRoutine = null;
    }

    private void CreateGraphic()
    {
        Transform cgParent = transform.Find("CGParent");
        Transform contentLining = transform.Find("ContentLining");
        if (cgParent == null || contentLining == null)
            throw new InvalidOperationException("剧情 Canvas 必须包含 CGParent 和 ContentLining，才能确定蒙版层级。");

        int cgIndex = cgParent.GetSiblingIndex();
        int contentIndex = contentLining.GetSiblingIndex();
        if (cgIndex >= contentIndex)
            throw new InvalidOperationException("剧情 Canvas 中 CGParent 必须位于 ContentLining 之前，才能将蒙版限制在场景和角色立绘层。");

        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
            throw new InvalidOperationException($"未找到剧情 UI 蒙版着色器：{ShaderName}。");

        var overlayObject = new GameObject("DialogMaskOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        overlayObject.layer = gameObject.layer;

        RectTransform overlayTransform = overlayObject.GetComponent<RectTransform>();
        overlayTransform.SetParent(transform, false);
        overlayTransform.anchorMin = Vector2.zero;
        overlayTransform.anchorMax = Vector2.one;
        overlayTransform.offsetMin = Vector2.zero;
        overlayTransform.offsetMax = Vector2.zero;
        overlayTransform.SetSiblingIndex(cgIndex + 1);

        _graphic = overlayObject.GetComponent<RawImage>();
        _graphic.texture = Texture2D.whiteTexture;
        _graphic.raycastTarget = false;

        _material = new Material(shader)
        {
            name = "DialogMaskOverlay (Runtime)"
        };
        _graphic.material = _material;
    }

    private void OnDestroy()
    {
        if (_material != null)
            Destroy(_material);
    }
}
