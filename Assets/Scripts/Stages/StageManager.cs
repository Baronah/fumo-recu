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
using static SkillTree_Manager;
using Image = UnityEngine.UI.Image;

public class StageManager : MonoBehaviour
{
    public int LevelIndex = 0;
    protected bool PressedAnyKey = false;
    protected bool IsStageStarted = false;
    protected bool IsStagePaused = false;
    public bool ReadIsStagePaused => IsStagePaused;

    protected static bool IsFirstTimeStageEnter = true;

    [SerializeField] private GameObject pauseOverlay, titleOverlay, mapOverview, mapCamera;
    [SerializeField] private EnemyCode[] appearingEnemies;
    [SerializeField] private TMP_Text RemainingEnemiesTxt;
    protected GameObject RemainingEnemiesGO => RemainingEnemiesTxt.transform.parent.gameObject;

    [SerializeField] private float extraEnemyWaittime = 0f, extraPlayerWaittime = 1.5f;
    [SerializeField] private TMP_Text Title, LoadingState;
    [SerializeField] private CharacterPrefabsStorage prefabStorage;

    protected CameraMovement mainCamera;
    [SerializeField] private float ShowcaseSize;
    [SerializeField] private Transform[] CameraShowcases;
    [SerializeField] private float[] Waittimes;

    [SerializeField] private Image PauseButton, SlowImg;
    [SerializeField] private Button o_QuitBtn, o_RetryBtn;
    [SerializeField] private Sprite PausedSprite, UnpausedSprite;

    protected string LevelName;
    protected AudioSource BGM;
    protected EnemySpawnpointScript[] enemySpawnpoints;

    [SerializeField] protected float ChallengeModeStatsModifier = 1.2f;

    [SerializeField] protected float timeScaleSlow = 0.4f;
    protected bool isSlowing = false;

    protected bool IsEnemyAlive => EntityManager.Enemies.Any(e => e && e.IsAlive()) || enemySpawnpoints.Any(e => !e.IsSpawnpointSpawned);

    public enum StageCompleteCondition
    {
        ELIMINATE_ALL_ENEMIES,
        RETRIEVE_FUMO,
        PROTECT_FUMO,
    };

    public StageCompleteCondition StageCompleteConditionType = StageCompleteCondition.ELIMINATE_ALL_ENEMIES;

    public enum EnvironmentType
    {
        KEYS,
        ORIGINIUM_TILE,
        ONE_WAY_PASSAGE,
        HEAT_PUMP_VENT,
        MEDICAL_TILE,
    };

    public EnvironmentType[] StageEvironmentTypes;

    private PlayerManager playerManager;


    bool IsStageReady = false,
        IsStageEnd = false,
        IsStageEndOverlayActive = false,
        IsResultVitory = false,
        StageClearedNMFirsttime = false;

    public virtual void Start()
    {
        LevelName = SceneManager.GetActiveScene().name;
        Title.text = LevelIndex < 1000 
            ? $"<b><size=120>{LevelName}</size></b>\n{prefabStorage.LevelTitles[LevelIndex]}"
            : $"<b><size=120>{LevelName}</size></b>\nDeath";

        o_QuitBtn.onClick.AddListener(QuitStage);
        o_RetryBtn.onClick.AddListener(RetryStage);
        PauseButton.GetComponent<Button>().onClick.AddListener(TogglePauseStage);

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

        foreach (var skillData in CharacterPrefabsStorage.Skills)
        {
            SkillName skill = skillData.Key;
            switch (skill)
            {
                case SkillName.GRAVITY:
                    float mspdReduction = 12f + enemy.weight * 3f;
                    enemy.ApplyEffect(Effect.AffectedStat.MSPD, "GRAVITY_DEBUFF", mspdReduction, 9999f, true);
                    break;
                case SkillName.OBSCURE_VISION:
                    enemy.b_attackRange *= 0.8f;
                    enemy.DetectionRange *= 0.8f;
                    break;
                case SkillName.HUNGER:
                    enemy.bAtk = (short)(enemy.bAtk * 1.5f);
                    enemy.ASPD += 30;
                    enemy.mHealth = (int)(enemy.mHealth * 0.6f);
                    break;
            }
        }
    }

    public virtual void OnPlayerSpawn(PlayerBase player)
    {
        foreach (var skillData in CharacterPrefabsStorage.Skills)
        {
            SkillName skill = skillData.Key;
            switch (skill)
            {
                case SkillName.GRAVITY:
                    float mspdReduction = 12f + player.weight * 3f;
                    player.ApplyEffect(Effect.AffectedStat.MSPD, "GRAVITY_DEBUFF", mspdReduction, 9999f, true);
                    break;
                case SkillName.OBSCURE_VISION:
                    player.b_attackRange *= 0.8f;
                    break;
            }
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
        else if (Input.GetKeyDown(InputManager.Instance.SlowKey))
        {
            isSlowing = !isSlowing;
            SlowImg.color = isSlowing ? Color.white : new(0.15f, 0.15f, 0.15f);
        }
        else if (Input.GetKeyDown(InputManager.Instance.ViewMapKey))
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
                OnStageEnd(ResultType.PLAYER_DEFEATED);
                FadeInResult();
                yield break; // Exit the coroutine if player is dead
            }

            if (StageCompleteConditionType == StageCompleteCondition.ELIMINATE_ALL_ENEMIES 
                && GetEnemyCount() <= 0)
            {
                IsStageStarted = false;
                OnStageEnd(playerManager.IsPlayerAlive ? ResultType.ENEMIES_DEFEATED : ResultType.PLAYER_DEFEATED);
                FadeInResult();
            }
        }
    }

    public enum ResultType
    {
        ENEMIES_DEFEATED,
        PLAYER_DEFEATED,
        FUMO_RETRIEVED,
        FUMO_PROTECTED,
        FUMO_LOST,
    }
    public virtual void OnStageEnd(ResultType resultType)
    {
        PauseButton.gameObject.SetActive(false);
        IsStageEnd = true;
        FindObjectsOfType<EnemySpawnpointScript>().ToList().ForEach(e => e.enabled = false);
        
        IsResultVitory = resultType == ResultType.ENEMIES_DEFEATED || resultType == ResultType.FUMO_RETRIEVED || resultType == ResultType.FUMO_PROTECTED;

        TMP_Text text = pauseOverlay.GetComponentInChildren<TMP_Text>();
        text.text = resultType switch
        {
            ResultType.ENEMIES_DEFEATED => 
                CharacterPrefabsStorage.EnableChallengeMode
                    ? "<color=#ff3b3b>Challenge Completed!</color>"
                    : "<color=green>Enemies Eliminated!</color>",
            ResultType.FUMO_RETRIEVED => 
                CharacterPrefabsStorage.EnableChallengeMode
                    ? "<color=#ff3b3b>Challenge Completed!</color>"
                    : "<color=green>Fumo Retrieved!</color>",
            ResultType.FUMO_PROTECTED => 
                CharacterPrefabsStorage.EnableChallengeMode
                    ? "<color=#ff3b3b>Challenge Completed!</color>"
                    : "<color=green>Fumo Protected!</color>",
            ResultType.PLAYER_DEFEATED => "<color=red>Defeated!</color>",
            ResultType.FUMO_LOST => "<color=red>Fumo Stolen!</color>",
        };

        if (IsResultVitory) ProceedAsVictory(text);
    }

    void ProceedAsVictory(TMP_Text text)
    {
        bool StageClearedCMFirsttime = false;
        foreach (var item in enemySpawnpoints)
        {
            Destroy(item);
        }

        List<string> CompletedLevels = PlayerPrefs.GetString("CompletedLevels", string.Empty).Split(" ").ToList();
        if (CharacterPrefabsStorage.EnableChallengeMode)
        {
            var CMLVL = LevelName + "_CM";
            StageClearedCMFirsttime = !CompletedLevels.Contains(CMLVL);

            if (StageClearedCMFirsttime)
            {
                if (CompletedLevels.Contains(LevelName)) CompletedLevels.Remove(LevelName);
                CompletedLevels.Add(CMLVL);
            }
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

        if (StageClearedNMFirsttime)
        {
            text.text += "\n<size=36>+1 Mint fumo\nChallenge Mode has been unlocked!</size>";
            AddFumo();
        }
        else if (StageClearedCMFirsttime)
        {
            text.text += "\n<size=36>+1 Mint fumo\nThis is crazy</size>";
            AddFumo();
        }
        else
            text.text += "\n<size=32><color=#00ffb7>But you already claimed the Fumo...</color></size>";

        o_QuitBtn.transform.localPosition = new Vector3(0, o_QuitBtn.transform.localPosition.y);
        o_RetryBtn.gameObject.SetActive(false);

        CharacterPrefabsStorage.EnableChallengeMode = false;
    }

    void AddFumo()
    {
        int fumoCount = PlayerPrefs.GetInt("Fumo", 0);
        int aFumoCount = PlayerPrefs.GetInt("AllTimeFumo", fumoCount);
        PlayerPrefs.SetInt("Fumo", fumoCount + 1);
        PlayerPrefs.SetInt("AllTimeFumo", aFumoCount + 1);
    }

    void FadeInResult()
    {
        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
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

        if (IsResultVitory) CharacterPrefabsStorage.ClearBattleData();

        IsFirstTimeStageEnter = true;
        Time.timeScale = 1f;

        SceneManager.LoadScene("Level_Selection");
    }

    public void OnPlayerFumoPickup(PlayerBase player, Collider2D FumoObj)
    {
        if (StageCompleteConditionType != StageCompleteCondition.RETRIEVE_FUMO) return;

        playerManager.enabled = playerManager.activePlayer.enabled = FumoObj.enabled = false;

        player.SetInvulnerable(9999f);
        EntityManager.Enemies.ForEach(e => { if (e) e.InstaKill(); });
        IsStageStarted = false; 
        OnStageEnd(ResultType.FUMO_RETRIEVED);
        StartCoroutine(ZoomInFumo(FumoObj.gameObject));
    }

    public void OnPlayerFumoProtected(FumoScript FumoObj)
    {
        if (StageCompleteConditionType != StageCompleteCondition.PROTECT_FUMO) return;

        playerManager.enabled = playerManager.activePlayer.enabled = FumoObj.enabled = false;

        EntityManager.Enemies.ForEach(e => { if (e) e.InstaKill(); });
        IsStageStarted = false;
        OnStageEnd(ResultType.FUMO_PROTECTED);
        StartCoroutine(ZoomInFumo(FumoObj.gameObject));
    }

    public void OnEnemyFumoPickup(EnemyBase enemy, Collider2D FumoObj)
    {
        if (StageCompleteConditionType != StageCompleteCondition.PROTECT_FUMO) return;

        playerManager.enabled = playerManager.activePlayer.enabled = FumoObj.enabled = false;

        IsStageStarted = false;
        Destroy(FumoObj.gameObject);
        OnStageEnd(ResultType.FUMO_LOST);
        FadeInResult();
    }

    IEnumerator ZoomInFumo(GameObject fumoObj)
    {
        var stageCompletedText = pauseOverlay.GetComponentInChildren<TMP_Text>();
        stageCompletedText.transform.position += new Vector3(0, 200);

        o_QuitBtn.transform.localScale *= 0.8f;
        o_QuitBtn.transform.position -= new Vector3(0, 220);

        FumoScript fumoScript = fumoObj.GetComponent<FumoScript>();
        var fumo = fumoScript.Fumo;

        BGM.clip = fumoScript.f_WinBGM;
        BGM.loop = false;
        BGM.Play();

        Vector3 fumoCurrentPostition = fumoScript.OnFumoPickUp();
        RawImage fumoGlowImg = fumoObj.GetComponentInChildren<RawImage>(true);

        mainCamera.enabled = false;
        yield return new WaitForSecondsRealtime(1.5f);

        Transform fumoSpriteTransform = fumo.transform;
        Vector3
            fumoInitScale = fumo.transform.localScale,
            fumoTargetScale = new(5.5f, 5.5f);

        Vector3 fumoGlowImgScale = fumoGlowImg.transform.localScale,
                fumoGlowImgTargetScale = fumoGlowImgScale * 50f;

        float c = 0, d = 4.2f, cJump = 0.01f;
        float rotateTimer = d * 0.75f;
        float rotateDegreePerLoop = 360 * 3 / rotateTimer * cJump;

        // Get screen center for lerping to center
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);

        while (c < d)
        {
            /*
            if (c < rotateTimer)
                fumoSpriteTransform.Rotate(0, rotateDegreePerLoop, 0);
            else
                fumoSpriteTransform.rotation = Quaternion.Euler(Vector3.zero);
            */

            // Lerp from current screen position to screen center
            Vector3 currentScreenPos = Vector3.Lerp(fumoCurrentPostition, screenCenter, c * 1.0f / d);
            fumoGlowImg.transform.position = fumo.transform.position = currentScreenPos;

            fumo.transform.localScale = Vector3.Lerp(fumoInitScale, fumoTargetScale, c * 1.0f / d);
            fumoGlowImg.transform.localScale = Vector3.Lerp(fumoGlowImgScale, fumoGlowImgTargetScale, c * 1.0f / d);

            c += cJump;
            yield return new WaitForSecondsRealtime(cJump);
        }

        fumo.transform.localPosition = Vector3.zero;
        fumo.transform.localScale = fumoTargetScale;
        fumoGlowImg.transform.localScale = fumoGlowImgTargetScale;

        fumoScript.FumoZoomInComplete();

        yield return new WaitForSecondsRealtime(2f);
        FadeInResult();
    }

    public static void SpecialStageAddsOn(EntityBase entity)
    {

    }
}