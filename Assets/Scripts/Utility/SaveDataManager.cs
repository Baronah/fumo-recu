using UnityEngine;

public class SaveDataManager
{
    public static int Fumos => PlayerPrefs.GetInt("Fumo", 0);
    public static int AllTimeFumos => PlayerPrefs.GetInt("AllTimeFumo", 0);
    public static bool IsResearchUnlocked => PlayerPrefs.GetInt("ResearchIntroPlayed", 0) != 0;
}