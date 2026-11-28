using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Hands;

public class DartHandGrabber : HandTrackingBehaviour
{
    [SerializeField] private Handedness handedness = Handedness.Right;
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private LayerMask dartLayers;

    [Header("Throw")] 
    [SerializeField] private float velocityWindow = 0.06f; // Longer = smoother, shorter = more responsive
    [SerializeField] private float throwForce = 1f;
    [SerializeField] private Vector3 localDartRotationOffset = new Vector3(0f, 180f, 0f);
    
    [Header("Snap")]
    [SerializeField] private Vector3 gripPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 gripRotationOffset = Vector3.zero;

    [Header("Feedback")]
    [SerializeField] private FeedbackModule feedbackModule;
    
    public Transform HeldDart => held != null ? held.transform : null;

    private static readonly HashSet<Rigidbody> HeldDarts = new HashSet<Rigidbody>(); // Already held darts

    private readonly List<DartMath.Sample> samples = new List<DartMath.Sample>(128);
    private XRHandSubsystem subsystem;
    private bool isTracked;
    private Vector3 lastThumbLocal;

    private readonly Collider[] overlaps = new Collider[16];
    private Rigidbody held;
    private RigidbodyInterpolation heldInterpolation;
    private Pose holdLocal;

    private Transform trackingSpace;

    private void Awake()
    {
        trackingSpace = xrOrigin.CameraFloorOffsetObject.transform;
    }
    
    protected override void OnDisable()
    {
        base.OnDisable();
        if (held != null) Release();
    }

    protected override void OnUpdatedHands(
        XRHandSubsystem handSubsystem,
        XRHandSubsystem.UpdateSuccessFlags flags,
        XRHandSubsystem.UpdateType updateType)
    {
        var left = handedness == Handedness.Left;
        var hand = left ? handSubsystem.leftHand : handSubsystem.rightHand;
        var jointsFlag = left
            ? XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints
            : XRHandSubsystem.UpdateSuccessFlags.RightHandJoints;

        isTracked = hand.isTracked && flags.HasFlag(jointsFlag);
        if (!isTracked) return;

        if (!hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out var thumb) ||
            !hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out var index) ||
            !hand.GetJoint(XRHandJointID.IndexDistal).TryGetPose(out var distal))
            return;

        // Skip duplicate poses
        var isNewPose = thumb.position != lastThumbLocal;
        lastThumbLocal = thumb.position;

        thumb = ToWorld(thumb);
        index = ToWorld(index);
        distal = ToWorld(distal);

        var pinch = HandShapes.Pinch(thumb, index, distal);
        var indexThumbDistance = HandShapes.PinchDistance(thumb, index);
        
        if (isNewPose) AddSample(pinch.position);
        
        if (held != null)
        {
            ApplyHold(pinch);

            if (indexThumbDistance > HandsSettings.ReleaseDistance) Release();
        } 
        else if (indexThumbDistance < HandsSettings.PinchingDistance)
        {
            TryGrab(pinch);
        }
    }

    private void TryGrab(Pose pinch)
    {
        var count = Physics.OverlapSphereNonAlloc(pinch.position, HandsSettings.GrabRadius, overlaps, dartLayers,
            QueryTriggerInteraction.Collide);

        Rigidbody best = null;
        DartGrip bestGrip = null;
        var bestDistance = float.MaxValue;
        for (var i = 0; i < count; i++)
        {
            var body = overlaps[i].attachedRigidbody;
            if (body == null || HeldDarts.Contains(body)) continue;

            if (!body.TryGetComponent<DartGrip>(out var grip) || !grip.CanGrab || grip.GrabPoint == null) continue;

            var distance = (body.position - pinch.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                best = body;
                bestGrip = grip;
                bestDistance = distance;
            }
        }

        if (best == null) return;

        if (best.TryGetComponent<DartPickupNotifier>(out var notifier)) notifier.NotifyPickedUp();

        held = best;
        HeldDarts.Add(best);

        // Disable physics (interpolation) so it doesn't fight the hand
        heldInterpolation = best.interpolation;
        best.isKinematic = true;
        best.interpolation = RigidbodyInterpolation.None;

        DartMath.ComputeGripHold(best.transform, bestGrip.GrabPoint, gripPositionOffset, gripRotationOffset,
            out holdLocal.position, out holdLocal.rotation);

        ApplyHold(pinch);
    }

    private void Release()
    {
        var dart = held;
        held = null;
        HeldDarts.Remove(dart);
        if (dart == null) return;

        var velocity = DartMath.EstimateVelocity(samples, velocityWindow) * throwForce;

        if (dart.TryGetComponent<ThrowData>(out var throwData))
        {
            throwData.RecordRelease(velocity, xrOrigin.transform.forward);

            if (feedbackModule != null)
                feedbackModule.OnReleased(throwData);
        }

        // Reset the physics (interpolation)
        dart.isKinematic = false;
        dart.interpolation = heldInterpolation;

        if (velocity.sqrMagnitude > 0.25f) // only align dart for throws with enough velocity
            dart.transform.rotation = DartMath.DartRotation(velocity, 0f, localDartRotationOffset);

        dart.linearVelocity = velocity;
        dart.angularVelocity = Vector3.zero;
    }

    private void AddSample(Vector3 position)
    {
        var now = Time.realtimeSinceStartupAsDouble;
        samples.Add(new DartMath.Sample { Time = now, Position = position });

        // Remove old elements (older than 1s)
        var old = 0;
        while (old < samples.Count && samples[old].Time < now - 1) old++;
        if (old > 0) samples.RemoveRange(0, old);
    }

    private Pose ToWorld(Pose pose) => trackingSpace != null ? pose.GetTransformedBy(trackingSpace) : pose;
    
    private void ApplyHold(Pose pinch)
    {
        var pose = DartMath.Compose(pinch, holdLocal);
        held.transform.SetPositionAndRotation(pose.position, pose.rotation);
    }
}