using UnityEngine;

public class FM_11 : StageManager
{
    [SerializeField] private GameObject[] CM_ActivateGroup;
    [SerializeField] private GameObject CM_Hibernator_Spawns_ToModify;
    [SerializeField] private float CM_Hibernator_Spawns_Delay = 15f;

    public override void EnableChallengeMode()
    {
        base.EnableChallengeMode();
        if (CharacterPrefabsStorage.EnableChallengeMode)
        {
            foreach (var item in CM_ActivateGroup)
            {
                item.SetActive(true);
            }

            EnemySpawnpointScript[] enemySpawnpointScripts = CM_Hibernator_Spawns_ToModify.GetComponentsInChildren<EnemySpawnpointScript>();
            foreach (var item in enemySpawnpointScripts)
            {
                item.InitDelay += CM_Hibernator_Spawns_Delay;
            }
        }
        else
        {
            for (int i = 0; i < CM_ActivateGroup.Length; i++)
            {
                Destroy(CM_ActivateGroup[i]);
            }
        }
    }

    public override void OnEnemySpawn(EnemyBase enemy)
    {
        base.OnEnemySpawn(enemy);
        if (enemy is Sudaram s)
        {
            s.originiumPollutionBonusASPD = 100;
        }
    }
}