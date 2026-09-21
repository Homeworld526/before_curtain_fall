using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("缩放")]
    public float hoverScale = 1.1f;
    public float smooth = 10f;

    [Header("外发光")]
    public Color glowColor = Color.cyan;
    public float normalGlowPower = 0f;
    public float hoverGlowPower = 2f;
    public float glowSize = 0.02f;

    private Image img;
    private Material matInstance;
    private Vector3 originScale;
    private bool isHover;

    void Awake()
    {
        img = GetComponent<Image>();
        originScale = transform.localScale;

        // 强制使用我们的发光Shader，避免默认材质报错
        Shader glowShader = Shader.Find("UI/GlowHover");
        matInstance = new Material(glowShader);
        img.material = matInstance;

        // 初始化发光参数
        matInstance.SetColor("_GlowColor", glowColor);
        matInstance.SetFloat("_GlowPower", normalGlowPower);
        matInstance.SetFloat("_GlowSize", glowSize);
    }

    public void OnPointerEnter(PointerEventData eventData) => isHover = true;
    public void OnPointerExit(PointerEventData eventData) => isHover = false;

    void Update()
    {
        // 平滑缩放
        float targetScale = isHover ? hoverScale : 1f;
        transform.localScale = Vector3.Lerp(transform.localScale, originScale * targetScale, smooth * Time.deltaTime);

        // 平滑发光
        float targetPower = isHover ? hoverGlowPower : normalGlowPower;
        float currentPower = matInstance.GetFloat("_GlowPower");
        float newPower = Mathf.Lerp(currentPower, targetPower, smooth * Time.deltaTime);
        matInstance.SetFloat("_GlowPower", newPower);
    }
}