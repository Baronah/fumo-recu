using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static EnemyBase;

public class EnemySpawnpointScript : MonoBehaviour
{
    public enum ActionType
    {
        NONE,
        DESTROY,
        ACTIVATE,
        DEACTIVATE,
    };

    [SerializeField] private float InitDelay = 0f;

    [SerializeField] private bool spotPlayerUponSpawn = false, immediateSpawn = false, showTooltips;
    [SerializeField] private short InitTooltipsPriority = 0;
    [SerializeField] public List<EnemyCheckpointScript> enemyCheckpoints;
    [SerializeField] private float InitWaittime;
    [SerializeField] private EnemyCode enemyPrefab;
    [SerializeField] private bool doSpawnEnemy = true;
    [SerializeField] private short Quantity = 1;
    [SerializeField] private float OffsetRadius = 5f;

    [SerializeField] private bool RepeatedSpawn = false;
    [ShowIf("RepeatedSpawn", true)]
    [SerializeField] private float WaittimeBeforeNextSpawn = 5f;

    [SerializeField] private GameObject[] TargetObjectsToInteract;

    [SerializeField] private ActionType OnEnemySpawn_Action = ActionType.NONE;

    [ShowIf("RepeatedSpawn", false)]
    [SerializeField] private ActionType OnEnemyDeath_Action = ActionType.NONE;

    private List<EnemyBase> SpawnEnemies = new();
    private float extraWaittime = 0;

    private static int TooltipsPriority = 0;
    public static void OnStageRetry() => TooltipsPriority = 0;

    private Transform[] SpawnPositions;
    
    private StageManager stageManager;

    public bool UsedByChallengeMode = false;

    private bool Spawned = false;
    public bool IsSpawnpointSpawned => Spawned;

    private void Awake()
    {
        stageManager = FindObjectOfType<StageManager>(true);
        SpawnPositions = transform.Find("Spawnposition").GetComponentsInChildren<Transform>();
    }

    private void Start()
    {
        if (immediateSpawn)
            StartCoroutine(SpawnEnemy());
    }

    public void OnStageStart(float extraWaittime = 0)
    {
        if (!this) return;
        this.extraWaittime += extraWaittime;
        enabled = true;
    }

    public IEnumerator SpawnEnemy()
    {
        if (immediateSpawn)
        {
            yield return new WaitForSeconds(InitDelay);
        }

        if (Spawned) yield break;

        if (doSpawnEnemy)
        {
            yield return StartCoroutine(CreateEnemySpawn());
            if (RepeatedSpawn)
            {
                StartCoroutine(DoRepeatedSpawn());
            }
        }

        foreach (var obj in TargetObjectsToInteract)
        {
            switch (OnEnemySpawn_Action)
            {
                case ActionType.NONE:
                    break;
                case ActionType.DESTROY:
                    EntityBase en = obj.GetComponent<EntityBase>();
                    if (en)
                    {
                        en.InstaKill();
                    }
                    else if (obj == this) Destroy(obj, 0.5f);
                    else Destroy(obj);
                    break;
                case ActionType.ACTIVATE:
                    obj.SetActive(true);
                    break;
                case ActionType.DEACTIVATE:
                    obj.SetActive(false);
                    break;
            }
        }
    }

    IEnumerator CreateEnemySpawn()
    {
        short maxSpawnPositions = (short)SpawnPositions.Length;

        for (int i = 0; i < maxSpawnPositions; i++)
        {
            for (int j = 0; j < Quantity; j++)
            {
                Transform spawnTransform = SpawnPositions[Mathf.Min(i, maxSpawnPositions - 1)];

                GameObject o = Instantiate(
                    CharacterPrefabsStorage.EnemyPrefabs[(int)enemyPrefab],
                    spawnTransform.position + new Vector3(Random.Range(-OffsetRadius, OffsetRadius), Random.Range(-OffsetRadius, OffsetRadius)),
                    Quaternion.identity);

                EnemyBase enemy = o.GetComponent<EnemyBase>();

                stageManager.OnEnemySpawn(enemy);

                enemyCheckpoints.Insert(0, new EnemyCheckpointScript { Checkpoint = spawnTransform, WaitTime = InitWaittime });
                enemy.SetCheckpoints(InitWaittime, enemyCheckpoints, showTooltips, TooltipsPriority + InitTooltipsPriority);
                if (showTooltips) TooltipsPriority++;
                enemy.enabled = true;
                Spawned = true;

                showTooltips = false;
                yield return null;

                if (spotPlayerUponSpawn)
                {
                    enemy.ForceSpotPlayer();
                }

                if (extraWaittime > 0) StartCoroutine(enemy.StartMovementLockout(extraWaittime));

                SpawnEnemies.Add(enemy);

                yield return null;
            }
        }
    }

    IEnumerator DoRepeatedSpawn()
    {
        if (!RepeatedSpawn) yield break;

        while (true)
        {
            yield return new WaitForSeconds(WaittimeBeforeNextSpawn);
            yield return StartCoroutine(CreateEnemySpawn());
        }
    }

    private void FixedUpdate()
    {
        OnSpawnedEnemyDeath();
    }

    private void OnSpawnedEnemyDeath()
    {
        if (!doSpawnEnemy || RepeatedSpawn) return;
        if (SpawnEnemies.Count <= 0 || SpawnEnemies.Any(e => e.IsAlive())) return;

        SpawnEnemies.Clear();
        foreach (var obj in TargetObjectsToInteract)
        {
            switch (OnEnemyDeath_Action)
            {
                case ActionType.NONE:
                    break;
                case ActionType.DESTROY:
                    EntityBase en = obj.GetComponent<EntityBase>();
                    if (en)
                    {
                        en.InstaKill();
                    }
                    else if (obj == this) Destroy(obj, 0.5f);
                    else Destroy(obj);
                    break;
                case ActionType.ACTIVATE:
                    obj.SetActive(true);
                    break;
                case ActionType.DEACTIVATE:
                    obj.SetActive(false);
                    break;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (immediateSpawn || Spawned) return;

        if (other.CompareTag("Player"))
        {
            StartCoroutine(SpawnEnemy());
        }
    }

    public int GetEnemiesCount()
    {
        if (!this.gameObject) return 0;

        if (!stageManager) stageManager = FindObjectOfType<StageManager>(true);
        SpawnPositions ??= transform.Find("Spawnposition").GetComponentsInChildren<Transform>();

        if (!doSpawnEnemy || RepeatedSpawn || SpawnPositions == null) return 0;
        if (Spawned) return SpawnEnemies.Count(e => e.IsAlive());
        return SpawnPositions.Length * Quantity;
    }
}

