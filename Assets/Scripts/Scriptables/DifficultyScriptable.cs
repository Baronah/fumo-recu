using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using static Effect;
using static LevelDifficultyModifier;

[CreateAssetMenu(fileName = "DifficultyScriptable", menuName = "ScriptableObjects/DifficultyScriptable")]
public class DifficultyScriptable : ScriptableObject
{
    [SerializeField] public LevelDifficultyData[] LevelDifficultyDataArray;
    public DiffType MedalAwardMilestone = DiffType.EnemiesLevelSpecificBuff;

    public float UncombatDR_1 = 0.3f, UncombatDR_2 = 0.6f;

    public float AlertMspdBonus = 0.3f;
    public float AlertMspdBonusJump = 0.1f;

    public float PlayerCD_Sp_Extra_1 = 0.5f, PlayerCD_Ult_Extra_1 = 0.3f;
    public float PlayerCD_SP_Extra_2 = 1f, PlayerCD_Ult_Extra_2 = 0.6f;

    public readonly string COMBAT_ENTER_KEY = "COMBAT_ENTER_BUFF";
    public float CombatEnterAtk_1 = 100f, CombatEnterAspd_1 = 50f;
    public float CombatEnterDuration_1 = 6f;
    public float CombatEnterAtk_2 = 100f, CombatEnterAspd_2 = 100f, CombatEnterMspd_2 = 60f;
    public float CombatEnterDuration_2 = 6f;

    public readonly string CC_DEF_KEY = "DEF_CC_WEAROFF", CC_RES_KEY = "RES_CC_WEAROFF";
    public float CCDefenseFlatBuff_1 = 30, CCDefenseFlatBuff_2 = 48;
    public float CCDefenseFlatFallOff_1 = 10, CCDefenseFlatFallOff_2 = 12;

    public readonly string FIELD_PENALTY_KEY = "PLAYER_FIELD_PENALTY";
    public float PlayerFieldStayTriggerWaitTime_1 = 10f;
    public float PlayerFieldStay_MaxRampupTime_1 = 20f;
    public float PlayerFieldStay_MaxAspdPenalty_1 = 50;

    public float PlayerFieldStayTriggerWaitTime_2 = 5f;
    public float PlayerFieldStay_MaxRampupTime_2 = 20f;
    public float PlayerFieldStay_MaxAspdPenalty_2 = 50;
    public float PlayerFieldStay_MaxMspdPenalty_2 = 50;

    public float Player_HealingEffectPenalty_Max = 0.9f;
    public float Player_HealingEffect_DecayAfter = 40f;
}

[Serializable]
public struct LevelDifficultyData
{
    public int LevelIndex;
    public StatModifier[] StatModifiers;
}

[Serializable]
public struct StatModifier
{
    public AffectedStat affectedStat;
    public Effect.ValueType valueType;
    public float Value;
}