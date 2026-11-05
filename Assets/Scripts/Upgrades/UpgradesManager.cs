using UnityEngine;
using static SkillTree_Manager;

public class UpgradesManager : MonoBehaviour
{
    public static UpgradesManager _instance { get; private set; }
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Upgrades upgradeData;
    public UpgradeData GetUpgradeData(SkillName skillName)
        => upgradeData.GetUpgradeData(skillName);
    public UpgradeData GetUpgradeData(SkillType type, SkillName skillName)
        => upgradeData.GetUpgradeData(type, skillName);

    public float[] GetUpgradeValues(SkillName skillName)
        => upgradeData.GetValues(skillName);

    public bool HasTheoria()
        => upgradeData.HasType(CharacterPrefabsStorage.Skills, SkillType.THEORIA);
}