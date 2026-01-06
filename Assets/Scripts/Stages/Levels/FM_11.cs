using UnityEngine;

public class FM_11 : StageManager
{
    [SerializeField] private GameObject[] CM_ActivateGroup;
    [SerializeField] private float CM_Hiber_WakeAtkBuffAdd = 0.25f, CM_Hiber_WakeMspdBuffAdd = 0.35f, CM_Hiber_SleepCountTimeAdd = 5f;

    public override void EnableChallengeMode()
    {
        base.EnableChallengeMode();
        if (CharacterPrefabsStorage.EnableChallengeMode)
        {
            foreach (var item in CM_ActivateGroup)
            {
                item.SetActive(true);
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
        else if (CharacterPrefabsStorage.EnableChallengeMode && enemy is HibernatorKnight h)
        {
            h.SleepCountTime += CM_Hiber_SleepCountTimeAdd;
            h.Wake_AtkBuff += CM_Hiber_WakeAtkBuffAdd;
            h.Wake_MspdBuff += CM_Hiber_WakeMspdBuffAdd;
            h.Wake_Buff_Duration += 1f;
        }
    }
}