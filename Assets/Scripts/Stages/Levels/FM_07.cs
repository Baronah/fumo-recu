using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FM_07 : StageManager
{
    [SerializeField] private GameObject laterSpawn, Vents;
    private List<EndlessEnemySpawn> EndlessSpawns;

    [SerializeField] private TMP_Text timerText;
    [SerializeField] private float targetTimer = 240f;
    float stageTimer = 0;

    FumoScript fumo;

    public override void Start()
    {
        fumo = FindFirstObjectByType<FumoScript>();
        EndlessSpawns = new List<EndlessEnemySpawn>(FindObjectsOfType<EndlessEnemySpawn>(true));
        base.Start();
    }

    public override void EnableChallengeMode()
    {
        base.EnableChallengeMode();
        if (CharacterPrefabsStorage.EnableChallengeMode) Vents.SetActive(true);
        else Destroy(Vents);
    }

    public override void OnEnemySpawn(EnemyBase enemy)
    {
        base.OnEnemySpawn(enemy);

        if (enemy.attackPattern == EntityBase.AttackPattern.MELEE)
            enemy.DetectionRange += 70f;

        if (enemy is Wetwork w)
        {
            w.ChargeMaxAtkStack();
        }
        else if (enemy is Archer a)
        {
            a.ChargeMaxAtkStack();
        }
        else if (enemy as Matterllurgist)
        {
            enemy.ASPD += 40;
        }
    }

    public override void OnPlayerSpawn(PlayerBase player)
    {
        base.OnPlayerSpawn(player);
        player.mHealth *= 2;
        player.bDef += 20;
        player.bRes += 10;
        player.bAtk = (short)(player.bAtk * 1.5f);
        player.ASPD += 35;
        player.defPen += 10;
        player.resPen += 15;
    }

    float modifySpawnTimer = 0;
    public override void Update()
    {
        if (stageTimer >= targetTimer) return;
        base.Update();

        stageTimer += Time.deltaTime;

        float countTimer = targetTimer - stageTimer;
        timerText.text = $"{Mathf.FloorToInt(countTimer / 60).ToString("00")}:{Mathf.FloorToInt(countTimer % 60).ToString("00")}";

        if (fumo && stageTimer >= targetTimer)
        {
            stageTimer = targetTimer;
            OnPlayerFumoProtected(FindFirstObjectByType<FumoScript>());
        }

        modifySpawnTimer += Time.deltaTime;

        if (stageTimer >= 120 && !laterSpawn.activeSelf)
        {
            laterSpawn.SetActive(true);
        }

        if (modifySpawnTimer >= 60)
        {
            ModifySpawnRate();
        }
    }

    short modifySpawnCount = 0;
    void ModifySpawnRate()
    {
        modifySpawnTimer = 0;
        switch (modifySpawnCount)
        {
            case 0:
                foreach (var spawn in EndlessSpawns)
                {
                    spawn.enemyPrefabs.Clear();
                    spawn.enemyPrefabs.Add(EnemyBase.EnemyCode.HOUND);
                }
                break;
            case 1:
                foreach (var spawn in EndlessSpawns)
                {
                    spawn.enemyPrefabs.Clear();
                    spawn.enemyPrefabs.Add(EnemyBase.EnemyCode.HEIR);
                }
                break;
            case 2:
                foreach (var spawn in EndlessSpawns)
                {
                    spawn.enemyPrefabs.Clear();
                    spawn.enemyPrefabs.Add(EnemyBase.EnemyCode.ZEALOT);
                    spawn.enemyPrefabs.Add(EnemyBase.EnemyCode.MATTERLLURGIST);
                }
                break;
            case 3:
                foreach (var spawn in EndlessSpawns)
                {
                    spawn.enemyPrefabs.Clear();
                    spawn.enemyPrefabs.Add(EnemyBase.EnemyCode.ORIGINIUM_SPIDER);
                    spawn.enemyPrefabs.Add(EnemyBase.EnemyCode.ORIGINIUM_SPIDER_ALPHA);
                }
                break;
            case 4:
                foreach (var spawn in EndlessSpawns)
                {
                    spawn.enemyPrefabs.Clear();
                    spawn.enemyPrefabs.Add(EnemyBase.EnemyCode.ORIGINIUM_SPIDER);
                    spawn.enemyPrefabs.Add(EnemyBase.EnemyCode.ORIGINIUTANT);
                    spawn.enemyPrefabs.Add(EnemyBase.EnemyCode.ZEALOT);
                    spawn.enemyPrefabs.Add(EnemyBase.EnemyCode.MATTERLLURGIST);
                }
                break;
            case 5:
                foreach (var spawn in EndlessSpawns)
                {
                    spawn.enemyPrefabs.Clear();
                    spawn.enemyPrefabs.Add(EnemyBase.EnemyCode.BLOODBOIL_KNIGHT);
                }
                break;
            case 6:
                foreach (var spawn in EndlessSpawns)
                {
                    spawn.enemyPrefabs.Clear();
                    spawn.enemyPrefabs.Add(EnemyBase.EnemyCode.ORIGINIUM_SPIDER_ALPHA);
                }
                break;
            case 7:
                foreach (var spawn in EndlessSpawns)
                {
                    spawn.enemyPrefabs.Clear();
                    spawn.enemyPrefabs.Add(EnemyBase.EnemyCode.HEIR);
                    spawn.enemyPrefabs.Add(EnemyBase.EnemyCode.ORIGINIUM_SPIDER_ALPHA);
                    spawn.enemyPrefabs.Add(EnemyBase.EnemyCode.SUDARAM);
                }
                break;
        }
        modifySpawnCount++;
    }
}