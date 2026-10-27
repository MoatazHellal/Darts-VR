using UnityEngine;

public class DartLauncher : MonoBehaviour
{
    [SerializeField] private Rigidbody dartPrefab;
    [SerializeField] private float launchSpeed = 8f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    [ContextMenu("Launch Test Dart")]
    public void LaunchDart()
    {
        if (!Application.isPlaying || dartPrefab == null)
            return;

        Rigidbody dart = Instantiate(dartPrefab, transform.position, transform.rotation * Quaternion.Euler(0f, 180f, 0f));

        dart.linearVelocity = transform.forward * launchSpeed;
    }
}
