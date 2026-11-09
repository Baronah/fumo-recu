using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FM_13 : StageManager
{
    [SerializeField] Transform PlayerInitSpawnPos, PlayerCMSpawnPos;

    public override void CheckForChallengeMode()
    {
        base.CheckForChallengeMode();
        if (CharacterPrefabsStorage.EnableChallengeMode) 
            PlayerInitSpawnPos.position = PlayerCMSpawnPos.position;
    }

    public override void OnEnemySpawn(EnemyBase enemy)
    {
        base.OnEnemySpawn(enemy);

        if (enemy as Candle) enemy.b_attackRange += 60f;

        if (enemy is Candleknight ck && CharacterPrefabsStorage.EnableChallengeMode)
        {
            ck.MaxCandles += 2;
        }
    }
}
