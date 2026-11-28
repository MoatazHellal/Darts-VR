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

    public DartThrowDemo Demo
    {
        get
        {
            if (handedness == Handedness.Right)
                return rightDemo;
            else 
                return leftDemo;
        }
    }
    
    public DartHandGrabber Grabber
    {
        get
        {
            if (handedness == Handedness.Right)
                return rightHandGrabber;
            else 
                return leftHandGrabber;
        }
    }

    private void Awake()
    {
        dartSpawner.handedness = handedness;
        if (handedness == Handedness.Right)
        {
            rightHandGrabber.gameObject.SetActive(true);
            leftHandGrabber.gameObject.SetActive(false);
            rightDemo.gameObject.SetActive(true);
            leftDemo.gameObject.SetActive(false);
        }
        else
        {
            rightHandGrabber.gameObject.SetActive(false);
            leftHandGrabber.gameObject.SetActive(true);
            rightDemo.gameObject.SetActive(false);
            leftDemo.gameObject.SetActive(true);
        }
    }
}
