using UnityEngine;
using System.Collections;

public class ComputerController : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private Transform cameraSitPosition;
    [SerializeField] private FPSMovement playerMovement;
    [SerializeField] private Transform playerCameraTransform; // Wajib diisi Main Camera!

    [Header("Cinemachine (Unity 6 Virtual Camera)")]
    // Diubah ke GameObject agar mutlak aman dari bug NullReference di Unity 6
    [SerializeField] private GameObject cinemachineCameraObject; 

    [Header("Settings")]
    [SerializeField] private float lerpSpeed = 2f;

    private bool isUsing = false;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private Coroutine cameraAnimCoroutine;
    private Transform originalParent;

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

    private IEnumerator EnterComputerAnimation()
    {
        playerMovement.enabled = false;

        // Catat koordinat posisi asli sebelum dilepas parent-nya
        originalParent = playerCameraTransform.parent;
        originalCameraPosition = playerCameraTransform.position;
        originalCameraRotation = playerCameraTransform.rotation;

        // Matikan Virtual Camera secara total agar Main Camera lepas dari kendali Cinemachine
        if (cinemachineCameraObject != null) cinemachineCameraObject.SetActive(false);

        // Lepas dari parent sementara agar pergerakan Lerp murni menggunakan koordinat global dunia
        playerCameraTransform.SetParent(null);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

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

        // Kembalikan Main Camera ke struktur hierarchy player semula
        playerCameraTransform.SetParent(originalParent);

        // Hidupkan kembali virtual camera agar kembali nempel mengikuti player
        if (cinemachineCameraObject != null) cinemachineCameraObject.SetActive(true);
        playerMovement.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        isUsing = false;
        cameraAnimCoroutine = null;
    }
}