using UnityEngine;

public class FM_01 : StageManager
{
    [SerializeField] private float MSPD_Multiplier = 1.25f;

    public override void OnEnemySpawn(EnemyBase enemy)
    {
        base.OnEnemySpawn(enemy);

        if (CharacterPrefabsStorage.EnableChallengeMode)
        {
            enemy.b_moveSpeed *= MSPD_Multiplier;

            if (enemy is BloodboilKnight bk)
            {
                bk.atkAddPerEnemyKilled *= 2;
                bk.mspdAddPerEnemyKilled *= 2;
                bk.aspdAddPerEnemyKilled *= 2;
                bk.maxStackCount /= 2;
            }
        }
    }
}