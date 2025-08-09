using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BloodboilKnight : EnemyBase
{
    [SerializeField] GameObject SkillEffect;
    public float atkAddPerEnemyKilled = 0.125f;
    public float aspdAddPerEnemyKilled = 10f;
    public float mspdAddPerEnemyKilled = 0.125f;
    public short maxStackCount = 10;

    private short stackCount = 0;

    public override void InitializeComponents()
    {
        base.InitializeComponents();

        short EnemyDeaths = EntityManager.GetEnemyDefeatedCount();
        for (int i = 0; i < EnemyDeaths && stackCount < maxStackCount; i++)
        {
            OnEnemyDeath();
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        SkillEffect.SetActive(stackCount >= (maxStackCount / 2) && IsAlive());
    }

    public void OnEnemyDeath()
    {
        if (stackCount >= maxStackCount) return;
        
        stackCount++;
        atk += (short)(bAtk * atkAddPerEnemyKilled);
        ASPD += aspdAddPerEnemyKilled;
        moveSpeed += (short)(b_moveSpeed * mspdAddPerEnemyKilled);
    }

    public override void WriteStats()
    {
        Description = "";
        Skillset = ".";
        TooltipsDescription = 
            $"Gains increased ATK, ASPD and MSPD every time an enemy is defeated. Stacks up to {maxStackCount} times.";

        base.WriteStats();
    }
}
