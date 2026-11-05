using UnityEngine;

public class PeripheralVignette : MonoBehaviour
{
    public static PeripheralVignette Instance { get; private set; }

    [Header("Refs")]
    public Renderer quadRenderer;

    [Header("Smoothing")]
    public float intensitySmoothing = 8f;
    public float sideSmoothing = 6f;

    private Material mat;
    private float targetIntensity, currentIntensity;
    private float targetSide, currentSide;
    private bool wasVisible;

    static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    static readonly int BiasId = Shader.PropertyToID("_Bias");

    void Awake()
    {
        Instance = this;
        if (quadRenderer == null) quadRenderer = GetComponent<Renderer>();
        if (quadRenderer == null)
        {
            Debug.LogError("[Vignette] no renderer assigned");
            enabled = false;
            return;
        }
        mat = quadRenderer.material; // per-instance copy
        quadRenderer.enabled = false;
        Debug.Log("[Vignette] ready");
    }

    // proximity01: 0..1, side: -1 left, +1 right, 0 symmetric
    public void SetIntensity(float proximity01, float side)
    {
        targetIntensity = Mathf.Clamp01(proximity01);
        targetSide = Mathf.Clamp(side, -1f, 1f);
    }

    public void Clear()
    {
        targetIntensity = 0f;
        Debug.Log("[Vignette] cleared");
    }

    void Update()
    {
        if (mat == null) return;

        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, 1f - Mathf.Exp(-intensitySmoothing * Time.deltaTime));
        currentSide = Mathf.Lerp(currentSide, targetSide, 1f - Mathf.Exp(-sideSmoothing * Time.deltaTime));

        bool visible = currentIntensity > 0.005f;
        if (visible != wasVisible)
        {
            quadRenderer.enabled = visible;
            wasVisible = visible;
            Debug.Log($"[Vignette] {(visible ? "shown" : "hidden")}");
        }
        if (!visible) return;

        mat.SetFloat(IntensityId, currentIntensity);
        mat.SetFloat(BiasId, currentSide);
    }

    void OnDestroy()
    {
        if (mat != null) Destroy(mat);
        if (Instance == this) Instance = null;
    }
}