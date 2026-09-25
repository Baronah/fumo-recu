using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static StageManager;

public class EnvironmentalTileBase : MonoBehaviour
{
    [SerializeField] protected float Interval = 1.0f;
    
    protected List<EntityBase> entitiesWithin = new List<EntityBase>();
    List<EntityBase> entitiesWithinClone;

    public virtual float GetValue() => 0;

    public virtual EnvironmentType GetEnvironmentType() => EnvironmentType.KEYS;

    private void Start()
    {
        OnStageStart();
    }

    public virtual void OnStageStart()
    {
        StartCoroutine(HandleUnitWithinRange());
    }

    public virtual IEnumerator HandleUnitWithinRange()
    {
        while (true)
        {
            yield return new WaitForSeconds(Interval);
            ProcessTick();
        }
    }

    public virtual void ProcessTick()
    {
        entitiesWithin.RemoveAll(e => !e || !e.IsAlive());

        entitiesWithinClone = new(entitiesWithin);
        entitiesWithinClone.ForEach(e =>
        {
            OnEntityStay(e);
        });
    }

    public virtual void OnEntityEnter(EntityBase entity)
    {
        entity.AddEnvironmentalTilesThisUnitStandingOn(GetEnvironmentType());
        entitiesWithin.Add(entity);
    }

    public virtual void OnEntityStay(EntityBase entity)
    {
        entity.AddEnvironmentalTilesThisUnitStandingOn(GetEnvironmentType());
        if (!entitiesWithin.Contains(entity)) entitiesWithin.Add(entity);
    }

    public virtual void OnEntityExit(EntityBase entity)
    {
        entity.RemoveEnvironmentalTilesThisUnitStandingOn(GetEnvironmentType());
        entitiesWithin.Remove(entity);
    }

    public void OnTriggerEnter2D(Collider2D collision)
        => OnEnter(collision);

    private void OnCollisionEnter2D(Collision2D collision)
        => OnEnter(collision.collider);

    public virtual void OnEnter(Collider2D collision)
    {
        if (!collision) return;

        EntityBase entityBase = collision.GetComponent<EntityBase>();
        if (!entityBase || !entityBase.IsAlive() || entitiesWithin.Contains(entityBase)) return;
        if (collision.isTrigger) return;

        OnEntityEnter(entityBase);
    }

    public void OnTriggerStay2D(Collider2D collision)
        => OnStay(collision);

    public void OnCollisionStay2D(Collision2D collision)
        => OnStay(collision.collider);

    public virtual void OnStay(Collider2D collision)
    {
        if (!collision) return;
        EntityBase entityBase = collision.GetComponent<EntityBase>();
        if (!entityBase || !entityBase.IsAlive() || entitiesWithin.Contains(entityBase)) return;
        if (collision.isTrigger) return;
        entitiesWithin.Add(entityBase);
    }

    public void OnTriggerExit2D(Collider2D collision)
        => OnExit(collision);

    public void OnCollisionExit2D(Collision2D collision)
        => OnExit(collision.collider);

    public virtual void OnExit(Collider2D collision)
    {
        if (!collision) return;
        EntityBase entityBase = collision.GetComponent<EntityBase>();
        if (!entityBase || !entityBase.IsAlive() || !entitiesWithin.Contains(entityBase)) return;
        if (collision.isTrigger) return;
        OnEntityExit(entityBase);
    }
}