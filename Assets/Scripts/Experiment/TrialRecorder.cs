
using System;
using UnityEngine;

public class TrialRecorder : MonoBehaviour
{
    public static TrialRecorder Instance { get; private set; }

    [Header("Dartboard Reference")]
    [SerializeField] private Transform boardCenter;

    [Header("Trial Configuration")]
    [SerializeField] private float impactTimeout = 5f;

    [SerializeField] private bool countTimeoutAsTrial = true;

    private enum RecorderState
    {
        Idle,
        Holding,
        InFlight,
        Finalizing
    }

    private RecorderState state = RecorderState.Idle;

    private TrialData currentTrial;
    private ThrowData activeDart;

    private bool recordingEnabled;
    private bool freePracticeEnabled;
    private bool initialized;

    private int conditionIndex;
    private string condition;

    // Experimental counters
    private int attemptNumber;
    private int trialNumber;
    private int globalTrialNumber;
    private int invalidAttemptCount;

    private double releaseTime;

    public int CompletedTrials => trialNumber;

    public int GlobalCompletedTrials => globalTrialNumber;

    public int TotalAttempts => attemptNumber;

    public int InvalidAttempts => invalidAttemptCount;

    public bool IsBusy => state != RecorderState.Idle;

    public bool RecordingEnabled => recordingEnabled;

    public bool CanGrab =>
        initialized &&
        recordingEnabled &&
        state == RecorderState.Idle;

    public bool CanInteract =>
        state == RecorderState.Idle &&
        (freePracticeEnabled ||
        (initialized && recordingEnabled));

    // Invoked after every successfully exported attempt
    public event Action<TrialData> AttemptCompleted;

    // Invoked only for counted experimental trials
    public event Action<TrialData> TrialCompleted;

    // Invoked when saving fails
    public event Action<string> RecordingError;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ==========================================
    // SESSION INITIALIZATION
    // ==========================================

    public bool InitializeRecorder()
    {
        if (IsBusy)
            return false;

        initialized = false;
        recordingEnabled = false;
        freePracticeEnabled = false;

        if (boardCenter == null)
        {
            Debug.LogError(
                "Board center is not assigned."
            );

            return false;
        }

        if (!CSVExporter.InitializeSession())
            return false;

        currentTrial = null;
        activeDart = null;

        attemptNumber = 0;
        trialNumber = 0;
        globalTrialNumber = 0;
        invalidAttemptCount = 0;

        conditionIndex = 0;
        condition = null;

        state = RecorderState.Idle;

        initialized = true;

        Debug.Log("TrialRecorder initialized.");

        return true;
    }

    // ==========================================
    // EXPERIMENTAL CONDITION MANAGEMENT
    // ==========================================

    public bool BeginCondition(
        int index,
        string conditionName)
    {
        if (!initialized || IsBusy)
            return false;

        if (string.IsNullOrEmpty(
            CSVExporter.CurrentFilePath))
        {
            return false;
        }

        if (index != 1 && index != 2)
            return false;

        if (conditionName != "Guidance" &&
            conditionName != "Biofeedback")
        {
            return false;
        }

        // Prevent restarting the same condition
        if (index != conditionIndex + 1)
        {
            Debug.LogError(
                "Invalid experimental condition order."
            );

            return false;
        }

        conditionIndex = index;
        condition = conditionName;

        trialNumber = 0;

        freePracticeEnabled = false;
        recordingEnabled = true;

        Debug.Log(
            $"Condition {conditionIndex}: {condition}"
        );

        return true;
    }

    public bool BeginFreePractice()
    {
        if (IsBusy)
            return false;

        recordingEnabled = false;
        freePracticeEnabled = true;

        Debug.Log("Free practice enabled.");

        return true;
    }

    public void StopRecording()
    {
        recordingEnabled = false;
        freePracticeEnabled = false;

        Debug.Log("New interactions disabled.");
    }

    // ==========================================
    // DART GRAB
    // ==========================================

    public bool OnDartGrabbed(ThrowData dart)
    {
        if (!CanGrab || dart == null)
            return false;

        activeDart = dart;

        currentTrial = new TrialData
        {
            ParticipantId =
                SessionSettings.ParticipantId,

            SessionId =
                SessionSettings.SessionId,

            IsTestMode =
                SessionSettings.IsTestMode,

            DominantHand =
                SessionSettings.DominantHand,

            Language =
                SessionSettings.Language,

            ConditionGroup =
                SessionSettings.ConditionGroup,

            ConditionOrder =
                SessionSettings.ConditionOrder,

            ConditionIndex = conditionIndex,

            Condition = condition,

            AttemptNumber = attemptNumber + 1,

            TrialNumber = trialNumber + 1,

            GlobalTrialNumber = globalTrialNumber + 1,

            IsCounted = false,

            GrabTime =
                Time.realtimeSinceStartupAsDouble,

            Status = TrialStatus.Pending
        };

        state = RecorderState.Holding;

        Debug.Log(
            $"Attempt {currentTrial.AttemptNumber}: " +
            "dart grabbed."
        );

        return true;
    }

    // ==========================================
    // DART RELEASE
    // ==========================================

    public void OnDartReleased(ThrowData dart)
    {
        if (state != RecorderState.Holding ||
            dart == null ||
            dart != activeDart ||
            !dart.HasBeenReleased)
        {
            return;
        }

        releaseTime =
            Time.realtimeSinceStartupAsDouble;

        currentTrial.ReleaseTime = releaseTime;

        currentTrial.ElapsedTime =
            releaseTime - currentTrial.GrabTime;

        currentTrial.ReleasePosition =
            ToBoardPosition(dart.ReleasePosition);

        currentTrial.ReleaseVelocity =
            ToBoardDirection(dart.ReleaseVelocity);

        currentTrial.ReleaseSpeed =
            dart.ReleaseSpeed;

        currentTrial.ReleaseDirection =
            ToBoardDirection(
                dart.ReleaseDirection
            ).normalized;

        currentTrial.VerticalTilt =
            dart.VerticalTilt;

        currentTrial.HorizontalTilt =
            dart.HorizontalTilt;

        currentTrial.ReleaseTimestampUtc =
            DateTime.UtcNow.ToString("O");

        state = RecorderState.InFlight;

        Debug.Log(
            $"Attempt {currentTrial.AttemptNumber}: " +
            "dart released."
        );
    }

    // ==========================================
    // VALID IMPACT
    // ==========================================

    public void OnDartImpact(
        ThrowData dart,
        Vector3 impactPosition,
        int score,
        string zone,
        float distanceMeters)
    {
        if (!IsActiveFlight(dart))
            return;

        currentTrial.ImpactPosition =
            ToBoardPosition(impactPosition);

        currentTrial.ImpactDistanceCm =
            distanceMeters * 100f;

        currentTrial.Score = score;

        currentTrial.Zone = zone;

        TrialStatus status = score > 0
            ? TrialStatus.Hit
            : TrialStatus.Outside;

        CompleteTrial(status);
    }

    // ==========================================
    // MISSED THROW
    // ==========================================

    public void OnDartMiss(
        ThrowData dart,
        string reason)
    {
        if (!IsActiveFlight(dart))
            return;

        currentTrial.Score = 0;

        CompleteTrial(
            TrialStatus.Miss,
            reason
        );
    }

    // ==========================================
    // REJECTED IMPACT
    // ==========================================

    public void OnDartDeflected(ThrowData dart)
    {
        if (!IsActiveFlight(dart))
            return;

        currentTrial.Score = 0;

        CompleteTrial(
            TrialStatus.Deflected,
            "RejectedBoardImpact"
        );
    }

    // ==========================================
    // INVALID ATTEMPT
    // ==========================================

    public bool CancelCurrentTrial(
        ThrowData dart,
        string reason)
    {
        if (currentTrial == null ||
            !ReferenceEquals(dart, activeDart))
        {
            return false;
        }

        if (state != RecorderState.Holding &&
            state != RecorderState.InFlight)
        {
            return false;
        }

        Debug.LogWarning(
            $"Attempt {currentTrial.AttemptNumber} " +
            $"invalidated: {reason}"
        );

        CompleteTrial(
            TrialStatus.Invalid,
            reason
        );

        return true;
    }

    private bool IsActiveFlight(ThrowData dart)
    {
        return state == RecorderState.InFlight &&
               dart != null &&
               dart == activeDart;
    }

    // ==========================================
    // TIMEOUT AND MISSING DART MANAGEMENT
    // ==========================================

    private void Update()
    {
        if (state == RecorderState.Holding ||
            state == RecorderState.InFlight)
        {
            if (activeDart == null)
            {
                CompleteTrial(
                    TrialStatus.Invalid,
                    "DartDestroyed"
                );

                return;
            }
        }

        if (state != RecorderState.InFlight)
            return;

        double elapsed =
            Time.realtimeSinceStartupAsDouble -
            releaseTime;

        if (elapsed >= impactTimeout)
        {
            CompleteTrial(
                TrialStatus.Timeout,
                "NoImpactDetected"
            );
        }
    }

    // ==========================================
    // TRIAL FINALIZATION
    // ==========================================

    private void CompleteTrial(
        TrialStatus status,
        string failureReason = "")
    {
        if (currentTrial == null)
            return;

        bool validState =
            state == RecorderState.InFlight ||
            (state == RecorderState.Holding &&
             status == TrialStatus.Invalid);

        if (!validState)
            return;

        state = RecorderState.Finalizing;

        currentTrial.Status = status;

        currentTrial.FailureReason = failureReason;

        currentTrial.IsCounted =
            status != TrialStatus.Invalid &&
            (status != TrialStatus.Timeout ||
             countTimeoutAsTrial);

        currentTrial.CompletionTimestampUtc =
            DateTime.UtcNow.ToString("O");

        SaveCompletedTrial();
    }

    // ==========================================
    // CSV EXPORT AND COUNTERS
    // ==========================================

    private void SaveCompletedTrial()
    {
        if (!CSVExporter.ExportTrial(currentTrial))
        {
            StopRecording();

            string message =
                "Trial export failed. Recording blocked.";

            Debug.LogError(message);

            RecordingError?.Invoke(message);

            return;
        }

        attemptNumber++;

        if (currentTrial.IsCounted)
        {
            trialNumber++;
            globalTrialNumber++;
        }

        if (currentTrial.Status == TrialStatus.Invalid)
        {
            invalidAttemptCount++;
        }

        TrialData completedTrial = currentTrial;

        currentTrial = null;
        activeDart = null;

        state = RecorderState.Idle;

        Debug.Log(
            $"Attempt: {attemptNumber} | " +
            $"Condition trials: {trialNumber} | " +
            $"Global trials: {globalTrialNumber} | " +
            $"Status: {completedTrial.Status}"
        );

        AttemptCompleted?.Invoke(completedTrial);

        if (completedTrial.IsCounted)
        {
            TrialCompleted?.Invoke(completedTrial);
        }
    }

    // ==========================================
    // DARTBOARD REFERENCE FRAME
    // ==========================================

    private Vector3 ToBoardPosition(
        Vector3 worldPosition)
    {
        return Quaternion.Inverse(
            boardCenter.rotation
        ) * (
            worldPosition - boardCenter.position
        );
    }

    private Vector3 ToBoardDirection(
        Vector3 worldVector)
    {
        return Quaternion.Inverse(
            boardCenter.rotation
        ) * worldVector;
    }
}
