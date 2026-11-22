using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Effect;

public class UI_TiedWithEffect : MonoBehaviour
{

    [SerializeField] Image GlowPart;
    EntityBase Target;
    string Key;
    Dictionary<string, Effect> TargetDictionary;

    bool Initialized = false;
    public void Inititialize(EntityBase target, string key, Dictionary<string, Effect> TargetDictionary)
        => StartCoroutine(WaitOneFrameThenInitialize(target, key, TargetDictionary));

    IEnumerator WaitOneFrameThenInitialize(EntityBase target, string key, Dictionary<string, Effect> TargetDictionary)
    {
        yield return null;
        Target = target;
        Key = key;
        this.TargetDictionary = TargetDictionary;

        Initialized = true;
    }

    bool DebuffInEffect => Target.IsAlive() && TargetDictionary.ContainsKey(Key) && TargetDictionary[Key].IsInEffect;
    private void Update()
    {
        if (!Initialized) return;
        if (!DebuffInEffect) Destroy(gameObject);
    }
}