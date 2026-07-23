using UnityEngine;

public enum SurfaceType
{
    Concrete,
    Wood,
    Dirt,
    Metal
}

public class SurfaceDefinition : MonoBehaviour
{
    public SurfaceType surfaceType;
}
