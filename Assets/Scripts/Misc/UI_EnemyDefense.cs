using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class UI_EnemyDefense : UI_TiedWithEffect
{
    [SerializeField] TMP_Text StackCnt;

    int amountPerCount;

    public void Inititialize(EntityBase target, string key, Dictionary<string, Effect> TargetDictionary, float amountPerCount)
        => StartCoroutine(WaitOneFrameThenInitialize(target, key, TargetDictionary, amountPerCount));

    protected IEnumerator WaitOneFrameThenInitialize(EntityBase target, string key, Dictionary<string, Effect> TargetDictionary, float amountPerCount)
    {
        yield return null;
        Target = target;
        Key = key;
        this.TargetDictionary = TargetDictionary;
        this.amountPerCount = (int) amountPerCount;

        Initialized = true;
    }

    short frmcnt = 0;
    protected override void Update()
    {
        if (!Initialized) return;
        if (!DebuffInEffect)
        {
            Destroy(gameObject);
            return;
        }

        frmcnt++;
        if (frmcnt < 5) return;
        frmcnt = 0;
        int count = (int)(TargetDictionary[Key].Value / amountPerCount);
        StackCnt.text = count.ToString();
    }
}