using UnityEngine;

public enum StampType { Tick, Cross }

[RequireComponent(typeof(Rigidbody))]
public class StampTool : MonoBehaviour
{
    public StampType stampType;
    public float minDownwardSpeed = 0.5f;

    Rigidbody _rb;

    void Awake() => _rb = GetComponent<Rigidbody>();

    void OnTriggerEnter(Collider other)
    {
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.IsGameOver) return;
        PaperObject paper = other.GetComponentInParent<PaperObject>();
        if (paper == null || paper.currentStage != PaperStage.Sorted) return;

        float downwardSpeed = -_rb.linearVelocity.y;
        if (downwardSpeed < minDownwardSpeed) return;

        RegisterStamp(paper);
    }


    void RegisterStamp(PaperObject paper)
    {
        if (Section3Feed.Instance == null) { Debug.LogError("[StampTool] Section3Feed.Instance is null, stamp ignored"); return; }
        bool correct = (stampType == StampType.Tick && !paper.isMismatch) || (stampType == StampType.Cross && paper.isMismatch);
        if (!correct) return; // wrong stamp: no-op

        paper.stampResult = stampType == StampType.Tick ? StampResult.Tick : StampResult.Cross;
        paper.currentStage = PaperStage.Stamped;
        paper.ShowStampVerdict(stampType == StampType.Tick);
        TaskManager.Instance.CompleteTask(2);

        AdvanceToSection3(paper);
    }
    void AdvanceToSection3(PaperObject paper)
    {
        if (Section3Feed.Instance == null) return;

        paper.ForceReleaseFromHand();
        Rigidbody paperRb = paper.GetComponent<Rigidbody>();
        paperRb.linearVelocity = Vector3.zero;
        paperRb.angularVelocity = Vector3.zero;
        Transform feed = Section3Feed.Instance.feedPoint;
        paper.transform.SetPositionAndRotation(feed.position, feed.rotation);
    }
}