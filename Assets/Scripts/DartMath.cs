using System.Collections.Generic;
using UnityEngine;

public static class DartMath
{
    public static void ComputeGripHold(
        Transform dartRoot, Transform grabPoint,
        Vector3 positionOffset, Vector3 rotationOffsetEuler,
        out Vector3 localPosition, out Quaternion localRotation)
    {
        var invRoot = Quaternion.Inverse(dartRoot.rotation);
        var gripRotation = invRoot * grabPoint.rotation;
        var gripPosition = invRoot * (grabPoint.position - dartRoot.position);

        localRotation = Quaternion.Euler(rotationOffsetEuler) * Quaternion.Inverse(gripRotation);
        localPosition = positionOffset - localRotation * gripPosition;
    }
    
    public struct Sample
    {
        public double Time;
        public Vector3 Position;
    }
    
    /// <summary>
    /// Estimate velocity based on the samples and the window given
    /// </summary>
    /// <param name="samples">Samples of the movement</param>
    /// <param name="window">Window of the samples to use</param>
    /// <returns>The velocity of the dart at release</returns>
    public static Vector3 EstimateVelocity(IReadOnlyList<Sample> samples, float window)
    {
        if (samples.Count < 2) return Vector3.zero;
 
        var last = samples.Count - 1;
        var end = samples[last].Time;
        var first = last;
        while (first > 0 && samples[first - 1].Time >= end - window) first--;
        first = Mathf.Min(first, last - 1);
        var n = samples.Count - first;
 
        double meanT = 0;
        var meanP = Vector3.zero;
        for (var i = first; i <= last; i++)
        {
            meanT += samples[i].Time - end;
            meanP += samples[i].Position;
        }
        meanT /= n;
        meanP /= n;
 
        var numerator = Vector3.zero;
        double denominator = 0;
        for (var i = first; i <= last; i++)
        {
            var dt = samples[i].Time - end - meanT;
            numerator += (float)dt * (samples[i].Position - meanP);
            denominator += dt * dt;
        }
 
        return denominator > 1e-9 ? numerator / (float)denominator : Vector3.zero;
    }
    
    /// <summary>
    /// Computes the launch velocity that moves a body by a displacement in exactly flightTime seconds. Matches Unity integration
    /// </summary>
    /// <param name="displacement">Target position minus launch position in world space</param>
    /// <param name="flightTime">Desired time to reach the target</param>
    /// <returns>The initial velocity to give the body.</returns>
    public static Vector3 VelocityFor(Vector3 displacement, float flightTime) =>
        (displacement - 0.5f * flightTime * (flightTime + Time.fixedDeltaTime) * Physics.gravity) / flightTime;
    
    /// <summary>
    /// Converts the Pose to the Parent's reference
    /// </summary>
    /// <param name="parent">Parent pose</param>
    /// <param name="local">Pose to convert</param>
    /// <returns>The new pose in the parent's reference</returns>
    public static Pose Compose(Pose parent, Pose local)
    {
        return new Pose(parent.position + parent.rotation * local.position, parent.rotation * local.rotation);
    }

    /// <summary>
    /// Inverse of the pose
    /// </summary>
    /// <param name="pose">Pose to inverse</param>
    /// <returns>Inversed pose</returns>
    public static Pose Inverse(Pose pose)
    {
        var inverse = Quaternion.Inverse(pose.rotation);
        return new Pose(inverse * -pose.position, inverse);
    }
    
    /// <summary>
    /// Expresses a world pose relative to parent
    /// </summary>
    /// <param name="parent">Parent</param>
    /// <param name="world">World pose</param>
    /// <returns>The pose in local reference to the parent</returns>
    public static Pose ToLocal(Pose parent, Pose world) => Compose(Inverse(parent), world);

    /// <summary>
    /// Find the rotation that turns the vector without any roll
    /// </summary>
    /// <param name="from">Initial vector</param>
    /// <param name="to">Wanted vector</param>
    /// <returns>The Quaternion that turns the vector</returns>
    public static Quaternion YawPitchBetween(Vector3 from, Vector3 to)
    {
        var yaw = Quaternion.FromToRotation(
            Vector3.ProjectOnPlane(from, Vector3.up),
            Vector3.ProjectOnPlane(to, Vector3.up));
        var pitch = Quaternion.FromToRotation(yaw * from, to);
        return pitch * yaw;
    }
    
    /// <summary>
    /// Finds the flattest throw that reaches the target without exceeding a speed
    /// </summary>
    /// <param name="displacement">The displacement to reach the target</param>
    /// <param name="speed">Speed to not exceed</param>
    /// <param name="flightTime">Flight time of the trajectory returned</param>
    /// <returns>The trajectory for the dart on release</returns>
    public static Vector3 SolveVelocity(Vector3 displacement, float speed, out float flightTime)
    {
        var bestTime = 1.5f;
        var bestSpeed = float.MaxValue;
        for (var t = 0.1f; t <= 1.5f; t += 0.001f)
        {
            var s = DartMath.VelocityFor(displacement, t).magnitude;
            if (s <= speed)
            {
                flightTime = t;
                return DartMath.VelocityFor(displacement, t);
            }

            if (s < bestSpeed)
            {
                bestSpeed = s;
                bestTime = t;
            }
        }

        flightTime = bestTime;
        return DartMath.VelocityFor(displacement, bestTime);
    }

    /// <summary>
    /// World rotation for a dart pointing along the direction, rolled by the roll
    /// </summary>
    /// <param name="direction">Direction the dart's tip should face</param>
    /// <param name="roll">Roll around the direction</param>
    /// <returns>The rotation in world space</returns>
    public static Quaternion DartRotation(Vector3 direction, float roll, Vector3 localOffset)
    {
        return Quaternion.AngleAxis(roll, direction.normalized) * Quaternion.LookRotation(direction) *
               Quaternion.Euler(localOffset);
    }
        
}