using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Effect;

public class UI_TiedWithEffect : MonoBehaviour
{
    [SerializeField] protected Image GlowPart;
    protected EntityBase Target;
    protected string Key;
    protected Dictionary<string, Effect> TargetDictionary;

    protected bool Initialized = false;
    public virtual void Inititialize(EntityBase target, string key, Dictionary<string, Effect> TargetDictionary)
        => StartCoroutine(WaitOneFrameThenInitialize(target, key, TargetDictionary));

    protected virtual IEnumerator WaitOneFrameThenInitialize(EntityBase target, string key, Dictionary<string, Effect> TargetDictionary)
    {
        yield return null;
        Target = target;
        Key = key;
        this.TargetDictionary = TargetDictionary;

        Initialized = true;
    }

    protected bool IsInEffect => 
        Target.IsAlive() 
        && TargetDictionary.ContainsKey(Key) 
        && TargetDictionary[Key].IsInEffect;
    protected virtual void Update()
    {
        if (!Initialized) return;
        if (!IsInEffect) Destroy(gameObject);
    }
}