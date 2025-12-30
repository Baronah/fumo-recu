using System.Linq;
using UnityEngine;

public class SaveDataManager
{
    public static int Fumos => PlayerPrefs.GetInt("Fumo", 0);
    public static int AllTimeFumos => PlayerPrefs.GetInt("AllTimeFumo", 0);
    public static bool IsResearchUnlocked => PlayerPrefs.GetInt("ResearchIntroPlayed", 0) != 0;
    public static bool AllResearchesUnlocked 
        => PlayerPrefs.GetInt("TechsUnlocked", 0) != 0 
        && PlayerPrefs.GetInt("SpecsUnlocked", 0) != 0 
        && PlayerPrefs.GetInt("SensesUnlocked", 0) != 0;
    public static bool UseDVDTittleSettings => PlayerPrefs.GetInt("DVDTitle", 1) != 0;
    public static bool HasMintInTitle => PlayerPrefs.GetInt("MintInTitle", 1) != 0;
    public static void SetDdTitleSettings(bool v, GameObject Title)
    {
        PlayerPrefs.SetInt("DVDTitle", v ? 1 : 0);
        Title.GetComponent<DVDLogo>().enabled = v;
    }

    public static void SetMintInTitle(bool v, GameObject Mint)
    {
        PlayerPrefs.SetInt("MintInTitle", v ? 1 : 0);
        Mint.SetActive(v);
    }

    public static bool IsEligibleForTechUnlock()
    {
        string[] CompletedLevels = PlayerPrefs.GetString("CompletedLevels", "").Split(' ');
        return GetAllTimeFumo() >= 4 && CompletedLevels.Any(c => c.EndsWith("_CM"));
    }

    public static int GetAllTimeFumo()
    {
        int aFumo = 0;
        string[] CompletedLevels = PlayerPrefs.GetString("CompletedLevels", "").Split(' ');
        foreach (var level in CompletedLevels)
        {
            if (level.StartsWith("FM-"))
                aFumo++;
            if (level.EndsWith("_CM"))
                aFumo++;
        }

        return aFumo;
    }
}