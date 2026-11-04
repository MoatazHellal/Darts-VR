using UnityEngine;
using UnityEngine.XR.Hands;

public class HandednessManager : MonoBehaviour
{
    [SerializeField] private Handedness handedness = Handedness.Right;
    [SerializeField] private DartSpawner dartSpawner;
    [SerializeField] private DartHandGrabber rightHandGrabber;
    [SerializeField] private DartHandGrabber leftHandGrabber;

    private void Start()
    {
        dartSpawner.handedness = handedness;
        if (handedness == Handedness.Right)
        {
            rightHandGrabber.gameObject.SetActive(true);
            leftHandGrabber.gameObject.SetActive(false);
        }
        else
        {
            rightHandGrabber.gameObject.SetActive(false);
            leftHandGrabber.gameObject.SetActive(true);
        }
    }
}
