using UnityEngine;

public class PaperPickupHandler : MonoBehaviour, IInteractable
{
    private bool isPickedUp;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public string GetInteractText()
    {
        return isPickedUp ? "" : "Press E to pick up the paper";
    }

    public void Interact()
    {
        if (isPickedUp) return;
        isPickedUp = true;

        if (rb != null) rb.isKinematic = true;

        Transform pickupSlot = Camera.main.transform.Find("PickupSlot");
        if (pickupSlot != null)
        {
            transform.SetParent(pickupSlot, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }
}
