using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Hands;

public class DartHandGrabber : MonoBehaviour
{
    [SerializeField] private Handedness handedness = Handedness.Right;
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private LayerMask dartLayers;

    [Header("Grab / release")] 
    [SerializeField] private float pinchingDistance = 0.02f;
    [SerializeField] private float releaseDistance = 0.035f;
    [SerializeField] private float grabRadius = 0.02f;

    [Header("Throw")] [SerializeField]
    private float velocityWindow = 0.06f; // Longer = smoother, shorter = more responsive

    [SerializeField] private float throwForce = 1f;

    [SerializeField] private Vector3 localDartRotationOffset = new Vector3(0f, 180f, 0f);

    private struct Sample
    {
        public double Time;
        public Vector3 Position;
    }

    private static readonly HashSet<Rigidbody> HeldDarts = new HashSet<Rigidbody>(); // Already held darts
    private static readonly List<XRHandSubsystem> Subsystems = new List<XRHandSubsystem>();

    private readonly List<Sample> samples = new List<Sample>(128);
    private XRHandSubsystem subsystem;
    private bool isTracked;
    private Vector3 lastThumbLocal;

    private readonly Collider[] overlaps = new Collider[16];
    private Rigidbody held;
    private RigidbodyInterpolation heldInterpolation;
    private Vector3 holdLocalPosition; // dart pose relative to the pinch, captured at grab
    private Quaternion holdLocalRotation;

    private Transform trackingSpace;

    private void Awake()
    {
        trackingSpace = xrOrigin.CameraFloorOffsetObject.transform;
    }

    private void Update()
    {
        if (subsystem == null || !subsystem.running)
        {
            Unbind();
            SubsystemManager.GetSubsystems(Subsystems);
            foreach (var handSubsystem in Subsystems)
            {
                if (!handSubsystem.running) continue;
                subsystem = handSubsystem;
                subsystem.updatedHands += OnUpdatedHands;
                break;
            }
        }
    }

    private void OnDisable()
    {
        Unbind();
        if (held != null) Release();
        isTracked = false;
    }

    private void Unbind()
    {
        if (subsystem != null) subsystem.updatedHands -= OnUpdatedHands;
        subsystem = null;
    }

    private void OnUpdatedHands(
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

        var pinch = new Pose((thumb.position + index.position) / 2f, distal.rotation);
        var indexThumbDistance = Vector3.Distance(thumb.position, index.position);

        if (isNewPose) AddSample(pinch.position);

        if (held != null)
        {
            held.transform.SetPositionAndRotation(
                pinch.position + pinch.rotation * holdLocalPosition,
                pinch.rotation * holdLocalRotation);

            if (indexThumbDistance > releaseDistance) Release();
        } 
        else if (indexThumbDistance < pinchingDistance)
        {
            TryGrab(pinch);
        }
    }

    private void TryGrab(Pose pinch)
    {
        var count = Physics.OverlapSphereNonAlloc(pinch.position, grabRadius, overlaps, dartLayers,
            QueryTriggerInteraction.Collide);
        Rigidbody best = null;
        var bestDistance = float.MaxValue;
        for (var i = 0; i < count; i++)
        {
            var body = overlaps[i].attachedRigidbody;
            if (body == null || HeldDarts.Contains(body)) continue;
            var distance = (body.position - pinch.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                best = body;
                bestDistance = distance;
            }
        }

        if (best == null) return;
        
        best.TryGetComponent<DartPickupNotifier>(out var notifier);
        if (notifier != null)
        {
            Debug.Log("Notified");
            notifier.NotifyPickedUp();
        }

        held = best;
        HeldDarts.Add(best);

        // Disable physics (interpolation) so it doesn't fight the hand
        heldInterpolation = best.interpolation;
        best.isKinematic = true;
        best.interpolation = RigidbodyInterpolation.None;

        // TODO: Should it snap in the hand?
        var inverse = Quaternion.Inverse(pinch.rotation);
        holdLocalPosition = inverse * (best.transform.position - pinch.position);
        holdLocalRotation = inverse * best.transform.rotation;
    }

    private void Release()
    {
        var dart = held;
        held = null;
        HeldDarts.Remove(dart);
        if (dart == null) return;

        var velocity = EstimateVelocity() * throwForce;

        // Reset the physics (interpolation)
        dart.isKinematic = false;
        dart.interpolation = heldInterpolation;

        if (velocity.sqrMagnitude > 0.25f) // only align dart for throws with enough velocity
            dart.transform.rotation = Quaternion.LookRotation(velocity) * Quaternion.Euler(localDartRotationOffset);

        dart.linearVelocity = velocity;
        dart.angularVelocity = Vector3.zero;
    }

    private void AddSample(Vector3 position)
    {
        var now = Time.realtimeSinceStartupAsDouble;
        samples.Add(new Sample { Time = now, Position = position });

        // Remove old elements (older than 1s)
        var old = 0;
        while (old < samples.Count && samples[old].Time < now - 1) old++;
        if (old > 0) samples.RemoveRange(0, old);
    }
    
    private Vector3 EstimateVelocity()
    {
        if (samples.Count < 2) return Vector3.zero;

        var end = samples[^1].Time;
        var first = samples.Count - 1;
        while (first > 0 && samples[first - 1].Time >= end - velocityWindow) first--;
        var n = samples.Count - first;
        if (n < 2) first = samples.Count - 2; // use last 2 if not enough sample in the window (can happen if there's a loss of hand tracking)
        n = samples.Count - first;

        double meanT = 0;
        var meanP = Vector3.zero;
        for (var i = first; i < samples.Count; i++)
        {
            meanT += samples[i].Time - end;
            meanP += samples[i].Position;
        }

        meanT /= n;
        meanP /= n;

        var numerator = Vector3.zero;
        double denominator = 0;
        for (var i = first; i < samples.Count; i++)
        {
            var dt = (samples[i].Time - end) - meanT;
            numerator += (float)dt * (samples[i].Position - meanP);
            denominator += dt * dt;
        }

        return denominator > 1e-9 ? numerator / (float)denominator : Vector3.zero;
    }

    private Pose ToWorld(Pose pose) => trackingSpace != null ? pose.GetTransformedBy(trackingSpace) : pose;
}