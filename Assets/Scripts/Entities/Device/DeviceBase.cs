using System.Collections;
using UnityEngine;

public class DeviceBase : MonoBehaviour
{
    public enum DeviceType
    {
        TURRET,

    }

    [SerializeField] protected HealthBar SPbar;
}