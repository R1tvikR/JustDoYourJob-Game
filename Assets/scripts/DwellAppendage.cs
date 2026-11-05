using UnityEngine;
using System.Collections;

public class DwellAppendage : MonoBehaviour
{
    [Header("Prefab + reference points")]
    public GameObject appendagePrefab;
    public Transform hiddenPoint;
    public Transform risenPoint;
    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Descent")]
    public float descentDuration = 1.5f;

    private Transform spawnedVisual;
    private Coroutine descentRoutine;
    private float currentT = 0f; // tracks last known progress, 0..1

    void Awake()
    {
        if (appendagePrefab != null && hiddenPoint != null)
        {
            GameObject instance = Instantiate(appendagePrefab, hiddenPoint.position, hiddenPoint.rotation, transform);
            spawnedVisual = instance.transform;
        }
    }

    public void SetProgress(float t)
    {
        if (spawnedVisual == null || hiddenPoint == null || risenPoint == null) return;

        // cancel any in-progress descent if dwell resumes
        if (descentRoutine != null)
        {
            StopCoroutine(descentRoutine);
            descentRoutine = null;
        }

        currentT = Mathf.Clamp01(t);
        float eased = riseCurve.Evaluate(currentT);
        spawnedVisual.position = Vector3.Lerp(hiddenPoint.position, risenPoint.position, eased);
    }

    public void ResetVisual()
    {
        if (spawnedVisual == null || hiddenPoint == null) return;
        if (descentRoutine != null) StopCoroutine(descentRoutine);
        descentRoutine = StartCoroutine(DescendRoutine());
    }

    IEnumerator DescendRoutine()
    {
        float startT = currentT;
        float elapsed = 0f;

        while (elapsed < descentDuration && startT > 0f)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Lerp(startT, 0f, elapsed / descentDuration);
            float eased = riseCurve.Evaluate(t);
            spawnedVisual.position = Vector3.Lerp(hiddenPoint.position, risenPoint.position, eased);
            yield return null;
        }

        spawnedVisual.position = hiddenPoint.position;
        currentT = 0f;
        descentRoutine = null;
    }
}