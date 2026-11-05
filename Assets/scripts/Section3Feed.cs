using UnityEngine;

public class Section3Feed : MonoBehaviour
{
    public static Section3Feed Instance { get; private set; }
    public Transform feedPoint;

    void Awake() => Instance = this;
}