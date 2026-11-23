using UnityEngine;
using UnityEngine.XR.Hands;

public class HandShapes : ScriptableObject
{
    public static readonly int JointCount = XRHandJointID.EndMarker.ToIndex();
    private static readonly int ThumbTip = XRHandJointID.ThumbTip.ToIndex();
    private static readonly int IndexTip = XRHandJointID.IndexTip.ToIndex();
    private static readonly int IndexDistal = XRHandJointID.IndexDistal.ToIndex();

    public Handedness handedness;
    public Vector3[] holdPositions;
    public Quaternion[] holdRotations;
    public Vector3[] openPositions;
    public Quaternion[] openRotations;

    public Pose Hold(int joint) => new Pose(holdPositions[joint], holdRotations[joint]);
    public Pose Open(int joint) => new Pose(openPositions[joint], openRotations[joint]);
    
    /// <summary>
    /// Finds the midpoint of thumb and index tips, index distal rotation
    /// </summary>
    /// <param name="thumbTip">Pose of the thumb tip</param>
    /// <param name="indexTip">Pose of the index tip</param>
    /// <param name="indexDistal">Pose of the index distal</param>
    /// <returns>Returns the midpoint between the thumb and index</returns>
    public static Pose Pinch(Pose thumbTip, Pose indexTip, Pose indexDistal) =>
        new Pose((thumbTip.position + indexTip.position) / 2f, indexDistal.rotation);

    /// <summary>
    /// Finds the midpoint of thumb and index tips, index distal rotation
    /// </summary>
    /// <param name="joints">The joints of the hand</param>
    /// <returns>Returns the midpoint between the thumb and index</returns>
    public static Pose Pinch(Pose[] joints) => Pinch( joints[ThumbTip], joints[IndexTip], joints[IndexDistal]);

    /// <summary>
    /// Distance between the thumb and index tips
    /// </summary>
    /// <param name="thumbTip">Pose of the thumb tip</param>
    /// <param name="indexTip">Pose of the index tip</param>
    /// <returns>Distance between the thumb and index</returns>
    public static float PinchDistance(Pose thumbTip, Pose indexTip) =>
        Vector3.Distance(thumbTip.position, indexTip.position);

    /// <summary>
    /// Distance between the thumb and index tips
    /// </summary>
    /// <param name="joints">The joints of the hand</param>
    /// <returns>Distance between the thumb and index</returns>
    public static float PinchDistance(Pose[] joints) => PinchDistance(
        joints[XRHandJointID.ThumbTip.ToIndex()],
        joints[XRHandJointID.IndexTip.ToIndex()]);
}