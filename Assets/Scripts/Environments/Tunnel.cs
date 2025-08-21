using UnityEngine;

public class Tunnel : MonoBehaviour
{
    [SerializeField] private Transform tunnelExit;
    public Transform TunnelEntrance => this.transform;
    public Transform TunnelExit => tunnelExit;

    private void OnDrawGizmos()
    {
        if (TunnelEntrance != null && tunnelExit != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(TunnelEntrance.position, tunnelExit.position);
        }
    }

    public void EnterTunnel(EntityBase entityBase) => entityBase.transform.position = tunnelExit.position;
}