using UnityEngine;
using System.Collections;

public class EntityB : MonoBehaviour, IEntity
{
    [Header("Arm window")]
    public float armWindow = 18f;   // seconds dwell counts after Fire()

    [Header("Dwell thresholds (seconds)")]
    public float warnThreshold = 4f;
    public float failThreshold = 7f;

    [Header("Warning feedback")]
    public AudioSource warnAudio;           // one-shot cue at warnThreshold
    public AudioSource risingLoopAudio;     // loop while dwelling
    public float hapticWarnIntensity = 0.4f; // TODO: wire to ESP32 UDP

    [Header("Per-section dwell appendages (index 0 = Section 1, etc.)")]
    public DwellAppendage[] dwellAppendages = new DwellAppendage[3];

    public bool IsBusy { get; private set; }

    private bool armed;
    private bool subscribed;
    private float dwellTimer;
    private bool warnFired;
    private int trackedSection;

    void OnDisable()
    {
        StopAllCoroutines();
        Unsubscribe();
        StopLoop();
        armed = false;
        IsBusy = false;
        Debug.Log("[EntityB] disabled, stopped");
    }

    public bool CanFire() => !IsBusy && HeadGazeTracker.Instance != null;

    public void Fire()
    {
        if (!CanFire()) { Debug.Log("[EntityB] Fire ignored (busy or no gaze tracker)"); return; }
        StartCoroutine(ArmRoutine());
    }

    IEnumerator ArmRoutine()
    {
        IsBusy = true;
        armed = true;
        trackedSection = HeadGazeTracker.Instance.CurrentSectionId;
        dwellTimer = 0f;
        warnFired = false;
        Subscribe();
        Debug.Log($"[EntityB] armed for {armWindow}s, starting on section {trackedSection}");

        yield return new WaitForSeconds(armWindow);

        // Window over: stop counting, let the appendage fall, then free the entity
        armed = false;
        Unsubscribe();
        StopLoop();
        var app = GetAppendage(trackedSection);
        app?.ResetVisual();
        dwellTimer = 0f;
        warnFired = false;
        Debug.Log("[EntityB] window ended, appendage descending");

        if (app != null) yield return new WaitForSeconds(app.descentDuration + 0.1f);
        IsBusy = false;
        Debug.Log("[EntityB] resolved");
    }

    void HandleSectionChanged(int from, int to)
    {
        if (!armed) return;
        GetAppendage(from)?.ResetVisual();
        StopLoop();
        trackedSection = to;
        dwellTimer = 0f;
        warnFired = false;
        Debug.Log($"[EntityB] section {from} -> {to}, dwell reset");
    }

    void Update()
    {
        if (!armed || trackedSection == 0) return;

        dwellTimer += Time.deltaTime;
        GetAppendage(trackedSection)?.SetProgress(dwellTimer / failThreshold);

        if (risingLoopAudio != null && !risingLoopAudio.isPlaying)
            risingLoopAudio.Play();

      if (!warnFired && dwellTimer >= warnThreshold)
{
    warnFired = true;
    if (warnAudio != null) warnAudio.Play();
    UdpHaptics.Pulse(hapticWarnIntensity, hapticWarnIntensity, 300);   // <- new
    Debug.Log($"[EntityB] warning in section {trackedSection}");
}

        if (dwellTimer >= failThreshold) Fail();
    }

    void Fail()
    {
        Debug.Log($"[EntityB] FAIL: dwell timeout in section {trackedSection}");
        StopLoop();
        GameFlowManager.Instance?.TriggerFail($"EntityB: dwell timeout in section {trackedSection}");
        enabled = false; // OnDisable cleans up; appendage stays risen as the fail visual
    }

    DwellAppendage GetAppendage(int sectionId)
    {
        int i = sectionId - 1;
        if (i < 0 || i >= dwellAppendages.Length) return null;
        return dwellAppendages[i];
    }

    void Subscribe()
    {
        if (subscribed || HeadGazeTracker.Instance == null) return;
        HeadGazeTracker.Instance.OnSectionChanged += HandleSectionChanged;
        subscribed = true;
    }

    void Unsubscribe()
    {
        if (!subscribed) return;
        if (HeadGazeTracker.Instance != null) HeadGazeTracker.Instance.OnSectionChanged -= HandleSectionChanged;
        subscribed = false;
    }

    void StopLoop()
    {
        if (risingLoopAudio != null && risingLoopAudio.isPlaying) risingLoopAudio.Stop();
    }
}