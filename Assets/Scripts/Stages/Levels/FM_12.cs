using UnityEngine;

public class FM_12 : StageManager
{
    [SerializeField] private float Candles_RangeReductionRatio = 0.5f;

    public override void OnEnemySpawn(EnemyBase enemy)
    {
        base.OnEnemySpawn(enemy);
        if (CharacterPrefabsStorage.EnableChallengeMode && enemy as Candle)
            enemy.b_attackRange *= (1 - Candles_RangeReductionRatio);
    }
}