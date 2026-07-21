using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class TrapDoorController : MonoBehaviour, IInteractable
{
    [Header("Hinge Settings")]
    [SerializeField] private Transform doorMeshTransform;
    [SerializeField] private Vector3 rotationAxis = Vector3.right;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private bool invertDirection = false;

    [Header("Timing")]
    [SerializeField] private float openDuration = 0.8f;
    [SerializeField] private float closeDuration = 0.6f;
    [SerializeField] private float autoCloseDelay = 5f;

    [Header("Interaction")]
    [SerializeField] private string interactPrompt = "E for Climb Down";

    [Header("Ladder Auto-Climb")]
    [SerializeField] private FPSMovement playerMovement;
    [SerializeField] private Transform topGrabTarget;
    [SerializeField] private Transform bottomGrabTarget;
    [SerializeField] private float grabDirectionAngleOffset;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    private enum DoorState { Closed, Opening, Open, Closing }
    private DoorState currentState = DoorState.Closed;

    private Quaternion closedLocalRotation;
    private Quaternion openLocalRotation;
    private int playerInsideCount = 0;
    private Collider _doorCollider;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Awake()
    {
        if (doorMeshTransform == null)
            doorMeshTransform = transform;

        _doorCollider = doorMeshTransform.GetComponent<Collider>();

        closedLocalRotation = doorMeshTransform.localRotation;

        float sign = invertDirection ? -1f : 1f;
        Vector3 targetAngles = rotationAxis * sign * openAngle;
        openLocalRotation = Quaternion.Euler(
            doorMeshTransform.localEulerAngles.x + targetAngles.x,
            doorMeshTransform.localEulerAngles.y + targetAngles.y,
            doorMeshTransform.localEulerAngles.z + targetAngles.z
        );

        if (playerMovement != null)
            playerMovement.OnLadderReleaseAtBoundary += OnPlayerReleasedLadderAtBoundary;
    }

    void OnDestroy()
    {
        if (playerMovement != null)
            playerMovement.OnLadderReleaseAtBoundary -= OnPlayerReleasedLadderAtBoundary;
    }

    private void OnPlayerReleasedLadderAtBoundary()
    {
        if (currentState == DoorState.Open)
            StartCoroutine(CloseRoutine());
    }

    public void OnPlayerEnterDetectionZone()
    {
        playerInsideCount++;
    }

    public void OnPlayerExitDetectionZone()
    {
        playerInsideCount = Mathf.Max(0, playerInsideCount - 1);
    }

    public string GetInteractText()
    {
        if (playerMovement == null) return "";
        if (playerMovement.IsClimbing()) return "";
        if (topGrabTarget != null)
        {
            float dist = Mathf.Abs(playerMovement.transform.position.y - topGrabTarget.position.y);
            if (dist > 2f) return "";
        }
        if (currentState == DoorState.Closed || currentState == DoorState.Open)
            return interactPrompt;
        return "";
    }

    public void Interact()
    {
        if (currentState == DoorState.Closed)
        {
            TryOpen();
        }
        else if (currentState == DoorState.Open)
        {
            StartCoroutine(ClimbDownFromOpenDoor());
        }
    }

    private IEnumerator ClimbDownFromOpenDoor()
    {
        if (topGrabTarget != null && playerMovement != null)
        {
            float topY = topGrabTarget.position.y;
            float bottomY = bottomGrabTarget != null ? bottomGrabTarget.position.y : topGrabTarget.position.y - 3f;
            Vector3 dir = bottomGrabTarget.forward;
            dir.y = 0;
            if (dir.magnitude > 0.01f) dir.Normalize();
            else dir = -topGrabTarget.forward;
            if (Mathf.Abs(grabDirectionAngleOffset) > 0.01f)
                dir = Quaternion.Euler(0, grabDirectionAngleOffset, 0) * dir;
            playerMovement.ForceStartClimbing(topGrabTarget.position, dir, topY, bottomY, -1);
        }
        yield break;
    }

    public void StartClimbFromBottom()
    {
        if (playerMovement == null || bottomGrabTarget == null) return;
        if (playerMovement.IsClimbing() || playerMovement.IsTransitioningToLadder()) return;

        Vector3 dir = bottomGrabTarget.forward;
        dir.y = 0;
        if (dir.magnitude > 0.01f) dir.Normalize();
        if (Mathf.Abs(grabDirectionAngleOffset) > 0.01f)
            dir = Quaternion.Euler(0, grabDirectionAngleOffset, 0) * dir;
        float topY = topGrabTarget != null ? topGrabTarget.position.y : bottomGrabTarget.position.y + 3f;
        float bottomY = bottomGrabTarget.position.y;
        playerMovement.ForceStartClimbing(bottomGrabTarget.position, dir, topY, bottomY, 1, false);
    }

    public void TryOpen()
    {
        if (currentState != DoorState.Closed) return;
        StartCoroutine(OpenAndClimbRoutine());
    }

    private IEnumerator OpenAndClimbRoutine()
    {
        currentState = DoorState.Opening;

        if (_doorCollider != null)
            _doorCollider.enabled = false;

        if (audioSource != null && openSound != null)
            audioSource.PlayOneShot(openSound);

        float elapsed = 0f;
        Quaternion startRot = doorMeshTransform.localRotation;

        while (elapsed < openDuration)
        {
            float t = elapsed / openDuration;
            t = SmoothProgress(t);
            doorMeshTransform.localRotation = Quaternion.Slerp(startRot, openLocalRotation, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        doorMeshTransform.localRotation = openLocalRotation;
        currentState = DoorState.Open;

        if (topGrabTarget != null && playerMovement != null)
        {
            float topY = topGrabTarget.position.y;
            float bottomY = bottomGrabTarget != null ? bottomGrabTarget.position.y : topGrabTarget.position.y - 3f;
            Vector3 dir = bottomGrabTarget.forward;
            dir.y = 0;
            if (dir.magnitude > 0.01f) dir.Normalize();
            else dir = -topGrabTarget.forward;
            if (Mathf.Abs(grabDirectionAngleOffset) > 0.01f)
                dir = Quaternion.Euler(0, grabDirectionAngleOffset, 0) * dir;
            playerMovement.ForceStartClimbing(topGrabTarget.position, dir, topY, bottomY, -1);
        }
    }

    private IEnumerator CloseRoutine()
    {
        currentState = DoorState.Closing;

        if (audioSource != null && closeSound != null)
            audioSource.PlayOneShot(closeSound);

        float elapsed = 0f;
        Quaternion startRot = doorMeshTransform.localRotation;

        while (elapsed < closeDuration)
        {
            float t = elapsed / closeDuration;
            t = SmoothProgress(t);
            doorMeshTransform.localRotation = Quaternion.Slerp(startRot, closedLocalRotation, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        doorMeshTransform.localRotation = closedLocalRotation;
        currentState = DoorState.Closed;

        if (_doorCollider != null)
            _doorCollider.enabled = true;
    }

    private float SmoothProgress(float t)
    {
        return t * t * (3f - 2f * t);
    }

    public bool IsOpen() => currentState == DoorState.Open;
    public bool IsClosed() => currentState == DoorState.Closed;
    public bool IsAnimating() => currentState == DoorState.Opening || currentState == DoorState.Closing;
    public bool HasPlayerInside() => playerInsideCount > 0;

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<FPSMovement>() != null)
            OnPlayerEnterDetectionZone();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<FPSMovement>() != null)
            OnPlayerExitDetectionZone();
    }
}
