using UnityEngine;
using System.Collections;

public class StairInteraction : MonoBehaviour, IInteractable
{
    [Header("Target Positions")]
    [SerializeField] private Transform topTarget;
    [SerializeField] private Transform bottomTarget;

    [Header("Player References")]
    [SerializeField] private FPSMovement playerMovement;
    [SerializeField] private Transform playerCameraTransform;
    [SerializeField] private GameObject cinemachineCameraObject;
    [SerializeField] private CharacterController characterController;

    [Header("Trap Door (optional)")]
    [SerializeField] private TrapDoorController trapDoor;

    [Header("Climb Settings")]
    [SerializeField] private float climbDuration = 1.0f;
    [SerializeField] private float midpointY = 0f;

    [Header("Interaction Text")]
    [SerializeField] private string climbDownPrompt = "Press [E] to Climb Down";
    [SerializeField] private string climbUpPrompt = "Press [E] to Climb Up";

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip climbSound;

    private Transform playerBodyTransform;
    private bool isClimbing = false;
    private Coroutine climbCoroutine;

    void Awake()
    {
        if (playerCameraTransform != null)
            playerBodyTransform = playerCameraTransform.parent;
    }

    public string GetInteractText()
    {
        if (isClimbing) return "";

        bool isAbove = IsPlayerAbove();

        if (!isAbove && trapDoor != null && !trapDoor.IsOpen())
            return "Trap door is closed...";

        return isAbove ? climbDownPrompt : climbUpPrompt;
    }

    public void Interact()
    {
        if (isClimbing || climbCoroutine != null) return;

        bool isAbove = IsPlayerAbove();

        if (!isAbove && trapDoor != null && !trapDoor.IsOpen())
            return;

        climbCoroutine = StartCoroutine(ClimbRoutine(isAbove));
    }

    private bool IsPlayerAbove()
    {
        if (playerBodyTransform == null) return true;
        return playerBodyTransform.position.y > midpointY;
    }

    private IEnumerator ClimbRoutine(bool climbingDown)
    {
        isClimbing = true;

        if (playerMovement != null) playerMovement.enabled = false;
        if (characterController != null) characterController.enabled = false;
        if (cinemachineCameraObject != null) cinemachineCameraObject.SetActive(false);

        if (audioSource != null && climbSound != null)
            audioSource.PlayOneShot(climbSound);

        Vector3 targetPos = climbingDown ? bottomTarget.position : topTarget.position;
        Quaternion targetRot = climbingDown ? bottomTarget.rotation : topTarget.rotation;

        Vector3 startPos = playerBodyTransform.position;
        Quaternion startRot = playerCameraTransform.rotation;

        playerCameraTransform.SetParent(null);

        float elapsed = 0f;
        while (elapsed < climbDuration)
        {
            float t = elapsed / climbDuration;
            t = SmoothStep(t);

            playerBodyTransform.position = Vector3.Lerp(startPos, targetPos, t);
            playerCameraTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        playerBodyTransform.position = targetPos;
        playerCameraTransform.rotation = targetRot;

        if (cinemachineCameraObject != null) cinemachineCameraObject.SetActive(true);

        playerCameraTransform.SetParent(playerBodyTransform);
        playerCameraTransform.localPosition = new Vector3(0, 0.6f, 0);
        playerCameraTransform.localRotation = Quaternion.identity;

        if (characterController != null) characterController.enabled = true;
        if (playerMovement != null) playerMovement.enabled = true;

        isClimbing = false;
        climbCoroutine = null;
    }

    private float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }

    public bool IsClimbing() => isClimbing;
}
