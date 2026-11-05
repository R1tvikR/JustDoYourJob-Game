using UnityEngine;
using System.Collections;

public class EntityD : MonoBehaviour, IEntity
{
    [Header("Decoy audio (one-shot, 3D spatial)")]
    public AudioSource decoyAudio;
    public AudioClip section1DecoyClip;
    public AudioClip section3DecoyClip;
    public Transform section1Source;
    public Transform section3Source;

    [Header("Timing")]
    public bool useInternalLoop = true; // untick once the manager exists
    public float minInterval = 20f;
    public float maxInterval = 40f;
    public float gazeCheckDuration = 3f;
    public float failGazeTime = 0.4f;

    public bool IsBusy { get; private set; }

    void Awake()
    {
        if (decoyAudio != null)
        {
            decoyAudio.loop = false;
            decoyAudio.spatialBlend = 1f;
            decoyAudio.dopplerLevel = 0f;
        }
        else Debug.LogWarning("[EntityD] decoyAudio not assigned");
    }

    void OnEnable()
    {
        Debug.Log("[EntityD] enabled");
        if (useInternalLoop) StartCoroutine(DecoyLoop());
    }

    void OnDisable()
    {
        StopAllCoroutines();
        IsBusy = false;
        if (decoyAudio != null) decoyAudio.Stop();
        Debug.Log("[EntityD] disabled, stopped");
    }

    public void Fire()
    {
        if (IsBusy) { Debug.Log("[EntityD] Fire ignored, busy"); return; }
        StartCoroutine(FireOnce());
    }

    public bool CanFire() => !IsBusy;

    IEnumerator DecoyLoop()
    {
        while (true)
        {
            float wait = Random.Range(minInterval, maxInterval);
            Debug.Log($"[EntityD] next decoy in {wait:F1}s");
            yield return new WaitForSeconds(wait);
            yield return FireOnce();
        }
    }

    IEnumerator FireOnce()
    {
        IsBusy = true;
        int decoySection = PickDecoySection();
        int cur = HeadGazeTracker.Instance != null ? HeadGazeTracker.Instance.CurrentSectionId : 0;
        Debug.Log($"[EntityD] decoy fired at section {decoySection} (player on {cur})");

        PlayDecoy(decoySection);
        yield return GazeCheck(decoySection);
        IsBusy = false;
    }

    int PickDecoySection()
    {
        int current = HeadGazeTracker.Instance != null ? HeadGazeTracker.Instance.CurrentSectionId : 0;
        if (current == 1) return 3;
        if (current == 3) return 1;
        return Random.value < 0.5f ? 1 : 3;
    }

    void PlayDecoy(int section)
    {
        if (decoyAudio == null) return;
        Transform src = section == 1 ? section1Source : section3Source;
        AudioClip clip = section == 1 ? section1DecoyClip : section3DecoyClip;
        if (src != null) decoyAudio.transform.position = src.position;
        if (clip == null)
        {
            Debug.LogWarning($"[EntityD] no decoy clip assigned for section {section}");
            return;
        }
        decoyAudio.PlayOneShot(clip);
        Debug.Log($"[EntityD] playing decoy clip '{clip.name}' at section {section}");
    }

    IEnumerator GazeCheck(int decoySection)
    {
        float elapsed = 0f, gazeHeld = 0f;
        Debug.Log($"[EntityD] gaze window open ({gazeCheckDuration}s) on section {decoySection}");

        while (elapsed < gazeCheckDuration)
        {
            bool looking = HeadGazeTracker.Instance != null &&
                           HeadGazeTracker.Instance.CurrentSectionId == decoySection;
            gazeHeld = looking ? gazeHeld + Time.deltaTime : 0f;

            if (gazeHeld >= failGazeTime)
            {
                Fail(decoySection);
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        Debug.Log("[EntityD] window closed, player ignored decoy");
    }

    void Fail(int section)
    {
        Debug.Log($"[EntityD] FAIL: looked at decoy section {section}");
        if (decoyAudio != null) decoyAudio.Stop();
        GameFlowManager.Instance?.TriggerFail($"EntityD: looked toward decoy at section {section}");
        enabled = false;
    }
}