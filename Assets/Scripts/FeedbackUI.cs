using TMPro;
using UnityEngine;

public class FeedbackUI : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text velocityText;
    [SerializeField] private TMP_Text verticalTiltText;
    [SerializeField] private TMP_Text horizontalTiltText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text distanceText;

    [Header("Icons")]
    [SerializeField] private GameObject velocityIcon;
    [SerializeField] private GameObject verticalTiltIcon;
    [SerializeField] private GameObject horizontalTiltIcon;
    [SerializeField] private GameObject scoreIcon;
    [SerializeField] private GameObject distanceIcon;

    private ThrowData currentThrow;

    public void Awake()
    {
        velocityText.text = "";
        verticalTiltText.text = "";
        horizontalTiltText.text = "";
        resultText.text = "";
        distanceText.text = "";
        SetReleaseIconsVisible(false);
        SetImpactIconsVisible(false);
    }

    public void ShowRelease(ThrowData data)
    {
        currentThrow = data;

        velocityText.text = $"Velocity: {data.ReleaseSpeed:F1} m/s";

        verticalTiltText.text = $"Vertical Tilt: {Mathf.Abs(data.VerticalTilt):F0}° " + (data.VerticalTilt >= 0f ? "UP" : "DOWN");

        horizontalTiltText.text = $"Horizontal Tilt: {Mathf.Abs(data.HorizontalTilt):F0}° " + (data.HorizontalTilt >= 0f ? "RIGHT" : "LEFT");

        resultText.text = "";
        distanceText.text = "";

        SetReleaseIconsVisible(true);
        SetImpactIconsVisible(false);
    }

    public void ShowImpact(ThrowData data, int score, string zone, float distance)
    {
        if (data == null || data != currentThrow)
            return;

        

        bool scored = score > 0;

        SetImpactIconsVisible(scored);

        if (!scored)
        {
            resultText.text = "";
            distanceText.text = "";
            return;
        }

        resultText.text = $"Zone/Score: {zone} · {score}";
        distanceText.text = $"Distance: {distance * 100f:F1} cm";
    }

    private void SetReleaseIconsVisible(bool visible)
    {
        velocityIcon.SetActive(visible);
        verticalTiltIcon.SetActive(visible);
        horizontalTiltIcon.SetActive(visible);
    }

    private void SetImpactIconsVisible(bool visible)
    {
        scoreIcon.SetActive(visible);
        distanceIcon.SetActive(visible);
    }
}