using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelDifficultyModifier : MonoBehaviour
{
    [SerializeField] Button DiffUp, DiffDown;
    [SerializeField] TMP_Text ModifierText, ModifierDetail;
    [SerializeField] RectTransform Content, Background;
    
    private short MinDiff = 0, MaxDiff = 16;

    private short CurrentDifficultyLevel => CharacterPrefabsStorage.DifficultyLevel;

    bool IsObserver => CurrentDifficultyLevel <= 0;
    bool IsNormal => CurrentDifficultyLevel == 1;

    private void Start()
    {
        MaxDiff = (short)(SaveDataManager.AllResearchesUnlocked ? 16 : 1);
        DiffUp.onClick.AddListener(AddDiff);
        DiffDown.onClick.AddListener(LowerDiff);
        SetDifficultyDescription();
    }

    [SerializeField] TMP_Text RecordText;
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
            $"<size=48>Records</size>\n\n" +
            $"<color=yellow>Normal:</color> <b>{NM}</b>" +
            $"\n<color=red>C.Mode:</color>  <b>{CM}</b>";
    }

    public void AdjustMaxDiffOnCMSelect(bool IsCM)
    {
        MinDiff = (short)(IsCM ? 1 : 0);

        CharacterPrefabsStorage.DifficultyLevel = (short) Mathf.Max(MinDiff, CurrentDifficultyLevel);
        SetDifficultyDescription();
    }

    public void SetDifficultyDescription()
    {
        DiffUp.interactable = CurrentDifficultyLevel < MaxDiff;
        DiffDown.interactable = CurrentDifficultyLevel > MinDiff;

        ModifierText.text = GetDifficultyName(out VertexGradient textGradientOut);
        ModifierText.colorGradient = textGradientOut;

        if (IsObserver)
        {
            ModifierDetail.text =
                $"- Your units have +100% HP, +50% ATK and their special and ultimate cooldowns are reduced by 40%.\n\n" +
                $"<color=yellow>- Completing a level in this difficulty will unlock the preceding level but will not unlock the Challenge Mode for this level.</color>\n\n" +
                $"- Observer is not available in Challenge Modes.";
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
                $"<color=green>{Mathf.CeilToInt(CharacterPrefabsStorage.GetEnemiesHpMultiplier() * 100)}%</color> and " +
                $"<color=#ff4545>{Mathf.CeilToInt(CharacterPrefabsStorage.GetEnemiesAtkMultiplier() * 100)}%</color>, respectively.";

            if (ConvertedDiffLevel >= 4)
                ModifierDetail.text += 
                    "\n\nAll enemies additionally have <color=yellow>+10 DEF</color> " +
                    "and <color=#00ffff>+4 RES</color>, quadrupled in <color=yellow>survival and defense</color> mission.";

            if (ConvertedDiffLevel >= 5)
                ModifierDetail.text += 
                    "\n\nAll enemies additionally have <color=#00ffbf>+5% MSPD</color> and <color=#ffd61f>+5 ASPD</color>, " +
                    "quadrupled in <color=#00ffbf>rescue</color> mission.";

            if (ConvertedDiffLevel >= 6)
                ModifierDetail.text += 
                    $"\n\nAll enemies additionally have <color=green>+5% HP</color> and <color=#ff4545>+5% ATK</color>, " +
                    $"quadrupled in <color=red>annihilation</color> mission.";

            if (ConvertedDiffLevel >= 7)
                ModifierDetail.text += $"\n\nEnemies who are not in combat mode takes <color=yellow>60% less</color> physical and magical damage.";
            
            if (ConvertedDiffLevel >= 8)
                ModifierDetail.text += $"\n\nMelee enemies gains <color=#ff00ff>status resistance</color>, which halves the durations of negative statuses on them.";

            if (ConvertedDiffLevel == 9)
                ModifierDetail.text += "\n\nYour units special's CD <color=#ff9717>+50%</color>, ultimate's CD <color=#ff9717>+25%</color>.";
            else if (ConvertedDiffLevel >= 10)
                ModifierDetail.text += "\n\nYour units special's CD <color=#ff9717>+100%</color>, ultimate's CD <color=#ff9717>+50%.</color>";

            if (ConvertedDiffLevel == 11)
                ModifierDetail.text += "\n\nSwap cool-down increased by <color=#ff9717>50%</color>.";
            else if (CurrentDifficultyLevel >= 12)
                ModifierDetail.text += "\n\nSwap cool-down increased by <color=#ff9717>100%</color>.";

            if (ConvertedDiffLevel == 13)
                ModifierDetail.text += "\n\nAfter staying on the field for 10 seconds, your character gradually has <color=#5b7ccf>reduced ATK</color>, up to 80% after 40 seconds (resets on swap).";
            else if (ConvertedDiffLevel >= 14)
                ModifierDetail.text += "\n\nWhile staying on the field, your character gradually has reduced <color=#5b7ccf>ATK and MSPD</color>, up to 80% and 50%, respectively, after 40 seconds (resets on swap).";

            if (ConvertedDiffLevel >= 15)
                ModifierDetail.text += "\n\nUpon entering the stage, <color=red>your HP becomes 1</color>.";
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

        string DefaultText = $"Researcher\nLV. {CurrentDifficultyLevel - 1}";

        if (CurrentDifficultyLevel >= 11)
        {
            textGradientOut.topLeft = textGradientOut.bottomLeft = Color.red;
            textGradientOut.topRight = textGradientOut.bottomRight = new(0.62f, 0.28f, 0.28f);
        }
        else if (CurrentDifficultyLevel >= 6)
        {
            textGradientOut.topLeft = textGradientOut.bottomLeft = new(1, 0.565f, 0);
            textGradientOut.topRight = textGradientOut.bottomRight = new(0.76f, 0.63f, 0.22f);
        }

        return DefaultText;
    }
}