using UnityEngine;

public class FM_10 : StageManager
{
    [SerializeField] private float Enemy_SpeedMultiplier = 1.2f;
    [SerializeField] private float Gloompincer_ASPD_bonus = 30f;

    public override void OnEnemySpawn(EnemyBase enemy)
    {
        base.OnEnemySpawn(enemy);
        if (CharacterPrefabsStorage.EnableChallengeMode)
        {
            enemy.b_moveSpeed *= Enemy_SpeedMultiplier;
            if (enemy is Gloompincer g)
            {
                g.shroudedAspdBuff = Gloompincer_ASPD_bonus;
            }
        }
    }
}