using UnityEngine;
using UnityEngine.UI;

public class UI_SuperConduct : MonoBehaviour
{
    [SerializeField] Image GlowPart;
    EntityBase Target;
    string Key;

    bool Initialized = false;
    public void Inititialize(EntityBase target, string key)
    {
        Target = target;
        Key = key;

        if (!DebuffInEffect) Destroy(gameObject);
        else Initialized = true;
    }

    bool DebuffInEffect => Target.DefDebuffs.ContainsKey(Key) && Target.DefDebuffs[Key].IsInEffect;
    private void Update()
    {
        if (!Initialized) return;
        if (!DebuffInEffect) Destroy(gameObject);

        transform.position = Target.transform.position + Vector3.up * 50f;
    }
}