using UnityEngine;

public class FM_04 : StageManager
{
    [SerializeField] private float SentinelExtraSpeedBuff = 0.5f;
    [SerializeField] private float SentinelExtraAtkBuff = 0.6f;
    [SerializeField] private short EnemyWeightIncrement = 1;

    public override void OnEnemySpawn(EnemyBase enemy)
    {
        base.OnEnemySpawn(enemy);

        if (enemy is Archer a)
        {
            a.ChargeMaxAtkStack();
        }

        if (CharacterPrefabsStorage.EnableChallengeMode)
        {
            enemy.weight += EnemyWeightIncrement;
            
            if (enemy is Sentinel s)
            {
                s.SpeedBuffOnAlert += SentinelExtraSpeedBuff;
                s.AtkBuffOnAlert += SentinelExtraAtkBuff;
            }
        }
    }
}