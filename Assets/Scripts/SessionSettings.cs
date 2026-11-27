
using System;
using UnityEngine;

public static class SessionSettings
{
    // Independent participant counters
    private const string ParticipantCounterKey =
        "LastParticipantNumber";

    private const string TestParticipantCounterKey =
        "LastTestParticipantNumber";

    // Participant identification
    public static int ParticipantNumber = 0;
    public static string ParticipantId = "";
    public static string SessionId = "";

    // User preferences
    public static string DominantHand = "Right";
    public static string Language = "FR";

    // Experimental conditions
    public static string ExperienceMode = "Guidance";
    public static string FirstCondition = "Guidance";

    public static string SecondCondition =>
        FirstCondition == "Guidance"
        ? "Biofeedback"
        : "Guidance";

    // Counterbalancing information
    public static string ConditionGroup =>
        FirstCondition == "Guidance" ? "A" : "B";

    public static string ConditionOrder =>
        FirstCondition == "Guidance" ? "G-B" : "B-G";

    // Experimental configuration
    public static int TrialsPerCondition = 20;
    public static float PreparationDuration = 10f;
    public static float BreakDuration = 120f;

    // Development mode
    public static bool IsTestMode = true;

    // Initialize a new experimental session
    public static void InitializeSession()
    {
        // Select the appropriate counter
        string counterKey = IsTestMode
            ? TestParticipantCounterKey
            : ParticipantCounterKey;

        // Generate participant number
        ParticipantNumber =
            PlayerPrefs.GetInt(counterKey, 0) + 1;

        // Generate participant identifier
        ParticipantId = IsTestMode
            ? $"TEST-P{ParticipantNumber:D3}"
            : $"P{ParticipantNumber:D3}";

        // Generate unique session identifier
        SessionId = Guid.NewGuid().ToString("N");

        // Automatic counterbalancing
        FirstCondition = ParticipantNumber % 2 == 1
            ? "Guidance"
            : "Biofeedback";

        ExperienceMode = FirstCondition;

        // Save the appropriate counter
        PlayerPrefs.SetInt(
            counterKey,
            ParticipantNumber
        );

        PlayerPrefs.Save();

        // Session information
        Debug.Log("===== EXPERIMENT SESSION =====");
        Debug.Log("Test mode: " + IsTestMode);
        Debug.Log("Participant ID: " + ParticipantId);
        Debug.Log("Participant number: " + ParticipantNumber);
        Debug.Log("Counter key: " + counterKey);
        Debug.Log("Session ID: " + SessionId);
        Debug.Log("Group: " + ConditionGroup);
        Debug.Log("Condition order: " + ConditionOrder);
        Debug.Log("First condition: " + FirstCondition);
        Debug.Log("Second condition: " + SecondCondition);
        Debug.Log("Dominant hand: " + DominantHand);
        Debug.Log("Language: " + Language);
        Debug.Log("==============================");
    }
}
