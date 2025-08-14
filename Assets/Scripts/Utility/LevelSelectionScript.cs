using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static EnemyBase;
using static StageManager;

public class LevelSelectionScript : MonoBehaviour
{
    [SerializeField] private GameObject LevelPrefabTemplate, LevelsPoint, Overlay, LevelSelectionConfirm, Nav;
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
    [SerializeField] private EnemyCode[] AppearingEnemies;

    [SerializeField] private GameObject MapPreviewObj;
    [SerializeField] private Image MapPreviewImg, MapPreviewImgOverlay;
    [SerializeField] private Sprite[] Map_SSs;

    private List<GameObject> LevelPrefabs = new();
    private int CurrentPageIndex = 0;
    private int MaxPageSize => Containers.Length;
    private int TotalLevels => characterPrefabsStorage.SceneAssetReferences.Length;
    private int TotalPages => Mathf.CeilToInt((float)TotalLevels / MaxPageSize);    

    private string selectedKey = null;
    private int selectedIndex = -1;
    private bool enableCM = false, isViewingMap = false;

    private List<bool> IsMapCleared = new();

    private void Start()
    {
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
                    EnvironmentType.KEYS => "<color=purple><Keys></color> Collect to remove the terrains with corresponding color.",
                    EnvironmentType.ORIGINIUM_TILES => "<color=#C40000><Originium Pollution></color> Continuously deals true damage to the player and enemy units standing on it.",
                    _ => "Unknown environment"
                };
                environmentDescription += $"{envDes}\n";
            }

            description += $"\n<color=#E5E5E5>{stageCompleteCondition}\n{environmentDescription}</color>";
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
        yield return StartCoroutine(OverlayFadeIn());

        CharacterPrefabsStorage.EnableChallengeMode = enableCM;
        Addressables.LoadSceneAsync(selectedKey, LoadSceneMode.Single, true);
    }

    public void ViewMap()
    {
        MapPreviewObj.SetActive(true);
        MapPreviewImgOverlay.sprite = MapPreviewImg.sprite = Map_SSs[selectedIndex];
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