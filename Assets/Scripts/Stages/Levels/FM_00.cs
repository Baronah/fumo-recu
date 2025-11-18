using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FM_00 : StageManager
{
    [SerializeField] private float HoundHpMultiplier = 1.5f, HoundAtkMultiplier = 2.5f;

    public override void OnEnemySpawn(EnemyBase enemy)
    {
        base.OnEnemySpawn(enemy);
        if (CharacterPrefabsStorage.EnableChallengeMode)
        {
            if (enemy as Hound)
            {
                enemy.mHealth = (int)(enemy.mHealth * HoundHpMultiplier);
                enemy.bAtk = (short)(enemy.bAtk * HoundAtkMultiplier);
            }
        }
    }
}
