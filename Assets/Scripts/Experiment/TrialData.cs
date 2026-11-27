
using System;
using UnityEngine;

public enum TrialStatus
{
    Pending,
    Hit,
    Outside,
    Miss,
    Deflected,
    Timeout,
    Invalid
}

[Serializable]
public class TrialData
{
    // Participant and session information
    public string ParticipantId;
    public string SessionId;
    public bool IsTestMode;

    public string DominantHand;
    public string Language;

    // Experimental condition
    public string ConditionGroup;
    public string ConditionOrder;
    public int ConditionIndex;
    public string Condition;

    // Trial identification
    public int AttemptNumber;
    public int TrialNumber;
    public int GlobalTrialNumber;

    // Indicates whether the trial counts
    // toward the experimental condition
    public bool IsCounted;

    // Timing
    public double GrabTime;
    public double? ReleaseTime;
    public double? ElapsedTime;

    // Release position in dartboard reference frame
    public Vector3? ReleasePosition;

    // Release velocity in dartboard reference frame
    public Vector3? ReleaseVelocity;
    public float? ReleaseSpeed;

    // Release direction in dartboard reference frame
    public Vector3? ReleaseDirection;

    // Release orientation angles
    public float? VerticalTilt;
    public float? HorizontalTilt;

    // Impact position in dartboard reference frame
    public Vector3? ImpactPosition;

    // Impact results
    public float? ImpactDistanceCm;
    public int? Score;
    public string Zone;

    // Trial outcome
    public TrialStatus Status = TrialStatus.Pending;
    public string FailureReason;

    // UTC timestamps
    public string ReleaseTimestampUtc;
    public string CompletionTimestampUtc;
}
