using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static StageManager;

public class EnvironmentalTileBase : MonoBehaviour
{
    [SerializeField] protected float Interval = 1.0f;
    protected List<EntityBase> entitiesWithin = new List<EntityBase>();

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

            entitiesWithin.ForEach(e =>
            {
                OnEntityStay(e);
            });
        }
    }

    public virtual void OnEntityEnter(EntityBase entity)
    {
        entitiesWithin.Add(entity);
        if (entity is PlayerBase pb)
        {
            if (pb.Skills.Contains(SkillTree_Manager.SkillName.GEOGOLIST_B))
            {
                pb.ApplyEffect(Effect.AffectedStat.ATK, "GEOGOLIST_ATK_BUFF", 50, Interval + 0.1f, true);
            }
            else if (pb.Skills.Contains(SkillTree_Manager.SkillName.GEOGOLIST_C))
            {
                pb.ApplyEffect(Effect.AffectedStat.MSPD, "GEOGOLIST_MSPD_BUFF", 50, Interval + 0.1f, true);
            }
        }
    }

    public virtual void OnEntityStay(EntityBase entity)
    {
        if (entity is PlayerBase pb)
        {
            if (pb.Skills.Contains(SkillTree_Manager.SkillName.GEOGOLIST_B))
            {
                pb.ApplyEffect(Effect.AffectedStat.ATK, "GEOGOLIST_ATK_BUFF", 50, Interval + 0.1f, true);
            }
            else if (pb.Skills.Contains(SkillTree_Manager.SkillName.GEOGOLIST_C))
            {
                pb.ApplyEffect(Effect.AffectedStat.MSPD, "GEOGOLIST_MSPD_BUFF", 50, Interval + 0.1f, true);
            }
        }
    }

    public virtual void OnEntityExit(EntityBase entity)
    {
        entitiesWithin.Remove(entity);
    }

    public virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision) return;

        EntityBase entityBase = collision.GetComponent<EntityBase>();
        if (!entityBase || !entityBase.IsAlive() || entitiesWithin.Contains(entityBase)) return;
        if (collision.isTrigger && collision.GetComponent<PlayerBase>()) return;

        OnEntityEnter(entityBase);
    }

    public virtual void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision) return;

        EntityBase entityBase = collision.GetComponent<EntityBase>();
        if (!entityBase || !entityBase.IsAlive() || entitiesWithin.Contains(entityBase)) return;
        if (collision.isTrigger && collision.GetComponent<PlayerBase>()) return;

        entitiesWithin.Add(entityBase);
    }

    public virtual void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision) return;

        EntityBase entityBase = collision.GetComponent<EntityBase>();
        if (!entityBase || !entityBase.IsAlive() || !entitiesWithin.Contains(entityBase)) return;
        if (collision.isTrigger && collision.GetComponent<PlayerBase>()) return;

        OnEntityExit(entityBase);
    }
}