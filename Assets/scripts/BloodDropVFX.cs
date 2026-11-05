using UnityEngine;
using System.Collections;

public class BloodDropVFX : MonoBehaviour
{
    [Header("Drop setup")]
    public Transform dropStartPoint;
    public Transform dropEndPoint;
    public float dropDuration = 1.2f;
    public AnimationCurve fallCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Visual")]
    public GameObject dropVisual;
    public AudioSource dropSound;

    [Header("Audio timing")]
    public float soundLeadTime = 0.1f; // seconds before landing to fire the clip

    private GameObject spawnedDrop;
    private Coroutine fallRoutine;

    public void PlayDrop()
    {
        StopFall();
        spawnedDrop = Instantiate(dropVisual, dropStartPoint.position, Quaternion.identity);
        fallRoutine = StartCoroutine(FallRoutine());
    }

    IEnumerator FallRoutine()
    {
        float t = 0f;
        Vector3 start = dropStartPoint.position;
        Vector3 end = dropEndPoint.position;
        bool soundPlayed = false;

        while (t < dropDuration)
        {
            if (spawnedDrop == null) yield break;

            t += Time.deltaTime;
            float normalized = fallCurve.Evaluate(t / dropDuration);
            spawnedDrop.transform.position = Vector3.Lerp(start, end, normalized);

            if (!soundPlayed && (dropDuration - t) <= soundLeadTime)
            {
                if (dropSound != null) dropSound.Play();
                soundPlayed = true;
            }

            yield return null;
        }

        fallRoutine = null;
    }

    public void Hide()
    {
        StopFall();
        if (spawnedDrop != null) Destroy(spawnedDrop);
        spawnedDrop = null;
    }

    void StopFall()
    {
        if (fallRoutine != null)
        {
            StopCoroutine(fallRoutine);
            fallRoutine = null;
        }
        if (spawnedDrop != null) Destroy(spawnedDrop);
    }
}