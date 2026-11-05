using UnityEngine;

public class VignetteTest : MonoBehaviour
{
    [Range(0f, 1f)] public float intensity;
    [Range(-1f, 1f)] public float side;

    void Update()
    {
        PeripheralVignette.Instance?.SetIntensity(intensity, side);
    }
}