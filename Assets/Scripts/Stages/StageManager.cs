using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static EnemyBase;

public class StageManager : MonoBehaviour
{
    private bool PressedAnyKey = false;
    private bool IsStageStarted = false;
    private bool IsStagePaused = false;
    public bool ReadIsStagePaused => IsStagePaused;

    [SerializeField] private GameObject pauseOverlay, titleOverlay;
    [SerializeField] private EnemyCode[] appearingEnemies;
    [SerializeField] private float extraEnemyWaittime = 0f, extraPlayerWaittime = 1.5f;
    [SerializeField] private TMP_Text LoadingState;
    [SerializeField] private CharacterPrefabsStorage prefabStorage;

    [SerializeField] private CameraMovement mainCamera;
    [SerializeField] private float ShowcaseSize;
    [SerializeField] private Transform[] CameraShowcases;
    [SerializeField] private float[] Waittimes;

    [SerializeField] private Image PauseButton, SlowImg;
    [SerializeField] private Sprite PausedSprite, UnpausedSprite;

    private string LevelName;
    protected AudioSource BGM;
    private EnemySpawnpointScript[] enemySpawnpoints;

    [SerializeField] protected float ChallengeModeStatsModifier = 1.1f;

    [SerializeField] private float timeScaleSlow = 0.4f;
    [SerializeField] private KeyCode SlowKeyCode = KeyCode.Q;
    private bool isSlowing = false;

    private bool IsEnemyAlive => EntityManager.Enemies.Any(e => e && e.IsAlive()) || enemySpawnpoints.Any(e => !e.IsSpawnpointSpawned);

    public enum StageCompleteCondition
    {
        ELIMINATE_ALL_ENEMIES,
        RETRIEVE_FUMO,
    };

    public StageCompleteCondition StageCompleteConditionType = StageCompleteCondition.ELIMINATE_ALL_ENEMIES;

    public enum EnvironmentType
    {
        ORIGINIUM_TILES,
    };

    public EnvironmentType[] StageEvironmentTypes;

    private PlayerManager playerManager;

    bool IsStageReady = false, IsStageEnd = false, StageClearedNMFirsttime = false;

    public virtual void Start()
    {
        LevelName = SceneManager.GetActiveScene().name;

        StartCoroutine(OnStartOverlayFadeout());

        BGM = GetComponent<AudioSource>();

        if (CharacterPrefabsStorage.EnableChallengeMode) EnableChallengeMode();

        enemySpawnpoints = FindObjectsOfType<EnemySpawnpointScript>();
        playerManager = GetComponent<PlayerManager>();
        LoadingState.text = "Loading stage, please wait...";

        StartCoroutine(LoadRequiredPrefabs());
        EnemyTooltipsScript.isAnyTooltipsShowing = false;
        Time.timeScale = 0f;
    }

    IEnumerator OnStartOverlayFadeout()
    {
        GameObject Overlay = GameObject.Find("StageOverlayTransition");
        if (!Overlay) yield break;

        Image image = Overlay.GetComponentInChildren<Image>();
        float fadeOutTime = 1f, c = 0, cJump = 0.02f;
        while (c < fadeOutTime)
        {
            image.color = Color.Lerp(Color.black, Color.clear, c * 1.0f / fadeOutTime);
            c += cJump;
            yield return new WaitForSecondsRealtime(cJump);
        }

        image.color = Color.clear;
        Destroy(Overlay);
    }

    public virtual void EnableChallengeMode()
    {

    }

    public virtual void OnEnemySpawn(EnemyBase enemy)
    {
        if (CharacterPrefabsStorage.EnableChallengeMode)
        {
            enemy.bAtk = (short)(enemy.bAtk * ChallengeModeStatsModifier);
            enemy.bDef = (short)(enemy.bDef * ChallengeModeStatsModifier);
            enemy.mHealth = (int)(enemy.mHealth * ChallengeModeStatsModifier);
        }
    }

    private IEnumerator LoadRequiredPrefabs()
    {
        // Load all player prefabs
        CharacterPrefabsStorage.PlayerPrefabs = new();
        int i = 0;
        foreach (var reference in prefabStorage.PlayerAssetReferences)
        {
            var handle = DataHandler.Instance.LoadAddressable<GameObject>(reference);
            yield return handle;
            CharacterPrefabsStorage.PlayerPrefabs[i] = handle.Result;
            i++;
        }

        // Load only required enemy prefabs
        CharacterPrefabsStorage.EnemyPrefabs = new();
        HashSet<int> uniqueIndices = new(); // prevent duplicate loads

        foreach (var code in appearingEnemies)
        {
            if (uniqueIndices.Add((int) code)) // only process unique ones
            {
                var reference = prefabStorage.EnemyAssetReferences[(int)code];
                var handle = DataHandler.Instance.LoadAddressable<GameObject>(reference);
                yield return handle;
                CharacterPrefabsStorage.EnemyPrefabs[(int)code] = handle.Result;
            }
        }

        IsStageReady = true;
        LoadingState.text = "<color=green>---Press any key to start---</color>";
    }

    IEnumerator TitleFadeOut()
    {
        BGM.Play();
        CanvasGroup canvasGroup = titleOverlay.GetComponent<CanvasGroup>();

        float c = 0, d = 1.25f, cJump = 0.02f;
        while (c < d)
        {
            canvasGroup.alpha = Mathf.Lerp(1, 0, c * 1.0f / d);
            c += cJump;
            yield return new WaitForSecondsRealtime(cJump);
        }

        canvasGroup.alpha = 0;
        Destroy(titleOverlay);

        Time.timeScale = 1f;
        yield return StartCoroutine(mainCamera.MoveShowcases(ShowcaseSize, CameraShowcases, Waittimes));

        foreach (var item in enemySpawnpoints)
        {
            item.OnStageStart(extraEnemyWaittime);
        }

        yield return new WaitForSeconds(extraPlayerWaittime);
        playerManager.enabled = true;
        yield return null;

        OnStageReady();
        IsStageStarted = true;
        StartCoroutine(CheckStageStatus());
    }

    public virtual void Update()
    {
        if (!IsStageReady) return;

        Time.timeScale = 
            IsStagePaused || playerManager.IsReadingSkillView 
            ? 0f 
            : isSlowing 
                ? timeScaleSlow : 1f;

        OnStageUpdate();

        if (!PressedAnyKey && !IsStageStarted && Input.anyKeyDown)
        {
            PressedAnyKey = true;
            StartCoroutine(TitleFadeOut());
        }

        if (!IsStageStarted) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseStage();
        }
        else if (Input.GetKeyDown(SlowKeyCode))
        {
            isSlowing = !isSlowing;
            SlowImg.color = isSlowing ? Color.white : new(0.15f, 0.15f, 0.15f);
        }
    }

    protected virtual void OnStageReady() { }

    protected virtual void OnStageUpdate() { }

    protected virtual IEnumerator CheckStageStatus()
    {
        while (IsStageStarted)
        {
            yield return new WaitForSeconds(2f);

            if (!playerManager.IsPlayerAlive)
            {
                IsStageStarted = false;
                OnStageEnd(false);
                yield break; // Exit the coroutine if player is dead
            }

            if (StageCompleteConditionType == StageCompleteCondition.ELIMINATE_ALL_ENEMIES 
                && !IsEnemyAlive)
            {
                IsStageStarted = false;
                OnStageEnd(playerManager.IsPlayerAlive);
            }
        }
    }

    public virtual void OnStageEnd(bool resultIsWin)
    {
        IsStageEnd = true;

        if (resultIsWin)
        {
            foreach (var item in enemySpawnpoints)
            {
                Destroy(item);
            }

            List<string> CompletedLevels = PlayerPrefs.GetString("CompletedLevels", string.Empty).Split(" ").ToList();
            if (CharacterPrefabsStorage.EnableChallengeMode)
            {
                var CMLVL = LevelName + "_CM";
                if (!CompletedLevels.Contains(CMLVL) && CompletedLevels.Contains(LevelName))
                {
                    CompletedLevels.Remove(LevelName);
                }

                CompletedLevels.Add(CMLVL);
            }
            else
            {
                if (!CompletedLevels.Contains(LevelName) && !CompletedLevels.Contains(LevelName + "_CM"))
                {
                    CompletedLevels.Add(LevelName);
                    StageClearedNMFirsttime = true;
                }
            }

            PlayerPrefs.SetString("CompletedLevels", string.Join(' ', CompletedLevels.ToArray()));
        }

        StartCoroutine(FadeIn(resultIsWin));
        if (resultIsWin) CharacterPrefabsStorage.EnableChallengeMode = false;
    }

    IEnumerator FadeIn(bool resultIsWin)
    {
        TMP_Text text = pauseOverlay.GetComponentInChildren<TMP_Text>();
        text.text = resultIsWin ? "<color=green>Stage Completed</color>" : "<color=red>Defeated</color>";
        if (StageClearedNMFirsttime && resultIsWin) text.text += "\n<size=39>Challenge Mode has been unlocked!</size>";
        CanvasGroup canvasGroup = pauseOverlay.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        pauseOverlay.SetActive(true);

        float c = 0, d = 1.25f, cJump = 0.02f;
        while (c < d)
        {
            canvasGroup.alpha = Mathf.Lerp(0, 1, c * 1.0f / d);
            c += cJump;
            yield return new WaitForSecondsRealtime(cJump);
        }
    }

    public void TogglePauseStage()
    {
        if (IsStageEnd) return;

        IsStagePaused = !IsStagePaused;
        pauseOverlay.SetActive(IsStagePaused);

        PauseButton.sprite = IsStagePaused ? PausedSprite : UnpausedSprite;
    }

    public virtual void RetryStage()
    {
        Time.timeScale = 1f;
        EnemySpawnpointScript.OnStageRetry();
        string currentSceneName = SceneManager.GetActiveScene().name;
        Addressables.LoadSceneAsync(currentSceneName, LoadSceneMode.Single, true);
    }

    public virtual void QuitStage()
    {
        EnemySpawnpointScript.OnStageRetry();
        CharacterPrefabsStorage.EnemyPrefabs.Clear();
        CharacterPrefabsStorage.PlayerPrefabs.Clear();
        CharacterPrefabsStorage.EnableChallengeMode = false;

        Time.timeScale = 1f;

        SceneManager.LoadScene("Level_Selection");
    }

    public void OnPlayerFumoPickup(PlayerBase player)
    {
        if (StageCompleteConditionType != StageCompleteCondition.RETRIEVE_FUMO) return;

        player.isInvulnerable = true;
        EntityManager.Enemies.ForEach(e => { if (e) e.InstaKill(); });
        IsStageStarted = false;
        OnStageEnd(true);
    }

    public static void SpecialStageAddsOn(EntityBase entity)
    {

    }
}