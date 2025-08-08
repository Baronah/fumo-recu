using UnityEngine;

public class FM_00 : StageManager
{
    [SerializeField] private GameObject[] CM_EnableObjs, CM_DisableObjs;

    public override void EnableChallengeMode()
    {
        base.EnableChallengeMode();
        foreach (GameObject obj in CM_EnableObjs) obj.SetActive(true);
        foreach (GameObject obj in CM_DisableObjs) obj.SetActive(false);
    }
}