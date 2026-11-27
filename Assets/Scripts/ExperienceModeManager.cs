
using UnityEngine;

public class ExperienceModeManager : MonoBehaviour
{
    [Header("Assistance Modules")]
    [SerializeField] private FeedbackModule feedbackModule;

    // To be assigned after guidance integration
    [SerializeField] private GameObject guidanceRoot;

    public string CurrentMode { get; private set; } = "None";

    private void Awake()
    {
        DisableAllModes();
    }

    public bool ApplyMode(string mode)
    {
        if (mode != "Guidance" && mode != "Biofeedback")
        {
            Debug.LogError("Invalid experience mode: " + mode);
            return false;
        }

        if (feedbackModule == null)
        {
            Debug.LogError("FeedbackModule is not assigned!");
            return false;
        }

        bool isGuidance = mode == "Guidance";
        bool isBiofeedback = mode == "Biofeedback";

        if (isGuidance && guidanceRoot == null)
        {
            Debug.LogWarning(
                "Guidance module is not assigned yet."
            );

            if (!SessionSettings.IsTestMode)
                return false;
        }

        DisableAllModes();

        SessionSettings.ExperienceMode = mode;
        CurrentMode = mode;

        feedbackModule.SetFeedbackEnabled(isBiofeedback);

        if (guidanceRoot != null)
        {
            guidanceRoot.SetActive(isGuidance);
        }

        Debug.Log("Applied experience mode: " + CurrentMode);
        Debug.Log("Feedback enabled: " + isBiofeedback);
        Debug.Log("Guidance requested: " + isGuidance);

        return true;
    }

    public void DisableAllModes()
    {
        if (feedbackModule != null)
        {
            feedbackModule.SetFeedbackEnabled(false);
        }

        if (guidanceRoot != null)
        {
            guidanceRoot.SetActive(false);
        }

        CurrentMode = "None";

        Debug.Log("All assistance modes disabled.");
    }
}
