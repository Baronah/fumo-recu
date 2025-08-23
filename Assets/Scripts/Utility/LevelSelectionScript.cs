using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Loading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static EnemyBase;
using static StageManager;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

public class LevelSelectionScript : MonoBehaviour
{
    [SerializeField] private CharacterPrefabsStorage prefabStorage;
    [SerializeField] private GameObject LevelPrefabTemplate, LevelsPoint, Overlay, LevelSelectionConfirm, Nav, LevelEnemyView, EnemyViewPrefab, EnemyViewContent;
    [SerializeField] private CharacterPrefabsStorage characterPrefabsStorage;
    [SerializeField] private Transform[] Containers;
    [SerializeField] private Sprite Incompleted, Completed, CompletedCM;

    [SerializeField] private Sprite NMSprite, CMSprite, LockedSprite;
    [SerializeField] private Button CMToggleButton;
    private Image CMToggleImg => CMToggleButton.GetComponent<Image>();

    [SerializeField] private TMP_Text SelectedLvlName, SelectedLvlDescription;
    [SerializeField] private string[] Names, Descriptions, ChallengeModes;
    [SerializeField] private StageCompleteCondition[] CompleteCondition;
    [SerializeField] private StageEnvironment[] Environments;
    [SerializeField] private AppearingEnemies[] AppearingEnemies;

    [SerializeField] private GameObject MapPreviewObj;
    [SerializeField] private Image MapPreviewImg, MapPreviewImgOverlay;
    [SerializeField] private Sprite[] Map_SSs;

    [SerializeField] private Sprite DefaultEnemyIcon;
    [SerializeField] private GameObject EnemyDetail, SelectedBorder;
    [SerializeField] private TMP_Text 
        EnemyName,
        EnemyPattern,
        EnemyDescription,
        Rating_HP, 
        Rating_DEF, 
        Rating_RES, 
        Rating_ATK, 
        Rating_ASPD, 
        Rating_ARNG, 
        Rating_MSPD, 
        Rating_DRNG,
        Rating_CALV;

    [SerializeField] private Image EnemyIcon;

    private bool IsLoadingEnemyPrefabs = false;
    private List<GameObject> LevelPrefabs = new();
    private int CurrentPageIndex = 0;
    private int MaxPageSize => Containers.Length;
    private int TotalLevels => characterPrefabsStorage.SceneAssetReferences.Length;
    private int TotalPages => Mathf.CeilToInt((float)TotalLevels / MaxPageSize);    

    private string selectedKey = null;
    private int selectedIndex = -1;
    private bool enableCM = false, isViewingMap = false;

    private List<bool> IsMapCleared = new();

    private List<GameObject> CreatedEnemyViewPrefabs = new();

    private List<EnemyCode> EncounteredEnemies = new();
    private bool IsEnemyEncoutered(EnemyCode enemyCode) => EncounteredEnemies.Contains(enemyCode);

    private AudioSource[] sfxs;

    private void Start()
    {
        sfxs = GetComponents<AudioSource>();
        sfxs[0].volume = PlayerPrefs.GetFloat("BGM", 1f);
        for (int i = 1; i < sfxs.Length; ++i)
        {
            sfxs[i].volume = PlayerPrefs.GetFloat("SFX", 1f);
        }

        string[] encounteredEnemies = PlayerPrefs.GetString("EncounteredEnemies", string.Empty).Split(" ").ToArray();
        foreach (var enemy in encounteredEnemies)
        {
            if (Enum.TryParse(enemy, out EnemyCode code))
            {
                EncounteredEnemies.Add(code);
            }
        }

        Time.timeScale = 1f;
        AssignLevels();
    }

    void AssignLevels()
    {
        foreach (var l in LevelPrefabs)
        {
            if (l != null) Destroy(l);
        }
        LevelPrefabs.Clear();

        Nav.SetActive(TotalPages > 1);

        int startLevelIndex = CurrentPageIndex * MaxPageSize;
        List<string> CompletedLevels = PlayerPrefs.GetString("CompletedLevels", string.Empty).Split(" ").ToList();

        for (int i = 0; i < MaxPageSize; ++i)
        {
            int levelIndex = startLevelIndex + i;

            if (levelIndex >= TotalLevels) break;

            var targetLevel = characterPrefabsStorage.SceneAssetReferences[levelIndex];
            var runtimeKey = targetLevel.RuntimeKey.ToString();

            GameObject level = Instantiate(LevelPrefabTemplate, Containers[i].position, Quaternion.identity, LevelsPoint.transform);
            TMP_Text nameText = level.GetComponentInChildren<TMP_Text>();
            Image completionStatus = level.transform.Find("CompletionStatus").GetComponent<Image>();

            string displayName = GetSceneName(levelIndex);
            nameText.text = displayName;

            if (CompletedLevels.Contains(displayName + "_CM"))
            {
                IsMapCleared.Add(true);
                completionStatus.sprite = CompletedCM;
            }
            else if (CompletedLevels.Contains(displayName))
            {
                IsMapCleared.Add(true);
                completionStatus.sprite = Completed;
            }
            else
            {
                IsMapCleared.Add(false);
                completionStatus.sprite = Incompleted;
            }

            string capturedKey = runtimeKey;
            level.GetComponent<Button>().onClick.AddListener(() => SelectLevel(levelIndex, capturedKey));

            LevelPrefabs.Add(level);
        }
    }

    string GetSceneName(int levelIndex)
    {
        return "FM-" + (levelIndex < 10 ? $"0{levelIndex}" : levelIndex);
    }

    void SelectLevel(int index, string runtimeKey)
    {
        if (isViewingMap) return;

        sfxs[1].Play();

        selectedIndex = index;
        selectedKey = runtimeKey;
        SelectedLvlName.text = GetSceneName(selectedIndex) + ": " + Names[selectedIndex];
        SelectedLvlDescription.text = GetLevelDescription(selectedIndex);

        if (!IsMapCleared[selectedIndex]) CMToggleImg.sprite = LockedSprite;
        CMToggleButton.interactable = IsMapCleared[selectedIndex];

        MapPreviewImgOverlay.sprite = MapPreviewImg.sprite = Map_SSs[selectedIndex];

        StartCoroutine(ScaleLevelSelection(true));
    }

    string GetLevelDescription(int index)
    {
        string description = Descriptions[index];
        if (enableCM)
        {
            description = $"<size=30><color=red><b>Conditions:</size></b>\n{ChallengeModes[index]}</color>";
        }
        else
        {
            string stageCompleteCondition = CompleteCondition[index] switch
            {
                StageCompleteCondition.ELIMINATE_ALL_ENEMIES => "<color=red><Annihilation></color> Eliminate all enemies to complete the stage.",
                StageCompleteCondition.RETRIEVE_FUMO => "<color=#00ffff><Rescue></color> Reach the location of the Fumo to complete the stage.",
                _ => "Unknown condition"
            };

            string environmentDescription = string.Empty;
            foreach (var env in Environments[index].Environments)
            {
                string envDes = env switch
                {
                    EnvironmentType.KEYS => "<color=purple><Key></color> Collect to remove the terrains with corresponding color.",
                    EnvironmentType.ORIGINIUM_TILES => "<color=#C40000><Originium Pollution></color> Continuously deals true damage to the player and enemy units standing on it.",
                    _ => "Unknown environment"
                };
                environmentDescription += $"{envDes}\n";
            }

            description += $"\n\n<color=#E5E5E5>{stageCompleteCondition}\n{environmentDescription}</color>";
        }
        return description.Replace(@"\n", "\n");
    }

    IEnumerator ScaleLevelSelection(bool toggleIn)
    {
        isViewingMap = true;

        Vector3 fullScale = Vector3.one, 
                hideScale = new(0.03f, 1, 1),
                fullPosition = Vector3.zero,
                hidePosition = new(0, -1000);

        Transform targetTransform = LevelSelectionConfirm.transform.Find("Body");

        float c, d;
        if (toggleIn)
        {
            LevelSelectionConfirm.SetActive(true);
            targetTransform.localScale = hideScale;
            targetTransform.localPosition = hidePosition;

            c = 0;
            d = 0.35f;
            while (c < d)
            {
                targetTransform.localPosition = Vector3.Lerp(hidePosition, fullPosition, c * 1.0f / d);

                c += Time.deltaTime;
                yield return null;
            }

            targetTransform.localPosition = fullPosition;
            yield return new WaitForSeconds(0.05f);

            c = 0;
            d = 0.3f;
            while (c < d)
            {
                targetTransform.localScale = Vector3.Lerp(hideScale, fullScale, c * 1.0f / d);

                c += Time.deltaTime;
                yield return null;
            }

            targetTransform.localScale = fullScale;
        }
        else
        {
            targetTransform.localScale = fullScale;
            targetTransform.localPosition = fullPosition;

            c = 0;
            d = 0.35f;
            while (c < d)
            {
                targetTransform.localScale = Vector3.Lerp(fullScale, hideScale, c * 1.0f / d);

                c += Time.deltaTime;
                yield return null;
            }

            targetTransform.localScale = hideScale;
            yield return new WaitForSeconds(0.05f);

            c = 0;
            d = 0.3f;
            while (c < d)
            {
                targetTransform.localPosition = Vector3.Lerp(fullPosition, hidePosition, c * 1.0f / d);

                c += Time.deltaTime;
                yield return null;
            }
            targetTransform.localPosition = hidePosition;

            LevelSelectionConfirm.SetActive(false); 
            CMToggleImg.sprite = NMSprite;
        }

        isViewingMap = false;
    }

    public void ToggleChallengeMode()
    {
        if (sfxs[2]) sfxs[2].Play();
        if (!enableCM)
        {
            enableCM = true;
            CMToggleImg.sprite = CMSprite;
        }
        else
        {
            enableCM = false;
            CMToggleImg.sprite = NMSprite;
        }

        SelectedLvlDescription.text = GetLevelDescription(selectedIndex);
    }

    IEnumerator ConfirmLevelSelection()
    {
        if (sfxs[2]) sfxs[2].Play();
        yield return StartCoroutine(OverlayFadeIn());

        CharacterPrefabsStorage.EnableChallengeMode = enableCM;
        Addressables.LoadSceneAsync(selectedKey, LoadSceneMode.Single, true);
    }

    public void ViewMap()
    {
        MapPreviewObj.SetActive(true);
        MapPreviewImgOverlay.sprite = MapPreviewImg.sprite = Map_SSs[selectedIndex];
    }

    public void ViewEnemy()
    {
        if (IsLoadingEnemyPrefabs) return;
        StartCoroutine(LoadEnemyPrefabs());
    }

    public void CloseEnemyView()
    {
        SelectedBorder.SetActive(false);
        LevelEnemyView.SetActive(false);
        foreach (var go in CreatedEnemyViewPrefabs)
        {
            Destroy(go);
        }
        CreatedEnemyViewPrefabs.Clear();
        ResetInformation();
    }

    void ResetInformation()
    {
        EnemyIcon.sprite = DefaultEnemyIcon;

        Rating_CALV.text = Rating_DRNG.text = Rating_MSPD.text = Rating_ASPD.text = 
            Rating_ARNG.text = Rating_DEF.text = Rating_RES.text = Rating_ATK.text = 
            Rating_HP.text = EnemyPattern.text = EnemyName.text = "N/A";

        EnemyDescription.text = 
            "Select an enemy to view their information.";
    }

    void SetUnknownEnemyInfo()
    {
        EnemyIcon.sprite = DefaultEnemyIcon;
        Rating_CALV.text = Rating_DRNG.text = Rating_MSPD.text = Rating_ASPD.text = 
            Rating_ARNG.text = Rating_DEF.text = Rating_RES.text = Rating_ATK.text = 
            Rating_HP.text = EnemyPattern.text = EnemyName.text = "N/A";
        EnemyDescription.text = 
            "You haven't encountered this enemy yet.";
    }

    public IEnumerator GetEnemyInformation(EnemyCode enemyCode, Vector3 position)
    {
        sfxs[1].Play();

        SelectedBorder.transform.localPosition = position;
        SelectedBorder.SetActive(true);

        if (!EncounteredEnemies.Contains(enemyCode))
        {
            SetUnknownEnemyInfo();
            yield break;
        }

        if (IsLoadingEnemyPrefabs) yield return new WaitUntil(() => !IsLoadingEnemyPrefabs);
        GameObject enemyGO = CharacterPrefabsStorage.EnemyPrefabs[(int) enemyCode];
        EnemyBase enemy = enemyGO.GetComponent<EnemyBase>();
        
        enemy.InitializeComponents();

        EnemyIcon.sprite = enemy.Icon;
        EnemyName.text = enemy.Name;
        EnemyPattern.text = $"{enemy.attackPattern} {enemy.damageType}";
        
        // hp
        if (enemy.mHealth <= 25) 
            Rating_HP.text = "E";
        else if (enemy.mHealth <= 50)
            Rating_HP.text = "D";
        else if (enemy.mHealth <= 90)
            Rating_HP.text = "C";
        else if (enemy.mHealth <= 125)
            Rating_HP.text = "C+";
        else if (enemy.mHealth <= 200)
            Rating_HP.text = "B";
        else if (enemy.mHealth <= 250)
            Rating_HP.text = "B+";
        else if (enemy.mHealth <= 400)
            Rating_HP.text = "A";
        else if (enemy.mHealth <= 550)
            Rating_HP.text = "A+";
        else if (enemy.mHealth <= 1000)
            Rating_HP.text = "S";
        else
            Rating_HP.text = "S+";

        // atk
        if (enemy.atk <= 0) 
            Rating_ATK.text = "E";
        else if (enemy.atk <= 10)
            Rating_ATK.text = $"D";
        else if (enemy.atk <= 15)
            Rating_ATK.text = "C";
        else if (enemy.atk <= 20)
            Rating_ATK.text = "C+";
        else if (enemy.atk <= 35)
            Rating_ATK.text = "B";
        else if (enemy.atk <= 50)
            Rating_ATK.text = "B+";
        else if (enemy.atk <= 75)
            Rating_ATK.text = "A";
        else if (enemy.atk <= 100)
            Rating_ATK.text = "A+";
        else if (enemy.atk <= 150)
            Rating_ATK.text = "S";
        else
            Rating_ATK.text = "S+";

        // def
        if (enemy.bDef <= 0) 
            Rating_DEF.text = "E";
        else if (enemy.bDef <= 5)
            Rating_DEF.text = "D";
        else if (enemy.bDef <= 10)
            Rating_DEF.text = "C";
        else if (enemy.bDef <= 15)
            Rating_DEF.text = "C+";
        else if (enemy.bDef <= 25)
            Rating_DEF.text = "B";
        else if (enemy.bDef <= 45)
            Rating_DEF.text = "B+";
        else if (enemy.bDef <= 65)
            Rating_DEF.text = "A";
        else if (enemy.bDef <= 80)
            Rating_DEF.text = "A+";
        else if (enemy.bDef <= 100)
            Rating_DEF.text = "S";
        else
            Rating_DEF.text = "SS";

        // res
        if (enemy.bRes <= 0) 
            Rating_RES.text = "E";
        else if (enemy.bRes <= 5)
            Rating_RES.text = "D";
        else if (enemy.bRes <= 10)
            Rating_RES.text = "C";
        else if (enemy.bRes <= 15)
            Rating_RES.text = "C+";
        else if (enemy.bRes <= 30)
            Rating_RES.text = "B";
        else if (enemy.bRes <= 40)
            Rating_RES.text = "B+";
        else if (enemy.bRes <= 50)
            Rating_RES.text = "A";
        else if (enemy.bRes <= 70)
            Rating_RES.text = "A+";
        else if (enemy.bRes <= 80)
            Rating_RES.text = "S";
        else
            Rating_RES.text = "SS";

        // arng
        if (enemy.attackPattern == EntityBase.AttackPattern.NONE) Rating_ARNG.text = "E";
        else 
        {
            float arngValue = enemy.attackPattern == EntityBase.AttackPattern.RANGED ? enemy.b_attackRange : enemy.b_attackRange * 2.25f;
            if (arngValue <= 0)
                Rating_ARNG.text = "E";
            else if (arngValue <= 100f)
                Rating_ARNG.text = "D";
            else if (arngValue <= 200f)
                Rating_ARNG.text = "C";
            else if (arngValue <= 250f)
                Rating_ARNG.text = "C+";
            else if (arngValue <= 350f)
                Rating_ARNG.text = "B";
            else if (arngValue <= 450f)
                Rating_ARNG.text = "B+";
            else if (arngValue <= 600f)
                Rating_ARNG.text = "A";
            else if (arngValue <= 750f)
                Rating_ARNG.text = "A+";
            else if (arngValue <= 900f)
                Rating_ARNG.text = "S";
            else
                Rating_ARNG.text = "S+";
        }

        // aspd
        if (enemy.b_attackInterval <= 0)
            Rating_ASPD.text = "E";
        else if (enemy.b_attackInterval < 0.25f) 
            Rating_ASPD.text = "SS";
        else if (enemy.b_attackInterval <= 0.8f)
            Rating_ASPD.text = "S";
        else if (enemy.b_attackInterval <= 1f)
            Rating_ASPD.text = "A+";
        else if (enemy.b_attackInterval <= 1.5f)
            Rating_ASPD.text = "A";
        else if (enemy.b_attackInterval <= 2f)
            Rating_ASPD.text = "B+";
        else if (enemy.b_attackInterval <= 2.5f)
            Rating_ASPD.text = "B";
        else if (enemy.b_attackInterval <= 3.5f)
            Rating_ASPD.text = "C+";
        else if (enemy.b_attackInterval <= 5f)
            Rating_ASPD.text = "C";
        else if (enemy.b_attackInterval <= 7f)
            Rating_ASPD.text = "D";
        else
            Rating_ASPD.text = "E";

        // mspd
        if (enemy.moveSpeed <= 0) 
            Rating_MSPD.text = "E";
        else if (enemy.moveSpeed <= 30f)
            Rating_MSPD.text = "D";
        else if (enemy.moveSpeed <= 60f)
            Rating_MSPD.text = "C";
        else if (enemy.moveSpeed <= 80f)
            Rating_MSPD.text = "C+";
        else if (enemy.moveSpeed <= 95f)
            Rating_MSPD.text = "B";
        else if (enemy.moveSpeed <= 130f)
            Rating_MSPD.text = "B+";
        else if (enemy.moveSpeed <= 180f)
            Rating_MSPD.text = "A";
        else if (enemy.moveSpeed <= 220f)
            Rating_MSPD.text = "A+";
        else if (enemy.moveSpeed <= 300f)
            Rating_MSPD.text = "S";
        else
            Rating_MSPD.text = "S+";

        // drng
        if (enemy.DetectionRange <= 0)
            Rating_DRNG.text = "E";
        else if (enemy.DetectionRange <= 100f)
            Rating_DRNG.text = "D";
        else if (enemy.DetectionRange <= 200f)
            Rating_DRNG.text = "C";
        else if (enemy.DetectionRange <= 250f)
            Rating_DRNG.text = "C+";
        else if (enemy.DetectionRange <= 350f)
            Rating_DRNG.text = "B";
        else if (enemy.DetectionRange <= 450f)
            Rating_DRNG.text = "B+";
        else if (enemy.DetectionRange <= 600f)
            Rating_DRNG.text = "A";
        else if (enemy.DetectionRange <= 750f)
            Rating_DRNG.text = "A+";
        else if (enemy.DetectionRange <= 900f)
            Rating_DRNG.text = "S";
        else
            Rating_DRNG.text = "S+";

        // calv
        float calvValue = enemy.DangerRange_RatioOfAttackRange;
        if (enemy.attackPattern == EntityBase.AttackPattern.MELEE) calvValue = 1 - calvValue;

        if (calvValue <= 0.15f)
            Rating_CALV.text = "Lo";
        else if (calvValue <= 0.8f)
            Rating_CALV.text = "Med";
        else
            Rating_CALV.text = "Hi";


        EnemyDescription.text = 
            $"<color=#b1b1b1><i>{enemy.Description}</i></color>\n\n" +
            $"<color=#E5E5E5>{enemy.Skillset}</color>";
    }

    private IEnumerator LoadEnemyPrefabs()
    {
        IsLoadingEnemyPrefabs = true;

        HashSet<int> uniqueIndices = new(); // prevent duplicate loads
        EnemyCode[] appearingEnemies = AppearingEnemies[selectedIndex].Enemies;

        Vector3 InitialPosition = new(75, -75);

        Vector3 CurrentPosition = InitialPosition;
        float X_Offset = 160f, Y_Offset = -160;
        short RowDisplayCount = 1;
        const short MaxDisplayPerRow = 4;

        foreach (var code in appearingEnemies)
        {
            if (code == EnemyCode.DUMMY) continue;

            GameObject btnEnemyViewGO = Instantiate(EnemyViewPrefab, CurrentPosition, Quaternion.identity, EnemyViewContent.transform);
            btnEnemyViewGO.transform.localPosition = CurrentPosition;

            Vector3 position = btnEnemyViewGO.transform.localPosition;
            Button e = btnEnemyViewGO.GetComponent<Button>();
            e.onClick.AddListener(() => StartCoroutine(GetEnemyInformation(code, position)));
            
            CreatedEnemyViewPrefabs.Add(btnEnemyViewGO);

            RowDisplayCount++;
            CurrentPosition = new(CurrentPosition.x + X_Offset, CurrentPosition.y);

            if (RowDisplayCount > MaxDisplayPerRow)
            {
                CurrentPosition = new(InitialPosition.x, CurrentPosition.y + Y_Offset);
                RowDisplayCount = 1;
            }

            Image enemyImage = e.GetComponent<Image>();
            if (CharacterPrefabsStorage.EnemyPrefabs.ContainsKey((int)code))
            {
                enemyImage.sprite = 
                    IsEnemyEncoutered(code)
                        ? CharacterPrefabsStorage.EnemyPrefabs[(int)code].GetComponent<EnemyBase>().Icon
                        : DefaultEnemyIcon;
                continue;
            }

            if (uniqueIndices.Add((int)code)) // only process unique ones
            {
                var reference = prefabStorage.EnemyAssetReferences[(int)code];
                var handle = DataHandler.Instance.LoadAddressable<GameObject>(reference);
                yield return handle;
                CharacterPrefabsStorage.EnemyPrefabs[(int)code] = handle.Result;
                enemyImage.sprite = IsEnemyEncoutered(code) 
                    ? handle.Result.GetComponent<EnemyBase>().Icon
                    : DefaultEnemyIcon;
            }
        }

        IsLoadingEnemyPrefabs = false;
        LevelEnemyView.SetActive(true);
    }

    IEnumerator OverlayFadeIn()
    {
        Image image = Overlay.GetComponentInChildren<Image>();
        Overlay.SetActive(true);
        float c = 0, d = 1;
        while (c < d)
        {
            image.color = Color.Lerp(Color.clear, Color.black, c * 1.0f / d);
            c += Time.deltaTime;
            yield return null;
        }

        image.color = Color.black;
        DontDestroyOnLoad(Overlay);
    }

    public void NextPage()
    {
        if (CurrentPageIndex < TotalPages - 1)
        {
            CurrentPageIndex++;
            AssignLevels();
        }
    }

    public void PrevPage()
    {
        if (CurrentPageIndex > 0)
        {
            CurrentPageIndex--;
            AssignLevels();
        }
    }

    public void Deselect()
    {
        if (isViewingMap) return;

        enableCM = false;
        StartCoroutine(ScaleLevelSelection(false));
    }

    public void Confirm() => StartCoroutine(ConfirmLevelSelection());

    public void Quit() => SceneManager.LoadScene("MainMenu");
}

[Serializable] public class StageEnvironment 
{ 
    public EnvironmentType[] Environments;
}

[Serializable] public class AppearingEnemies
{
    public EnemyBase.EnemyCode[] Enemies;
}