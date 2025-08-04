using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelectionScript : MonoBehaviour
{
    [SerializeField] private GameObject LevelPrefabTemplate, LevelsPoint, Overlay;
    [SerializeField] private CharacterPrefabsStorage characterPrefabsStorage;
    [SerializeField] private Transform[] Containers;
    [SerializeField] private Sprite Incompleted, Completed, CompletedCM;

    private List<GameObject> LevelPrefabs = new();
    private int CurrentPageIndex = 0;
    private int MaxPageSize => Containers.Length;
    private int TotalLevels => characterPrefabsStorage.SceneAssetReferences.Length;

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

            if (CompletedLevels.Contains(displayName))
                completionStatus.sprite = Completed;
            else 
                completionStatus.sprite = Incompleted;

            string capturedKey = runtimeKey;
            level.GetComponent<Button>().onClick.AddListener(() => StartCoroutine(SelectLevel(capturedKey)));

            LevelPrefabs.Add(level);
        }
    }

    string GetSceneName(int levelIndex)
    {
        return "FM-" + (levelIndex < 10 ? $"0{levelIndex}" : levelIndex);
    }

    IEnumerator SelectLevel(string runtimeKey)
    {
        yield return StartCoroutine(OverlayFadeIn());

        Addressables.LoadSceneAsync(runtimeKey, LoadSceneMode.Single, true);
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
        int totalPages = Mathf.CeilToInt((float)TotalLevels / MaxPageSize);
        if (CurrentPageIndex < totalPages - 1)
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

    public void Quit() => Application.Quit();
}