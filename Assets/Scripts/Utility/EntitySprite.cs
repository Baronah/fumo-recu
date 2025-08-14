using UnityEngine;

public class EntitySprite : MonoBehaviour
{
    private EntityBase Parent;

    void Start()
    {
        Parent = GetComponentInParent<EntityBase>();
    }

    public void OnAttackComplete() => StartCoroutine(Parent.OnAttackComplete());
}