public class Sudaram : EnemyBase
{
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

        b_detectionRange = DetectionRange;
        Enhanced = true;
        DetectionRange = attackRange = 9999f;
        StopMovement();
        attackPattern = AttackPattern.RANGED;
    }
    
    public void OnOriginiumPollutionExit()
    {
        if (!Enhanced) return;

        Enhanced = false;
        attackRange = b_attackRange;
        DetectionRange = b_detectionRange;
        attackPattern = AttackPattern.MELEE;
    }

    public override void WriteStats()
    {
        Description = "";
        Skillset = "";
        TooltipsDescription = "While in an <color=#CC40000>Originium Pollution</color>, stops moving and gains " +
            "the ability to perform global-ranged attacks.";
        base.WriteStats();
    }
}