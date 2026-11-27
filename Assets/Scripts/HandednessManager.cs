using UnityEngine;
using UnityEngine.XR.Hands;

public class HandednessManager : MonoBehaviour
{
    [SerializeField] private Handedness handedness = Handedness.Right;
    [SerializeField] private DartSpawner dartSpawner;
    [SerializeField] private DartHandGrabber rightHandGrabber;
    [SerializeField] private DartHandGrabber leftHandGrabber;
    [SerializeField] private DartThrowDemo rightDemo;
    [SerializeField] private DartThrowDemo leftDemo;

    private void Awake()
    {
        // Retrieve the selected dominant hand
        handedness = SessionSettings.DominantHand == "Left"
            ? Handedness.Left
            : Handedness.Right;

        Debug.Log("Applied dominant hand: " + handedness);

        dartSpawner.handedness = handedness;

        if (handedness == Handedness.Right)
        {
            rightHandGrabber.gameObject.SetActive(true);
            leftHandGrabber.gameObject.SetActive(false);

            if (rightDemo != null)
                rightDemo.gameObject.SetActive(true);

            if (leftDemo != null)
                leftDemo.gameObject.SetActive(false);
        }
        else
        {
            rightHandGrabber.gameObject.SetActive(false);
            leftHandGrabber.gameObject.SetActive(true);

            if (rightDemo != null)
                rightDemo.gameObject.SetActive(false);

            if (leftDemo != null)
                leftDemo.gameObject.SetActive(true);
        }
    }
}