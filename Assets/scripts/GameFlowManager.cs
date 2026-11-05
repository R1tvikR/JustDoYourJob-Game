using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }
    public bool IsGameOver { get; private set; } = false;
    public bool NightComplete { get; private set; } = false;

    [System.Serializable]
    public class EntitySlot
    {
        public MonoBehaviour entity;   // must implement IEntity
        public float weight = 1f;
    }

    [Header("Night")]
    public PaperSpawner paperSpawner;
    public int totalPapers = 30;
    public float spawnStagger = 2f;

    [Header("Entity roll (FNAF-style)")]
    public EntitySlot[] entities;
    public float tickInterval = 1f;
    public float baseChance = 0.02f;   // chance after a reset
    public float chanceStep = 0.02f;   // added each eligible tick
    public float minGapAfterResolve = 8f;

    private int papersSpawned, papersDisposed;
    private float chance;
    private float cooldownUntil;
    private bool wasBusy;
    private string activeEntity = "";  // NEW: for dashboard state

    void Awake() => Instance = this;

    void Start()
    {
        chance = baseChance;
        Debug.Log($"[Night] start: {totalPapers} papers, tick {tickInterval}s");
        CloudLog.Event("night_start", $"papers={totalPapers}");           // NEW
        CloudLog.State("running", "", 0);                                  // NEW
        StartCoroutine(SpawnRoutine());
        StartCoroutine(RollRoutine());
    }

    // Call from DisposalBin when a paper is successfully disposed
    public void OnPaperDisposed()
    {
        if (IsGameOver || NightComplete) return;
        papersDisposed++;
        Debug.Log($"[Night] disposed {papersDisposed}/{totalPapers}");
        CloudLog.State("running", activeEntity, papersDisposed);           // NEW
        if (papersDisposed >= totalPapers) EndNight();
    }

    IEnumerator SpawnRoutine()
    {
        while (papersSpawned < totalPapers && !IsGameOver && !NightComplete)
        {
            if (paperSpawner == null) { Debug.LogError("[Night] paperSpawner not assigned"); yield break; }
            paperSpawner.SpawnPaper();
            papersSpawned++;
            Debug.Log($"[Night] spawned {papersSpawned}/{totalPapers}");
            if (spawnStagger > 0f) yield return new WaitForSeconds(spawnStagger);
            else yield return null;
        }
    }

    IEnumerator RollRoutine()
    {
        var wait = new WaitForSeconds(tickInterval);
        while (!IsGameOver && !NightComplete)
        {
            yield return wait;
            Tick();
        }
        Debug.Log("[Night] roll loop stopped");
    }

    void Tick()
    {
        if (!EntityE.Cleared) return;
        if (IsGameOver || NightComplete) return;

        bool anyBusy = false;
        foreach (var s in entities)
            if (s.entity is IEntity e && s.entity.isActiveAndEnabled && e.IsBusy) { anyBusy = true; break; }

        if (wasBusy && !anyBusy)
        {
            cooldownUntil = Time.time + minGapAfterResolve;
            Debug.Log($"[Night] entity resolved, cooldown {minGapAfterResolve}s");
            CloudLog.Event("entity_resolved", activeEntity);               // NEW
            activeEntity = "";                                             // NEW
            CloudLog.State("running", "", papersDisposed);                 // NEW
        }
        wasBusy = anyBusy;

        if (anyBusy || Time.time < cooldownUntil) return;

        chance = Mathf.Clamp01(chance + chanceStep);
        float roll = Random.value;
        Debug.Log($"[Night] roll {roll:F2} vs chance {chance:F2}");
        if (roll >= chance) return;

        var pick = PickEntity();
        if (pick == null) { Debug.Log("[Night] roll hit but no eligible entity"); return; }

        activeEntity = pick.GetType().Name;                                // NEW
        Debug.Log($"[Night] firing {activeEntity}");
        CloudLog.Event("entity_fired", activeEntity);                      // NEW
        CloudLog.State("running", activeEntity, papersDisposed);           // NEW
        pick.Fire();
        chance = baseChance;
    }

    IEntity PickEntity()
    {
        var eligible = new List<EntitySlot>();
        float total = 0f;
        foreach (var s in entities)
        {
            if (s.entity is IEntity e && s.entity.isActiveAndEnabled && e.CanFire() && s.weight > 0f)
            {
                eligible.Add(s);
                total += s.weight;
            }
        }
        if (eligible.Count == 0) return null;

        float r = Random.value * total;
        foreach (var s in eligible)
        {
            r -= s.weight;
            if (r <= 0f) return (IEntity)s.entity;
        }
        return (IEntity)eligible[eligible.Count - 1].entity;
    }

    void EndNight()
    {
        NightComplete = true;
        Debug.Log("[Night] COMPLETE: all papers disposed");
        CloudLog.Event("win", $"disposed={papersDisposed}");               // NEW
        CloudLog.State("won", "", papersDisposed);                         // NEW
        foreach (var s in entities)
            if (s.entity != null) s.entity.enabled = false;
        DeathSequence.Instance?.PlayWin();
    }

    public void TriggerFail(string reason)
    {
        if (IsGameOver || NightComplete) return;
        IsGameOver = true;
        Debug.Log($"[GAME FAIL] {reason}");
        CloudLog.Event("fail", reason);                                    // NEW
        CloudLog.State("failed", activeEntity, papersDisposed);            // NEW
        foreach (var s in entities)
            if (s.entity != null) s.entity.enabled = false;
        DeathSequence.Instance?.Play(reason);
    }
}