using UnityEngine;

public class FM_00 : StageManager
{
    [SerializeField] private GameObject[] CM_EnableObjs, CM_DisableObjs;

    public override void EnableChallengeMode()
    {
        if (!CharacterPrefabsStorage.EnableChallengeMode) return;
        base.EnableChallengeMode();
        foreach (GameObject obj in CM_EnableObjs) obj.SetActive(true);
        foreach (GameObject obj in CM_DisableObjs) obj.SetActive(false);
    }
}