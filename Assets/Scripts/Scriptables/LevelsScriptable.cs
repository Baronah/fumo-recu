using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static EnemyBase;
using static SkillTree_Manager;
using static StageManager;

[CreateAssetMenu(fileName = "LevelsScriptable", menuName = "ScriptableObjects/LevelsScriptable")]
public class LevelsScriptable : ScriptableObject
{
    public LevelData[] Levels;

    public LevelData GetLevel(int index)
    {
        if (Levels == null || Levels.Length <= index)
        {
            Debug.LogError("Level index out of range or Levels array is null.");
            return default;
        }
        return Levels[index];
    }

    public LevelData GetLevelByTitle(string title)
    {
        if (Levels == null)
        {
            Debug.LogError("Levels array is null.");
            return default;
        }
        foreach (var level in Levels)
        {
            if (level.Title.Equals(title, StringComparison.OrdinalIgnoreCase))
            {
                return level;
            }
        }
        Debug.LogWarning($"Level with title '{title}' not found.");
        return default;
    }

    public AssetReference[] GetLevelAssets => Levels.Select(level => level.SceneAssetReferences).ToArray();
}

[Serializable]
public struct LevelData
{
    public AssetReference SceneAssetReferences;
    public string Title;
    public string Description;
    public string AddOn;
    public StageCompleteCondition StageCompleteCondition;
    public EnemyCode[] AppearingEnemies;
    public EnvironmentType[] EnvironmentTypes;
    public SkillName[] BanSkills;
    public string ChallengeModeModifier;
    public string SecretDes;
}