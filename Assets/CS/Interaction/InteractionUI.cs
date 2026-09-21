using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI text;
    public Vector3 offset = new Vector3(0, 1.5f, 0); 

    private Transform target;
    private Camera mainCam;

    void Awake()
    {
        mainCam = Camera.main;
        canvasGroup.alpha = 0;
    }

    void Update()
    {
        if (target == null) return;

        // UI 跟随目标位置
        transform.position = target.position + offset;
        transform.forward = mainCam.transform.forward; 
    }

    public void Show(Transform t, string message)
    {
        target = t;
        text.text = message;
        StopAllCoroutines();
        StartCoroutine(Fade(1));
    }

    public void Hide()
    {
        target = null;
        StopAllCoroutines();
        StartCoroutine(Fade(0));
    }

    private System.Collections.IEnumerator Fade(float to)
    {
        float from = canvasGroup.alpha;
        for (float t = 0; t < 0.2f; t += Time.deltaTime)
        {
            canvasGroup.alpha = Mathf.Lerp(from, to, t / 0.2f);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}