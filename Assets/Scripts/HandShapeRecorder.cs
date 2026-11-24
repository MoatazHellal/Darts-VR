using UnityEngine;
using UnityEngine.XR.Hands;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class HandShapeRecorder : HandTrackingBehaviour
{
    [SerializeField] private Handedness handedness = Handedness.Right;
    [SerializeField] private bool armed = true;

    [SerializeField] private float openDelay = 0.15f;
    [SerializeField] private string folderName = "HandShapes";

    private XRHandSubsystem subsystem;
    private Pose[] lastPinching;
    private double releaseTime = -1;

    /// <summary>
    /// Called when hands joints positions are updated. Saves the positions of the joints and checks if
    /// the pose is pinching or open.
    /// </summary>
    /// <param name="handSubsystem"></param>
    /// <param name="flags"></param>
    /// <param name="updateType"></param>
    protected override void OnUpdatedHands(
        XRHandSubsystem handSubsystem,
        XRHandSubsystem.UpdateSuccessFlags flags,
        XRHandSubsystem.UpdateType updateType)
    {
        if (!armed) return;

        var hand = handedness == Handedness.Left ? handSubsystem.leftHand : handSubsystem.rightHand;
        if (!hand.isTracked || !TryGetJoints(hand, out var joints)) return;

        var distance = HandShapes.PinchDistance(joints);
        var now = Time.realtimeSinceStartupAsDouble;

        if (releaseTime < 0)
        {
            if (distance < HandsSettings.PinchingDistance) lastPinching = joints;
            else if (lastPinching != null && distance > HandsSettings.ReleaseDistance) releaseTime = now;
        }
        else if (now - releaseTime >= openDelay)
        {
            Save(lastPinching, joints);
            lastPinching = null;
            releaseTime = -1;
        }
    }

    /// <summary>
    /// Returns the joints of the hand
    /// </summary>
    /// <param name="hand">The XRHand containing the joints</param>
    /// <param name="joints">The joints found</param>
    /// <returns>Return false if there was an error</returns>
    private static bool TryGetJoints(XRHand hand, out Pose[] joints)
    {
        joints = new Pose[HandShapes.JointCount];
        for (var i = 0; i < joints.Length; i++)
        {
            if (!hand.GetJoint(XRHandJointIDUtility.FromIndex(i)).TryGetPose(out joints[i])) return false;
        }
        return true;
    }

    /// <summary>
    /// Save the hand shape in the editor
    /// </summary>
    /// <param name="hold"></param>
    /// <param name="open"></param>
    private void Save(Pose[] hold, Pose[] open)
    {
#if UNITY_EDITOR
        var shapes = ScriptableObject.CreateInstance<HandShapes>();
        shapes.handedness = handedness;
        ToWristSpace(hold, out shapes.holdPositions, out shapes.holdRotations);
        ToWristSpace(open, out shapes.openPositions, out shapes.openRotations);
 
        if (!AssetDatabase.IsValidFolder("Assets/" + folderName)) AssetDatabase.CreateFolder("Assets", folderName);
        var path = AssetDatabase.GenerateUniqueAssetPath($"Assets/{folderName}/HandShapes.asset");
        AssetDatabase.CreateAsset(shapes, path);
        AssetDatabase.SaveAssets();
#endif
    }

    /// <summary>
    /// Convert world pose to local positions
    /// </summary>
    /// <param name="joints"></param>
    /// <param name="positions"></param>
    /// <param name="rotations"></param>
    private static void ToWristSpace(Pose[] joints, out Vector3[] positions, out Quaternion[] rotations)
    {
        var wrist = joints[XRHandJointID.Wrist.ToIndex()];
        positions = new Vector3[joints.Length];
        rotations = new Quaternion[joints.Length];
        for (var i = 0; i < joints.Length; i++)
        {
            var local = DartMath.ToLocal(wrist, joints[i]);
            positions[i] = local.position;
            rotations[i] = local.rotation;
        }
    }
}