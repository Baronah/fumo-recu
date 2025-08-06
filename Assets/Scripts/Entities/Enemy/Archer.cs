using System.Collections;
using UnityEngine;

public class Archer : EnemyBase
{
    [SerializeField] private float AtkPercentageUp_perSecond = 0.05f;
    [SerializeField] private float AtkPercentageUp_cap = 2.0f;
    [SerializeField] private GameObject PS;

    private float AtkPercentageUp_count = 0;

    public override void InitializeComponents()
    {
        base.InitializeComponents();
        StartCoroutine(IncreasesAtk());
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
        short AtkAdd = (short)(bAtk * AtkPercentageUp_count);
        atk += AtkAdd;

        yield return StartCoroutine(base.Attack());

        AtkPercentageUp_count = 0f;
        atk -= AtkAdd;
    }

    public override void WriteStats()
    {
        Description = "";
        Skillset = ".";
        TooltipsDescription = "Seasoned archer that can perform long-range shots. When not attacking, ATK continuously increases " +
            "and reset after the next attack.";

        base.WriteStats();
    }
}