using UnityEngine;

public class DartSpawner : MonoBehaviour
{
    [SerializeField] private GameObject dartPrefab;
    [SerializeField] private Transform spawnPoint;

    private GameObject availableDart;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        SpawnDart();
    }

    private void SpawnDart()
    {
        if (availableDart != null)
            return;

        if (spawnPoint == null || dartPrefab == null)
            return;

        availableDart = Instantiate(dartPrefab, spawnPoint.position, spawnPoint.rotation);  
        availableDart.TryGetComponent(out DartPickupNotifier notifier);
        if (notifier != null)
        {
            notifier.Initialize(this);
        }
    }
    
    public void OnDartPickedUp(GameObject pickedUpDart)
    {
        if (pickedUpDart != availableDart)
            return;

        availableDart = null;
        SpawnDart();

        availableDart.GetComponent<DartPickupNotifier>().Initialize(this);
    }
}
