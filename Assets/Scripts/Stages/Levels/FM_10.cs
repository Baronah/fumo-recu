using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FM_10 : StageManager
{
    [SerializeField] private GameObject activateGameObject;

    bool spawnMoreEnemies = false;
    ShroudedAssassin shroudedAssassin;
    bool assassinReviveTriggered = false;

    public override void Update()
    {
        if (IsStageStarted && shroudedAssassin != null 
            && 
            !assassinReviveTriggered 
            && (!shroudedAssassin.IsAlive() || shroudedAssassin.WarppedShroudedTriggered)
            )
        {
            OnAssassinSkillTrigger();
            assassinReviveTriggered = true;
        }

        base.Update();
    }

    [SerializeField] Tilemap bg1, bg2;
    void OnAssassinSkillTrigger()
    {
        if (spawnMoreEnemies) activateGameObject.SetActive(true);
        StartCoroutine(TilemapDarkens());
    }

    [SerializeField] Color darkenColor;
    IEnumerator TilemapDarkens()
    {
        float c = 0, d = 5;
        while (c < d)
        {
            bg1.color = Color.Lerp(bg1.color, darkenColor, c * 1.0f / d);
            bg2.color = Color.Lerp(bg2.color, darkenColor, c * 1.0f / d);
            c += Time.deltaTime;
            yield return null;
        }

        bg1.color = bg2.color = darkenColor;
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

        enemy.detectionRange = 5000f;

        if (enemy is ShroudedAssassin a)
        {
            shroudedAssassin = a;
        }

        if (enemy as Sudaram)
        {
            enemy.bAtk = (short)(enemy.bAtk * 0.6f);
            enemy.mHealth = 200;
            enemy.bDef /= 2;
            enemy.bRes /= 2;
        }
    }
}