using UnityEngine;

public class FM_05 : StageManager
{
    [SerializeField] private float PlayerBonusMSPD_Ratio = 1.3f;
    [SerializeField] private float CM_SentinelBonusHP_Ratio = 2f;

    public override void OnEnemySpawn(EnemyBase enemy)
    {
        base.OnEnemySpawn(enemy);
        if (enemy is Sudaram sr)
        {
            sr.originiumPollutionBonusASPD = 100f;
            sr.originiumPollutionDamageMultiplier = 0f;
        }

        if (CharacterPrefabsStorage.EnableChallengeMode && enemy as Sentinel)
            enemy.mHealth += (int) (enemy.mHealth * CM_SentinelBonusHP_Ratio);
    }

    public override void OnPlayerSpawn(PlayerBase player)
    {
        base.OnPlayerSpawn(player);
        player.b_moveSpeed *= PlayerBonusMSPD_Ratio;
    }
}