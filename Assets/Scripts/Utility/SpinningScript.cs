using UnityEngine;

public class SpinningScript : MonoBehaviour
{
    [SerializeField] private Vector3 RotationPerSec;

    Quaternion init;
    private void Start()
    {
        init = transform.rotation;
    }

    private void Update()
    {
        transform.Rotate(RotationPerSec * Time.deltaTime);
    }

    private void OnDisable()
    {
        transform.rotation = init;
    }
}