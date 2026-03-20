using UnityEngine;

public class FM_03 : StageManager
{
    [SerializeField] private GameObject DeactivateSentinel, ActivateSentinel;
    [SerializeField] private float SentinelBonusMSPD_Ratio = 1.5f;

    public override void EnableChallengeMode()
    {
        base.EnableChallengeMode();

        if (CharacterPrefabsStorage.EnableChallengeMode)
        {
            ActivateSentinel.SetActive(true);
            Destroy(DeactivateSentinel);
        }
        else
        {
            Destroy(ActivateSentinel);
        }
    }

    public override void OnEnemySpawn(EnemyBase enemy)
    {
        if (CharacterPrefabsStorage.EnableChallengeMode)
        {
            if (enemy as Sentinel) enemy.b_moveSpeed *= SentinelBonusMSPD_Ratio;
        }

        if (enemy as BloodboilKnight)
        {
            enemy.mHealth = 350;
            enemy.def -= 10;
            enemy.res -= 10;
        }

        base.OnEnemySpawn(enemy);
    }
}