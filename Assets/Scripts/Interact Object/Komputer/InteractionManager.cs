using UnityEngine;
using TMPro;

public class InteractionManager : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private float interactionDistance = 1f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private Transform playerCamera;

    [Header("UI References")]
    [SerializeField] private GameObject interactionUIPanel;
    [SerializeField] private TextMeshProUGUI interactionText;

    private ComputerController activeComputer;
    private FPSMovement _fpsMovement;

    void Awake()
    {
        _fpsMovement = FindFirstObjectByType<FPSMovement>();
    }

    void Update()
    {
        if (_fpsMovement == null)
            _fpsMovement = FindFirstObjectByType<FPSMovement>();

        if (_fpsMovement != null && _fpsMovement.IsClimbing())
        {
            return;
        }

        if (activeComputer != null && activeComputer.IsUsing)
        {
            if (interactionUIPanel != null && !interactionUIPanel.activeSelf)
                interactionUIPanel.SetActive(true);

            if (interactionText != null)
                interactionText.text = activeComputer.GetInteractText();

            if (Input.GetKeyDown(KeyCode.E))
            {
                activeComputer.Interact();
                if (!activeComputer.IsUsing)
                {
                    activeComputer = null;
                    HideUI();
                }
            }
            return;
        }

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactableLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                if (interactionUIPanel != null) interactionUIPanel.SetActive(true);
                if (interactionText != null) interactionText.text = interactable.GetInteractText();

                if (Input.GetKeyDown(KeyCode.E))
                {
                    interactable.Interact();

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
