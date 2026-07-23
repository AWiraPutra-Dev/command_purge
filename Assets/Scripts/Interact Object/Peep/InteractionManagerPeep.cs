using UnityEngine;
using TMPro;

public class InteractionManagerPeep : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private float interactionDistance = 1f;
    [SerializeField] private float windowMinDistance = 0.5f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private Transform playerCamera;

    [Header("UI Canvas References")]
    [SerializeField] private GameObject interactionUIPanel;
    [SerializeField] private TextMeshProUGUI interactionText;

    private ComputerController _activeComputer;
    private WindowInteraction _activeWindow;
    private FPSMovement _fpsMovement;

    void Awake()
    {
        _fpsMovement = FindFirstObjectByType<FPSMovement>();
    }

    void Update()
    {
        if (_fpsMovement == null)
            _fpsMovement = FindFirstObjectByType<FPSMovement>();

        if (_fpsMovement != null && (_fpsMovement.IsClimbing() || _fpsMovement.IsTransitioningToLadder()))
        {
            return;
        }

        if (_activeWindow != null && _activeWindow.IsPeeping)
        {
            if (interactionUIPanel != null && !interactionUIPanel.activeSelf)
                interactionUIPanel.SetActive(true);

            if (interactionText != null)
                interactionText.text = _activeWindow.GetInteractText();

            if (Input.GetKeyDown(KeyCode.E))
            {
                _activeWindow.Interact();
                if (!_activeWindow.IsPeeping)
                {
                    _activeWindow = null;
                    HideUI();
                }
            }
            return;
        }

        if (_activeComputer != null && _activeComputer.IsUsing)
        {
            if (interactionUIPanel != null && !interactionUIPanel.activeSelf)
                interactionUIPanel.SetActive(true);

            if (interactionText != null)
                interactionText.text = _activeComputer.GetInteractText();

            if (Input.GetKeyDown(KeyCode.E))
            {
                _activeComputer.Interact();
                if (!_activeComputer.IsUsing)
                {
                    _activeComputer = null;
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
                if (interactable is WindowInteraction && hit.distance > windowMinDistance)
                {
                    HideUI();
                    return;
                }

                if (interactionUIPanel != null) interactionUIPanel.SetActive(true);
                if (interactionText != null) interactionText.text = interactable.GetInteractText();

                if (Input.GetKeyDown(KeyCode.E))
                {
                    interactable.Interact();

                    WindowInteraction window = hit.collider.GetComponent<WindowInteraction>();
                    if (window != null) _activeWindow = window;

                    ComputerController comp = hit.collider.GetComponent<ComputerController>();
                    if (comp != null) _activeComputer = comp;
                }
                return;
            }
        }
        HideUI();
    }

    private void HideUI()
    {
        if (interactionUIPanel != null && interactionUIPanel.activeSelf)
        {
            interactionUIPanel.SetActive(false);
        }
    }
}
