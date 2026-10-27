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

        DartboardSurface surface = collision.collider.GetComponentInParent<DartboardSurface>();

        if (surface == null)
            return;

        if (tip == null)
            return;

        bool hit = false;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);

            bool isNearTip = Vector3.Distance(tip.position, contact.point) <= tipContactDistance;
            bool isStickAngle = Vector3.Angle(-transform.forward, -contact.normal) <= maxStickAngle;

            if (isNearTip && isStickAngle)
            {
                hit = true;
                break;
            }
        }

        if (!hit)
        {
            PlayDeflectSound();
            return;
        }
            

        isStuck = true;

        rigidBody.linearVelocity = Vector3.zero;
        rigidBody.angularVelocity = Vector3.zero;
        rigidBody.isKinematic = true;

        PlayRandomHitSound();
    }

    private void PlayRandomHitSound()
    {
        if (audioSource == null || hitSounds == null || hitSounds.Length == 0)
            return;

        int index = Random.Range(0, hitSounds.Length);
        AudioClip clip = hitSounds[index];

        if (clip != null)
            audioSource.PlayOneShot(clip);
    }

    private void PlayDeflectSound()
    {
        if (audioSource == null || deflectSound == null)
            return;

        audioSource.PlayOneShot(deflectSound);
    }
}
