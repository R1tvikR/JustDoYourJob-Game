using TMPro;
using UnityEngine;
using System.Collections.Generic;

public enum PaperStage { Spawned, Sorted, Stamped, Disposed }
public enum StampResult { None, Tick, Cross }

[RequireComponent(typeof(Rigidbody))]

public class PaperObject : MonoBehaviour
{
    static readonly Color ApprovedColour = Color.green;
    static readonly Color DeniedColour = Color.red;
    [Header("Section 1 - Colour Sort")]
    public Color colourTag;

    [Header("Section 2 - Accept/Reject")]
    public int expectedValue;
    public int actualValue;
    public bool isMismatch;

    [Header("State")]
    public PaperStage currentStage = PaperStage.Spawned;
    public StampResult stampResult = StampResult.None;

    [Header("Visuals")]
    public Renderer borderRenderer;      // thin frame/quad child, colour set via MaterialPropertyBlock
    public TextMeshProUGUI expectedText; // world-space canvas child
    public TextMeshProUGUI actualText;

    static readonly int ColorProp = Shader.PropertyToID("_BaseColor");
    MaterialPropertyBlock _mpb;

    void Awake()
    {
        _mpb = new MaterialPropertyBlock();
    }

    public void ShowStampVerdict(bool approved)
    {
        if (expectedText == null) return;
        expectedText.text = approved ? "APPROVED" : "DENIED";
        expectedText.color = approved ? ApprovedColour : DeniedColour;
    }
    public void SetBorderColour(Color c)
    {
        colourTag = c;
        if (borderRenderer == null) return;
        borderRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(ColorProp, c);
        borderRenderer.SetPropertyBlock(_mpb);
    }

    public void SetupSection2Data(int expected, int actual)
    {
        expectedValue = expected;
        actualValue = actual;
        isMismatch = expected != actual;

        if (expectedText != null) expectedText.text = expected.ToString();
        if (actualText != null) actualText.text = actual.ToString();
    }

    // Called before any scripted teleport, in case the player is still holding the paper.
    // XRGrabInteractable namespace assumes XRITK 3.x — adjust if your project uses an older version.
    public void ForceReleaseFromHand()
    {
        var grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab == null || !grab.isSelected) return;

        var interactors = new System.Collections.Generic.List<UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor>(grab.interactorsSelecting);
        foreach (var interactor in interactors)
        {
            grab.interactionManager.SelectExit(interactor, grab);
        }
    }
    public static readonly List<PaperObject> Active = new List<PaperObject>();

    void OnEnable() => Active.Add(this);
    void OnDisable() => Active.Remove(this);

    public static bool AnyInSection(int section)
    {
        PaperStage want = section == 1 ? PaperStage.Spawned
                        : section == 2 ? PaperStage.Sorted
                        : PaperStage.Stamped;
        if (section < 1 || section > 3) return false;
        foreach (var p in Active)
            if (p != null && p.currentStage == want) return true;
        return false;
    }
}