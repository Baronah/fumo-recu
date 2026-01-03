using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using static PlayerManager;

[CreateAssetMenu(fileName = "CharacterPrefabsStorage", menuName = "ScriptableObjects/CharacterPrefabsStorage")]
public class CharacterPrefabsStorage : ScriptableObject
{
	public static string LevelSelectionKey = "Level_Selection";
    public AssetReference LevelSelectionScene;

	public AssetReference[] PlayerAssetReferences;
	public AssetReference[] EnemyAssetReferences;
	public AssetReference[] SceneAssetReferences;
	public string[] LevelTitles;

	public static PlayerType startingPlayer = PlayerType.MELEE;
	public static bool EnableChallengeMode = false;
	public static Dictionary<SkillTree_Manager.SkillName, SkillDataSet> Skills = new();

	public static void ClearBattleData()
	{
		var skillNames = Skills.Select(s => s.Key).ToArray();

        PlayerPrefs.SetString("InventionsUsed", string.Join(" ", skillNames));
		startingPlayer = PlayerType.MELEE;
		Skills.Clear();
	}

    public static Dictionary<int, GameObject> PlayerPrefabs = new();
	public static Dictionary<int, GameObject> EnemyPrefabs = new();
}
