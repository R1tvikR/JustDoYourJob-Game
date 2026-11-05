using UnityEngine;

public class SectionDwellTracker : MonoBehaviour
{
    public static SectionDwellTracker Instance;
    private float[] dwellTimes = new float[4]; // index 1-3 used, 0 unused

    void Awake() => Instance = this;

    void OnEnable() => HeadGazeTracker.Instance.OnSectionChanged += HandleSectionChanged;
    void OnDisable() { if (HeadGazeTracker.Instance != null) HeadGazeTracker.Instance.OnSectionChanged -= HandleSectionChanged; }

    void HandleSectionChanged(int from, int to)
    {
        if (from >= 1 && from <= 3) dwellTimes[from] = 0f; // reset on leaving a section
            Debug.Log("From: " + from);
            Debug.Log("To: " + to);
    }

    void Update()
    {
        int current = HeadGazeTracker.Instance.CurrentSectionId;
        if (current >= 1 && current <= 3) dwellTimes[current] += Time.deltaTime;
    }

    public float GetDwellTime(int sectionId) => (sectionId >= 1 && sectionId <= 3) ? dwellTimes[sectionId] : 0f;
}