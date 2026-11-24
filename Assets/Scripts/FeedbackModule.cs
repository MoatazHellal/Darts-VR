using UnityEngine;

public class FeedbackModule : MonoBehaviour
{
    [SerializeField] private GameObject feedbackRoot;
    [SerializeField] private FeedbackUI dashboard;
    [SerializeField] private bool feedbackEnabled = true;
    [SerializeField] private GameObject previousReleasePrefab;

    private ThrowData currentThrow;
    private GameObject previousRelease;

    public bool FeedbackEnabled => feedbackEnabled;

    private void Start()
    {
        SetFeedbackEnabled(feedbackEnabled);
    }

    private void OnDisable()
    {
        ClearPreviousRelease();
    }

    public void SetFeedbackEnabled(bool enabled)
    {
        feedbackEnabled = enabled;
        currentThrow = null;
        ClearPreviousRelease();

        if (feedbackRoot != null)
        {
            feedbackRoot.SetActive(enabled);
        }

        if (dashboard != null)
        {
            dashboard.ClearDisplay();
        }
    }

    public void OnReleased(ThrowData data)
    {
        if (!feedbackEnabled || data == null || dashboard == null)
            return;

        currentThrow = data;
        dashboard.ShowRelease(data);

        ClearPreviousRelease();

        if (previousReleasePrefab != null)
        {
            previousRelease = Instantiate(previousReleasePrefab, data.ReleasePosition, data.ReleaseRotation);
        }
    }

    public void OnImpact(ThrowData data, int score, string zone, float distance)
    {
        if (!feedbackEnabled || dashboard == null)
            return;

        if (data == null || data != currentThrow)
            return;

        dashboard.ShowImpact(data, score, zone, distance);
    }

    private void ClearPreviousRelease()
    {
        if (previousRelease == null)
            return;

        previousRelease.SetActive(false);
        Destroy(previousRelease);
        previousRelease = null;
    }
}

