using UnityEngine;
using System;

public class HeadGazeTracker : MonoBehaviour
{
    public static HeadGazeTracker Instance;

    [Header("Section targets, index 0 = Section 1, etc.")]
    public Transform[] sectionTargets = new Transform[3];

    [Header("Desk reference for Entity A's yaw-out-of-bounds check")]
    public Transform deskForwardReference; // an empty facing straight at the desk center

    public float gazeAngleThreshold = 20f;
    public float deskYawMaxAngle = 70f; // beyond this = looking away from desk entirely
    public float lookUpPitchThreshold = 30f;

    public int CurrentSectionId { get; private set; } = 0; // 0 = none

    public event Action<int, int> OnSectionChanged; // (fromSection, toSection)

    void Update()
    {
        int detected = 0;
        float bestAngle = gazeAngleThreshold;

        for (int i = 0; i < sectionTargets.Length; i++)
        {
            if (sectionTargets[i] == null) continue;
            Vector3 dir = (sectionTargets[i].position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dir);
            if (angle < bestAngle)
            {
                bestAngle = angle;
                detected = i + 1; // section IDs are 1-based
            }
        }

        if (detected != CurrentSectionId)
        {
            OnSectionChanged?.Invoke(CurrentSectionId, detected);
            CurrentSectionId = detected;
        }
    }

    // For Entity A: has the player turned away from the desk entirely?
    public bool IsBeyondDeskYaw()
    {
        if (deskForwardReference == null) return false;
        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        Vector3 flatDeskForward = Vector3.ProjectOnPlane(deskForwardReference.forward, Vector3.up);
        return Vector3.Angle(flatForward, flatDeskForward) > deskYawMaxAngle;
    }

    // For Entity E / D: is the player looking up?
    public bool IsLookingUp()
    {
        return transform.eulerAngles.x < 360f - lookUpPitchThreshold && transform.eulerAngles.x > 270f;
        // adjust if pitch convention differs; test in Play and tune threshold
    }

    void Awake() => Instance = this;
}