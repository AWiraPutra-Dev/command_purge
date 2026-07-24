using UnityEngine;
using System.Collections;

public class PrinterController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform paperAnimObject;
    [SerializeField] private Transform paperRetractedMarker;
    [SerializeField] private Transform paperExtendedMarker;
    [SerializeField] private GameObject paperPickupObject;

    [Header("Animation Settings")]
    [SerializeField] private float extendDuration = 5f;

    private void Start()
    {
        if (paperPickupObject != null)
            paperPickupObject.SetActive(false);

        if (paperAnimObject != null && paperRetractedMarker != null && paperExtendedMarker != null)
            StartCoroutine(PrintAnimation());
    }

    private IEnumerator PrintAnimation()
    {
        float elapsed = 0f;
        Vector3 from = paperRetractedMarker.localPosition;
        Vector3 to = paperExtendedMarker.localPosition;

        while (elapsed < extendDuration)
        {
            float t = elapsed / extendDuration;
            t = t * t * (3f - 2f * t);
            paperAnimObject.localPosition = Vector3.Lerp(from, to, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        paperAnimObject.localPosition = to;
        paperAnimObject.gameObject.SetActive(false);

        if (paperPickupObject != null)
            paperPickupObject.SetActive(true);
    }
}
