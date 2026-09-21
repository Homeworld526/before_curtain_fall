using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlashEffect : MonoBehaviour, IEffect
{
    [SerializeField] private GameObject _effect;
    [SerializeField] private AnimationCurve curve;
    [SerializeField] private float duration;
    
    private List<GameObject> InstancePool;
    
    public bool IsRunning()
    {
        throw new System.NotImplementedException();
    }

    [ContextMenu("启动效果")]
    private void ManualStart()
    {
        StartCoroutine(Flash());
    }
    
    public void StartEffect(string param)
    {
        if (param == "灯")
        {
            StartCoroutine(Flash());
        }
    }

    private IEnumerator Flash()
    {
        var newIns = Instantiate(_effect, transform);
        //InstancePool.Add(newIns);
        
        var InsIma = newIns.GetComponent<Image>();
        
        float t = 0;
        while (t < duration)
        {
            t+=Time.deltaTime;
            InsIma.color = new Color(InsIma.color.r, InsIma.color.g, InsIma.color.b, curve.Evaluate(t / duration));
            yield return null;
        }
        Destroy(newIns);
    }
    
    public void PauseEffect()
    {
        throw new System.NotImplementedException();
    }

    public void ContinueEffect()
    {
        throw new System.NotImplementedException();
    }

    public void ResetEffect()
    {
        StopAllCoroutines();
        foreach (var VARIABLE in InstancePool)
        {
            Destroy(VARIABLE);
        }
        InstancePool.Clear();
    }
}
