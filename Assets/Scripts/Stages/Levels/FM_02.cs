using UnityEngine;

public class FM_02 : StageManager
{
    [SerializeField] private GameObject DeactivateSentinel, ActivateSentinel;
    [SerializeField] private float SentinelBonusMSPD_Ratio = 1.5f;

    public override void EnableChallengeMode()
    {
        base.EnableChallengeMode();
        ActivateSentinel.SetActive(true);
        DeactivateSentinel.SetActive(false);
    }

    public override void OnEnemySpawn(EnemyBase enemy)
    {
        base.OnEnemySpawn(enemy);

        if (CharacterPrefabsStorage.EnableChallengeMode)
        {
            if (enemy as Sentinel) enemy.b_moveSpeed *= SentinelBonusMSPD_Ratio;
        }
    }
}