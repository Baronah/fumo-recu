using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FM_04 : StageManager
{
    [SerializeField] private GameObject hiddenOriginiumTiles;

    public override void EnableChallengeMode()
    {
        base.EnableChallengeMode();
        if (hiddenOriginiumTiles != null)
        {
            hiddenOriginiumTiles.SetActive(true);
        }
    }

    public override void OnEnemySpawn(EnemyBase enemy)
    {
        base.OnEnemySpawn(enemy);

        if (CharacterPrefabsStorage.EnableChallengeMode && enemy is Sudaram sr) sr.originiumPollutionDamageMultiplier = 0f;
    }
}
