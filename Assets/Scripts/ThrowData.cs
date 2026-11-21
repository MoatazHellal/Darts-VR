using UnityEngine;

public class ThrowData : MonoBehaviour
{
    public bool HasBeenReleased { get; private set; }
    public Vector3 ReleasePosition { get; private set; }
    public Vector3 ReleaseDirection { get; private set; }
    public Quaternion ReleaseRotation { get; private set; }
    public Vector3 ReleaseVelocity { get; private set; }
    public float VerticalTilt { get; private set; }
    public float HorizontalTilt { get; private set; }

    public float ReleaseSpeed => ReleaseVelocity.magnitude;

    public void RecordRelease(Vector3 velocity, Vector3 forwardReference)
    {
        ReleasePosition = transform.position;
        ReleaseRotation = transform.rotation;

        ReleaseDirection = -transform.forward;
        Vector3 normalizedDirection = ReleaseDirection.normalized;

        float vertical = Vector3.Dot(normalizedDirection, Vector3.up);
        VerticalTilt = Mathf.Asin(Mathf.Clamp(vertical, -1f, 1f)) * Mathf.Rad2Deg;

        Vector3 forward = Vector3.ProjectOnPlane(forwardReference, Vector3.up);
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        HorizontalTilt = Mathf.Atan2(Vector3.Dot(normalizedDirection, right), Vector3.Dot(normalizedDirection, forward.normalized)) * Mathf.Rad2Deg;

        ReleaseVelocity = velocity;
        HasBeenReleased = true; 
    }
}
