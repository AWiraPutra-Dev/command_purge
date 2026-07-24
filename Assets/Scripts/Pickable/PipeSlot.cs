using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PipeSlot : MonoBehaviour, IInteractable
{
    [Header("Slot Settings")]
    public string acceptedItemName = "Kertas";

    [Header("Idle Glow (selalu menyala redup)")]
    public Color idleGlowColor = new Color(0.2f, 0.6f, 1f);
    [Range(0.1f, 2f)] public float idleIntensity = 0.4f;

    [Header("Active Glow (dilihat + item cocok)")]
    public Color activeGlowColor = Color.green;
    [Range(1f, 6f)] public float activeIntensity = 3f;

    [Header("Reject Glow (dilihat + item TIDAK cocok)")]
    public Color rejectGlowColor = Color.red;
    [Range(1f, 6f)] public float rejectIntensity = 2f;

    [Header("Store Animation")]
    public float storeAnimDuration = 0.3f;
    public AnimationCurve storeScaleCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    public Color storeFlashColor = Color.green;
    [Range(1f, 8f)] public float storeFlashIntensity = 4f;

    private Renderer[] renderers;
    private MaterialPropertyBlock propBlock;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    private float lastLookedTime = -10f;
    private const float LookTimeout = 0.15f;
    private bool isBeingLooked;
    private bool isAnimating;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();
        ApplyGlow(idleGlowColor * idleIntensity);
    }

    void Update()
    {
        if (isBeingLooked && Time.time - lastLookedTime > LookTimeout)
        {
            isBeingLooked = false;
            if (!isAnimating)
                ApplyGlow(idleGlowColor * idleIntensity);
        }
    }

    public string GetInteractText()
    {
        lastLookedTime = Time.time;
        isBeingLooked = true;

        if (isAnimating)
            return "";

        bool holding = PlayerInventory.Instance != null && PlayerInventory.Instance.IsHolding;

        if (!holding)
        {
            if (!isAnimating)
                ApplyGlow(idleGlowColor * idleIntensity);
            return "Paper storage slot";
        }

        bool matches = PlayerInventory.Instance.heldItem.itemName == acceptedItemName;

        if (matches)
        {
            if (!isAnimating)
                ApplyGlow(activeGlowColor * activeIntensity);
            return $"Press [E] to store {acceptedItemName}";
        }
        else
        {
            if (!isAnimating)
                ApplyGlow(rejectGlowColor * rejectIntensity);
            return $"{PlayerInventory.Instance.heldItem.itemName} cannot be stored here";
        }
    }

    public void Interact()
    {
        if (isAnimating) return;
        if (PlayerInventory.Instance == null || !PlayerInventory.Instance.IsHolding)
        {
            Debug.Log("Nothing is being held.");
            return;
        }

        var held = PlayerInventory.Instance.heldItem;

        if (held.itemName != acceptedItemName)
        {
            Debug.Log($"{held.itemName} cannot be stored in this pipe!");
            return;
        }

        Debug.Log($"{acceptedItemName} successfully stored in pipe!");
        isAnimating = true;
        StartCoroutine(StoreAnimation(held.gameObject));
    }

    private IEnumerator StoreAnimation(GameObject itemObject)
    {
        // Un-parent from camera so it stays in world space during animation
        itemObject.transform.SetParent(null, true);

        // Phase 1: flash + shrink item
        float half = storeAnimDuration * 0.5f;
        float elapsed = 0f;
        Vector3 startScale = itemObject.transform.localScale;

        while (elapsed < half)
        {
            float t = storeScaleCurve.Evaluate(elapsed / half);
            itemObject.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            float flashT = 1f - (elapsed / half);
            Color flash = storeFlashColor * storeFlashIntensity * flashT;
            ApplyGlow(flash);

            elapsed += Time.deltaTime;
            yield return null;
        }

        itemObject.transform.localScale = Vector3.zero;

        // Phase 2: pipe glow returns to idle
        float idleElapsed = 0f;
        while (idleElapsed < half)
        {
            float t = idleElapsed / half;
            Color c = Color.Lerp(storeFlashColor * storeFlashIntensity, idleGlowColor * idleIntensity, t);
            ApplyGlow(c);
            idleElapsed += Time.deltaTime;
            yield return null;
        }

        // Destroy item & release inventory
        Destroy(itemObject);
        PlayerInventory.Instance.Release();

        ApplyGlow(idleGlowColor * idleIntensity);
        isAnimating = false;
    }

    void ApplyGlow(Color c)
    {
        foreach (var rend in renderers)
        {
            rend.GetPropertyBlock(propBlock);
            propBlock.SetColor(EmissionColor, c);
            rend.SetPropertyBlock(propBlock);
        }
    }
}
