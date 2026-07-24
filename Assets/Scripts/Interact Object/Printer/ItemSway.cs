using UnityEngine;

public class ItemSway : MonoBehaviour
{
    [Header("Sway Settings")]
    public float swayAmount = 0.02f;
    public float maxSwayAmount = 0.06f;
    public float smoothSway = 6f;

    private Vector3 initialPosition;

    void Start()
    {
        initialPosition = transform.localPosition;
    }

    void Update()
    {
        float mouseX = -Input.GetAxis("Mouse X") * swayAmount;
        float mouseY = -Input.GetAxis("Mouse Y") * swayAmount;

        float moveX = -Input.GetAxis("Horizontal") * swayAmount;
        float moveY = -Input.GetAxis("Vertical") * swayAmount;

        float finalMoveX = Mathf.Clamp(mouseX + moveX, -maxSwayAmount, maxSwayAmount);
        float finalMoveY = Mathf.Clamp(mouseY + moveY, -maxSwayAmount, maxSwayAmount);

        Vector3 finalPosition = new Vector3(finalMoveX, finalMoveY, 0);

        transform.localPosition = Vector3.Lerp(transform.localPosition, finalPosition + initialPosition, Time.deltaTime * smoothSway);
    }
}
