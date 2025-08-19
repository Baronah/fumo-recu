using System.Collections;
using UnityEngine;

public class Wetwork : EnemyBase
{
    [SerializeField] private float DefPenUp_perSecond = 1f;
    [SerializeField] private float DefPenUp_cap = 50f;
    [SerializeField] private float AtkPercentageUp_perSecond = 0.05f;
    [SerializeField] private float AtkPercentageUp_cap = 2.0f;
    [SerializeField] private GameObject PS;

    private float AtkPercentageUp_count = 0;
    private short AtkAdd = 0;   

    public override void InitializeComponents()
    {
        base.InitializeComponents();
        if (!ViewOnlyMode) StartCoroutine(IncreasesAtk());
    }

    IEnumerator IncreasesAtk()
    {
        while (true)
        {
            if (!IsAlive()) yield break;

            yield return new WaitForSeconds(1f);

            PS.SetActive(IsAlive() && AtkPercentageUp_count >= AtkPercentageUp_cap / 2);
            if (IsAttackLocked) continue;

            AtkPercentageUp_count = Mathf.Min(AtkPercentageUp_count + AtkPercentageUp_perSecond, AtkPercentageUp_cap);
        }
    }

    public override IEnumerator Attack()
    {
        AtkAdd = (short)(bAtk * AtkPercentageUp_count);
        atk += AtkAdd;

        yield return StartCoroutine(base.Attack());

    }

    public override IEnumerator OnAttackComplete()
    {
        yield return StartCoroutine(base.OnAttackComplete());
        AtkPercentageUp_count = 0f;
        atk -= AtkAdd;
    }

    public override void WriteStats()
    {
        Description = "An assassin who makes up for their average skills with clever plots. They are adept at hiding in the darkness, sharpening their blades and continually increasing their ATK.";
        Skillset = "• When not attacking, ATK continuously increases and resets after the next attack.";
        TooltipsDescription = "An assassin who prefers close-ranged combat. When not attacking, ATK continuously increases " +
            "and resets after the next attack.";

        base.WriteStats();
    }
}