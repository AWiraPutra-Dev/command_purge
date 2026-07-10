using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WindowInteraction : MonoBehaviour, IInteractable
{
    [Header("Camera Positions")]
    [SerializeField] private Transform peepCameraPosition;
    [SerializeField] private Transform mainCameraTransform;
    [SerializeField] private GameObject cinemachineCameraObject;

    [Header("Player Movement Script")]
    [SerializeField] private MonoBehaviour playerMovement;

    [Header("UI Canvas & HUD References")]
    [SerializeField] private GameObject windowUIPanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI promptHintText; // Tarik Teks Petunjuk Bawah ke sini (misal: "Press [E] to Look Outside")
    [SerializeField] private GameObject crosshairObject; 

    [Header("Button Components")]
    [SerializeField] private Button buttonAllow; 
    [SerializeField] private Button buttonDeny;  

    [Header("Peep Camera Look Limits (Horizontal X Only)")]
    [SerializeField] private float clampAngleX = 40f; 
    [SerializeField] private float mouseSensitivity = 2f;

    [Header("Movement Settings")]
    [SerializeField] private float lerpSpeed = 5f;

    private Transform originalParent;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    
    private bool isPeeping = false;
    public bool IsPeeping => isPeeping;

    // --- SISTEM DATA DIALOG ---
    private List<string> dialogueLines = new List<string>();
    private int currentLineIndex = 0;
    private bool isDialogueActive = false;
    private bool isOptionPhase = false; // Flag penanda jika teks habis dan wajib klik opsi

    private float _peepYaw = 0f;
    private Quaternion basePeepRotation;

    private void Start()
    {
        if (buttonAllow != null) buttonAllow.onClick.AddListener(OnClickAllow);
        if (buttonDeny != null) buttonDeny.onClick.AddListener(OnClickDeny);
    }

    public string GetInteractText()
    {
        return "Press [E] to Look Outside";
    }

    public void Interact()
    {
        // Pemicu pertama untuk masuk mengintip jendela jika belum masuk
        if (!isPeeping)
        {
            StartCoroutine(EnterPeepAnimation());
        }
    }

   private void Update()
{
    if (isPeeping)
    {
        // Crosshair wajib mati total saat mengintip
        if (crosshairObject != null && crosshairObject.activeSelf) crosshairObject.SetActive(false);

        // --- 1. KONTROL KEYBOARD SEBELUM FASE OPSI (MASIH BACA CERITA) ---
        if (!isOptionPhase)
        {
            // Tekan ENTER untuk maju ke baris kalimat selanjutnya
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                NextLine();
            }

            // Tekan E kapan saja saat membaca untuk langsung KELUAR/BATAL
            if (Input.GetKeyDown(KeyCode.E))
            {
                ExitPeep();
            }

            // Logika rotasi kamera menengok kanan-kiri
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            _peepYaw += mouseX;
            _peepYaw = Mathf.Clamp(_peepYaw, -clampAngleX, clampAngleX);
            mainCameraTransform.rotation = basePeepRotation * Quaternion.Euler(0, _peepYaw, 0);
        }
        // --- 2. KONTROL SAAT FASE OPSI (TEKS HABIS, WAJIB KLIK MOUSE) ---
        else
        {
            // Paksa kursor terbuka setiap frame agar tidak dilock oleh script movement/kamera lain
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            
            // Opsional: Jika kamu ingin pemain bisa keluar pakai tombol Escape/E saat darurat stuck
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ExitPeep();
            }
        }
    }
}

    private IEnumerator EnterPeepAnimation()
    {
        playerMovement.enabled = false;
        if (crosshairObject != null) crosshairObject.SetActive(false);

        originalParent = mainCameraTransform.parent;
        originalCameraPosition = mainCameraTransform.position;
        originalCameraRotation = mainCameraTransform.rotation;

        if (cinemachineCameraObject != null) cinemachineCameraObject.SetActive(false);
        mainCameraTransform.SetParent(null);

        // --- DAFTAR SKENARIO DIALOG PANJANG ---
        dialogueLines.Clear();
        dialogueLines.Add("\"Halo...? Apakah ada orang di dalam? Tolong saya...\"");
        dialogueLines.Add("\"Sesuatu... sesuatu yang mengerikan sedang mengejarku dari arah hutan!\"");
        dialogueLines.Add("\"Badan saya gemetar, di luar dingin sekali. Tolong buka pintunya sekarang!!\"");
        dialogueLines.Add("\"Aku mohon... demi Tuhan, aku bener-bener manusia, bukan monster itu!\"");

        currentLineIndex = 0;
        isDialogueActive = true;
        isOptionPhase = false;
        _peepYaw = 0f; 
        basePeepRotation = peepCameraPosition.rotation;

        // Amankan tombol agar tidak muncul di awal cerita
        if (buttonAllow != null) buttonAllow.gameObject.SetActive(false);
        if (buttonDeny != null) buttonDeny.gameObject.SetActive(false);

        // Set teks pembuka dan petunjuk Enter bawah layar
        if (dialogueText != null) dialogueText.text = dialogueLines[currentLineIndex];
        if (promptHintText != null) promptHintText.text = "Tekan [Enter] untuk lanjut / [E] untuk keluar";

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (windowUIPanel != null) windowUIPanel.SetActive(true);

        // Animasi transisi kamera meluncur ke jendela
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
    }

    public void NextLine()
    {
        currentLineIndex++;

        // Jika kalimat cerita berikutnya masih tersedia
        if (currentLineIndex < dialogueLines.Count)
        {
            if (dialogueText != null) dialogueText.text = dialogueLines[currentLineIndex];
        }
        // JIKA DIALOG SUDAH HABIS: Masuk Fase Opsi (Kunci Keyboard, Buka Mouse)
        else
        {
            isDialogueActive = false;
            isOptionPhase = true; // Kunci kontrol keyboard aktif

            if (dialogueText != null) dialogueText.text = "Apa keputusanmu? Izinkan dia masuk?";
            if (promptHintText != null) promptHintText.text = "Pilih opsi menggunakan Mouse Anda";

            // Munculkan tombol pilihan dengan nama kustom dinamis
            if (buttonAllow != null)
            {
                buttonAllow.gameObject.SetActive(true);
                TextMeshProUGUI t = buttonAllow.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) t.text = "Izinkan Masuk (Allow)";
            }

            if (buttonDeny != null)
            {
                buttonDeny.gameObject.SetActive(true);
                TextMeshProUGUI t = buttonDeny.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) t.text = "Tolak & Biarkan (Deny)";
            }

            // Bebaskan kursor mouse agar fokus memilih tombol
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void OnClickAllow()
    {
        Debug.Log("Pilihan diambil: Allow.");
        ExitPeep();
    }

    public void OnClickDeny()
    {
        Debug.Log("Pilihan diambil: Deny.");
        ExitPeep();
    }

    public void ExitPeep()
    {
        if (isPeeping)
        {
            StartCoroutine(ExitPeepAnimation());
        }
    }

    private IEnumerator ExitPeepAnimation()
    {
        if (windowUIPanel != null) windowUIPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        float elapsed = 0f;
        Vector3 currentCamPos = mainCameraTransform.position;
        Quaternion currentCamRot = mainCameraTransform.rotation;

        while (elapsed < 1f)
        {
            mainCameraTransform.position = Vector3.Lerp(currentCamPos, originalCameraPosition, elapsed);
            mainCameraTransform.rotation = Quaternion.Slerp(currentCamRot, originalCameraRotation, elapsed);
            elapsed += Time.deltaTime * lerpSpeed;
            yield return null;
        }

        mainCameraTransform.SetParent(originalParent);
        mainCameraTransform.position = originalCameraPosition;
        mainCameraTransform.rotation = originalCameraRotation;

        if (cinemachineCameraObject != null) cinemachineCameraObject.SetActive(true);
        playerMovement.enabled = true;
        
        if (crosshairObject != null) crosshairObject.SetActive(true);

        isPeeping = false;
        isOptionPhase = false;
    }
}