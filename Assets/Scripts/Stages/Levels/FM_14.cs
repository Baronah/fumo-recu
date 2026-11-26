using UnityEngine;

public class FM_14 : StageManager
{
    [SerializeField] private short CandleHoldMax = 1;
    [SerializeField] private float Toys_HPBonus = 0.35f;
    [SerializeField] private short Toys_DefBonus = 60, Toys_ResBonus = 40;

    public override void OnEnemySpawn(EnemyBase enemy)
    {
        base.OnEnemySpawn(enemy);
        if (CharacterPrefabsStorage.EnableChallengeMode)
        {
            if (enemy is Candleknight ck)
                ck.MaxCandles = CandleHoldMax;
            else if (enemy as Toy)
            {
                enemy.mHealth += (enemy.mHealth * Toys_HPBonus);
                enemy.bDef += Toys_DefBonus;
                enemy.bRes += Toys_ResBonus;
            }
        }
    }
}