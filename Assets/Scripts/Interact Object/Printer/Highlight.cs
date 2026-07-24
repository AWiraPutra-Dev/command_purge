using UnityEngine;

public class Highlight : MonoBehaviour
{
    private Renderer meshRenderer;
    private Color originalColor;

    void Awake()
    {
        meshRenderer = GetComponentInChildren<Renderer>();
        if (meshRenderer != null)
        {
            originalColor = meshRenderer.material.color;
        }
    }

    public void ToggleHighlight(bool status)
    {
        if (meshRenderer == null) return;

        if (status)
        {
            meshRenderer.material.color = Color.yellow;
        }
        else
        {
            meshRenderer.material.color = originalColor;
        }
    }
}
