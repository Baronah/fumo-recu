using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FM_00 : StageManager
{
    [SerializeField] private GameObject[] CM_Removes;
    [SerializeField] private float HoundHpMultiplier = 1.5f, HoundAtkMultiplier = 2.5f;

    public override void EnableChallengeMode()
    {
        base.EnableChallengeMode();
        if (CharacterPrefabsStorage.EnableChallengeMode)
        {
            foreach (var i in CM_Removes)
            {
                Destroy(i);
            }
        }
    }

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
