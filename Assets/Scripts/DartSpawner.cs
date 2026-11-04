using System.Collections;
using UnityEngine;
using UnityEngine.XR.Hands;

public class DartSpawner : MonoBehaviour
{
    [SerializeField] private GameObject dartPrefab;
    [SerializeField] private Transform rightSpawnPoint;
    [SerializeField] private Transform leftSpawnPoint;
    [SerializeField] private float respawnDelay = 2f;

    [System.NonSerialized] public Handedness handedness = Handedness.Right;
    
    private GameObject availableDart;
    
    private Transform SpawnPoint => handedness == Handedness.Right ? rightSpawnPoint : leftSpawnPoint;

    private void Start()
    {
        SpawnDart();
    }

    private void SpawnDart()
    {
        if (availableDart != null || SpawnPoint == null || dartPrefab == null)
            return;

        availableDart = Instantiate(dartPrefab, SpawnPoint.position, Quaternion.identity);
        var root = availableDart.transform;

        var tipDown = Quaternion.LookRotation(Vector3.down, SpawnPoint.forward);

        if (availableDart.TryGetComponent(out DartGrip grip) && grip.GrabPoint != null)
        {
            var gripRot = grip.GrabPoint.rotation;
            var gripPos = grip.GrabPoint.position - root.position;

            var rotation = tipDown * Quaternion.Inverse(gripRot);
            root.SetPositionAndRotation(SpawnPoint.position - rotation * gripPos, rotation);
        }
        else
        {
            Debug.LogError("DartGrip not found");
            root.rotation = Quaternion.LookRotation(Vector3.up, SpawnPoint.forward);
        }

        if (availableDart.TryGetComponent(out Rigidbody body))
            body.isKinematic = true;

        if (availableDart.TryGetComponent(out DartPickupNotifier notifier))
            notifier.Initialize(this);
    }

    public void OnDartPickedUp(GameObject pickedUpDart)
    {
        if (pickedUpDart != availableDart)
            return;

        availableDart = null;
        StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        SpawnDart();
    }
}
