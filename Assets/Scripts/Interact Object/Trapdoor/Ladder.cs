using UnityEngine;

public class Ladder : MonoBehaviour, IInteractable
{
    [SerializeField] private FPSMovement playerMovement;
    [SerializeField] private TrapDoorController trapDoorController;
    [SerializeField] private Transform bottomGrabTarget;
    [SerializeField] private Transform topGrabTarget;

    private void Awake()
    {
        if (trapDoorController == null)
            trapDoorController = GetComponentInParent<TrapDoorController>();
    }

    public string GetInteractText()
    {
        if (playerMovement == null) return "";
        if (playerMovement.IsClimbing() || playerMovement.IsTransitioningToLadder()) return "";

        if (bottomGrabTarget == null) return "";

        float distY = Mathf.Abs(playerMovement.transform.position.y - bottomGrabTarget.position.y);
        if (distY > 2f) return "";

        Vector3 playerXZ = playerMovement.transform.position;
        playerXZ.y = bottomGrabTarget.position.y;
        float distXZ = Vector3.Distance(playerXZ, bottomGrabTarget.position);
        if (distXZ > 2f) return "";

        bool playerIsAbove = playerMovement.transform.position.y > (topGrabTarget != null ? topGrabTarget.position.y : bottomGrabTarget.position.y + 2f);
        if (playerIsAbove) return "";

        if (trapDoorController != null && trapDoorController.IsAnimating()) return "";

        return "Press [E] To Go Up";
    }

    public void Interact()
    {
        if (playerMovement == null || bottomGrabTarget == null) return;
        if (playerMovement.IsClimbing() || playerMovement.IsTransitioningToLadder()) return;

        bool playerIsAbove = playerMovement.transform.position.y > (topGrabTarget != null ? topGrabTarget.position.y : bottomGrabTarget.position.y + 2f);
        if (playerIsAbove) return;

        if (trapDoorController != null)
        {
            trapDoorController.StartClimbFromBottom();
        }
        else
        {
            // Direction: dari player ke ladder (menghadap tangga)
            // seperti original sebelum refactor
            Vector3 grabPos = bottomGrabTarget.position;
            Vector3 dir = (bottomGrabTarget.position - playerMovement.transform.position);
            dir.y = 0;
            if (dir.magnitude > 0.01f) dir.Normalize();
            else if (topGrabTarget != null) dir = -topGrabTarget.forward;
            else dir = -transform.forward;

            float topY = topGrabTarget != null ? topGrabTarget.position.y : bottomGrabTarget.position.y + 3f;
            float bottomY = bottomGrabTarget.position.y;
            playerMovement.StartLadderClimb(grabPos, dir, topY, bottomY, false);
        }
    }
}
