using UnityEngine;

public class FM_08 : StageManager
{
    [SerializeField] private GameObject CM_DisableSpawns, CM_EnableSpawns;

    public override void OnEnemySpawn(EnemyBase enemy)
    {
        base.OnEnemySpawn(enemy);

        if (enemy is Sudaram s)
        {
            s.detectionRange *= 0.6f;
            s.originiumPollutionBonusASPD += 40f;
            s.originiumPollutionDamageMultiplier = 0f;
            s.mHealth *= 0.6f;
        }
        else if (enemy as OriginiumSpiderAlpha) enemy.bAtk = (short)(enemy.bAtk * 0.85f);
        else if (enemy is BloodboilKnight b)
        {
            b.bDef += 30;
            b.bRes += 20;
            
            b.maxStackCount *= 2;
            b.mspdAddPerEnemyKilled /= 2;
            b.aspdAddPerEnemyKilled /= 2;
            b.atkAddPerEnemyKilled /= 2;
        }
    }

    public override void EnableChallengeMode()
    {
        base.EnableChallengeMode();
        if (CharacterPrefabsStorage.EnableChallengeMode)
        {
            Destroy(CM_DisableSpawns);
            CM_EnableSpawns.SetActive(true);
        }
        else
        {
            Destroy(CM_EnableSpawns);
            CM_DisableSpawns.SetActive(true);
        }
    }
}