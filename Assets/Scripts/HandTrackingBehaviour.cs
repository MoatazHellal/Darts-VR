using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

public abstract class HandTrackingBehaviour : MonoBehaviour
{
    private static readonly List<XRHandSubsystem> Subsystems = new List<XRHandSubsystem>();
    private XRHandSubsystem subsystem;

    protected virtual void Update()
    {
        if (subsystem != null && subsystem.running) return;

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

    protected virtual void OnDisable() => Unbind();

    private void Unbind()
    {
        if (subsystem != null) subsystem.updatedHands -= OnUpdatedHands;
        subsystem = null;
    }

    protected abstract void OnUpdatedHands(
        XRHandSubsystem handSubsystem,
        XRHandSubsystem.UpdateSuccessFlags flags,
        XRHandSubsystem.UpdateType updateType);
}