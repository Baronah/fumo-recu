using UnityEngine;

public class FM_12 : StageManager
{
    [SerializeField] private float Candles_RangeReductionRatio = 0.5f;
    [SerializeField] private float Toys_HPBonus = 0.35f;
    [SerializeField] private short Toys_DefBonus = 60, Toys_ResBonus = 40;

    public override void OnEnemySpawn(EnemyBase enemy)
    {
        base.OnEnemySpawn(enemy);
        if (CharacterPrefabsStorage.EnableChallengeMode)
        {
            if (enemy as Candle) enemy.b_attackRange *= (1 - Candles_RangeReductionRatio);
            else if (enemy as Toy)
            {
                enemy.mHealth += (int)(enemy.mHealth * Toys_HPBonus);
                enemy.bDef += Toys_DefBonus;
                enemy.bRes += Toys_ResBonus;
            }
        }
    }
}