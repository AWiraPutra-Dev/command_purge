using System.Collections;
using UnityEngine;

public class WindowInteraction : MonoBehaviour, IInteractable
{
    [Header("Camera Positions")]
    [SerializeField] private Transform peepCameraPosition;
    [SerializeField] private Transform mainCameraTransform;
    [SerializeField] private GameObject cinemachineCameraObject;

    [Header("Player Movement Script")]
    [SerializeField] private MonoBehaviour playerMovement;

    [Header("UI")]
    [SerializeField] private GameObject crosshairUI;

    [Header("Settings")]
    [SerializeField] private float lerpSpeed = 5f;

    private Transform originalParent;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;

    private bool isPeeping = false;
    public bool IsPeeping => isPeeping;

    private Coroutine cameraAnimCoroutine;

    private void Awake()
    {
        if (crosshairUI == null)
            crosshairUI = GameObject.Find("Crosshair");
    }

    public string GetInteractText()
    {
        return isPeeping ? "Press [E] to close" : "Press [E] to Look Outside";
    }

    public void Interact()
    {
        if (cameraAnimCoroutine != null) return;

        if (isPeeping)
            cameraAnimCoroutine = StartCoroutine(ExitPeepAnimation());
        else
            cameraAnimCoroutine = StartCoroutine(EnterPeepAnimation());
    }

    private IEnumerator EnterPeepAnimation()
    {
        playerMovement.enabled = false;

        originalParent = mainCameraTransform.parent;
        originalCameraPosition = mainCameraTransform.position;
        originalCameraRotation = mainCameraTransform.rotation;

        if (cinemachineCameraObject != null) cinemachineCameraObject.SetActive(false);
        mainCameraTransform.SetParent(null);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (crosshairUI != null) crosshairUI.SetActive(false);

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            mainCameraTransform.position = Vector3.Lerp(originalCameraPosition, peepCameraPosition.position, elapsed);
            mainCameraTransform.rotation = Quaternion.Slerp(originalCameraRotation, peepCameraPosition.rotation, elapsed);
            elapsed += Time.deltaTime * lerpSpeed;
            yield return null;
        }

        mainCameraTransform.position = peepCameraPosition.position;
        mainCameraTransform.rotation = peepCameraPosition.rotation;

        isPeeping = true;
        cameraAnimCoroutine = null;
    }

    private IEnumerator ExitPeepAnimation()
    {
        Vector3 startPos = mainCameraTransform.position;
        Quaternion startRot = mainCameraTransform.rotation;

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            mainCameraTransform.position = Vector3.Lerp(startPos, originalCameraPosition, elapsed);
            mainCameraTransform.rotation = Quaternion.Slerp(startRot, originalCameraRotation, elapsed);
            elapsed += Time.deltaTime * lerpSpeed;
            yield return null;
        }

        mainCameraTransform.SetParent(originalParent);
        mainCameraTransform.position = originalCameraPosition;
        mainCameraTransform.rotation = originalCameraRotation;

        if (cinemachineCameraObject != null) cinemachineCameraObject.SetActive(true);
        playerMovement.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (crosshairUI != null) crosshairUI.SetActive(true);

        isPeeping = false;
        cameraAnimCoroutine = null;
    }
}
