
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Hands;

public class DartHandGrabber : HandTrackingBehaviour
{
    [SerializeField]
    private Handedness handedness =
        Handedness.Right;

    [SerializeField] private XROrigin xrOrigin;

    [SerializeField] private LayerMask dartLayers;

    [Header("Throw")]

    [SerializeField] private float velocityWindow = 0.06f;

    [SerializeField] private float throwForce = 1f;

    [SerializeField]
    private Vector3 localDartRotationOffset =
        new Vector3(0f, 180f, 0f);

    [Header("Snap")]

    [SerializeField]
    private Vector3 gripPositionOffset =
        Vector3.zero;

    [SerializeField]
    private Vector3 gripRotationOffset =
        Vector3.zero;

    [Header("Feedback")]

    [SerializeField] private FeedbackModule feedbackModule;

    // ==========================================
    // EXPERIMENT CONFIGURATION
    // ==========================================

    [Header("Experiment Safety")]

    [SerializeField] private float trackingLossTolerance = 1f;

    private double lastValidTrackingTime;

    private bool trackingInterrupted;

    private bool requireOpenHand;

    // Already held darts

    private static readonly HashSet<Rigidbody> HeldDarts =
        new HashSet<Rigidbody>();

    private readonly List<DartMath.Sample> samples =
        new List<DartMath.Sample>(128);

    private bool isTracked;

    private Vector3 lastThumbLocal;

    private readonly Collider[] overlaps =
        new Collider[16];

    private Rigidbody held;

    private RigidbodyInterpolation heldInterpolation;

    private Pose holdLocal;

    private Transform trackingSpace;

    private void Awake()
    {
        trackingSpace =
            xrOrigin.CameraFloorOffsetObject.transform;
    }

    // ==========================================
    // COMPONENT DISABLED
    // ==========================================

    protected override void OnDisable()
    {
        base.OnDisable();

        if (held != null)
        {
            CancelHeldDart("HandGrabberDisabled");
        }

        samples.Clear();

        isTracked = false;
        trackingInterrupted = false;
    }

    // ==========================================
    // TRACKING LOSS MONITORING
    // ==========================================

    protected override void Update()
    {
        // Preserve the original hand tracking behaviour
        base.Update();

        // Monitor tracking loss while holding a dart
        if (held == null)
            return;

        double elapsed =
            Time.realtimeSinceStartupAsDouble -
            lastValidTrackingTime;

        if (elapsed >= trackingLossTolerance)
        {
            CancelHeldDart("HandTrackingLost");
        }
    }


    protected override void OnUpdatedHands(
        XRHandSubsystem handSubsystem,
        XRHandSubsystem.UpdateSuccessFlags flags,
        XRHandSubsystem.UpdateType updateType)
    {
        var left = handedness == Handedness.Left;

        var hand = left
            ? handSubsystem.leftHand
            : handSubsystem.rightHand;

        var jointsFlag = left
            ? XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints
            : XRHandSubsystem.UpdateSuccessFlags.RightHandJoints;

        isTracked = hand.isTracked &&
                    flags.HasFlag(jointsFlag);

        if (!isTracked)
        {
            trackingInterrupted = true;
            return;
        }

        if (!hand.GetJoint(XRHandJointID.ThumbTip)
                .TryGetPose(out var thumb) ||

            !hand.GetJoint(XRHandJointID.IndexTip)
                .TryGetPose(out var index) ||

            !hand.GetJoint(XRHandJointID.IndexDistal)
                .TryGetPose(out var distal))
        {
            trackingInterrupted = true;
            return;
        }

        double now =
            Time.realtimeSinceStartupAsDouble;

        // Check whether tracking was lost for too long
        // before accepting the recovered pose

        if (held != null &&
            now - lastValidTrackingTime >=
                trackingLossTolerance)
        {
            CancelHeldDart("HandTrackingLost");
        }

        lastValidTrackingTime = now;

        bool recoveredTracking = trackingInterrupted;

        trackingInterrupted = false;

        if (recoveredTracking)
        {
            samples.Clear();
        }

        // Skip duplicate poses

        var isNewPose =
            thumb.position != lastThumbLocal;

        lastThumbLocal = thumb.position;

        thumb = ToWorld(thumb);
        index = ToWorld(index);
        distal = ToWorld(distal);

        var pinch = HandShapes.Pinch(
            thumb,
            index,
            distal
        );

        var indexThumbDistance =
            HandShapes.PinchDistance(
                thumb,
                index
            );

        // Require the participant to open the hand
        // after an invalid attempt

        if (requireOpenHand)
        {
            if (indexThumbDistance >
                HandsSettings.ReleaseDistance)
            {
                requireOpenHand = false;
            }

            return;
        }

        // A release detected immediately after
        // tracking recovery is considered ambiguous

        if (recoveredTracking &&
            held != null &&
            indexThumbDistance >
                HandsSettings.ReleaseDistance)
        {
            CancelHeldDart(
                "ReleaseDuringTrackingLoss"
            );

            return;
        }

        if (isNewPose || recoveredTracking)
        {
            AddSample(pinch.position);
        }

        if (held != null)
        {
            ApplyHold(pinch);

            if (indexThumbDistance >
                HandsSettings.ReleaseDistance)
            {
                Release();
            }
        }
        else if (indexThumbDistance <
                 HandsSettings.PinchingDistance)
        {
            TryGrab(pinch);
        }
    }

    // ==========================================
    // DART GRAB
    // ==========================================

    private void TryGrab(Pose pinch)
    {
        var recorder = TrialRecorder.Instance;

        if (recorder != null &&
            !recorder.CanInteract)
        {
            return;
        }

        var count = Physics.OverlapSphereNonAlloc(
            pinch.position,
            HandsSettings.GrabRadius,
            overlaps,
            dartLayers,
            QueryTriggerInteraction.Collide
        );

        Rigidbody best = null;

        var bestDistance = float.MaxValue;

        for (var i = 0; i < count; i++)
        {
            var body =
                overlaps[i].attachedRigidbody;

            if (body == null ||
                HeldDarts.Contains(body))
            {
                continue;
            }

            // Do not grab previously released darts

            if (body.TryGetComponent<ThrowData>(
                out var candidateData))
            {
                if (candidateData.HasBeenReleased)
                    continue;
            }
            else if (recorder != null)
            {
                continue;
            }

            var distance =
                (body.position -
                 pinch.position).sqrMagnitude;

            if (distance < bestDistance)
            {
                best = body;
                bestDistance = distance;
            }
        }

        if (best == null)
            return;

        best.TryGetComponent<ThrowData>(
            out var throwData
        );

        // Reset previous release data

        if (throwData != null)
        {
            throwData.ResetReleaseData();
        }

        // Start experimental recording

        if (recorder != null &&
            recorder.RecordingEnabled)
        {
            if (throwData == null)
            {
                Debug.LogError(
                    "ThrowData missing on dart."
                );

                return;
            }

            if (!recorder.OnDartGrabbed(throwData))
                return;
        }

        // Original pickup mechanism

        if (best.TryGetComponent<DartPickupNotifier>(
            out var notifier))
        {
            notifier.NotifyPickedUp();
        }

        held = best;

        HeldDarts.Add(best);

        lastValidTrackingTime =
            Time.realtimeSinceStartupAsDouble;

        trackingInterrupted = false;

        heldInterpolation = best.interpolation;

        best.isKinematic = true;

        best.interpolation =
            RigidbodyInterpolation.None;

        if (best.TryGetComponent<DartGrip>(
            out var grip) &&
            grip.GrabPoint != null)
        {
            DartMath.ComputeGripHold(
                best.transform,
                grip.GrabPoint,
                gripPositionOffset,
                gripRotationOffset,
                out holdLocal.position,
                out holdLocal.rotation
            );
        }
        else
        {
            holdLocal = DartMath.ToLocal(
                pinch,
                new Pose(
                    best.transform.position,
                    best.transform.rotation
                )
            );
        }

        ApplyHold(pinch);
    }

    // ==========================================
    // NORMAL RELEASE
    // ==========================================

    private void Release()
    {
        var dart = held;

        held = null;

        HeldDarts.Remove(dart);

        if (dart == null)
            return;

        var velocity =
            DartMath.EstimateVelocity(
                samples,
                velocityWindow
            ) * throwForce;

        if (dart.TryGetComponent<ThrowData>(
            out var throwData))
        {
            throwData.RecordRelease(
                velocity,
                xrOrigin.transform.forward
            );

            // Experimental data collection

            if (TrialRecorder.Instance != null)
            {
                TrialRecorder.Instance.OnDartReleased(
                    throwData
                );
            }

            // Original feedback mechanism

            if (feedbackModule != null)
            {
                feedbackModule.OnReleased(
                    throwData
                );
            }
        }

        // Original physics mechanism

        dart.isKinematic = false;

        dart.interpolation = heldInterpolation;

        if (velocity.sqrMagnitude > 0.25f)
        {
            dart.transform.rotation =
                DartMath.DartRotation(
                    velocity,
                    0f,
                    localDartRotationOffset
                );
        }

        dart.linearVelocity = velocity;

        dart.angularVelocity = Vector3.zero;
    }

    // ==========================================
    // INVALID ATTEMPT / FORCED CANCELLATION
    // ==========================================

    public void CancelHeldDart(string reason)
    {
        var dart = held;

        held = null;

        HeldDarts.Remove(dart);

        samples.Clear();

        requireOpenHand = true;

        trackingInterrupted = false;

        if (dart == null)
            return;

        dart.TryGetComponent<ThrowData>(
            out var throwData
        );

        // Prevent the cancelled dart from moving
        // or generating new collisions

        dart.isKinematic = true;

        dart.gameObject.SetActive(false);

        // Notify the experimental recorder

        if (TrialRecorder.Instance != null &&
            throwData != null)
        {
            TrialRecorder.Instance.CancelCurrentTrial(
                throwData,
                reason
            );
        }

        if (throwData != null)
        {
            throwData.ResetReleaseData();
        }

        // DartSpawner already schedules a replacement
        // when a dart is picked up

        Destroy(dart.gameObject);

        Debug.LogWarning(
            $"Held dart cancelled: {reason}"
        );
    }

    // ==========================================
    // ORIGINAL VELOCITY SAMPLING
    // ==========================================

    private void AddSample(Vector3 position)
    {
        var now =
            Time.realtimeSinceStartupAsDouble;

        samples.Add(
            new DartMath.Sample
            {
                Time = now,
                Position = position
            }
        );

        var old = 0;

        while (old < samples.Count &&
               samples[old].Time < now - 1)
        {
            old++;
        }

        if (old > 0)
        {
            samples.RemoveRange(0, old);
        }
    }

    private Pose ToWorld(Pose pose)
    {
        return trackingSpace != null
            ? pose.GetTransformedBy(trackingSpace)
            : pose;
    }

    private void ApplyHold(Pose pinch)
    {
        var pose = DartMath.Compose(
            pinch,
            holdLocal
        );

        held.transform.SetPositionAndRotation(
            pose.position,
            pose.rotation
        );
    }
}
