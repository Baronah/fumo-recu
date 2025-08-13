using UnityEngine;

public class OriginiumSpider : EnemyBase
{
    [SerializeField] private float explosionAtkScale = 3.0f;
    [SerializeField] private float explosionRadius = 250f;

    public override void OnDeath()
    {
        var player = DetectPlayer(explosionRadius, true);
        if (player)
        {
            DealDamage(player, new DamageInstance(0,0, (int)(atk * explosionAtkScale)));
        }
        base.OnDeath();
    }

    public override void WriteStats()
    {
        Description = "A spider-like creature that has been mutated by Originium.";
        Skillset = "";
        TooltipsDescription = "Explodes upon death, dealing massive true damage. Will be instantly defeated upon taking damage from <color=#CC4000>Originium Pollution</color>";
        base.WriteStats();
    }
}