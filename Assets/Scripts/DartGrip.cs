using UnityEngine;

public class DartGrip : MonoBehaviour
{
    [SerializeField] private Transform grabPoint;
    public Transform GrabPoint => grabPoint;
}
