using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ActiveOriginiumTile : MonoBehaviour
{
    [SerializeField] private float Interval = 1.0f;
    [SerializeField] private float TrueDamagePerTick = 15f;
    [SerializeField] private float EnemyDamageMultiplier = 1.0f;

    private List<EntityBase> entitiesWithin = new List<EntityBase>();

    private void Start()
    {
        StartCoroutine(DamageUnitsWithinRange());
    }

    IEnumerator DamageUnitsWithinRange()
    {
        while (true)
        {
            yield return new WaitForSeconds(Interval);

            entitiesWithin.ForEach(e =>
            {
                int damage = (int)(e as EnemyBase ? TrueDamagePerTick * EnemyDamageMultiplier : TrueDamagePerTick); 
                if (e && e.IsAlive())
                    e.TakeDamage(new(0, 0, damage), null);
            });
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision) return;

        EntityBase entityBase = collision.GetComponent<EntityBase>();
        if (!entityBase || !entityBase.IsAlive() || entitiesWithin.Contains(entityBase)) return;
    
        entitiesWithin.Add(entityBase);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision) return;

        EntityBase entityBase = collision.GetComponent<EntityBase>();
        if (!entityBase || !entityBase.IsAlive() || !entitiesWithin.Contains(entityBase)) return;

        entitiesWithin.Remove(entityBase);
    }
}