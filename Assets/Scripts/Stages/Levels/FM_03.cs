using UnityEngine;

public class FM_03 : StageManager
{
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
        }
    }
}