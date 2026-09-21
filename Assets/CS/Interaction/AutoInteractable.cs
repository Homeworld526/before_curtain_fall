using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public abstract class AutoInteractable : MonoBehaviour
{
    [Header("交互提示")]
    public string promptText = "点击交互";

    protected virtual void Reset()
    {
        var col = gameObject.GetComponent<BoxCollider2D>();
        if (col == null)
            col = gameObject.AddComponent<BoxCollider2D>();

        col.isTrigger = true;
        
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            col.size = sr.bounds.size;
        }
    }

    public virtual void OnPlayerEnter()
    {
        InteractionUIManager.Instance.ShowUI(transform, promptText);
    }

    public virtual void OnPlayerExit()
    {
        InteractionUIManager.Instance.HideUI();
    }
    public abstract void OnInteract();
}