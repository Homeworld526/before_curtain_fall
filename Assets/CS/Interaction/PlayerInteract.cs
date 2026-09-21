using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    private AutoInteractable curTarget;

    private void Update()
    {
        if (curTarget != null && Input.GetKeyDown(KeyCode.E))
        {
            curTarget.OnInteract();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var target = collision.GetComponent<AutoInteractable>();
        if (target != null)
        {
            curTarget = target;
            target.OnPlayerEnter();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        var target = collision.GetComponent<AutoInteractable>();
        if (target != null && target == curTarget)
        {
            target.OnPlayerExit();
            curTarget = null;
        }
    }
}