using UnityEngine;

public class DemoModule : MonoBehaviour
{
    [SerializeField] private DartThrowDemo leftDartThrowDemo;
    [SerializeField] private DartThrowDemo rightDartThrowDemo;

    [ContextMenu("Enable")]
    public void Enable()
    {
        SetFeedbackEnabled(true);
    }
    
    [ContextMenu("Disable")]
    public void Disable()
    {
        SetFeedbackEnabled(false);
    }
    
    public void SetFeedbackEnabled(bool enabled)
    {
        leftDartThrowDemo.Enabled = enabled;
        rightDartThrowDemo.Enabled = enabled;
    }
}
