using UnityEngine;
using System.Collections;

public class ComputerController : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private Transform cameraSitPosition;
    [SerializeField] private FPSMovement playerMovement;
    [SerializeField] private Transform playerCameraTransform;
    [SerializeField] private GameObject crosshairUI;

    [Header("Cinemachine (Unity 6 Virtual Camera)")]
    [SerializeField] private GameObject cinemachineCameraObject;

    [Header("Settings")]
    [SerializeField] private float lerpSpeed = 0.6f;
    [SerializeField] private float lookSensitivity = 0.5f;
    [SerializeField] private float maxLookAngle = 15f;

    private bool isUsing = false;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private Coroutine cameraAnimCoroutine;
    private Transform originalParent;
    private Quaternion _computerBaseRotation;
    private float _computerLookYaw;

    private void Awake()
    {
        if (crosshairUI == null)
            crosshairUI = GameObject.Find("Crosshair");
    }

    public bool IsUsing => isUsing;

    public void Interact()
    {
        if (cameraAnimCoroutine != null) return;

        if (playerCameraTransform == null || cameraSitPosition == null || playerMovement == null)
        {
            Debug.LogError($"[{gameObject.name}] Tolong lengkapi slot variable di Inspector! Ada yang masih kosong.", this);
            return;
        }

        if (isUsing)
            cameraAnimCoroutine = StartCoroutine(ExitComputerAnimation());
        else
            cameraAnimCoroutine = StartCoroutine(EnterComputerAnimation());
    }

    public string GetInteractText()
    {
        return isUsing ? "Press [E] to Quit" : "Press [E] to Use Computer";
    }

    void Update()
    {
        if (!isUsing) return;

        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        _computerLookYaw = Mathf.Clamp(_computerLookYaw + mouseX, -maxLookAngle, maxLookAngle);
        playerCameraTransform.rotation = _computerBaseRotation * Quaternion.Euler(0, _computerLookYaw, 0);
    }

    private IEnumerator EnterComputerAnimation()
    {
        playerMovement.enabled = false;

        originalParent = playerCameraTransform.parent;
        originalCameraPosition = playerCameraTransform.position;
        originalCameraRotation = playerCameraTransform.rotation;

        if (cinemachineCameraObject != null) cinemachineCameraObject.SetActive(false);

        playerCameraTransform.SetParent(null);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (crosshairUI != null) crosshairUI.SetActive(false);

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            playerCameraTransform.position = Vector3.Lerp(originalCameraPosition, cameraSitPosition.position, elapsed);
            playerCameraTransform.rotation = Quaternion.Slerp(originalCameraRotation, cameraSitPosition.rotation, elapsed);
            elapsed += Time.deltaTime * lerpSpeed;
            yield return null;
        }

        playerCameraTransform.position = cameraSitPosition.position;
        playerCameraTransform.rotation = cameraSitPosition.rotation;

        _computerBaseRotation = playerCameraTransform.rotation;
        _computerLookYaw = 0f;

        isUsing = true;
        cameraAnimCoroutine = null;
    }

    private IEnumerator ExitComputerAnimation()
    {
        Vector3 startPos = playerCameraTransform.position;
        Quaternion startRot = playerCameraTransform.rotation;

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            playerCameraTransform.position = Vector3.Lerp(startPos, originalCameraPosition, elapsed);
            playerCameraTransform.rotation = Quaternion.Slerp(startRot, originalCameraRotation, elapsed);
            elapsed += Time.deltaTime * lerpSpeed;
            yield return null;
        }

        playerCameraTransform.position = originalCameraPosition;
        playerCameraTransform.rotation = originalCameraRotation;

        playerCameraTransform.SetParent(originalParent);

        if (cinemachineCameraObject != null) cinemachineCameraObject.SetActive(true);
        playerMovement.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (crosshairUI != null) crosshairUI.SetActive(true);

        isUsing = false;
        cameraAnimCoroutine = null;
    }
}
