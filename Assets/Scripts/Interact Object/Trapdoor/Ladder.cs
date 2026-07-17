using UnityEngine;

public class Ladder : MonoBehaviour, IInteractable
{
    [SerializeField] private FPSMovement playerMovement;
    [SerializeField] private Transform bottomGrabTarget;
    [SerializeField] private Transform topGrabTarget;

    public string GetInteractText()
    {
        if (playerMovement == null) return "";
        if (playerMovement.IsClimbing()) return "";
        float dist = Mathf.Abs(playerMovement.transform.position.y - bottomGrabTarget.position.y);
        if (dist > 2f) return "";
        return "E for Climb Up";
    }

    public void Interact()
    {
        if (playerMovement == null || bottomGrabTarget == null) return;
        if (playerMovement.IsClimbing()) return;

        Vector3 grabDir = (bottomGrabTarget.position - playerMovement.transform.position).normalized;
        float topY = topGrabTarget != null ? topGrabTarget.position.y : bottomGrabTarget.position.y + 3f;
        playerMovement.ForceStartClimbing(bottomGrabTarget.position, grabDir, topY, 1);
    }
}
