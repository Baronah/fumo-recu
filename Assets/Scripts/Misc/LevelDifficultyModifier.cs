using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelDifficultyModifier : MonoBehaviour
{
    [SerializeField] Button DiffUp, DiffDown;
    [SerializeField] TMP_Text ModifierText, ModifierDetail;
    [SerializeField] TMP_Text RecordText;
    [SerializeField] RectTransform Content, Background;

    private short MinDiff = 0;
    private short MaxDiff;

    public static int GetMaxDiff() => Enum.GetNames(typeof(DiffType)).Length;
    private short CurrentDifficultyLevel => CharacterPrefabsStorage.DifficultyLevel;

    bool IsObserver => CurrentDifficultyLevel <= 0;
    bool IsNormal => CurrentDifficultyLevel == 1;

    LevelDifficultyDataManager difficultyManager => LevelDifficultyDataManager._instance;

    private void Start()
    {
        if (DiffUp.onClick.GetPersistentEventCount() <= 0) DiffUp.onClick.AddListener(AddDiff);
        if (DiffDown.onClick.GetPersistentEventCount() <= 0) DiffDown.onClick.AddListener(LowerDiff);
        GetDifficulties();
    }

    public void GetDifficulties()
    {
        MaxDiff = (short)(SaveDataManager.AllResearchesUnlocked ? GetMaxDiff() : 1);
        SetDifficultyDescription();
    }

    public void SetRecordText(string levelName)
    {
        var (NM_Difficulty, CM_Difficulty) = SaveDataManager.GetLevelHighestDifficulty(levelName);

        string NM;
        if (NM_Difficulty == null) NM = "N/A";
        else if (NM_Difficulty <= 0) NM = "OB.";
        else if (NM_Difficulty == 1) NM = "EX.";
        else NM = $"RS.{NM_Difficulty - 1}";

        string CM;
        if (CM_Difficulty == null) CM = "N/A";
        else if (CM_Difficulty <= 0) CM = "OB.";
        else if (CM_Difficulty == 1) CM = "EX.";
        else CM = $"RS.{CM_Difficulty - 1}";

        RecordText.text =
            $"<size=48>RECORDS</size>\n\n" +
            $"<color=yellow>Normal:</color> <b>{NM}</b>" +
            $"\n<color=red>C.Mode:</color>  <b>{CM}</b>\n\n";
    }

    public void AdjustMaxDiffOnCMSelect(bool IsCM)
    {
        MinDiff = (short)(IsCM ? 1 : 0);

        CharacterPrefabsStorage.DifficultyLevel = (short) Mathf.Max(MinDiff,
            CurrentDifficultyLevel);
        SetDifficultyDescription();
    }

    public enum DiffType 
    {
        Observer = 0,
        Normal = 1,
        EnemiesUncombatDRBuff_1 = 2,
        EnemiesUncombatDRBuff_2 = 3,
        ExtraEnemies = 4,
        EnemiesLevelSpecificBuff = 5,
        EnemiesAlertBuff = 6,
        EnemiesDefCCBuff_1 = 7,
        EnemiesDefCCBuff_2 = 8,
        EnemiesStatusResistant = 9,
        EnemiesCombatBuff_1 = 10,
        EnemiesCombatBuff_2 = 11,
        PlayerCD_1 = 12, 
        PlayerCD_2 = 13,
        PlayerFieldDebuff_1 = 14, 
        PlayerFieldDebuff_2 = 15,
        Player1HP = 16,
    };

    public void SetDifficultyDescription()
    {
        DiffUp.interactable = CurrentDifficultyLevel < MaxDiff;
        DiffDown.interactable = CurrentDifficultyLevel > MinDiff;

        ModifierText.text = GetDifficultyName(out VertexGradient textGradientOut);
        ModifierText.colorGradient = textGradientOut;

        if (IsObserver)
        {
            ModifierDetail.text =
                $"- Your units have <color=green>+50% HP</color>, <color=#ff4545>+30% ATK</color> and their special and ultimate cooldowns are reduced by 40%.\n\n" +
                $"<color=yellow>- Completing a level in this difficulty will unlock the next level but will not unlock the Challenge Mode for this level.</color>\n\n" +
                $"- Observer is not available in Challenge Mode.";
        }
        else if (IsNormal)
        {
            ModifierDetail.text = "Standard gameplay, everything is normal here.";
        }
        else
        {
            int ConvertedDiffLevel = CurrentDifficultyLevel - 1;

            ModifierDetail.text =
                $"<i>The battle receives these following changes:</i>\n\n" +
                $"All enemies have their <color=green>HP</color> and <color=#ff4545>ATK</color> increased by " +
                $"<color=green>{Mathf.CeilToInt(difficultyManager.GetEnemiesHpMultiplier() * 100)}%</color> and " +
                $"<color=#ff4545>{Mathf.CeilToInt(difficultyManager.GetEnemiesAtkMultiplier() * 100)}%</color>, respectively.";

            if (ConvertedDiffLevel == (int)DiffType.EnemiesUncombatDRBuff_1)
                ModifierDetail.text += $"\n\nEnemies who are not in combat mode takes <color=yellow>{difficultyManager.UncombatDR_1 * 100}% less</color> physical and magical damage.";
            else if (ConvertedDiffLevel >= (int)DiffType.EnemiesUncombatDRBuff_2)
                ModifierDetail.text += $"\n\nEnemies who are not in combat mode takes <color=yellow>{difficultyManager.UncombatDR_2 * 100}% less</color> physical and magical damage.";

            if (ConvertedDiffLevel >= (int)DiffType.ExtraEnemies)
                ModifierDetail.text += "\n\nMore enemies will appear.";

            if (ConvertedDiffLevel >= (int)DiffType.EnemiesLevelSpecificBuff)
            {
                LevelDifficultyData data = difficultyManager.GetLevelDifficultyData(LevelSelectionScript._instance.SelectingLevel);
                if (data.StatModifiers.Length > 0)
                {
                    ModifierDetail.text += $"\n\n<color=yellow>[Varies]</color> All enemies have ";

                    string[] statsStr = new string[data.StatModifiers.Length];
                    for (int i = 0; i < data.StatModifiers.Length; i++)
                    {
                        statsStr[i] = PhraseStatToText(data.StatModifiers[i]);
                    }
                    ModifierDetail.text += string.Join(", ", statsStr) + ".";
                }
            }

            if (ConvertedDiffLevel >= (int)DiffType.EnemiesAlertBuff)
                ModifierDetail.text += $"\n\nWhen patrolling enemies are attacked, " +
                    $"they gain <color=yellow>+{difficultyManager.AlertMspdBonus * 100}% MSPD</color> when moving toward the source of the attack.";

            if (ConvertedDiffLevel == (int)DiffType.EnemiesDefCCBuff_1)
                ModifierDetail.text += 
                    $"\n\nEnemies gains <color=#ff9717>+{difficultyManager.CCDefenseFlatBuff_1}</color> <color=yellow>DEF</color> and <color=#00ffff>RES</color>. " +
                    $"This bonus falls off by <color=#ff9717>-{difficultyManager.CCDefenseFlatFallOff_1}</color> every time " +
                    $"they are inflicted by negative statuses, up to {difficultyManager.CCDefenseFlatBuff_1 / difficultyManager.CCDefenseFlatFallOff_1} times.";
            else if (ConvertedDiffLevel >= (int)DiffType.EnemiesDefCCBuff_2)
                ModifierDetail.text +=
                    $"\n\nEnemies gains <color=#ff9717>+{difficultyManager.CCDefenseFlatBuff_2}</color> <color=yellow>DEF</color> and <color=#00ffff>RES</color>. " +
                    $"This bonus falls off by <color=#ff9717>-{difficultyManager.CCDefenseFlatFallOff_2}</color> every time " +
                    $"they are inflicted by negative statuses, up to {difficultyManager.CCDefenseFlatBuff_2 / difficultyManager.CCDefenseFlatFallOff_2} times.";

            if (ConvertedDiffLevel >= (int)DiffType.EnemiesStatusResistant)
                ModifierDetail.text += $"\n\nMelee enemies gains <color=#ff00ff>status resistance</color>, which halves the durations of negative statuses on them.";

            if (ConvertedDiffLevel == (int)DiffType.EnemiesCombatBuff_1)
            {
                ModifierDetail.text += $"\n\nWhen enemy enters combat for the first time, " +
                    $"they gains <color=red>+{difficultyManager.CombatEnterAtk_1}% ATK</color> " +
                    $"and <color=#ffd61f>+{difficultyManager.CombatEnterAspd_1} ASPD</color>. " +
                    $"Effect lasts {difficultyManager.CombatEnterDuration_1} seconds.";
            }
            else if (ConvertedDiffLevel >= (int)DiffType.EnemiesCombatBuff_2)
                ModifierDetail.text += $"\n\nWhen enemy enters combat for the first time, " +
                    $"they gains <color=red>+{difficultyManager.CombatEnterAtk_2}% ATK</color>, " +
                    $"<color=#ffd61f>+{difficultyManager.CombatEnterAspd_2} ASPD</color> " +
                    $"and <color=#00ffbf>+{difficultyManager.CombatEnterMspd_2}% MSPD</color>. " +
                    $"Effect lasts {difficultyManager.CombatEnterDuration_2} seconds.";

            if (ConvertedDiffLevel == (int) DiffType.PlayerCD_1)
                ModifierDetail.text += $"\n\nYour units " +
                    $"special's CD <color=#ff9717>+{difficultyManager.PlayerCD_Sp_Extra_1 * 100}%</color>, " +
                    $"ultimate's CD <color=#ff9717>+{difficultyManager.PlayerCD_Ult_Extra_1 * 100}%</color>.";
            else if (ConvertedDiffLevel >= (int) DiffType.PlayerCD_2)
                ModifierDetail.text += $"\n\nYour units " +
                    $"special's CD <color=#ff9717>+{difficultyManager.PlayerCD_Sp_Extra_2 * 100}%</color>, " +
                    $"ultimate's CD <color=#ff9717>+{difficultyManager.PlayerCD_Ult_Extra_2 * 100}%.</color>";

            if (ConvertedDiffLevel == (int) DiffType.PlayerFieldDebuff_1)
                ModifierDetail.text += 
                    $"\n\nAfter staying on the field for {difficultyManager.PlayerFieldStayTriggerWaitTime_1} seconds, " +
                    $"your character gradually has <color=#5b7ccf>reduced ASPD</color>, " +
                    $"up to {difficultyManager.PlayerFieldStay_MaxAspdPenalty_1} " +
                    $"after {difficultyManager.PlayerFieldStay_MaxRampupTime_1} seconds (resets on swap).";
            else if (ConvertedDiffLevel >= (int) DiffType.PlayerFieldDebuff_2)
                ModifierDetail.text += 
                    $"\n\nAfter staying on the field for {difficultyManager.PlayerFieldStayTriggerWaitTime_2} seconds, " +
                    $"your character gradually has reduced <color=#5b7ccf>ASPD and MSPD</color>, " +
                    $"up to {difficultyManager.PlayerFieldStay_MaxAspdPenalty_2} " +
                    $"and {difficultyManager.PlayerFieldStay_MaxMspdPenalty_2}%, respectively, " +
                    $"after {difficultyManager.PlayerFieldStay_MaxRampupTime_2} seconds (resets on swap).";

            if (ConvertedDiffLevel >= (int) DiffType.Player1HP)
                ModifierDetail.text += 
                    $"\n\nUpon entering the stage, <color=red>your HP becomes 1</color>. " +
                    $"Afterward, healing effectiveness -{difficultyManager.Player_HealingEffectPenalty_Max * 100}%, " +
                    $"effect gradually expires over {difficultyManager.Player_HealingEffect_DecayAfter} seconds.";
        
            if (ConvertedDiffLevel >= (int) difficultyManager.MedalAwardMilestone)
                ModifierDetail.text += "\n\n<color=#00FFD5><i>Unveils something interesting when completed in Challenge Mode ;)</i></color>";
        }
    }

    string PhraseStatToText(StatModifier stat)
    {
        switch (stat.affectedStat)
        {
            case Effect.AffectedStat.HP:
                if (stat.valueType == Effect.ValueType.PERCENTAGE)
                    return $"<color=green>+{stat.Value}% HP</color>";
                else
                    return $"<color=green>+{stat.Value} HP</color>";
            
            case Effect.AffectedStat.ATK:
                if (stat.valueType == Effect.ValueType.PERCENTAGE)
                    return $"<color=red>+{stat.Value}% ATK</color>";
                else
                    return $"<color=red>+{stat.Value} ATK</color>";
            
            case Effect.AffectedStat.DEF:
                if (stat.valueType == Effect.ValueType.PERCENTAGE)
                    return $"<color=yellow>+{stat.Value}% DEF</color>";
                else
                    return $"<color=yellow>+{stat.Value} DEF</color>";

            case Effect.AffectedStat.RES:
                if (stat.valueType == Effect.ValueType.PERCENTAGE)
                    return $"<color=#00ffff>+{stat.Value}% RES</color>";
                else
                    return $"<color=#00ffff>+{stat.Value} RES</color>";

            case Effect.AffectedStat.ASPD:
                if (stat.valueType == Effect.ValueType.PERCENTAGE)
                    return $"<color=#ffd61f>+{stat.Value}% ASPD</color>";
                else
                    return $"<color=#ffd61f>+{stat.Value} ASPD</color>";

            case Effect.AffectedStat.MSPD:
                if (stat.valueType == Effect.ValueType.PERCENTAGE)
                    return $"<color=#00ffbf>+{stat.Value}% MSPD</color>";
                else
                    return $"<color=#00ffbf>+{stat.Value} MSPD</color>";

            case Effect.AffectedStat.ARNG:
                if (stat.valueType == Effect.ValueType.PERCENTAGE)
                    return $"<color=#ff00ff>+{stat.Value}% ARNG</color>";
                else
                    return $"<color=#ff00ff>+{stat.Value} ARNG</color>";

            case Effect.AffectedStat.WEIGHT:
                if (stat.valueType == Effect.ValueType.PERCENTAGE)
                    return $"<color=#ff00ff>+{stat.Value}% weight</color>";
                else
                    return $"<color=#ff00ff>+{stat.Value} weight</color>";

            default:
                if (stat.valueType == Effect.ValueType.PERCENTAGE)
                    return $"<color=#ff00ff>+{stat.Value}% {stat.affectedStat.ToString()}</color>";
                else
                    return $"<color=#ff00ff>+{stat.Value} {stat.affectedStat.ToString()}</color>";
        }
    }

    public void SetContentSize()
    {
        Canvas.ForceUpdateCanvases();
        Background.sizeDelta = new Vector2(Background.sizeDelta.x, Mathf.Max(150, Content.sizeDelta.y + 50f));
    }

    public void AddDiff()
    {
        if (CurrentDifficultyLevel >= MaxDiff) return;

        CharacterPrefabsStorage.DifficultyLevel++;
        SetDifficultyDescription();
    }

    public void LowerDiff()
    {
        if (CurrentDifficultyLevel <= MinDiff) return;

        CharacterPrefabsStorage.DifficultyLevel--;
        SetDifficultyDescription();
    }

    string GetDifficultyName(out VertexGradient textGradientOut)
    {
        int Diff = CurrentDifficultyLevel - 1;

        // default at yellow
        textGradientOut = new VertexGradient();
        textGradientOut.topLeft = textGradientOut.bottomLeft = Color.yellow;
        textGradientOut.topRight = textGradientOut.bottomRight = new(0.76f, 0.63f, 0.22f);

        if (IsObserver)
        {
            textGradientOut.topLeft = textGradientOut.bottomLeft = new (0, 1, 0.685f);
            textGradientOut.topRight = textGradientOut.bottomRight = new(0.46f, 0.75f, 0.54f);
            return "Observer";
        }

        if (IsNormal)
        {
            textGradientOut.topLeft = textGradientOut.bottomLeft = new Color(1, 0.97f, 0.53f);
            textGradientOut.topRight = textGradientOut.bottomRight = new(0.8f, 0.8f, 0.05f);
            return "Explorer";
        }

        string DefaultText = $"Researcher\nLV. {Diff}";

        if (Diff >= (int) DiffType.PlayerCD_1)
        {
            textGradientOut.topLeft = textGradientOut.bottomLeft = Color.red;
            textGradientOut.topRight = textGradientOut.bottomRight = new(0.62f, 0.28f, 0.28f);
        }
        else if (Diff >= (int) DiffType.ExtraEnemies)
        {
            textGradientOut.topLeft = textGradientOut.bottomLeft = new(1, 0.565f, 0);
            textGradientOut.topRight = textGradientOut.bottomRight = new(0.76f, 0.63f, 0.22f);
        }

        return DefaultText;
    }
}