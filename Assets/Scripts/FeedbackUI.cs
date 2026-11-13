using TMPro;
using UnityEngine;

public class FeedbackUI : MonoBehaviour
{
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text verticalTiltText;
    [SerializeField] private TMP_Text horizontalTiltText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text distanceText;

    private ThrowData currentThrow;

    public void ShowRelease(ThrowData data)
    {
        currentThrow = data;

        speedText.text = $"{data.ReleaseSpeed:F1} m/s";

        verticalTiltText.text = $"{Mathf.Abs(data.VerticalTilt):F0}° " + (data.VerticalTilt >= 0f ? "UP" : "DOWN");

        horizontalTiltText.text = $"{Mathf.Abs(data.HorizontalTilt):F0}° " + (data.HorizontalTilt >= 0f ? "RIGHT" : "LEFT");

        resultText.text = "IN FLIGHT";
        distanceText.text = "—";
    }

    public void ShowImpact(ThrowData data, int score, string zone, float distance)
    {
        if (data == null || data != currentThrow)
            return;

        resultText.text = $"{zone} · {score}";
        distanceText.text = $"{distance * 100f:F1} cm";
    }
}