
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

public enum ExperimentPhase
{
    WaitingForHands,
    Preparation,
    FirstCondition,
    Break,
    SecondCondition,
    Completed,
    Error
}

public class ExperimentManager : MonoBehaviour
{
    public static ExperimentManager Instance { get; private set; }

    [Header("Module References")]
    [SerializeField] private TrialRecorder trialRecorder;
    [SerializeField] private ExperienceModeManager experienceModeManager;

    [Header("Experiment Configuration")]
    [SerializeField, Min(1)]
    private int trialsPerCondition = 20;

    [SerializeField, Min(0f)]
    private float preparationDuration = 10f;

    [SerializeField, Min(0f)]
    private float breakDuration = 120f;

    [Header("Development")]
    [SerializeField] private bool simulateHandTracking = false;

    private readonly List<XRHandSubsystem> handSubsystems =
        new List<XRHandSubsystem>();

    private double phaseEndTime;
    private bool initialized;

    public ExperimentPhase CurrentPhase { get; private set; }
        = ExperimentPhase.WaitingForHands;

    public int TrialsPerCondition => trialsPerCondition;

    public int CompletedTrials =>
        trialRecorder != null
            ? trialRecorder.CompletedTrials
            : 0;

    public int GlobalCompletedTrials =>
        trialRecorder != null
            ? trialRecorder.GlobalCompletedTrials
            : 0;

    public int TotalRequiredTrials => trialsPerCondition * 2;

    public string CurrentCondition { get; private set; } = "";

    public string ErrorMessage { get; private set; } = "";

    public float RemainingTime
    {
        get
        {
            if (CurrentPhase != ExperimentPhase.Preparation &&
                CurrentPhase != ExperimentPhase.Break)
            {
                return 0f;
            }

            return Mathf.Max(
                0f,
                (float)(phaseEndTime -
                Time.realtimeSinceStartupAsDouble)
            );
        }
    }

    // Events for the future experimental UI
    public event Action<ExperimentPhase> PhaseChanged;
    public event Action<TrialData> TrialUpdated;
    public event Action ExperimentCompleted;
    public event Action<string> ExperimentFailed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (trialRecorder == null ||
            experienceModeManager == null)
        {
            FailExperiment("Experiment references are missing.");
            return;
        }

        trialRecorder.StopRecording();
        experienceModeManager.DisableAllModes();

        if (string.IsNullOrEmpty(SessionSettings.ParticipantId) ||
            string.IsNullOrEmpty(SessionSettings.SessionId))
        {
            FailExperiment(
                "Session not initialized. Start from WelcomeScene."
            );

            return;
        }

        if (trialsPerCondition < 1 ||
            preparationDuration < 0f ||
            breakDuration < 0f)
        {
            FailExperiment("Invalid experiment configuration.");
            return;
        }

        // Synchronize session configuration
        SessionSettings.TrialsPerCondition = trialsPerCondition;
        SessionSettings.PreparationDuration = preparationDuration;
        SessionSettings.BreakDuration = breakDuration;

        // Subscribe to recorder events
        trialRecorder.TrialCompleted += OnTrialCompleted;
        trialRecorder.AttemptCompleted += OnAttemptCompleted;
        trialRecorder.RecordingError += OnRecordingError;

        if (!trialRecorder.InitializeRecorder())
        {
            FailExperiment("TrialRecorder initialization failed.");
            return;
        }

        initialized = true;

        SetPhase(ExperimentPhase.WaitingForHands);

        Debug.Log("===== EXPERIMENT INITIALIZED =====");
        Debug.Log("Participant: " + SessionSettings.ParticipantId);
        Debug.Log("Order: " + SessionSettings.ConditionOrder);
        Debug.Log("Trials per condition: " + trialsPerCondition);
        Debug.Log("Preparation: " + preparationDuration + " s");
        Debug.Log("Break: " + breakDuration + " s");
        Debug.Log("Waiting for hand tracking...");
    }

    private void Update()
    {
        if (!initialized)
            return;

        switch (CurrentPhase)
        {
            case ExperimentPhase.WaitingForHands:

                if (IsDominantHandTracked())
                {
                    StartPreparation();
                }

                break;

            case ExperimentPhase.Preparation:

                if (!IsDominantHandTracked())
                {
                    SetPhase(ExperimentPhase.WaitingForHands);
                    break;
                }

                if (RemainingTime <= 0f)
                {
                    StartCondition(1);
                }

                break;

            case ExperimentPhase.Break:

                if (RemainingTime <= 0f &&
                    IsDominantHandTracked())
                {
                    StartCondition(2);
                }

                break;
        }
    }

    // ==========================================
    // HAND TRACKING
    // ==========================================

    private bool IsDominantHandTracked()
    {
        if (Application.isEditor &&
            SessionSettings.IsTestMode &&
            simulateHandTracking)
        {
            return true;
        }

        SubsystemManager.GetSubsystems(handSubsystems);

        foreach (XRHandSubsystem subsystem in handSubsystems)
        {
            if (!subsystem.running)
                continue;

            XRHand hand =
                SessionSettings.DominantHand == "Left"
                    ? subsystem.leftHand
                    : subsystem.rightHand;

            if (hand.isTracked)
                return true;
        }

        return false;
    }

    // ==========================================
    // PREPARATION
    // ==========================================

    private void StartPreparation()
    {
        trialRecorder.StopRecording();
        experienceModeManager.DisableAllModes();

        CurrentCondition = "";

        phaseEndTime =
            Time.realtimeSinceStartupAsDouble +
            preparationDuration;

        SetPhase(ExperimentPhase.Preparation);

        Debug.Log("Preparation started.");
    }

    // ==========================================
    // EXPERIMENTAL CONDITIONS
    // ==========================================

    private void StartCondition(int index)
    {
        if (trialRecorder.IsBusy)
        {
            FailExperiment(
                "Cannot change condition while a trial is active."
            );

            return;
        }

        string condition = index == 1
            ? SessionSettings.FirstCondition
            : SessionSettings.SecondCondition;

        if (!experienceModeManager.ApplyMode(condition))
        {
            FailExperiment(
                "Could not activate condition: " + condition
            );

            return;
        }

        if (!trialRecorder.BeginCondition(index, condition))
        {
            FailExperiment(
                "Could not start condition: " + condition
            );

            return;
        }

        CurrentCondition = condition;

        SetPhase(
            index == 1
                ? ExperimentPhase.FirstCondition
                : ExperimentPhase.SecondCondition
        );

        Debug.Log("===== CONDITION " + index + " =====");
        Debug.Log("Mode: " + condition);
        Debug.Log("Required trials: " + trialsPerCondition);
    }

    // ==========================================
    // TRIAL EVENTS
    // ==========================================

    private void OnAttemptCompleted(TrialData trial)
    {
        TrialUpdated?.Invoke(trial);

        if (!trial.IsCounted)
        {
            Debug.LogWarning(
                "Attempt not counted: " + trial.FailureReason
            );
        }
    }

    private void OnTrialCompleted(TrialData trial)
    {
        if (CurrentPhase != ExperimentPhase.FirstCondition &&
            CurrentPhase != ExperimentPhase.SecondCondition)
        {
            return;
        }

        Debug.Log(
            $"Progress: {CompletedTrials}/{trialsPerCondition}"
        );

        Debug.Log(
            $"Global progress: " +
            $"{GlobalCompletedTrials}/{TotalRequiredTrials}"
        );

        if (CompletedTrials < trialsPerCondition)
            return;

        // Prevent any additional trial
        trialRecorder.StopRecording();

        if (CurrentPhase == ExperimentPhase.FirstCondition)
        {
            StartBreak();
        }
        else
        {
            FinishExperiment();
        }
    }

    // ==========================================
    // BREAK
    // ==========================================

    private void StartBreak()
    {
        trialRecorder.StopRecording();
        experienceModeManager.DisableAllModes();

        CurrentCondition = "";

        phaseEndTime =
            Time.realtimeSinceStartupAsDouble +
            breakDuration;

        SetPhase(ExperimentPhase.Break);

        Debug.Log("===== BREAK STARTED =====");
        Debug.Log("Duration: " + breakDuration + " seconds");
    }

    // ==========================================
    // EXPERIMENT COMPLETION
    // ==========================================

    private void FinishExperiment()
    {
        trialRecorder.StopRecording();
        experienceModeManager.DisableAllModes();

        CurrentCondition = "";

        if (GlobalCompletedTrials != TotalRequiredTrials)
        {
            FailExperiment("Unexpected final trial count.");
            return;
        }

        SetPhase(ExperimentPhase.Completed);

        Debug.Log("===== EXPERIMENT COMPLETED =====");
        Debug.Log("Participant: " + SessionSettings.ParticipantId);
        Debug.Log("Counted trials: " + GlobalCompletedTrials);
        Debug.Log("Total attempts: " + trialRecorder.TotalAttempts);
        Debug.Log("Invalid attempts: " + trialRecorder.InvalidAttempts);
        Debug.Log("CSV file: " + CSVExporter.CurrentFilePath);

        ExperimentCompleted?.Invoke();
    }

    // ==========================================
    // ERROR MANAGEMENT
    // ==========================================

    private void OnRecordingError(string message)
    {
        FailExperiment(message);
    }

    private void FailExperiment(string message)
    {
        initialized = false;
        ErrorMessage = message;

        if (trialRecorder != null)
            trialRecorder.StopRecording();

        if (experienceModeManager != null)
            experienceModeManager.DisableAllModes();

        CurrentCondition = "";

        SetPhase(ExperimentPhase.Error);

        Debug.LogError("Experiment stopped: " + message);

        ExperimentFailed?.Invoke(message);
    }

    // ==========================================
    // PHASE MANAGEMENT
    // ==========================================

    private void SetPhase(ExperimentPhase phase)
    {
        CurrentPhase = phase;

        Debug.Log("Experiment phase: " + phase);

        PhaseChanged?.Invoke(phase);
    }

    private void OnDestroy()
    {
        if (trialRecorder != null)
        {
            trialRecorder.TrialCompleted -= OnTrialCompleted;
            trialRecorder.AttemptCompleted -= OnAttemptCompleted;
            trialRecorder.RecordingError -= OnRecordingError;
        }

        if (Instance == this)
            Instance = null;
    }
}
