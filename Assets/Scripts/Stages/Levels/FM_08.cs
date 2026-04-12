using UnityEngine;

public class FM_08 : StageManager
{
    [SerializeField] private GameObject CM_DisableSpawns, CM_EnableSpawns;

    public override void EnableChallengeMode()
    {
        base.EnableChallengeMode();
        if (CharacterPrefabsStorage.EnableChallengeMode)
        {
            Destroy(CM_DisableSpawns);
            CM_EnableSpawns.SetActive(true);
        }
        else
        {
            Destroy(CM_EnableSpawns);
            CM_DisableSpawns.SetActive(true);
        }
    }
}