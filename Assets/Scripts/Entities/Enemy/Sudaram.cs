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

    public override void WriteStats()
    {
        Description = "";
        Skillset = "";
        TooltipsDescription = "Takes reduced damage from <color=#CC4000>Originium Pollutions</color>. " +
            "While in an <color=#CC4000>Originium Pollution</color>, stops moving and gains " +
            "the ability to perform global-ranged attacks.";
        base.WriteStats();
    }
}