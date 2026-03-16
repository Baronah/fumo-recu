using System.Collections.Generic;
using UnityEngine;

public class FM_07 : StageManager
{
    [SerializeField] private List<GameObject> CM_ActiveSudaram, CM_InactiveSudaram;
    [SerializeField] private float PlayerBonusHP_Ratio = 0.25f;
    [SerializeField] private float PlayerBonusMSPD_Ratio = 0.3f;

    public override void EnableChallengeMode()
    {
        base.EnableChallengeMode();

        if (CharacterPrefabsStorage.EnableChallengeMode)
        {
            CM_ActiveSudaram.ForEach(g => g.SetActive(true));
            foreach (var i in CM_InactiveSudaram)
            {
                Destroy(i);
            }
        }
        else
        {
            foreach (var i in CM_ActiveSudaram)
            {
                Destroy(i);
            }
        }
    }

    public override void OnEnemySpawn(EnemyBase enemy)
    {
        base.OnEnemySpawn(enemy);
        if (enemy is Sudaram sr)
        {
            sr.originiumPollutionBonusASPD = 100f;
            sr.originiumPollutionDamageMultiplier = 0f;
        }
        else if (enemy as OriginiumSpider || enemy as OriginiumSpiderAlpha)
        {
            enemy.bAtk = (short)(enemy.bAtk * 0.85f);
        }
        else if (enemy as Sentinel)
        {
            enemy.mHealth *= 2;
            enemy.bDef += 10;
            enemy.bRes += 10;
        }
    }

    public override void OnPlayerSpawn(PlayerBase player)
    {
        base.OnPlayerSpawn(player);
        player.b_moveSpeed += player.b_moveSpeed * PlayerBonusMSPD_Ratio;
        player.mHealth += (int)(player.mHealth * PlayerBonusHP_Ratio);
    }
}