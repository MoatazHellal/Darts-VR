using UnityEngine;

public class DartGrip : MonoBehaviour
{
    [SerializeField] private Transform grabPoint;
    [SerializeField] private bool canGrab = true;
    
    public bool CanGrab => canGrab;
    
    public Transform GrabPoint => grabPoint;
}
