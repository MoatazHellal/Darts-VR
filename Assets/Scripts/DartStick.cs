
using UnityEngine;

public class DartStick : MonoBehaviour
{
    private Rigidbody rigidBody;
    private bool isStuck;

    [SerializeField] private Transform tip;
    [SerializeField] private float tipContactDistance = 0.05f;
    [SerializeField] private float maxStickAngle = 45f;

    // Audio
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField] private AudioClip deflectSound;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isStuck)
            return;

        ThrowData throwData = GetComponent<ThrowData>();

        DartboardSurface surface =
            collision.collider
                .GetComponentInParent<DartboardSurface>();

        // ==========================================
        // EXPERIMENT INTEGRATION
        // Collision with a non-target surface
        // ==========================================

        if (surface == null)
        {
            if (throwData != null &&
                throwData.HasBeenReleased &&
                TrialRecorder.Instance != null)
            {
                TrialRecorder.Instance.OnDartMiss(
                    throwData,
                    "NonTargetCollision"
                );
            }

            return;
        }

        // Original collision validation

        if (tip == null)
            return;

        bool hit = false;
        Vector3 impactPosition = Vector3.zero;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact =
                collision.GetContact(i);

            bool isNearTip =
                Vector3.Distance(
                    tip.position,
                    contact.point
                ) <= tipContactDistance;

            bool isStickAngle =
                Vector3.Angle(
                    -transform.forward,
                    -contact.normal
                ) <= maxStickAngle;

            if (isNearTip && isStickAngle)
            {
                hit = true;
                impactPosition = contact.point;
                break;
            }
        }

        if (!hit)
        {
            PlayDeflectSound();

            // ======================================
            // EXPERIMENT INTEGRATION
            // Rejected dartboard impact
            // ======================================

            if (throwData != null &&
                throwData.HasBeenReleased &&
                TrialRecorder.Instance != null)
            {
                TrialRecorder.Instance.OnDartDeflected(
                    throwData
                );
            }

            return;
        }

        // Original sticking mechanism

        isStuck = true;

        rigidBody.linearVelocity = Vector3.zero;
        rigidBody.angularVelocity = Vector3.zero;
        rigidBody.isKinematic = true;

        surface.RegisterHit(
            impactPosition,
            throwData
        );

        PlayRandomHitSound();
    }

    private void PlayRandomHitSound()
    {
        if (audioSource == null ||
            hitSounds == null ||
            hitSounds.Length == 0)
        {
            return;
        }

        int index = Random.Range(0, hitSounds.Length);

        AudioClip clip = hitSounds[index];

        if (clip != null)
            audioSource.PlayOneShot(clip);
    }

    private void PlayDeflectSound()
    {
        if (audioSource == null ||
            deflectSound == null)
        {
            return;
        }

        audioSource.PlayOneShot(deflectSound);
    }
}
