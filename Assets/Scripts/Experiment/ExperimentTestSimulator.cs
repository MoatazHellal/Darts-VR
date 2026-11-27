using System.Collections;
using UnityEngine;

#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

public class ExperimentTestSimulator : MonoBehaviour
{
#if UNITY_EDITOR

    [Header("References")]
    [SerializeField] private TrialRecorder trialRecorder;
    [SerializeField] private Transform boardCenter;

    [Header("Simulation Configuration")]
    [SerializeField] private float simulatedGrabDuration = 1f;
    [SerializeField] private float simulatedFlightDuration = 0.3f;

    private bool simulationInProgress;
    private GameObject simulatedDart;

    private enum SimulationType
    {
        Hit,
        Miss,
        Invalid,
        Outside,
        Deflected,
        Timeout
    }

    private void Update()
    {
        if (!SessionSettings.IsTestMode)
            return;

        if (trialRecorder == null ||
            boardCenter == null)
            return;

        if (simulationInProgress ||
            !trialRecorder.CanGrab)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard.f1Key.wasPressedThisFrame)
            StartCoroutine(SimulateTrial(SimulationType.Hit));

        else if (keyboard.f2Key.wasPressedThisFrame)
            StartCoroutine(SimulateTrial(SimulationType.Miss));

        else if (keyboard.f3Key.wasPressedThisFrame)
            StartCoroutine(SimulateTrial(SimulationType.Invalid));

        else if (keyboard.f4Key.wasPressedThisFrame)
            StartCoroutine(SimulateTrial(SimulationType.Outside));

        else if (keyboard.f5Key.wasPressedThisFrame)
            StartCoroutine(SimulateTrial(SimulationType.Deflected));

        else if (keyboard.f6Key.wasPressedThisFrame)
            StartCoroutine(SimulateTrial(SimulationType.Timeout));
    }

    private IEnumerator SimulateTrial(SimulationType type)
    {
        simulationInProgress = true;

        Debug.Log("[TEST] Simulating: " + type);

        // Create an invisible virtual dart
        simulatedDart = new GameObject("SimulatedDart");

        Vector3 releasePosition =
            boardCenter.position +
            boardCenter.forward * 2.37f +
            boardCenter.right * 0.1f;

        simulatedDart.transform.position = releasePosition;

        Vector3 direction =
            (boardCenter.position - releasePosition).normalized;

        // ThrowData uses -transform.forward
        simulatedDart.transform.rotation =
            Quaternion.LookRotation(-direction, Vector3.up);

        ThrowData dart =
            simulatedDart.AddComponent<ThrowData>();

        // Simulate grabbing the dart
        if (!trialRecorder.OnDartGrabbed(dart))
        {
            Cleanup();
            yield break;
        }

        yield return new WaitForSecondsRealtime(
            simulatedGrabDuration
        );

        // Invalid attempt before release
        if (type == SimulationType.Invalid)
        {
            trialRecorder.CancelCurrentTrial(
                dart,
                "SimulatedTrackingLoss"
            );

            Cleanup();
            yield break;
        }

        // Simulate releasing the dart
        Vector3 velocity = direction * 6f;

        dart.RecordRelease(velocity, direction);

        trialRecorder.OnDartReleased(dart);

        yield return new WaitForSecondsRealtime(
            simulatedFlightDuration
        );

        switch (type)
        {
            case SimulationType.Hit:

                Vector3 hitPosition =
                    boardCenter.position +
                    boardCenter.right * 0.05f;

                trialRecorder.OnDartImpact(
                    dart,
                    hitPosition,
                    20,
                    "SimulatedSingle20",
                    0.05f
                );

                break;

            case SimulationType.Miss:

                trialRecorder.OnDartMiss(
                    dart,
                    "SimulatedWallImpact"
                );

                break;

            case SimulationType.Outside:

                Vector3 outsidePosition =
                    boardCenter.position +
                    boardCenter.right * 0.20f;

                trialRecorder.OnDartImpact(
                    dart,
                    outsidePosition,
                    0,
                    "Outside",
                    0.20f
                );

                break;

            case SimulationType.Deflected:

                trialRecorder.OnDartDeflected(dart);

                break;

            case SimulationType.Timeout:

                // Let TrialRecorder handle the timeout
                while (trialRecorder != null &&
                       trialRecorder.IsBusy &&
                       trialRecorder.RecordingEnabled)
                {
                    yield return null;
                }

                break;
        }

        Cleanup();
    }

    private void Cleanup()
    {
        if (simulatedDart != null)
        {
            Destroy(simulatedDart);
            simulatedDart = null;
        }

        simulationInProgress = false;

        Debug.Log("[TEST] Simulation finished.");
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        if (simulatedDart != null)
        {
            ThrowData dart =
                simulatedDart.GetComponent<ThrowData>();

            if (trialRecorder != null)
            {
                trialRecorder.CancelCurrentTrial(
                    dart,
                    "SimulatorDisabled"
                );
            }

            Destroy(simulatedDart);
            simulatedDart = null;
        }

        simulationInProgress = false;
    }

#endif
}