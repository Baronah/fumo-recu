using System.Collections;
using UnityEngine;

public class HibernatorKnight : EnemyBase
{
    bool IsSleeping => environmentalTilesStandingOn.Contains(StageManager.EnvironmentType.DARK_ZONE);

    public override void InitializeComponents()
    {
        base.InitializeComponents();
        if (!IsSleeping)
        {
            ApplyEffect(Effect.AffectedStat.DEF, "HIBERNATOR_NATURAL_DEF_BUFF", DefBuff, 9999f, false);
            ApplyEffect(Effect.AffectedStat.RES, "HIBERNATOR_NATURAL_RES_BUFF", ResBuff, 9999f, false);
        }
    }

    public override void EnemyFixedBehaviors()
    {
        if (IsSleeping) return;
        base.EnemyFixedBehaviors();
    }

    public override void Move()
    {
        if (IsSleeping) return;
        base.Move();
    }

    public override IEnumerator Attack()
    {
        if (IsSleeping) yield break;
        StartCoroutine(base.Attack());
    }

    public override void OnAttackReceive(EntityBase source)
    {
        if (IsSleeping) return;
        base.OnAttackReceive(source);
    }

    [SerializeField] private float DefBuff = 40, ResBuff = 20;
    public void OnShroudedZoneEnter()
    {
        RemoveEffect("HIBERNATOR_NATURAL_DEF_BUFF");
        RemoveEffect("HIBERNATOR_NATURAL_RES_BUFF");
        CancelAttack();
        StopMovement();
        animator.SetBool("sleep", true);
    }

    public void OnShroudedZoneExit()
    {
        ApplyEffect(Effect.AffectedStat.DEF, "HIBERNATOR_NATURAL_DEF_BUFF", DefBuff, 9999f, false);
        ApplyEffect(Effect.AffectedStat.RES, "HIBERNATOR_NATURAL_RES_BUFF", ResBuff, 9999f, false);
        animator.SetBool("sleep", false);
    }

    public override void WriteStats()
    {
        Description = "A commonly seen burly knight whose Knightclub affiliation is unknown, considered rogue. " +
            "They're often found passed out drunk in dark alleyways, " +
            "and should be avoided as they demonstrate ferociously violent tendencies when woken up.";
        Skillset = 
            "• Normally has increased DEF and RES, disabled under shrouded zones.\n" +
            "• Unable to move and attack while in shrouded zones.";
        TooltipsDescription = "Has increased DEF and RES. While in shrouded zones, the effect is disabled, and self becomes " +
            "<color=yellow>unable to act</color>.";

        base.WriteStats();
    }
}