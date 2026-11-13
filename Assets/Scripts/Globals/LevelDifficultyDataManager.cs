using Unity.VisualScripting;
using UnityEngine;
using static LevelDifficultyModifier;

[Singleton]
public class LevelDifficultyDataManager : MonoBehaviour
{
    public static LevelDifficultyDataManager _instance;

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

    [SerializeField] private DifficultyScriptable difficultyScriptable;
    public DifficultyScriptable DifficultyScriptable => difficultyScriptable;

    public LevelDifficultyData GetLevelDifficultyData(int levelIndex)
    {
        foreach (var data in difficultyScriptable.LevelDifficultyDataArray)
        {
            if (data.LevelIndex == levelIndex)
            {
                return data;
            }
        }
        return default;
    }

    public float DifficultyHpMultiplierBase = 0.04f;
    public float DifficultyAtkMultiplierBase = 0.025f;

    static int GetDiff => Mathf.Min(CharacterPrefabsStorage.DifficultyLevel - 1, LevelDifficultyModifier.GetMaxDiff());

    public float GetEnemiesHpMultiplier()
    {
        if (CharacterPrefabsStorage.DifficultyLevel <= 1) return 0;

        int Diff = GetDiff;

        float finalMul = 0;
        for (int i = 1; i <= Diff; i++)
        {
            if (i == 1) finalMul = 0.3f - DifficultyHpMultiplierBase;

            finalMul += DifficultyHpMultiplierBase;
        }

        return finalMul;
    }

    public float GetEnemiesAtkMultiplier()
    {
        if (CharacterPrefabsStorage.DifficultyLevel <= 1) return 0;

        int Diff = GetDiff;

        float finalMul = 0;
        for (int i = 1; i <= Diff; i++)
        {
            if (i == 1) finalMul = 0.2f - DifficultyAtkMultiplierBase;

            finalMul += DifficultyAtkMultiplierBase;
        }

        return finalMul;
    }

    public DiffType MedalAwardMilestone => difficultyScriptable.MedalAwardMilestone;

    public float UncombatDR_1 => difficultyScriptable.UncombatDR_1; 
    public float UncombatDR_2 => difficultyScriptable.UncombatDR_2;

    public float AlertMspdBonus => difficultyScriptable.AlertMspdBonus;
    public float AlertMspdBonusJump => difficultyScriptable.AlertMspdBonusJump;

    public float PlayerCD_Sp_Extra_1 => difficultyScriptable.PlayerCD_Sp_Extra_1;
    public float PlayerCD_Ult_Extra_1 => difficultyScriptable.PlayerCD_Ult_Extra_1;
    public float PlayerCD_Sp_Extra_2 => difficultyScriptable.PlayerCD_SP_Extra_2;
    public float PlayerCD_Ult_Extra_2 => difficultyScriptable.PlayerCD_Ult_Extra_2;

    public string COMBAT_ENTER_KEY => difficultyScriptable.COMBAT_ENTER_KEY;
    public float CombatEnterAtk_1 => difficultyScriptable.CombatEnterAtk_1;
    public float CombatEnterAspd_1 => difficultyScriptable.CombatEnterAspd_1;
    public float CombatEnterDuration_1 => difficultyScriptable.CombatEnterDuration_1;
    public float CombatEnterAtk_2 => difficultyScriptable.CombatEnterAtk_2;
    public float CombatEnterAspd_2 => difficultyScriptable.CombatEnterAspd_2;
    public float CombatEnterMspd_2 => difficultyScriptable.CombatEnterMspd_2;
    public float CombatEnterDuration_2 => difficultyScriptable.CombatEnterDuration_2;

    public string CC_DEF_KEY => difficultyScriptable.CC_DEF_KEY;
    public string CC_RES_KEY => difficultyScriptable.CC_RES_KEY;
    public float CCDefenseFlatBuff_1 => difficultyScriptable.CCDefenseFlatBuff_1;
    public float CCDefenseFlatBuff_2 => difficultyScriptable.CCDefenseFlatBuff_2;
    public float CCDefenseFlatFallOff_1 => difficultyScriptable.CCDefenseFlatFallOff_1;
    public float CCDefenseFlatFallOff_2 => difficultyScriptable.CCDefenseFlatFallOff_2;

    public string FIELD_PENALTY_KEY => difficultyScriptable.FIELD_PENALTY_KEY;
    public float PlayerFieldStayTriggerWaitTime_1 => difficultyScriptable.PlayerFieldStayTriggerWaitTime_1;
    public float PlayerFieldStay_MaxRampupTime_1 => difficultyScriptable.PlayerFieldStay_MaxRampupTime_1;
    public float PlayerFieldStay_MaxAspdPenalty_1 => difficultyScriptable.PlayerFieldStay_MaxAspdPenalty_1;

    public float PlayerFieldStayTriggerWaitTime_2 => difficultyScriptable.PlayerFieldStayTriggerWaitTime_2;
    public float PlayerFieldStay_MaxRampupTime_2 => difficultyScriptable.PlayerFieldStay_MaxRampupTime_2;
    public float PlayerFieldStay_MaxAspdPenalty_2 => difficultyScriptable.PlayerFieldStay_MaxAspdPenalty_2;
    public float PlayerFieldStay_MaxMspdPenalty_2 => difficultyScriptable.PlayerFieldStay_MaxMspdPenalty_2;

    public float Player_HealingEffectPenalty_Max => difficultyScriptable.Player_HealingEffectPenalty_Max;
    public float Player_HealingEffect_DecayAfter => difficultyScriptable.Player_HealingEffect_DecayAfter;
    public void ProcessEnemyDifficultyScaling(EnemyBase enemy, int levelIndex)
    {
        short diffLevel = (short)(CharacterPrefabsStorage.DifficultyLevel - 1);
        if (diffLevel <= 0) return;

        enemy.bAtk = (short)(enemy.bAtk * (1f + GetEnemiesAtkMultiplier()));
        enemy.mHealth *= 1f + GetEnemiesHpMultiplier();

        if (diffLevel >= (int)DiffType.EnemiesLevelSpecificBuff)
        {
            if (CharacterPrefabsStorage.DifficultyLevel >= (int)DiffType.EnemiesLevelSpecificBuff)
            {
                var data = LevelDifficultyDataManager._instance.GetLevelDifficultyData(levelIndex);
                foreach (var statModifier in data.StatModifiers)
                {
                    bool isPercentage = statModifier.valueType == Effect.ValueType.PERCENTAGE;

                    float value = statModifier.Value;
                    if (isPercentage) value /= 100f;

                    switch (statModifier.affectedStat)
                    {
                        case Effect.AffectedStat.HP:
                            if (isPercentage)
                                enemy.mHealth *= 1 + value;
                            else
                                enemy.mHealth += value;
                            break;

                        case Effect.AffectedStat.MSPD:
                            if (isPercentage)
                                enemy.b_moveSpeed *= 1 + value;
                            else
                                enemy.b_moveSpeed += value;
                            break;

                        case Effect.AffectedStat.ASPD:
                            enemy.ASPD += value;
                            break;

                        case Effect.AffectedStat.ATK:
                            if (isPercentage)
                                enemy.bAtk = (short)(enemy.bAtk * (1 + value));
                            else
                                enemy.bAtk += (short)value;
                            break;

                        case Effect.AffectedStat.DEF:
                            if (isPercentage)
                                enemy.bDef = (short)(enemy.bDef * (1 + value));
                            else
                                enemy.bDef += (short)value;
                            break;

                        case Effect.AffectedStat.RES:
                            if (isPercentage)
                                enemy.bRes = (short)(enemy.bRes * (1 + value));
                            else
                                enemy.bRes += (short)value;
                            break;

                        case Effect.AffectedStat.WEIGHT:
                            enemy.weight += (short)value;
                            break;
                    }
                }
            }
        }

        if (diffLevel >= (int)DiffType.EnemiesAlertBuff)
        {
            enemy.MoveToOverridePositionSpeedMultiplier += AlertMspdBonus;
            enemy.MoveToOverridePositionSpeedMultiplierJump += AlertMspdBonusJump;
        }

        if (diffLevel == (int)DiffType.EnemiesUncombatDRBuff_1)
        {
            enemy.DamageReductionOutsideCombat += UncombatDR_1;
        }
        else if (diffLevel >= (int)DiffType.EnemiesUncombatDRBuff_2)
        {
            enemy.DamageReductionOutsideCombat += UncombatDR_2;
        }

        if (diffLevel >= (int)DiffType.EnemiesStatusResistant
            && (enemy.attackPattern == EntityBase.AttackPattern.MELEE || enemy.attackPattern == EntityBase.AttackPattern.NONE))
            enemy.StatusResistTimer += 9999f;
    }
}