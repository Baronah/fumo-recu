using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FM_13 : StageManager
{
    [SerializeField] Transform PlayerInitSpawnPos, PlayerCMSpawnPos;

    public override void CheckForChallengeMode()
    {
        base.CheckForChallengeMode();
        if (CharacterPrefabsStorage.EnableChallengeMode) 
            PlayerInitSpawnPos.position = PlayerCMSpawnPos.position;
    }
}
