using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PickableItem : MonoBehaviour, IInteractable
{
    [Header("Item Info")]
    public string itemName = "Kertas";

    [Header("Highlight Settings")]
    public Color highlightColor = Color.yellow;
    [Range(0.5f, 5f)] public float highlightIntensity = 2f;

    [Header("Held Position Offset (relatif ke HoldPosition)")]
    public Vector3 heldPositionOffset = new Vector3(0.03f, -0.04f, 0.05f);
    public Vector3 heldRotationOffset = new Vector3(0f, 5f, -15f);

    [Header("Pickup Animation")]
    public float pickupAnimDuration = 0.25f;
    public AnimationCurve pickupScaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public Color pickupFlashColor = Color.white;
    [Range(1f, 8f)] public float pickupFlashIntensity = 5f;
    public float pickupFlashDuration = 0.15f;

    private Renderer[] renderers;
    private MaterialPropertyBlock propBlock;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private Rigidbody rb;
    private Collider col;

    private bool isHighlighted;
    private float lastLookedTime = -10f;
    private const float LookTimeout = 0.15f;
    private bool isPickedUp;
    private bool isHeld;
    private Coroutine idleAnim;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    void Update()
    {
        if (isHighlighted && Time.time - lastLookedTime > LookTimeout)
        {
            SetHighlight(false);
        }
    }

    public string GetInteractText()
    {
        if (isPickedUp || !enabled) return "";
        lastLookedTime = Time.time;
        if (!isHighlighted) SetHighlight(true);
        return $"Press [E] to pick up {itemName}";
    }

    public void Interact()
    {
        if (isPickedUp) return;
        if (PlayerInventory.Instance == null)
        {
            Debug.LogWarning("PlayerInventory tidak ditemukan di scene!");
            return;
        }
        isPickedUp = true;
        SetHighlight(false);
        PlayerInventory.Instance.Hold(this);
    }

    public void SetHighlight(bool active)
    {
        isHighlighted = active;
        Color c = active ? highlightColor * highlightIntensity : Color.black;
        foreach (var rend in renderers)
        {
            rend.GetPropertyBlock(propBlock);
            propBlock.SetColor(EmissionColor, c);
            rend.SetPropertyBlock(propBlock);
        }
    }

    public void OnPickedUp(Transform holdParent)
    {
        // Matikan physics supaya transform bisa diatur manual
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Matikan collider supaya tidak kena raycast lagi
        if (col != null)
            col.enabled = false;

        // Parent ke hold position (atau langsung ke camera jika null)
        if (holdParent != null)
        {
            transform.SetParent(holdParent, false);
            transform.localPosition = heldPositionOffset;
            transform.localRotation = Quaternion.Euler(heldRotationOffset);
            Debug.Log($"[PickableItem] Parented to {holdParent.name}, pos={heldPositionOffset} rot={heldRotationOffset}");
        }
        else
        {
            // Fallback: cari camera langsung
            Camera cam = Camera.main;
            if (cam != null)
            {
                transform.SetParent(cam.transform, false);
                transform.localPosition = heldPositionOffset + new Vector3(0.35f, -0.45f, 0.65f);
                transform.localRotation = Quaternion.Euler(heldRotationOffset + new Vector3(10f, -20f, 0f));
                Debug.LogWarning("[PickableItem] Fallback: parented langsung ke Camera.main");
            }
            else
            {
                Debug.LogError("[PickableItem] Tidak ada holdParent dan tidak bisa cari Camera.main!");
            }
        }

        isHeld = true;
        StartCoroutine(PickupSequence());
    }

    private IEnumerator PickupSequence()
    {
        // Phase 1: scale in + flash
        Vector3 targetScale = transform.localScale;
        transform.localScale = Vector3.zero;

        float halfDuration = pickupAnimDuration * 0.5f;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            float t = pickupScaleCurve.Evaluate(elapsed / halfDuration);
            transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);

            float flashT = 1f - (elapsed / pickupFlashDuration);
            if (flashT > 0f)
            {
                Color flash = pickupFlashColor * pickupFlashIntensity * flashT;
                foreach (var rend in renderers)
                {
                    rend.GetPropertyBlock(propBlock);
                    propBlock.SetColor(EmissionColor, flash);
                    rend.SetPropertyBlock(propBlock);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = targetScale;

        // Phase 2: fade flash to normal
        float flashElapsed = 0f;
        float remainingFlash = halfDuration;
        while (flashElapsed < remainingFlash)
        {
            float t = 1f - (flashElapsed / remainingFlash);
            Color c = pickupFlashColor * pickupFlashIntensity * t;
            foreach (var rend in renderers)
            {
                rend.GetPropertyBlock(propBlock);
                propBlock.SetColor(EmissionColor, c);
                rend.SetPropertyBlock(propBlock);
            }
            flashElapsed += Time.deltaTime;
            yield return null;
        }

        foreach (var rend in renderers)
        {
            rend.GetPropertyBlock(propBlock);
            propBlock.SetColor(EmissionColor, Color.black);
            rend.SetPropertyBlock(propBlock);
        }

        // Phase 3: idle hold animation
        idleAnim = StartCoroutine(HoldIdleAnimation());
    }

    private IEnumerator HoldIdleAnimation()
    {
        float t = 0f;
        Vector3 basePos = transform.localPosition;
        Quaternion baseRot = transform.localRotation;

        while (isHeld)
        {
            float y = Mathf.Sin(t * 2f) * 0.003f;
            float rotZ = Mathf.Sin(t * 1.5f) * 1f;
            float rotX = Mathf.Cos(t * 1.8f) * 0.5f;
            transform.localPosition = basePos + new Vector3(0, y, 0);
            transform.localRotation = baseRot * Quaternion.Euler(rotX, 0, rotZ);
            t += Time.deltaTime;
            yield return null;
        }
    }

    void OnDestroy()
    {
        isHeld = false;
    }
}
