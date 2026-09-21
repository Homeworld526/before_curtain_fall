using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class AutoInteractableTrigger : MonoBehaviour
{
    private AutoInteractable interactable;

    void Awake()
    {
        interactable = GetComponent<AutoInteractable>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            interactable?.OnPlayerEnter();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            interactable?.OnPlayerExit();
        }
    }
}