using UnityEngine;
using System.Collections;

public class EntityC : MonoBehaviour, IEntity
{
    [Header("Bell audio (looping, 3D spatial)")]
    public AudioSource bellAudio;   // own child object, not the manager
    public float startPitch = 0.8f;
    public float maxPitch = 1.8f;
    public float startVolume = 0.2f;
    public float maxVolume = 1f;

    [Header("Timing")]
    public float minIdleToFire = 6f;   // target section must be this idle for CanFire
    public float resolveWindow = 18f;  // seconds to complete a task there before fail

    public bool IsBusy { get; private set; }

    private int targetSection;
    private float elapsed;
    private float nextLog;
    private bool subscribed;

    void Awake()
    {
        if (bellAudio == null) { Debug.LogWarning("[EntityC] bellAudio not assigned"); return; }
        bellAudio.loop = true;
        bellAudio.dopplerLevel = 0f;
        bellAudio.spatialBlend = 1f;
    }

    void OnEnable() => Debug.Log("[EntityC] enabled");

    void OnDisable()
    {
        StopAllCoroutines();
        Unsubscribe();
        StopRinging();
        IsBusy = false;
        Debug.Log("[EntityC] disabled, stopped");
    }

    public bool CanFire()
    {
        if (IsBusy) return false;
        var idleT = SectionIdleTracker.Instance;
        var gaze = HeadGazeTracker.Instance;
        if (idleT == null || gaze == null || TaskManager.Instance == null) return false;

        int s = PickTarget();
        if (s < 1) return false;
        return idleT.GetIdleTime(s) >= minIdleToFire;
    }

    public void Fire()
    {
        if (!CanFire()) { Debug.Log("[EntityC] Fire ignored (busy or no valid section)"); return; }
        StartCoroutine(Ring());
    }

    // Most idle section among those that currently hold a paper; 0 if none
    int PickTarget()
    {
        var idleT = SectionIdleTracker.Instance;
        var gaze = HeadGazeTracker.Instance;
        int best = 0;
        float bestIdle = -1f;
        for (int s = 1; s <= gaze.sectionTargets.Length; s++)
        {
            if (!PaperObject.AnyInSection(s)) continue;
            float idle = idleT.GetIdleTime(s);
            if (idle > bestIdle) { bestIdle = idle; best = s; }
        }
        return best;
    }

    IEnumerator Ring()
    {
        targetSection = PickTarget(); // locked once
        if (targetSection < 1)
        {
            Debug.Log("[EntityC] no section with a paper, aborting");
            yield break;
        }

        IsBusy = true;
        elapsed = 0f;
        Subscribe();

        var target = HeadGazeTracker.Instance.sectionTargets[targetSection - 1];
        if (bellAudio != null && target != null) bellAudio.transform.position = target.position;
        else Debug.LogWarning($"[EntityC] no sectionTarget/audio for section {targetSection}");

        StartRinging();
        Debug.Log($"[EntityC] fired, locked to section {targetSection}, window {resolveWindow}s");

        while (elapsed < resolveWindow)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / resolveWindow);
            if (bellAudio != null)
            {
                bellAudio.pitch = Mathf.Lerp(startPitch, maxPitch, t);
                bellAudio.volume = Mathf.Lerp(startVolume, maxVolume, t);
            }
            if (Time.time >= nextLog)
            {
                Debug.Log($"[EntityC] section {targetSection}, elapsed {elapsed:F1}/{resolveWindow}s");
                nextLog = Time.time + 1f;
            }
            yield return null;
        }
        Fail();
    }

    void HandleTaskCompleted(int sectionId)
    {
        if (!IsBusy || sectionId != targetSection) return;
        Resolve();
    }

    void Resolve()
    {
        Debug.Log($"[EntityC] resolved, task completed in section {targetSection}");
        StopAllCoroutines();
        Unsubscribe();
        StopRinging();
        IsBusy = false;
    }

    void Fail()
    {
        Debug.Log($"[EntityC] FAIL: section {targetSection} not served in time");
        StopRinging();
        GameFlowManager.Instance?.TriggerFail($"EntityC: section {targetSection} ignored too long");
        enabled = false; // OnDisable cleans up
    }

    void Subscribe()
    {
        if (subscribed || TaskManager.Instance == null) return;
        TaskManager.Instance.OnTaskCompleted += HandleTaskCompleted;
        subscribed = true;
    }

    void Unsubscribe()
    {
        if (!subscribed) return;
        if (TaskManager.Instance != null) TaskManager.Instance.OnTaskCompleted -= HandleTaskCompleted;
        subscribed = false;
    }

    void StartRinging()
    {
        if (bellAudio != null && !bellAudio.isPlaying) bellAudio.Play();
    }

    void StopRinging()
    {
        if (bellAudio != null && bellAudio.isPlaying) bellAudio.Stop();
    }
}