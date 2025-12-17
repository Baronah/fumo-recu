using System.Collections;
using UnityEngine;

public class Toy : EnemyBase
{
    public bool IsActive => environmentalTilesStandingOn.Contains(StageManager.EnvironmentType.DARK_ZONE) && IsAlive();

    public override void EnemyFixedBehaviors()
    {
        if (!IsActive) return;
        base.EnemyFixedBehaviors();
    }

    public override void Move()
    {
        if (!IsActive) return;
        base.Move();
    }

    public override IEnumerator Attack()
    {
        if (!IsActive) yield break;
        yield return StartCoroutine(base.Attack());
    }

    public override IEnumerator OnAttackComplete()
    {
        if (!CanAttack || !SpottedPlayer) yield break;

        float ProjectileBaseSpeed = 600, ProjectileAcceleration = 150;

        Vector2 playerDir = (SpottedPlayer.transform.position - AttackPosition.position).normalized;
        Vector3 sourcePosition = AttackPosition.position;

        for (int i = 0; i < 360; i += 36)
        {
            float Speed = ProjectileBaseSpeed;
            if (i == 0)
            {
                Speed *= 1.5f;
                ProjectileAcceleration *= 1.33f;
            }

            float angle = i;
            Vector2 rotatedDir = Quaternion.Euler(0, 0, angle) * playerDir;

            Vector3 targetPosition = sourcePosition + (Vector3)rotatedDir;

            CreateProjectileAndShootToward(
                ProjectilePrefab,
                new DamageInstance(atk, 0, 0),
                sourcePosition,
                targetPosition,
                projectileType: ProjectileScript.ProjectileType.CATCH_FIRST_TARGET_OF_TYPE,
                travelSpeed: Speed,
                acceleration: ProjectileAcceleration,
                lifeSpan: 8f,
                targetType: typeof(PlayerBase));
        }
    }

    public void OnShroudedZoneExit()
    {
        CancelAttack();
        StopMovement();
        animator.SetBool("sleep", true);
    }

    public void OnShroudedZoneEnter()
    {
        animator.SetBool("sleep", false);
    }

    public override void WriteStats()
    {
        Description = "";
        Skillset =
            "• Unable to act.\n" +
            "• Comes into life when inside shrouded areas.";
        TooltipsDescription =
            "<color=yellow>Comes into life</color> while inside shrouded areas, performs long-ranged magical attacks.";

        base.WriteStats();
    }
}