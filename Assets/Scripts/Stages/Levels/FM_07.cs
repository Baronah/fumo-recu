using System.Collections.Generic;
using UnityEngine;

public class FM_07 : StageManager
{
    [SerializeField] private List<GameObject> CM_ActiveSudaram, CM_InactiveSudaram;
    [SerializeField] private float PlayerBonusHP_Ratio = 0.25f;
    [SerializeField] private float PlayerBonusMSPD_Ratio = 0.3f;

    public override void EnableChallengeMode()
    {
        base.EnableChallengeMode();

        if (CharacterPrefabsStorage.EnableChallengeMode)
        {
            CM_ActiveSudaram.ForEach(g => g.SetActive(true));
            foreach (var i in CM_InactiveSudaram)
            {
                Destroy(i);
            }
        }
        else
        {
            foreach (var i in CM_ActiveSudaram)
            {
                Destroy(i);
            }
        }
    }

    public override void OnPlayerSpawn(PlayerBase player)
    {
        base.OnPlayerSpawn(player);
        player.b_moveSpeed += player.b_moveSpeed * PlayerBonusMSPD_Ratio;
        player.mHealth += (int)(player.mHealth * PlayerBonusHP_Ratio);
    }
}