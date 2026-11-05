using UnityEngine;

public class ColourBin : MonoBehaviour
{
    public Color binColour;
    public Transform section2SpawnPoint;
    public AudioClip depositClip;
    [Range(0f, 1f)] public float depositVolume = 1f;
    AudioSource audioSource;
    [Range(0f, 1f)] public float colourMatchTolerance = 0.1f;

        void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        Debug.Log($"[ColourBin:{name}] Awake, clip assigned: {depositClip != null}");
    }
    void OnTriggerEnter(Collider other)
    {
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.IsGameOver) return;
        PaperObject paper = other.GetComponent<PaperObject>();
        if (paper == null || paper.currentStage != PaperStage.Spawned) return;
        if (!ColoursMatch(paper.colourTag, binColour)) return; // wrong bin: no-op
        if (depositClip != null) audioSource.PlayOneShot(depositClip, depositVolume);
        Debug.Log($"[ColourBin:{name}] Deposit OK, sound played: {depositClip != null}");
        TaskManager.Instance.CompleteTask(1);
        AdvanceToSection2(paper);
    }

    bool ColoursMatch(Color a, Color b) => Vector4.Distance(a, b) <= colourMatchTolerance;

    void AdvanceToSection2(PaperObject paper)
    {
        paper.ForceReleaseFromHand();
        paper.currentStage = PaperStage.Sorted;

        Rigidbody rb = paper.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        paper.transform.SetPositionAndRotation(section2SpawnPoint.position, section2SpawnPoint.rotation);

        int expected = Random.Range(1000, 9999);
        bool wantMismatch = Random.value < 0.5f;
        int actual = wantMismatch ? expected + Random.Range(1, 9) : expected;
        paper.SetupSection2Data(expected, actual);
    }
}