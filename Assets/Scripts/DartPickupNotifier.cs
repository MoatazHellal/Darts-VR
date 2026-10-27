using UnityEngine;

public class DartPickupNotifier : MonoBehaviour
{

    private DartSpawner dartSpawner;
    private bool isPickedUp;
    
    public void Initialize(DartSpawner owner)
    {
        dartSpawner = owner; 
    }

    public void NotifyPickedUp()
    {
        if (isPickedUp || dartSpawner == null)
            return;

        isPickedUp = true;
        dartSpawner.OnDartPickedUp(gameObject);
    }

    
}
