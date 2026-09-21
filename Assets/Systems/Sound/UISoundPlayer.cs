using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum UISfxType
{
    Click,
    Confirm,
    MapExpand,
    Hover
}

/// <summary>
/// 音效播放器 - 同时支持 UI 物体和场景物体。
/// UI 物体：通过 EventSystem 接口检测点击/悬停。
/// 场景物体：通过 Physics2D/Physics 射线检测点击/悬停。
/// </summary>
public class UISoundPlayer : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
{
    [Header("音效类型")]
    [SerializeField] private UISfxType _sfxType = UISfxType.Click;

    [Header("自定义 AudioClip（留空则使用 SoundsManager 默认值）")]
    [SerializeField] private AudioClip _customClip;
    [SerializeField] private AudioClip _customHoverClip;

    private Selectable _selectable;
    private bool _isUI;          // 是否是 UI 物体
    private bool _isSceneObj;    // 是否是场景物体（有 Collider）
    private bool _isHovering;

    void Start()
    {
        _selectable = GetComponent<Selectable>();
        _isUI = GetComponent<Graphic>() != null;
        _isSceneObj = GetComponent<Collider2D>() != null || GetComponent<Collider>() != null;

        // UI 物体：有 Button/Toggle 时用 onClick
        if (_isUI && _selectable != null && _sfxType != UISfxType.Hover)
        {
            if (_selectable is Button btn)
                btn.onClick.AddListener(OnClicked);
            else if (_selectable is Toggle toggle)
                toggle.onValueChanged.AddListener((_) => OnClicked());
        }
    }

    void Update()
    {
        // 只对场景物体做射线检测
        if (!_isSceneObj || _isUI) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        bool hitThis = false;

        // 优先 Physics2D
        if (GetComponent<Collider2D>() != null)
        {
            Vector2 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
            hitThis = hit.collider != null &&
                      (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform));
        }
        // 3D Collider
        else if (GetComponent<Collider>() != null)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                hitThis = hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform);
            }
        }

        // Hover
        if (hitThis && !_isHovering)
        {
            _isHovering = true;
            if (_sfxType == UISfxType.Hover || _customHoverClip != null)
                PlayHoverSound();
        }
        else if (!hitThis && _isHovering)
        {
            _isHovering = false;
        }

        // Click
        if (hitThis && Input.GetMouseButtonDown(0))
        {
            PlayTypeSfx();
        }
    }

    private void OnClicked()
    {
        PlayTypeSfx();
    }

    /// <summary>
    /// UI 物体无 Button 时，由 EventSystem 直接回调
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_selectable != null) return;
        PlayTypeSfx();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_sfxType == UISfxType.Hover || _customHoverClip != null)
        {
            PlayHoverSound();
        }
    }

    private void PlayTypeSfx()
    {
        if (SoundsManager.Instance == null) return;

        switch (_sfxType)
        {
            case UISfxType.Click:
                if (_customClip != null) SoundsManager.Instance.PlayClickSfx(_customClip);
                else SoundsManager.Instance.PlayClickSfx();
                break;
            case UISfxType.Confirm:
                if (_customClip != null) SoundsManager.Instance.PlayConfirmSfx(_customClip);
                else SoundsManager.Instance.PlayConfirmSfx();
                break;
            case UISfxType.MapExpand:
                if (_customClip != null) SoundsManager.Instance.PlayMapExpandSfx(_customClip);
                else SoundsManager.Instance.PlayMapExpandSfx();
                break;
            case UISfxType.Hover:
                PlayHoverSound();
                break;
        }
    }

    public void PlaySound()
    {
        PlayTypeSfx();
    }

    public void PlayHoverSound()
    {
        if (SoundsManager.Instance == null) return;
        if (_customHoverClip != null)
            SoundsManager.Instance.PlayHoverSfx(_customHoverClip);
        else
            SoundsManager.Instance.PlayHoverSfx();
    }
}
