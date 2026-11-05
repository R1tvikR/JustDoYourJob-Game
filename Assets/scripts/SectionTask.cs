using UnityEngine;

public class SectionTask : MonoBehaviour
{
public int sectionId; // set this to 1, 2, or 3 in the Inspector per cube

    // Call this method from the XR Simple Interactable's "Select Entered" event
    public void OnInteracted()
    {
        TaskManager.Instance.CompleteTask(sectionId);

        // TEMP: just move the cube away to simulate "task done" until real art/animation exists
        transform.position += Vector3.up * 2f;
    }
}
