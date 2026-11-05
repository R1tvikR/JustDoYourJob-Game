using UnityEngine;

public class SectionIdleTracker : MonoBehaviour
{
    public static SectionIdleTracker Instance;
    private float[] idleTimes = new float[4]; // time since last completed task, index 1-3

    void Awake() => Instance = this;

    void OnEnable() => TaskManager.Instance.OnTaskCompleted += HandleTaskCompleted;
    void OnDisable() { if (TaskManager.Instance != null) TaskManager.Instance.OnTaskCompleted -= HandleTaskCompleted; }

    void HandleTaskCompleted(int sectionId)
    {
        if (sectionId >= 1 && sectionId <= 3) idleTimes[sectionId] = 0f;
        Debug.Log(sectionId);
    }

    void Update()
    {
        for (int i = 1; i <= 3; i++) idleTimes[i] += Time.deltaTime;
    }

    public float GetIdleTime(int sectionId) => (sectionId >= 1 && sectionId <= 3) ? idleTimes[sectionId] : 0f;

    public int GetMostIdleSection()
    {
        int maxId = 1;
        for (int i = 2; i <= 3; i++) if (idleTimes[i] > idleTimes[maxId]) maxId = i;
        return maxId;
    }
}