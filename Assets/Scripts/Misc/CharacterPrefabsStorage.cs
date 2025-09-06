using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using static PlayerManager;

[CreateAssetMenu(fileName = "CharacterPrefabsStorage", menuName = "ScriptableObjects/CharacterPrefabsStorage")]
public class CharacterPrefabsStorage : ScriptableObject
{
	public AssetReference[] PlayerAssetReferences;
	public AssetReference[] EnemyAssetReferences;
	public AssetReference[] SceneAssetReferences;

	public static PlayerType startingPlayer = PlayerType.MELEE;
	public static bool EnableChallengeMode = false;
	public static Dictionary<SkillTree_Manager.SkillName, SkillDataSet> Skills = new();

    public static Dictionary<int, GameObject> PlayerPrefabs = new();
	public static Dictionary<int, GameObject> EnemyPrefabs = new();
}
