using UnityEngine;
using System.Collections;

public class EntityA : MonoBehaviour, IEntity
{
    [Header("Proximity (0..1)")]
    public float startProximity = 0.2f;
    public float riseRate = 0.10f;   // per second while looking at the A section
    public float drainRate = 0.30f;  // per second while looking away
    public float failProximity = 1f;

    [Header("Fail rules")]
    public bool failOnBeyondDeskYaw = true;

    [Header("Timing (placeholder until manager)")]
    public bool useInternalLoop = true; // untick once the manager exists
    public float minInterval = 25f;
    public float maxInterval = 45f;
  

    [Header("Directional audio (looping, 3D)")]
    public AudioSource approachAudio;   // must be on its own child, not the manager object
    public float startPitch = 0.9f;
    public float maxPitch = 1.4f;
    public float startVolume = 0.15f;
    public float maxVolume = 1f;

    [Header("Haptics (stub)")]
    public float hapticMaxIntensity = 1f;

    public bool IsBusy { get; private set; }
    public float Proximity { get; private set; }

    private int sideSection;   // 1 or 3, locked at fire
    private float side;        // -1 left, +1 right
    private float nextLog;

    void Awake()
{
    if (approachAudio == null) { Debug.LogWarning("[EntityA] approachAudio not assigned"); return; }
    approachAudio.loop = true;
    approachAudio.dopplerLevel = 0f;
    approachAudio.spatialBlend = 1f;
}
    void StartAudio()
{
    if (approachAudio == null) return;
    var targets = HeadGazeTracker.Instance.sectionTargets;
    if (sideSection >= 1 && sideSection <= targets.Length && targets[sideSection - 1] != null)
        approachAudio.transform.position = targets[sideSection - 1].position;
    else
        Debug.LogWarning($"[EntityA] no sectionTarget for section {sideSection}, audio not positioned");

    approachAudio.pitch = startPitch;
    approachAudio.volume = startVolume;
    if (!approachAudio.isPlaying) approachAudio.Play();
    Debug.Log($"[EntityA] audio started at section {sideSection}");
}

    void UpdateAudio(float p)
    {
        if (approachAudio == null) return;
        approachAudio.pitch = Mathf.Lerp(startPitch, maxPitch, p);
        approachAudio.volume = Mathf.Lerp(startVolume, maxVolume, p);
    }

    void StopAudio()
    {
        if (approachAudio != null && approachAudio.isPlaying) approachAudio.Stop();
    }
    void OnEnable()
    {
        Debug.Log("[EntityA] enabled");
        if (useInternalLoop) StartCoroutine(ALoop());
    }

    void OnDisable()
    {
        StopAllCoroutines();
        IsBusy = false;
        PeripheralVignette.Instance?.Clear();
        SendHaptic(0f);
        StopAudio();
        Debug.Log("[EntityA] disabled, stopped");
    }

    public bool CanFire()
    {
        if (IsBusy || HeadGazeTracker.Instance == null) return false;
        int cur = HeadGazeTracker.Instance.CurrentSectionId;
        return cur == 1 || cur == 3;
    }

    public void Fire()
    {
        if (!CanFire()) { Debug.Log("[EntityA] Fire ignored (busy or no valid section)"); return; }
        StartCoroutine(Approach());
    }

    IEnumerator ALoop()
    {
        while (true)
        {
            float wait = Random.Range(minInterval, maxInterval);
            Debug.Log($"[EntityA] next approach in {wait:F1}s");
            yield return new WaitForSeconds(wait);

            while (!CanFire()) yield return null; // wait until player looks at 1 or 3
            yield return Approach();
        }
    }

    IEnumerator Approach()
    {
        IsBusy = true;
        sideSection = HeadGazeTracker.Instance.CurrentSectionId;
        side = sideSection == 1 ? -1f : 1f;
        Proximity = startProximity;
        Debug.Log($"[EntityA] approach started from section {sideSection} (side {side}), proximity {Proximity:F2}");
            StartAudio();
        while (true)
        {
            var gaze = HeadGazeTracker.Instance;
            bool lookingAtSide = gaze != null && gaze.CurrentSectionId == sideSection;

            Proximity += (lookingAtSide ? riseRate : -drainRate) * Time.deltaTime;
            Proximity = Mathf.Clamp01(Proximity);

            PeripheralVignette.Instance?.SetIntensity(Proximity, side);
            SendHaptic(Proximity);
            UpdateAudio(Proximity);

            if (Time.time >= nextLog)
            {
                Debug.Log($"[EntityA] proximity {Proximity:F2} (looking at side: {lookingAtSide})");
                nextLog = Time.time + 1f;
            }

            if (failOnBeyondDeskYaw && gaze != null && gaze.IsBeyondDeskYaw())
            {
                Fail("head turned past desk edge");
                yield break;
            }
            if (Proximity >= failProximity)
            {
                Fail("proximity maxed");
                yield break;
            }
            if (Proximity <= 0f)
            {
                Resolve();
                yield break;
            }
            yield return null;
        }
    }

    void Resolve()
    {
        Debug.Log("[EntityA] resolved, player looked away in time");
        PeripheralVignette.Instance?.Clear();
        SendHaptic(0f);
        StopAudio();
        IsBusy = false;
    }

    void Fail(string reason)
    {
        Debug.Log($"[EntityA] FAIL: {reason}");
        PeripheralVignette.Instance?.Clear();
        SendHaptic(0f);
        StopAudio();
        GameFlowManager.Instance?.TriggerFail($"EntityA: {reason} (section {sideSection})");
        enabled = false;
    }

    // TODO: wire to the ESP32 UDP sender (left motor for side -1, right for +1)
void SendHaptic(float proximity01)
{
    float intensity = proximity01 * hapticMaxIntensity;
    UdpHaptics.Send(side < 0 ? intensity : 0f, side > 0 ? intensity : 0f);
}
}