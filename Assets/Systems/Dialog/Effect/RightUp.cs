using UnityEngine;

public class RightUp : MonoBehaviour, IEffect
{
    public void StartEffect(string param)
    {
        if (param == "开始")
            VisualLocator.Instance.ChangeVisual(VisualLocator.VisualState.R);
        else if (param == "结束")
            VisualLocator.Instance.ChangeVisual(VisualLocator.VisualState.L);
    }

    public bool IsRunning() => false;
    public void PauseEffect() { }
    public void ContinueEffect() { }
    public void ResetEffect() { }
}
