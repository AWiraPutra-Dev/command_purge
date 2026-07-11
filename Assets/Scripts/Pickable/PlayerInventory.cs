using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    [Header("Hold Visual")]
    [SerializeField] private Transform holdPosition;

    [HideInInspector] public PickableItem heldItem;

    public bool IsHolding => heldItem != null;

    public Transform HoldPosition => holdPosition;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Ada lebih dari satu PlayerInventory di scene! Menghapus yang duplikat.");
            Destroy(this);
            return;
        }
        Instance = this;

        if (holdPosition == null)
            AutoCreateHoldPosition();
    }

    private void AutoCreateHoldPosition()
    {
        Camera cam = GetComponentInChildren<Camera>();
        if (cam == null)
            cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("Tidak bisa auto-create HoldPosition karena tidak ada Camera ditemukan.");
            return;
        }

        GameObject go = new GameObject("HoldPosition");
        go.transform.SetParent(cam.transform, false);
        go.transform.localPosition = new Vector3(0.35f, -0.45f, 0.65f);
        go.transform.localRotation = Quaternion.Euler(10f, -20f, 0f);
        holdPosition = go.transform;
        Debug.Log("HoldPosition auto-created under " + cam.name);
    }

    public void Hold(PickableItem item)
    {
        if (IsHolding)
        {
            Debug.Log("Tangan sudah penuh, taruh dulu barang yang dibawa sekarang.");
            return;
        }

        Debug.Log($"[PlayerInventory] Hold item: {item.name}, holdPosition={(holdPosition != null ? holdPosition.name : "NULL")}");

        // Final fallback: jika holdPosition masih null, cari camera langsung
        Transform target = holdPosition;
        if (target == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                target = cam.transform;
                Debug.LogWarning("[PlayerInventory] Fallback: holdPosition null, pakai Camera.main");
            }
        }

        heldItem = item;
        item.OnPickedUp(target);
    }

    public void Release()
    {
        heldItem = null;
    }
}
