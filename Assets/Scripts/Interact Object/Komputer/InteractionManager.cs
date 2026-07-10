using UnityEngine;
using TMPro; // Wajib mengimport TextMeshPro

public class InteractionManager : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private Transform playerCamera;

    [Header("UI References")]
    [SerializeField] private GameObject interactionUIPanel; // Induk GameObject UI
    [SerializeField] private TextMeshProUGUI interactionText; // Komponen Teks TMP

    private ComputerController activeComputer;

    void Update()
    {
        // JIKA SEDANG LOCK-IN DI KOMPUTER
        if (activeComputer != null && activeComputer.IsUsing)
        {
            // Pastikan UI Prompt tetap menyala menampilkan "Press [E] to Quit"
            if (interactionUIPanel != null && !interactionUIPanel.activeSelf)
                interactionUIPanel.SetActive(true);
            
            if (interactionText != null)
                interactionText.text = activeComputer.GetInteractText();

            // Deteksi tombol E untuk keluar dari mode komputer
            if (Input.GetKeyDown(KeyCode.E))
            {
                activeComputer.Interact();
                // Jika setelah interact statusnya sudah keluar (false), lepas referensinya
                if (!activeComputer.IsUsing)
                {
                    activeComputer = null;
                    HideUI();
                }
            }
            return; // Lewati deteksi Raycast biasa karena sedang fokus di komputer
        }

        // MODE NORMAL (MENGGUNAKAN RAYCAST)
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                // Tampilkan UI dan ubah teksnya sesuai objek yang dilihat
                if (interactionUIPanel != null) interactionUIPanel.SetActive(true);
                if (interactionText != null) interactionText.text = interactable.GetInteractText();

                if (Input.GetKeyDown(KeyCode.E))
                {
                    interactable.Interact();

                    // Jika yang diinteraksi adalah komputer, simpan referensinya untuk mode lock-in
                    ComputerController comp = hit.collider.GetComponent<ComputerController>();
                    if (comp != null && comp.IsUsing)
                    {
                        activeComputer = comp;
                    }
                }
            }
            else
            {
                HideUI();
            }
        }
        else
        {
            HideUI();
        }
    }

    private void HideUI()
    {
        if (interactionUIPanel != null && interactionUIPanel.activeSelf)
        {
            interactionUIPanel.SetActive(false);
        }
    }
}