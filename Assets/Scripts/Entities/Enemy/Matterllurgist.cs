using System.Collections;
using UnityEngine;

public class Matterllurgist : EnemyBase
{
    [SerializeField] private Transform ProjectilePosition;

    public override void FlipAttackPosition()
    {
        base.FlipAttackPosition();
        ProjectilePosition.localPosition = new Vector3(
            -ProjectilePosition.localPosition.x,
            ProjectilePosition.localPosition.y,
            ProjectilePosition.localPosition.z
        );
    }

    public override void OnFirsttimePlayerSpot(bool viaAlert = false)
    {
        base.OnFirsttimePlayerSpot(viaAlert);
        if (viaAlert) ASPD += 200f;
    }

    public override IEnumerator OnAttackComplete()
    {
        if (!CanAttack) yield break;

        short ProjectileBaseSpeed = 600, ProjectileAcceleration = 250;

        Vector3 targetPosition = SpottedPlayer.transform.position;
        CreateProjectileAndShootToward(SpottedPlayer, ProjectilePosition.position, targetPosition, ProjectileType, ProjectileBaseSpeed, ProjectileAcceleration * 2);
        CreateProjectileAndShootToward(SpottedPlayer, ProjectilePosition.position, targetPosition + new Vector3(30, 0), ProjectileType, ProjectileBaseSpeed, ProjectileAcceleration);
        CreateProjectileAndShootToward(SpottedPlayer, ProjectilePosition.position, targetPosition - new Vector3(30, 0), ProjectileType, ProjectileBaseSpeed, ProjectileAcceleration);
    }

    public override void InitializeComponents()
    {
        attackPattern = AttackPattern.RANGED;
        damageType = DamageType.PHYSICAL;

        base.InitializeComponents();
    }

    public override void WriteStats()
    {
        Description = "A ranged combatant. Integrating Sarkaz tactics with Leithanien Arts, they are capable of inflicting lethal damage with a block of energy.";
        Skillset = "• Attacks fire up to 3 projectiles at once.\n" +
            "• When alerted by a Sentinel, ASPD is greatly increased."; ;
        TooltipsDescription = "Ranged unit, attacks fire 3 projectiles that deal physical damage. <color=yellow>Keeps distance</color> from the player unit. <color=yellow>If alerted early</color>, ASPD greatly increases.";
    
        base.WriteStats();
    }
}