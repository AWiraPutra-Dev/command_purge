using UnityEngine;
using TMPro;

public class PlayerInteract : MonoBehaviour
{
    [Header("Referensi Objek")]
    public Transform playerCamera;
    public Transform pickupSlot;
    public LayerMask pickableLayer;

    [Header("Pengaturan Jarak")]
    public float hitRange = 3f;

    [Header("UI Interaction Prompt")]
    [SerializeField] private GameObject interactionPanel;
    [SerializeField] private TextMeshProUGUI interactionText;

    [Header("Pengaturan Visual Kertas Di Tangan")]
    [SerializeField] private float heldScale = 0.055f;
    [SerializeField] private Vector3 heldPositionOffset = new Vector3(-0.023f, -0.02f, 0.027f);
    [SerializeField] private Vector3 heldRotationOffset = new Vector3(-10f, 10f, 5f);

    private GameObject inHandItem;
    private Highlight currentHighlight;

    void Awake()
    {
        if (playerCamera == null)
        {
            Camera cam = FindFirstObjectByType<Camera>();
            if (cam != null) playerCamera = cam.transform;
        }
        if (pickupSlot == null && playerCamera != null)
            pickupSlot = playerCamera.Find("PickupSlot");
        if (pickableLayer == 0)
            pickableLayer = LayerMask.GetMask("Pickable");

    }

    void Update()
    {
        HandleRaycast();

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (inHandItem == null && currentHighlight != null)
            {
                TryPickUp(currentHighlight.gameObject);
            }
        }
    }

    private void HandleRaycast()
    {
        if (currentHighlight != null)
        {
            currentHighlight.ToggleHighlight(false);
            currentHighlight = null;
        }

        if (inHandItem != null)
        {
            // Jangan sentuh UI apapun — biar InteractionManager tetap bisa nampilin prompt ladder/computer
            return;
        }

        if (playerCamera == null) return;

        if (Physics.Raycast(playerCamera.position, playerCamera.forward, out RaycastHit hit, hitRange, pickableLayer, QueryTriggerInteraction.Collide))
        {
            Highlight highlight = hit.collider.GetComponent<Highlight>();
            if (highlight != null)
            {
                highlight.ToggleHighlight(true);
                currentHighlight = highlight;
                ShowInteractionUI("Press [E] to pick up the paper");
            }
            else
            {
                HideInteractionUI();
            }
        }
        else
        {
            HideInteractionUI();
        }
    }

    private void ShowInteractionUI(string text)
    {
        if (interactionPanel != null) interactionPanel.SetActive(true);
        if (interactionText != null) interactionText.text = text;
    }

    private void HideInteractionUI()
    {
        if (interactionPanel != null && interactionPanel.activeSelf)
            interactionPanel.SetActive(false);
    }

    private void TryPickUp(GameObject item)
    {
        if (item.GetComponent<PaperItem>() != null)
        {
            inHandItem = item;

            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            item.transform.SetParent(pickupSlot, false);

            item.transform.localPosition = heldPositionOffset;
            item.transform.localRotation = Quaternion.Euler(heldRotationOffset);
            item.transform.localScale = new Vector3(heldScale, heldScale, heldScale);

            // Daftarkan ke PlayerInventory supaya PipeSlot bisa deteksi
            PickableItem pickable = item.GetComponent<PickableItem>();
            if (pickable != null && PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.heldItem = pickable;
            }

            // Matikan collider supaya tidak trigger event aneh
            Collider col = item.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // Apply generated paper texture + material anti-clipping
            // MODIFIKASI material asli (URP Lit printer), bukan ganti shader!
            Renderer[] renderers = item.GetComponentsInChildren<Renderer>();
            Texture2D paperTex = PaperTextureGenerator.Generate(256, 256);
            foreach (Renderer rend in renderers)
            {
                Material mat = rend.material;
                mat.mainTexture = paperTex;
                mat.color = Color.white;
                mat.SetFloat("_Surface", 0f); // Opaque
                mat.SetColor("_EmissionColor", Color.white * 0.3f); // subtle glow biar selalu terang
                mat.EnableKeyword("_EMISSION");

                // ZTest Always + ZWrite ON + Cull Off
                mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                mat.SetInt("_ZWrite", 1);
                mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                mat.renderQueue = 2000;

                rend.material = mat;

                // Expand mesh bounds supaya renderer tidak di-frustum cull
                MeshFilter mf = rend.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    mf.sharedMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100);
                }
            }

            HideInteractionUI();

            if (currentHighlight != null)
            {
                currentHighlight.ToggleHighlight(false);
                currentHighlight = null;
            }
        }
    }
}
