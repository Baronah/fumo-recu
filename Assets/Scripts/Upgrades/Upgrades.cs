using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using static SkillTree_Manager;

[CreateAssetMenu(fileName = "Upgrades", menuName = "ScriptableObjects/Upgrades", order = 1)]
public class Upgrades : ScriptableObject
{
    [FormerlySerializedAs("upgrades")]
    public UpgradeData[] Inventios;
    public UpgradeData[] Ars;
    public UpgradeData[] Theorias;

    public Sprite GetIconOfUpgrade(SkillName skillName)
    {
        UpgradeData upgradeData = GetUpgradeData(skillName);
        return upgradeData.icon;
    }

    public float[] GetValues(SkillName skillName)
    {
        UpgradeData upgradeData = GetUpgradeData(skillName);
        return upgradeData.values;
    }

    public float GetValue(SkillName skillName, int level)
    {
        UpgradeData upgradeData = GetUpgradeData(skillName);
        if (level < 0 || level >= upgradeData.values.Length)
        {
            Debug.LogWarning($"Level {level} is out of range for skill {skillName}. Returning 0.");
            return 0f;
        }
        return upgradeData.values[level];
    }

    public UpgradeData GetUpgradeData(SkillType type, SkillName skillName)
    {
        UpgradeData[] upgradesArray = type switch
        {
            SkillType.INVENTIO => Inventios,
            SkillType.ARS => Ars,
            SkillType.THEORIA => Theorias,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        foreach (var upgrade in upgradesArray)
        {
            if (upgrade.skillName == skillName)
            {
                return upgrade;
            }
        }
        Debug.LogWarning($"Upgrade data for skill '{skillName}' not found.");
        return default;
    }

    public UpgradeData GetUpgradeData(SkillName skillName)
    {
        foreach (var upgrade in Inventios)
        {
            if (upgrade.skillName == skillName)
            {
                return upgrade;
            }
        }
        foreach (var upgrade in Ars)
        {
            if (upgrade.skillName == skillName)
            {
                return upgrade;
            }
        }
        foreach (var upgrade in Theorias)
        {
            if (upgrade.skillName == skillName)
            {
                return upgrade;
            }
        }
        Debug.LogWarning($"Upgrade data for skill '{skillName}' not found.");
        return default;
    }

    public bool HasType(HashSet<SkillName> set, SkillType type)
    {
        UpgradeData[] upgradesArray = type switch
        {
            SkillType.INVENTIO => Inventios,
            SkillType.ARS => Ars,
            SkillType.THEORIA => Theorias,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        foreach (var upgrade in upgradesArray)
        {
            if (set.Contains(upgrade.skillName))
            {
                return true;
            }
        }
        return false;
    }
}

[Serializable]
public struct UpgradeData
{
    public SkillName skillName;
    public SkillType skillType;
    public string name;
    public Sprite icon;
    public string description;
    public float[] values;
    public string shortDescription;
}