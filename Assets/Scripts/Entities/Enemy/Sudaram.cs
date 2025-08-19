using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class Sudaram : EnemyBase
{
    [SerializeField] private float bonusASPD = 40f;
    [SerializeField] public float originiumPollutionDamageMultiplier = 0.5f;
    private bool Enhanced = false;
    private float b_detectionRange;

    public override void Move()
    {
        if (Enhanced) return;
        base.Move();
    }

    public void OnOriginiumPollutionEnter()
    { 
        if (Enhanced) return;

        ASPD += bonusASPD;
        b_detectionRange = DetectionRange;
        Enhanced = true;
        DetectionRange = attackRange = 9999f;
        StopMovement();
        attackPattern = AttackPattern.RANGED;
    }
    
    public void OnOriginiumPollutionExit()
    {
        if (!Enhanced) return;

        ASPD -= bonusASPD;
        Enhanced = false;
        attackRange = b_attackRange;
        DetectionRange = b_detectionRange;
        attackPattern = AttackPattern.MELEE;
    }


    public override IEnumerator OnAttackComplete()
    {
        if (!CanAttack) yield break;

        if (Enhanced)
        {
            Vector2 playerDir = (SpottedPlayer.transform.position - AttackPosition.position).normalized;
            Vector3 sourcePosition = AttackPosition.position;

            float[] angles = { 0f, 90f, -90f, 180f };

            foreach (float angle in angles)
            {
                Vector2 rotatedDir = Quaternion.Euler(0, 0, angle) * playerDir;

                Vector3 targetPosition = sourcePosition + (Vector3)rotatedDir;

                CreateProjectileAndShootToward(
                    ProjectilePrefab,
                    new DamageInstance(atk, 0, 0),
                    sourcePosition,
                    targetPosition,
                    projectileType: ProjectileScript.ProjectileType.CATCH_FIRST_TARGET_OF_TYPE,
                    travelSpeed: ProjectileSpeed,
                    acceleration: 0,
                    lifeSpan: 8f,
                    targetType: typeof(PlayerBase));
            }
        }
        else
        {
            yield return StartCoroutine(base.OnAttackComplete());
        }
    }


    public override void WriteStats()
    {
        Description = "The Sudaram is the shroud of the dead. " +
            "Once gaunt from hunger, it is now nourished by the power of the Nachzehrer King. " +
            "For the Nachzehrer, consumption and nourishment—greed and devotion—are just two sides of the same dreaded coin.";
        Skillset = "• Takes reduced damage from <color=#CC4000>'Originium Pollutions'</color>." +
            "\n• While standing on a <color=#CC4000>'Originium Pollution'</color>, stops moving to gain increased ASPD. Attack now lauches 4 projectiles with unlimited range.";
        TooltipsDescription = "Takes reduced damage from <color=#CC4000>Originium Pollutions</color>. " +
            "While in an <color=#CC4000>Originium Pollution</color>, stops moving and gains " +
            "the ability to perform global-ranged attacks.";
        base.WriteStats();
    }
}