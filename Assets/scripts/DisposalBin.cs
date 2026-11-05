using UnityEngine;

public enum DisposalType { CorrectPile, Dustbin }

[RequireComponent(typeof(AudioSource))]
public class DisposalBin : MonoBehaviour
{
    public DisposalType disposalType;
    public float destroyDelay = 0.5f;
    public AudioClip correctSound; // ding for CorrectPile, shredder sound for Dustbin

    AudioSource _audioSource;

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.IsGameOver) return;
        PaperObject paper = other.GetComponent<PaperObject>();
        if (paper == null || paper.currentStage != PaperStage.Stamped) return;

        bool correct = (disposalType == DisposalType.CorrectPile && paper.stampResult == StampResult.Tick) ||
                       (disposalType == DisposalType.Dustbin && paper.stampResult == StampResult.Cross);
        if (!correct) return; // wrong destination: no-op

        paper.currentStage = PaperStage.Disposed;
        TaskManager.Instance.CompleteTask(3);
        GameFlowManager.Instance?.OnPaperDisposed();

        if (correctSound != null)
            _audioSource.PlayOneShot(correctSound);

        Destroy(paper.gameObject, destroyDelay);
    }
}