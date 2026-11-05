using UnityEngine;
using System;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance;

    public int quota = 20;
    public int processedCount = 0;

    // Other scripts (dwell timers, idle timers for Entities B/C later) subscribe to this
    public event Action<int> OnTaskCompleted; // passes sectionId
    public event Action OnQuotaReached;

    void Awake()
    {
        Instance = this; // simple singleton, fine for a solo scene like this
    }

    public void CompleteTask(int sectionId)
    {
        processedCount++;
        Debug.Log($"Task completed in Section {sectionId}. Total: {processedCount}/{quota}");

        OnTaskCompleted?.Invoke(sectionId);

        if (processedCount >= quota)
        {
            Debug.Log("Quota reached — shift complete!");
            OnQuotaReached?.Invoke();
        }
    }
}