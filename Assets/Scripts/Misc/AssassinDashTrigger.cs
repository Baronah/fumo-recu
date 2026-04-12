using UnityEngine;

public class AssassinDashTrigger : MonoBehaviour
{
    ShroudedAssassin Assassin;
    Collider2D[] Colliders;

    private void Start()
    {
        Assassin = GetComponentInParent<ShroudedAssassin>();
        Colliders = GetComponents<Collider2D>();
    }

    public void EnableColliders(bool enable)
    {
        foreach (var collider in Colliders)
        {
            collider.enabled = enable;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        if (!collision.TryGetComponent<PlayerBase>(out var player)) return;

        // Assassin.OnDashTriggerEnter(player);
    }
}