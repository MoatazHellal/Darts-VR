using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

public class WristDashboardAnchor : MonoBehaviour
{
    [SerializeField] private Handedness wristHand = Handedness.Left;
    [SerializeField] private Transform trackingSpace;
    [SerializeField] private GameObject dashboard;

    private XRHandSubsystem subsystem;

    private readonly List<XRHandSubsystem> subsystems = new List<XRHandSubsystem>();

    private void LateUpdate()
    {
        if (subsystem == null || !subsystem.running)
        {
            subsystem = null;
            SubsystemManager.GetSubsystems(subsystems);

            foreach (XRHandSubsystem candidate in subsystems)
            {
                if (candidate.running)
                {
                    subsystem = candidate;
                    break;
                }
            }
        }

        if (subsystem == null || trackingSpace == null)
        {
            SetVisible(false);
            return;
        }

        XRHand hand = wristHand == Handedness.Left ? subsystem.leftHand : subsystem.rightHand;

        if (!hand.isTracked || !hand.GetJoint(XRHandJointID.Wrist).TryGetPose(out Pose wrist))
        {
            SetVisible(false);
            return;
        }

        transform.SetPositionAndRotation(trackingSpace.TransformPoint(wrist.position), trackingSpace.rotation * wrist.rotation);

        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        if (dashboard != null && dashboard.activeSelf != visible)
            dashboard.SetActive(visible);
    }
}