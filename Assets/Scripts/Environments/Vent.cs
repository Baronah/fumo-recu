using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static OneDirectionalPassage;

public class Vent : MonoBehaviour
{
    public float Strength = 1.0f, Duration = 0.2f, Interval = 2f;

    public TargetDirection EmitDirection;
    private readonly Dictionary<EntityBase, Collider2D> Entities = new();

    private void Start()
    {
        StartCoroutine(Blow());
    }

    IEnumerator Blow()
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

                pair.Key.PushEntityFrom(pair.Key, pushDirection, Strength, Duration, false);
            }

            yield return new WaitForSeconds(Interval + Duration);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision
                || collision.isTrigger
                    || !collision.gameObject 
                        || !collision.GetComponent<EntityBase>()
                            || Entities.ContainsKey(collision.GetComponent<EntityBase>())) return;
        
        Entities.Add(collision.GetComponent<EntityBase>(), collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision
                || collision.isTrigger
                    || !collision.gameObject
                        || !collision.GetComponent<EntityBase>()
                            || Entities.ContainsKey(collision.GetComponent<EntityBase>())) return;

        Entities.Add(collision.GetComponent<EntityBase>(), collision);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision
                || !collision.gameObject
                    || !collision.GetComponent<EntityBase>()
                        || !Entities.ContainsKey(collision.GetComponent<EntityBase>())) return;

        Entities.Remove(collision.GetComponent<EntityBase>());
    }
}