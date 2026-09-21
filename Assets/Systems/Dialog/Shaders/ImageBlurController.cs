using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ����UI Image�Ķ�̬ģ��Ч��
/// </summary>

public class ImageBlurController : MonoBehaviour
{
    [Header("ģ������")]
    [Range(0f, 10f)] public float blurRadius = 1f; // ģ���뾶����̬������
    [Range(1, 8)] public int blurIterations = 2;   // ģ����������
    [Range(0f, 0.5f)] public float blurSpread = 0.1f; // ģ����ɢ��

    public Image _targetImage;
    public Material _blurMaterial; // ģ������
    //private Material _blurMaterial;
    // ��ʼ��
    private void Awake()
    {
        //_targetImage = GetComponent<Image>();

        // ���Ʋ���ʵ���������޸�ȫ�ֲ��ʣ�
        //_blurMaterial = new Material(blurMaterial);
        _targetImage.material = _blurMaterial;
    }

    // ÿһ֡����ģ����������֤��̬�ԣ�
    private void Update()
    {
        UpdateBlurParams();
    }

    /// <summary>
    /// �ֶ�����ģ���������ⲿ���ã�
    /// </summary>
    /// <param name="radius">ģ���뾶</param>
    /// <param name="iterations">��������</param>
    public void SetBlurParams(float radius, int iterations = 2)
    {
        blurRadius = Mathf.Clamp(radius, 0f, 10f);
        blurIterations = Mathf.Clamp(iterations, 1, 8);
        UpdateBlurParams();
    }

    // Ӧ�ò���������
    private void UpdateBlurParams()
    {
        if (_blurMaterial == null) return;

        _blurMaterial.SetFloat("_BlurRadius", blurRadius);
        _blurMaterial.SetInt("_BlurIterations", blurIterations);
        _blurMaterial.SetFloat("_BlurSpread", blurSpread);
    }
    
    private void OnDestroy()
    {
        
    }
}
