using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Hands;

public class DartThrowDemo : MonoBehaviour
{
    [Header("Scene")] [SerializeField] private XRHandSkeletonDriver ghostHand;
    [SerializeField] private Transform participantHead;
    [SerializeField] private Transform releasePoint;
    [SerializeField, Range(0.5f, 1.2f)] private float releaseHeightRatio = 0.95f;
    [SerializeField] private Transform bullseye;

    [Header("Hand shapes")] [SerializeField]
    private HandShapes handShapes;

    [Header("Dart")] [SerializeField] private Rigidbody dartPrefab;
    [SerializeField] private Vector3 dartTipOffset = new Vector3(0f, 0f, -0.12f);
    [SerializeField] private float dartLifetime = 6f;

    [Header("Same values as DartHandGrabber")] [SerializeField]
    private Vector3 gripPositionOffset = Vector3.zero;

    [SerializeField] private Vector3 gripRotationOffset = Vector3.zero;
    [SerializeField] private Vector3 localDartRotationOffset = new Vector3(0f, 180f, 0f);
    [SerializeField] private float velocityWindow = 0.06f;
    [SerializeField] private float throwForce = 1f;
    [SerializeField] private float trackingRate = 72f;

    [Header("Throw motion")] [SerializeField]
    private float dartSpeed = 6f;

    [SerializeField] private float forearmLength = 0.35f;
    [SerializeField] private float aimAngle = -25f;
    [SerializeField] private float windupAngle = -75f;
    [SerializeField] private float followThroughAngle = 40f;
    [SerializeField] private float wristFollow = 0.5f;
    [SerializeField] private float handRoll = 0f;
    [SerializeField] private float aimDuration = 0.6f;
    [SerializeField] private float backswingDuration = 0.35f;
    [SerializeField] private float fingerOpenDuration = 0.06f;
    
    [Header("Alignment")]
    [SerializeField] private DartHandGrabber dartGrabber;
    [SerializeField] private float positionTolerance = 0.04f;
    [SerializeField] private float angleTolerance = 20f;
    [SerializeField] private float alignHoldDuration = 1f;

    private float alignProgress = 0f;

    // Only used for estimating the grabber movement speed
    private readonly System.Collections.Generic.List<DartMath.Sample> swingSamples =
        new System.Collections.Generic.List<DartMath.Sample>(16);

    private Transform[] joints;
    private Pose[] sample;
    private Pose[] holdShape;
    private Pose[] releaseShape;
    private Pose pinchInWrist;

    // Swing data
    private Vector3 elbow;
    private Vector3 swingUp;
    private Vector3 swingForward;
    private Quaternion releasePinchRotation;
    private float omega;
    private float forwardDuration;
    private float followDuration;
    
    private Coroutine currentRoutine;
    private Rigidbody currentDart;

    private bool wasHoldingDart;
    public bool Enabled { get; set; }

    private void Awake()
    {
        var count = HandShapes.JointCount;
        joints = new Transform[count];
        foreach (var reference in ghostHand.jointTransformReferences)
            joints[reference.xrHandJointID.ToIndex()] = reference.jointTransform;

        ghostHand.enabled = false;
        ghostHand.gameObject.SetActive(false);

        sample = new Pose[count];
        holdShape = new Pose[count];
        releaseShape = new Pose[count];
    }

    private void Update()
    {
        if (!Enabled) return;
        
        if (dartGrabber.HeldDart != null)
        {
            if (!wasHoldingDart)
            {
                Play();
            }
            wasHoldingDart = true;
        }
        else
        {
            wasHoldingDart = false;
        }
    }

    [ContextMenu("Play")]
    public void Play()
    {
        StopDemo();
        currentRoutine = StartCoroutine(PlayRoutine());
    }
    
    /// <summary>
    /// Stops the current demo and removes what it left in the scene
    /// </summary>
    public void StopDemo()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        if (currentDart != null)
        {
            Destroy(currentDart.gameObject);
            currentDart = null;
        }

        if (ghostHand != null) ghostHand.gameObject.SetActive(false);
        alignProgress = 0f;
    }
    
    private void OnDisable() => StopDemo();

    /// <summary>
    /// The routine that generate the swing and plays the movement
    /// </summary>
    /// <returns>Enumerator for a coroutine</returns>
    private IEnumerator PlayRoutine()
    {
        LoadHandShapes();

        var release = ReleasePosition();
        var dart = Instantiate(dartPrefab);
        currentDart = dart;
        var interpolation = dart.interpolation;
        dart.isKinematic = true;
        dart.interpolation = RigidbodyInterpolation.None;

        DartMath.ComputeGripHold(dart.transform, dart.GetComponent<DartGrip>().GrabPoint,
            gripPositionOffset, gripRotationOffset, out var holdPosition, out var holdRotation);
        var grip = new Pose(holdPosition, holdRotation);

        var velocity = SolveReleaseVelocity(release, grip);
        releasePinchRotation = DartMath.DartRotation(velocity, handRoll, localDartRotationOffset) * Quaternion.Inverse(grip.rotation);
        CalibrateSwing(release, velocity);

        var releaseTime = aimDuration + backswingDuration + forwardDuration;
        var endTime = releaseTime + followDuration + 0.3f;

        ghostHand.gameObject.SetActive(true);
        
        Hold(dart, PoseGhost(GhostAngle(0f, -releaseTime), 0f), grip);
        if (dartGrabber != null)
            yield return WaitForAlignment(dart.transform);
        
        var released = false;
        
        for (var time = 0f; time <= endTime; time += Time.deltaTime)
        {
            var relativeTime = time - releaseTime;
            if (!released && relativeTime >= 0f)
            {
                Hold(dart, PinchOnSwing(0f), grip);
                dart.isKinematic = false;
                dart.interpolation = interpolation;
                dart.linearVelocity = velocity;
                dart.angularVelocity = Vector3.zero;
                released = true;
                
                Destroy(dart.gameObject, dartLifetime);
                currentDart = null;
            }

            var open = relativeTime <= 0f ? 0f : Mathf.Clamp01(relativeTime / fingerOpenDuration);
            var pinch = PoseGhost(GhostAngle(time, relativeTime), open);
            if (!released) Hold(dart, pinch, grip);
            yield return null;
        }

        ghostHand.gameObject.SetActive(false);
        currentRoutine = null;  
    }

    /// <summary>
    /// Loads the hand shapes into pose arrays
    /// </summary>
    private void LoadHandShapes()
    {
        for (var joint = 0; joint < holdShape.Length; joint++)
        {
            holdShape[joint] = handShapes.Hold(joint);
            releaseShape[joint] = handShapes.Open(joint);
        }

        pinchInWrist = HandShapes.Pinch(holdShape);
    }

    /// <summary>
    /// Finds the release velocity that puts the dart's tip on the bullseye.
    /// Iterative, because the aim point depends on the dart's orientation at impact,
    /// which depends on the velocity.
    /// </summary>
    private Vector3 SolveReleaseVelocity(Vector3 release, Pose grip)
    {
        var velocity = (bullseye.position - release).normalized * dartSpeed;
        var flightTime = 0.4f;
        for (var i = 0; i < 5; i++)
        {
            var pinchRotation = DartMath.DartRotation(velocity, handRoll, localDartRotationOffset) *
                                Quaternion.Inverse(grip.rotation);
            var dartPosition = release + pinchRotation * grip.position;
            var impactRotation = DartMath.DartRotation(velocity + Physics.gravity * flightTime, handRoll,
                localDartRotationOffset);
            var aim = bullseye.position - impactRotation * dartTipOffset;
            velocity = DartMath.SolveVelocity(aim - dartPosition, dartSpeed, out flightTime);
        }

        return velocity;
    }

    /// <summary>
    /// Adjusts the swing so the grabber's velocity estimate times throwForce equals velocity
    /// </summary>
    /// <param name="release">Release position</param>
    /// <param name="velocity">Velocity targeted</param>
    private void CalibrateSwing(Vector3 release, Vector3 velocity)
    {
        var velocityNormalized = velocity.normalized;
        var handSpeed = velocity.magnitude / throwForce;
        for (var i = 0; i < 4; i++)
        {
            SetupSwing(release, velocityNormalized, handSpeed);
            var estimated = EstimateGrabberVelocity() * throwForce;
            velocityNormalized = DartMath.YawPitchBetween(estimated, velocity) * velocityNormalized;
            handSpeed *= velocity.magnitude / estimated.magnitude;
        }

        SetupSwing(release, velocityNormalized, handSpeed);
    }


    /// <summary>
    /// Swing angle at time <paramref name="time"/>: hold at the aim angle, ease back to the windup,
    /// then the forward swing and follow-through. <paramref name="relativeTime"/> is the time relative to release.
    /// </summary>
    /// <param name="time">Time of the movement</param>
    /// <param name="relativeTime">Relative time of the release</param>
    /// <returns>Angle of the ghost hand</returns>
    private float GhostAngle(float time, float relativeTime)
    {
        var aimRad = aimAngle * Mathf.Deg2Rad;
        if (time < aimDuration) return aimRad;
        if (time < aimDuration + backswingDuration)
            return Mathf.Lerp(aimRad, windupAngle * Mathf.Deg2Rad,
                Mathf.SmoothStep(0f, 1f, (time - aimDuration) / backswingDuration));
        return SwingAngle(relativeTime);
    }

    /// <summary>
    /// Angle of the movement based on the time in the movement
    /// </summary>
    /// <param name="relativeTime">Time in the movement</param>
    /// <returns>Angle of the movement</returns>
    private float SwingAngle(float relativeTime)
    {
        if (relativeTime <= 0f)
        {
            var tau = Mathf.Min(-relativeTime, forwardDuration);
            return -omega * tau + 0.5f * (omega / forwardDuration) * tau * tau;
        }

        var u = Mathf.Min(relativeTime, followDuration);
        return omega * u - 0.5f * (omega / followDuration) * u * u;
    }

    /// <summary>
    /// Release point on x, z and at the ratio of the height
    /// </summary>
    private Vector3 ReleasePosition()
    {
        return new Vector3(releasePoint.position.x, participantHead.position.y * releaseHeightRatio,
            releasePoint.position.z);
    }

    /// <summary>
    /// Generate the swing movement
    /// </summary>
    /// <param name="release">Release position</param>
    /// <param name="speedDirection">Direction of the speed on release</param>
    /// <param name="handSpeed">Hand speed on release</param>
    private void SetupSwing(Vector3 release, Vector3 speedDirection, float handSpeed)
    {
        swingForward = speedDirection.normalized;
        swingUp = Vector3.ProjectOnPlane(Vector3.up, swingForward).normalized;
        elbow = release - swingUp * forearmLength;
        omega = handSpeed / forearmLength;
        forwardDuration = 2f * Mathf.Abs(windupAngle * Mathf.Deg2Rad) / omega;
        followDuration = 2f * Mathf.Abs(followThroughAngle * Mathf.Deg2Rad) / omega;
    }

    /// <summary>
    /// Pinch position based on the angle of the movement
    /// </summary>
    /// <param name="angle">Angle of the movement</param>
    /// <returns>Pinch pose</returns>
    private Pose PinchOnSwing(float angle)
    {
        var direction = Mathf.Cos(angle) * swingUp + Mathf.Sin(angle) * swingForward;
        var swing = Quaternion.FromToRotation(swingUp, direction);
        var rotation = Quaternion.Slerp(Quaternion.identity, swing, wristFollow) * releasePinchRotation;
        return new Pose(elbow + forearmLength * direction, rotation);
    }

    /// <summary>
    /// Finds the velocity the hand grabber would find
    /// </summary>
    /// <returns>The hand grabber velocity</returns>
    private Vector3 EstimateGrabberVelocity()
    {
        swingSamples.Clear();
        var count = Mathf.Max(2, Mathf.FloorToInt(velocityWindow * trackingRate) + 1);
        for (var k = count - 1; k >= 0; k--)
        {
            var t = -k / trackingRate;
            swingSamples.Add(new DartMath.Sample { Time = t, Position = PinchOnSwing(SwingAngle(t)).position });
        }

        return DartMath.EstimateVelocity(swingSamples, velocityWindow);
    }

    /// <summary>
    /// Updates the sample and the joints based on the angle and how open the hand should be
    /// </summary>
    /// <param name="angle">Angle of the movement</param>
    /// <param name="open">How open the hand is</param>
    /// <returns>Returns the midpoint between the index and thumb (pinch position)</returns>
    private Pose PoseGhost(float angle, float open)
    {
        var wrist = DartMath.Compose(PinchOnSwing(angle), DartMath.Inverse(pinchInWrist));
        for (var i = 0; i < sample.Length; i++)
        {
            var local = new Pose(
                Vector3.Lerp(holdShape[i].position, releaseShape[i].position, open),
                Quaternion.Slerp(holdShape[i].rotation, releaseShape[i].rotation, open));
            sample[i] = DartMath.Compose(wrist, local);
            if (joints[i] != null) joints[i].SetPositionAndRotation(sample[i].position, sample[i].rotation);
        }

        return HandShapes.Pinch(sample);
    }

    /// <summary>
    /// Places the dart at the pinch point based on the grip pose
    /// </summary>
    /// <param name="dart">The dart</param>
    /// <param name="pinch">The pinch pose</param>
    /// <param name="localGrip">Local Grip</param>
    private static void Hold(Rigidbody dart, Pose pinch, Pose localGrip)
    {
        var pose = DartMath.Compose(pinch, localGrip);
        dart.transform.SetPositionAndRotation(pose.position, pose.rotation);
    }
    
    /// <summary>
    /// Waits until the participant's dart matches the ghost dart for a duration
    /// </summary>
    /// <param name="ghostDart">The dart transform</param>
    private IEnumerator WaitForAlignment(Transform ghostDart)
    {
        var axis = dartTipOffset.normalized;
        var alignedTime = 0f;
        alignProgress = 0f;
        ghostDart.TryGetComponent<DartTint>(out var tint);

        while (alignedTime < alignHoldDuration)
        {
            var held = dartGrabber.HeldDart;
            if (held != null)
            {
                var distance = Vector3.Distance(held.position, ghostDart.position);
                var angle = Vector3.Angle(held.rotation * axis, ghostDart.rotation * axis);
                var inside = distance <= positionTolerance && angle <= angleTolerance;
                alignedTime = inside ? alignedTime + Time.deltaTime : 0f;
            }
            else
            {
                alignedTime = 0f;
            }

            alignProgress = Mathf.Clamp01(alignedTime / alignHoldDuration);
            if (tint != null) tint.SetTint(alignProgress);
            yield return null;
        }

        if (tint != null) tint.ClearTint();
        alignProgress = 0f;
    }
}