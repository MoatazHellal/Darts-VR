using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
public class DartFlightAlign : MonoBehaviour
{
    [SerializeField] private Vector3 localFlightAxis = Vector3.back;
    [SerializeField] private float minSpeed = 1f;

    private Rigidbody body;
    private bool flying;

    private void Awake() => body = GetComponent<Rigidbody>();

    private void FixedUpdate()
    {
        if (body.isKinematic)
        {
            flying = true;
            return;
        }

        if (!flying) return;

        var velocity = body.linearVelocity;
        if (velocity.sqrMagnitude < minSpeed * minSpeed) return;

        var axis = body.rotation * localFlightAxis;
        body.MoveRotation(Quaternion.FromToRotation(axis, velocity) * body.rotation);
    }

    private void OnCollisionEnter(Collision collision) => flying = false;
}