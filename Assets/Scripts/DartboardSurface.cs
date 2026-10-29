using UnityEngine;
using TMPro;


public class DartboardSurface : MonoBehaviour
{

    [SerializeField] private Transform boardCenter;
    private int[] sectorScores = {20, 1, 18, 4, 13, 6, 10, 15, 2, 17, 3, 19, 7, 16, 8, 11, 14, 9, 12, 5 };
    [SerializeField] private float innerBullRadius = 0.00635f;
    [SerializeField] private float outerBullRadius = 0.0159f;
    [SerializeField] private float tripleInnerRadius = 0.099f;
    [SerializeField] private float tripleOuterRadius = 0.107f;
    [SerializeField] private float doubleInnerRadius = 0.162f;
    [SerializeField] private float doubleOuterRadius = 0.170f;

    [SerializeField] private TMP_Text hitDisplay;

    public void RegisterHit(Vector3 impactPosition)
    {
        if (boardCenter == null)
            return;

        Vector3 offset = impactPosition - boardCenter.position;

        float x = -Vector3.Dot(offset, boardCenter.right);
        float y = Vector3.Dot(offset, boardCenter.up);

        float distance = new Vector2(x, y).magnitude;

        float angle = Mathf.Atan2(x, y) * Mathf.Rad2Deg;

        angle = Mathf.Repeat(angle + 9f, 360f);

        int sectorIndex = Mathf.FloorToInt(angle / 18f);
        int sectorScore = sectorScores[sectorIndex];

        int score;
        string zone;

        if (distance > doubleOuterRadius)
        {
            score = 0;
            zone = "Outside";
        }
        else if (distance <= innerBullRadius)
        {
            score = 50;
            zone = "Bullseye";
        }
        else if (distance <= outerBullRadius)
        {
            score = 25;
            zone = "Outer Bull";
        }
        else if (distance >= doubleInnerRadius)
        {
            score = sectorScore * 2;
            zone = $"Double {sectorScore}";
        }
        else if (distance >= tripleInnerRadius && distance <= tripleOuterRadius)
        {
            score = sectorScore * 3;
            zone = $"Triple {sectorScore}";
        }
        else
        {
            score = sectorScore;
            zone = $"Single {sectorScore}";
        }

        if (hitDisplay != null)
        {
            hitDisplay.text = $"{zone}\n" + $"Score: {score}\n" + $"Distance: {distance * 100f:F1} cm";
        }
    }

}
