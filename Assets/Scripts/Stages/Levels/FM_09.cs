using UnityEngine;

public class FM_09 : StageManager
{
    [SerializeField] private GameObject activateGameObject;

    bool spawnMoreEnemies = false;
    ShroudedAssassin shroudedAssassin;
    bool assassinReviveTriggered = false;

    public override void Update()
    {
        if (spawnMoreEnemies && IsStageStarted && shroudedAssassin != null && !assassinReviveTriggered && !shroudedAssassin.canRevive)
        {
            assassinReviveTriggered = true;
            activateGameObject.SetActive(true);
        }

        base.Update();
    }

    public override void EnableChallengeMode()
    {
        base.EnableChallengeMode();

        spawnMoreEnemies = CharacterPrefabsStorage.EnableChallengeMode;

        if (!CharacterPrefabsStorage.EnableChallengeMode)
        {
            Destroy(activateGameObject);
        }
    }

    public override void OnEnemySpawn(EnemyBase enemy)
    {
        base.OnEnemySpawn(enemy);

        enemy.DetectionRange = 5000f;

        if (CharacterPrefabsStorage.EnableChallengeMode && enemy is ShroudedAssassin assassin)
        {
            shroudedAssassin = assassin;
            assassin.MaxMspdBuff += 0.2f;
            assassin.MaxAtkBuff += 0.2f;
        }

        if (enemy as Sudaram)
        {
            enemy.bAtk = (short)(enemy.bAtk * 0.6f);
            enemy.mHealth = 100;
            enemy.bDef = enemy.bRes = 5;
        }
    }
}