using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static OneDirectionalPassage;

public class Vent : EnvironmentalTileBase
{
    public float Strength = 1.0f, Duration = 0.2f;

    public TargetDirection EmitDirection;
    private readonly Dictionary<EntityBase, Collider2D> Entities = new();

    public override void OnStageStart()
    {
        Interval += Duration;
        base.OnStageStart();
    }

    public override IEnumerator HandleUnitWithinRange()
    {
        while (true)
        {
            Vector2 pushDirection = EmitDirection switch
            {
                TargetDirection.NONE => Vector2.zero,
                TargetDirection.UP => Vector2.up,
                TargetDirection.DOWN => Vector2.down,
                TargetDirection.LEFT => Vector2.left,
                TargetDirection.RIGHT => Vector2.right,
                _ => Vector2.zero
            };

            foreach (var pair in Entities)
            {
                var item = pair.Value;
                if (!item || item.isTrigger) continue;

                var entity = pair.Key;
                entity.PushEntityFrom(pair.Key, pushDirection, Strength, Duration, false);
                OnEntityStay(entity);
            }

            yield return new WaitForSeconds(Interval);
        }
    }

    public override void OnTriggerEnter2D(Collider2D collision)
    {
        EntityBase entityBase = collision.GetComponent<EntityBase>();
        if (!collision
                || collision.isTrigger
                    || !collision.gameObject 
                        || !entityBase
                            || Entities.ContainsKey(collision.GetComponent<EntityBase>())) return;
        
        Entities.Add(entityBase, collision);
        OnEntityEnter(entityBase);
    }

    public override void OnTriggerStay2D(Collider2D collision)
    {
        EntityBase entityBase = collision.GetComponent<EntityBase>();
        if (!collision
                || collision.isTrigger
                    || !collision.gameObject
                        || !entityBase
                            || Entities.ContainsKey(collision.GetComponent<EntityBase>())) return;

        Entities.Add(entityBase, collision);
    }

    public override void OnTriggerExit2D(Collider2D collision)
    {
        EntityBase entityBase = collision.GetComponent<EntityBase>();
        if (!collision
                || !collision.gameObject
                    || !entityBase
                        || !Entities.ContainsKey(collision.GetComponent<EntityBase>())) return;

        Entities.Remove(entityBase);
        OnEntityExit(entityBase);
    }
}