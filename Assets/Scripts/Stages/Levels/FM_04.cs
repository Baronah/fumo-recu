using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FM_04 : StageManager
{
    [SerializeField] private GameObject hiddenOriginiumTiles;

    public override void EnableChallengeMode()
    {
        if (!CharacterPrefabsStorage.EnableChallengeMode) return;
        base.EnableChallengeMode();
        if (hiddenOriginiumTiles != null)
        {
            hiddenOriginiumTiles.SetActive(true);
        }
    }

    public override void OnEnemySpawn(EnemyBase enemy)
    {
        base.OnEnemySpawn(enemy);

        if (enemy is OriginiumSpiderAlpha alp) alp.mHealth = 60;
        if (CharacterPrefabsStorage.EnableChallengeMode && enemy is Sudaram sr) sr.originiumPollutionDamageMultiplier = 0f;
    }
}
