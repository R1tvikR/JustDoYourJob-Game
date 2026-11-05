using UnityEngine;
using System.Collections;

public class EntityE : MonoBehaviour
{
    [Header("Trigger timing (seconds since shift start)")]
    public float triggerWindowMin = 15f;
    public float triggerWindowMax = 30f;

    [Header("Gaze-check window after event fires")]
    public float gazeCheckDuration = 3f;

    [Header("Refs")]
    public BloodDropVFX bloodDropVFX;
    public MonoBehaviour[] entitiesToGate;

    [Header("Puddle")]
    public GameObject puddlePrefab;
    public Transform puddleSpawnPoint; // likely same as dropEndPoint

    public static bool Cleared { get; private set; } = false;

    private bool hasTriggered = false;
    private GameObject spawnedPuddle;

      void Awake() { Cleared = false; }

    void Start()
    {
        SetGatedEntities(false);
        StartCoroutine(ScheduleTrigger());
    }

    IEnumerator ScheduleTrigger()
    {
        float delay = Random.Range(triggerWindowMin, triggerWindowMax);
        yield return new WaitForSeconds(delay);

        if (hasTriggered) yield break;
        hasTriggered = true;

        TriggerEvent();
    }

    void TriggerEvent()
    {
        if (bloodDropVFX != null) bloodDropVFX.PlayDrop();
        SpawnPuddle();
        StartCoroutine(GazeCheckWindow());
    }

    void SpawnPuddle()
    {
        if (puddlePrefab == null || puddleSpawnPoint == null) return;
        if (spawnedPuddle != null) Destroy(spawnedPuddle);
        spawnedPuddle = Instantiate(puddlePrefab, puddleSpawnPoint.position, Quaternion.identity);
    }

    void RemovePuddle()
    {
        if (spawnedPuddle != null) Destroy(spawnedPuddle);
        spawnedPuddle = null;
    }

    IEnumerator GazeCheckWindow()
    {
        float elapsed = 0f;
        while (elapsed < gazeCheckDuration)
        {
            if (HeadGazeTracker.Instance != null && HeadGazeTracker.Instance.IsLookingUp())
            {
                Fail();
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        Clear();
    }

    void Fail()
    {
        if (bloodDropVFX != null) bloodDropVFX.Hide();
        RemovePuddle();
        GameFlowManager.Instance?.TriggerFail("EntityE: player looked up during blood drop event");
    }

    void Clear()
    {
        Cleared = true;
        if (bloodDropVFX != null) bloodDropVFX.Hide();
        RemovePuddle();
        SetGatedEntities(true);
        Debug.Log("Entity E cleared — A-D now active.");
    }

    void SetGatedEntities(bool state)
    {
        foreach (var e in entitiesToGate)
            if (e != null) e.enabled = state;
    }
}