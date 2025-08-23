using System.Collections;
using UnityEngine;

public class Archer : EnemyBase
{
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
        if (sfxs[0]) sfxs[0].Play();
        yield return StartCoroutine(base.OnAttackComplete());
        AtkPercentageUp_count = 0f;
        atk -= AtkAdd;
    }

    public override void OnDeath()
    {
        base.OnDeath();
        PS.SetActive(false);
    }

    public override void WriteStats()
    {
        Description = "A crossbowman that is adept at hiding in the darkness and assassinating their targets with special bolts.";
        Skillset = "• When not attacking, ATK continuously increases and resets after the next attack.";
        TooltipsDescription = "Seasoned archer that can perform long-range shots. When not attacking, ATK continuously increases " +
            "and resets after the next attack.";

        base.WriteStats();
    }
}