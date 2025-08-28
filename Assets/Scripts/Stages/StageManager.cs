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

    private static bool IsFirstTimeStageEnter = true;

    [SerializeField] private GameObject pauseOverlay, titleOverlay, mapOverview, mapCamera;
    [SerializeField] private EnemyCode[] appearingEnemies;
    [SerializeField] private TMP_Text RemainingEnemiesTxt;
    private GameObject RemainingEnemiesGO => RemainingEnemiesTxt.transform.parent.gameObject;

    [SerializeField] private float extraEnemyWaittime = 0f, extraPlayerWaittime = 1.5f;
    [SerializeField] private TMP_Text Title, LoadingState;
    [SerializeField] private CharacterPrefabsStorage prefabStorage;

    private CameraMovement mainCamera;
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
    [SerializeField] private KeyCode SlowKeyCode = KeyCode.Q, ViewMapKeyCode = KeyCode.M;
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
        KEYS,
        ORIGINIUM_TILES,
    };

    public EnvironmentType[] StageEvironmentTypes;

    private PlayerManager playerManager;

    bool IsStageReady = false, IsStageEnd = false, IsStageEndOverlayActive = false, StageClearedNMFirsttime = false;

    public virtual void Start()
    {
        LevelName = SceneManager.GetActiveScene().name;

        StartCoroutine(OnStartOverlayFadeout());

        mainCamera = GetComponentInChildren<CameraMovement>(true);
        BGM = GetComponent<AudioSource>();
        BGM.volume = PlayerPrefs.GetFloat("BGM", 1f);

        var sfxs = FindObjectsOfType<AudioSource>(true).Where(a => a != BGM);
        float sfxValue = PlayerPrefs.GetFloat("SFX", 1f);
        foreach (var item in sfxs)
        {
            item.volume = sfxValue;
        }

        RemainingEnemiesGO.SetActive(StageCompleteConditionType == StageCompleteCondition.ELIMINATE_ALL_ENEMIES);

        EnableChallengeMode();

        enemySpawnpoints = FindObjectsOfType<EnemySpawnpointScript>(true).ToArray();
        playerManager = GetComponent<PlayerManager>();
        LoadingState.text = "Loading stage, please wait...";

        StartCoroutine(LoadRequiredPrefabs());
        EnemyTooltipsScript.isAnyTooltipsShowing = false;
        Time.timeScale = 0f;
    }

    private int GetEnemyCount()
    {
        int count = 0;
        foreach (var enemy in enemySpawnpoints)
        {
            if (!enemy) continue;
            count += enemy.GetEnemiesCount();
        }

        return count;
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
        if (CharacterPrefabsStorage.EnableChallengeMode) Title.text += "\n<color=red><size=52>[CHALLENGE MODE]</size></color>";
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

    public virtual void OnPlayerSpawn(PlayerBase player)
    {

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
        if (IsFirstTimeStageEnter)
        {
            yield return StartCoroutine(mainCamera.MoveShowcases(ShowcaseSize, CameraShowcases, Waittimes));
            IsFirstTimeStageEnter = false;
        }
        else if (extraPlayerWaittime > 0) extraPlayerWaittime = 0.5f;

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
            IsStagePaused || mainCamera.TriggerStopHit || playerManager.IsReadingSkillView || mapOverview.activeSelf || IsStageEndOverlayActive
            ? 0f 
            : isSlowing 
                ? timeScaleSlow : 1f;

        if (!PressedAnyKey && !IsStageStarted && Input.anyKeyDown)
        {
            PressedAnyKey = true;
            StartCoroutine(TitleFadeOut());
        }

        if (!IsStageStarted) return;

        OnStageUpdate();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseStage();
        }
        else if (Input.GetKeyDown(SlowKeyCode))
        {
            isSlowing = !isSlowing;
            SlowImg.color = isSlowing ? Color.white : new(0.15f, 0.15f, 0.15f);
        }
        else if (Input.GetKeyDown(ViewMapKeyCode))
        {
            ViewMap();
        }
    }

    private void ViewMap()
    {
        if (mapOverview.activeSelf)
        {
            mapOverview.SetActive(false);
            mapCamera.SetActive(false);
        }
        else
        {
            mapOverview.SetActive(true);
            mapCamera.SetActive(true);
        }
    }

    protected virtual void OnStageReady() { }

    private short SearchCnt = 0;
    protected virtual void OnStageUpdate() 
    { 
        SearchCnt++;
        if (SearchCnt >= 5 && RemainingEnemiesGO.activeSelf)
        {
            int remainingEnemies = GetEnemyCount();
            RemainingEnemiesTxt.text = $"Enemies: {remainingEnemies}";
            SearchCnt = 0;
        }
    }

    protected virtual IEnumerator CheckStageStatus()
    {
        while (IsStageStarted)
        {
            yield return new WaitForSeconds(2f);

            if (!playerManager.IsPlayerAlive)
            {
                IsStageStarted = false;
                OnStageEnd(false);
                FadeInResult(false);
                yield break; // Exit the coroutine if player is dead
            }

            if (StageCompleteConditionType == StageCompleteCondition.ELIMINATE_ALL_ENEMIES 
                && GetEnemyCount() <= 0)
            {
                IsStageStarted = false;
                OnStageEnd(playerManager.IsPlayerAlive);
                FadeInResult(playerManager.IsPlayerAlive);
            }
        }
    }

    public virtual void OnStageEnd(bool resultIsWin)
    {
        IsStageEnd = true;
        FindObjectsOfType<EnemySpawnpointScript>().ToList().ForEach(e => e.enabled = false);

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

        if (resultIsWin) CharacterPrefabsStorage.EnableChallengeMode = false;
    }

    void FadeInResult(bool resultIsWin)
    {
        StartCoroutine(FadeIn(resultIsWin));
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

        canvasGroup.alpha = 1;
        IsStageEndOverlayActive = true;
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

        IsFirstTimeStageEnter = true;
        Time.timeScale = 1f;

        SceneManager.LoadScene("Level_Selection");
    }

    public void OnPlayerFumoPickup(PlayerBase player, Collider2D FumoObj)
    {
        if (StageCompleteConditionType != StageCompleteCondition.RETRIEVE_FUMO) return;

        playerManager.enabled = FumoObj.enabled = false;
        player.isInvulnerable = true;
        EntityManager.Enemies.ForEach(e => { if (e) e.InstaKill(); });
        IsStageStarted = false; 
        OnStageEnd(true);
        StartCoroutine(ZoomInFumo(FumoObj.gameObject));
    }

    IEnumerator ZoomInFumo(GameObject fumo)
    {
        SpriteRenderer sr = fumo.GetComponent<SpriteRenderer>();
        sr.sortingLayerID = SortingLayer.NameToID("UI");
        sr.sortingOrder = 100;

        mainCamera.enabled = false;
        Vector3 originalPosition = mainCamera.transform.position, 
            fumoInitScale = fumo.transform.localScale,
            fumoTargetScale = fumo.transform.localScale * 8.5f;
        float c = 0, d = 2f, cJump = 0.02f;
        while (c < d)
        {
            fumo.transform.position = Vector3.Lerp(fumo.transform.position, originalPosition, c * 1.0f / d);
            fumo.transform.localScale = Vector3.Lerp(fumoInitScale, fumoTargetScale, c * 1.0f / d);
            c += cJump;
            yield return new WaitForSecondsRealtime(cJump);
        }

        fumo.transform.position = originalPosition;
        fumo.transform.localScale = fumoTargetScale;

        yield return new WaitForSecondsRealtime(0.5f);
        FadeInResult(true);
    }

    public static void SpecialStageAddsOn(EntityBase entity)
    {

    }
}