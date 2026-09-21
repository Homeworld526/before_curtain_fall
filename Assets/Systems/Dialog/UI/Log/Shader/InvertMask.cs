using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class InvertMask : Image
{
    private Material _invertedMaterial;

    public override Material materialForRendering
    {
        get
        {
            // 只在第一次访问时创建一次材质实例
            if (_invertedMaterial == null)
            {
                // 复制基础材质
                _invertedMaterial = new Material(base.materialForRendering);
                // 设置反相模板比较
                _invertedMaterial.SetInt("_StencilComp", (int)CompareFunction.NotEqual);
            }
            return _invertedMaterial;
        }
    }

    // 释放材质实例，防止内存泄漏
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_invertedMaterial != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(_invertedMaterial);
            else
                Destroy(_invertedMaterial);
#else
            Destroy(_invertedMaterial);
#endif
        }
    }

    // 编辑器模式下也需要清理，防止编辑器内存泄漏
    protected new void OnValidate()
    {
        // 如果材质实例存在但属性改变了，强制刷新
        if (_invertedMaterial != null)
        {
            _invertedMaterial.SetInt("_StencilComp", (int)CompareFunction.NotEqual);
        }
    }
}