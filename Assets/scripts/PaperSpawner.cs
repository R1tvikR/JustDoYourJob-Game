using UnityEngine;

public class PaperSpawner : MonoBehaviour
{
    public bool useTimer = false; 
    public GameObject paperPrefab;
    public Transform spawnPoint;
    public Color[] sortColours = new Color[3];
    public float spawnInterval = 6f;

    float _timer;

    void Update()
    {
        if (!useTimer) return;
        _timer += Time.deltaTime;
        if (_timer >= spawnInterval)
        {
            _timer = 0f;
            SpawnPaper();
        }
    }

    public void SpawnPaper()
    {
        GameObject go = Instantiate(paperPrefab, spawnPoint.position, spawnPoint.rotation);
        PaperObject paper = go.GetComponent<PaperObject>();
        paper.SetBorderColour(sortColours[Random.Range(0, sortColours.Length)]);
        paper.currentStage = PaperStage.Spawned;
    }
}