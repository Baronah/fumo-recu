using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static EnemyBase;

[Singleton]
public class LevelManager : MonoBehaviour
{
    public static LevelManager _instance;
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    [SerializeField] private LevelsScriptable levelsScriptable;
    public LevelsScriptable LevelsScriptable => levelsScriptable;

    public LevelData[] GetLevelDatas() => levelsScriptable.Levels;

    public LevelData GetLevel(int index)
    {
        return levelsScriptable.GetLevel(index);
    }

    public LevelData GetLevelByTitle(string title)
    {
        return levelsScriptable.GetLevelByTitle(title);
    }

    public string GetTitle(int index)
    {
        return GetLevel(index).Title;
    }

    public EnemyCode[] GetAppearingEnemies(int index)
    {
        return GetLevel(index).AppearingEnemies;
    }

    public AssetReference[] GetLevelAssets => levelsScriptable.GetLevelAssets;
}