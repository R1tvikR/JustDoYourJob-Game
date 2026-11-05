using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class DeathSequence : MonoBehaviour
{
    public static DeathSequence Instance { get; private set; }
    static bool restarting;   // survives scene reload, used for the fade-in

    [Header("Rig")]
    public Transform rig;     // XR Origin root
    public Transform head;    // Main Camera

    [Header("Pull-back")]
    public float pullDistance = 0.3f;
    public float pitchDegrees = 20f;
    public float pullDuration = 0.6f;

    [Header("Fade")]
    public Renderer fadeRenderer;   // black quad, child of the camera
    public float fadeDelay = 0.2f;
    public float fadeDuration = 0.4f;
    public float fadeInOnRestart = 1f;

    [Header("Audio / restart")]
    public AudioSource screamAudio; // 2D, Play On Awake off
    public float blackHold = 3f;

    [Header("Stop on death")]
    public MonoBehaviour[] entitiesToDisable;  // A, B, C, D, E, manager scheduler
    public UnityEvent onDeathStart;            // e.g. force-release held paper

    [Header("Win")]
    public AudioSource winAudio;       // 2D, Play On Awake off
    public float winFadeDuration = 2f;
    public float winHold = 5f;       
    public bool IsPlaying { get; private set; }

    void Awake()
    {
        Instance = this;
        SetFade(restarting ? 1f : 0f);
        if (restarting) { restarting = false; StartCoroutine(FadeIn()); }
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    public void Play(string reason)
    {
        if (IsPlaying) return;
        IsPlaying = true;
        Debug.Log($"[Death] start: {reason}");

        foreach (var e in entitiesToDisable) if (e != null) e.enabled = false;
        PeripheralVignette.Instance?.Clear();
        onDeathStart?.Invoke();
        UdpHaptics.Pulse(1f, 1f, 600);

        if (screamAudio != null)
        {
            screamAudio.spatialBlend = 0f;
            screamAudio.volume = 1f;
            screamAudio.Play();
        }
        StartCoroutine(Sequence());
    }
    IEnumerator WinSequence()
{
    for (float t = 0f; t < winFadeDuration; t += Time.deltaTime)
    {
        SetFade(t / winFadeDuration);
        yield return null;
    }
    SetFade(1f);

    float hold = (winAudio != null && winAudio.clip != null) ? winAudio.clip.length : winHold;
    yield return new WaitForSeconds(Mathf.Max(0f, hold - winFadeDuration));

    restarting = true;             // triggers the fade-in after reload
    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
}
    public void PlayWin()
{
    if (IsPlaying) return;         // shared guard: death and win can't both run
    IsPlaying = true;
    Debug.Log("[Win] shift end");

    foreach (var e in entitiesToDisable) if (e != null) e.enabled = false;
    PeripheralVignette.Instance?.Clear();
    UdpHaptics.Stop();

    if (winAudio != null) { winAudio.spatialBlend = 0f; winAudio.Play(); }
    StartCoroutine(WinSequence());
}
    IEnumerator Sequence()
    {
        Vector3 startPos = rig.position;
        Quaternion startRot = rig.rotation;
        Vector3 pivot = head.position;
        Vector3 right = head.right;
        Vector3 back = -Vector3.ProjectOnPlane(head.forward, Vector3.up);
        back = back.sqrMagnitude < 0.001f ? -rig.forward : back.normalized;

        float total = Mathf.Max(pullDuration, fadeDelay + fadeDuration);
        float t = 0f;
        while (t < total)
        {
            t += Time.deltaTime;
            float e = EaseOut(Mathf.Clamp01(t / pullDuration));
            rig.SetPositionAndRotation(startPos, startRot);
            rig.RotateAround(pivot, right, -pitchDegrees * e);  // negative = head tips back
            rig.position += back * pullDistance * e;
            SetFade(Mathf.Clamp01((t - fadeDelay) / fadeDuration));
            yield return null;
        }
        SetFade(1f);
        Debug.Log("[Death] black, restarting after hold");

        yield return new WaitForSeconds(blackHold);
        restarting = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    IEnumerator FadeIn()
    {
        for (float t = 0f; t < fadeInOnRestart; t += Time.deltaTime)
        {
            SetFade(1f - t / fadeInOnRestart);
            yield return null;
        }
        SetFade(0f);
    }

    static float EaseOut(float x) => 1f - Mathf.Pow(1f - x, 3f);

    void SetFade(float a)
    {
        if (fadeRenderer == null) return;
        var c = fadeRenderer.material.color;
        c.a = a;
        fadeRenderer.material.color = c;
        fadeRenderer.enabled = a > 0.001f;
    }
}