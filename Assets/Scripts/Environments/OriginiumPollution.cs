using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class OriginiumPollution : MonoBehaviour
{
    [SerializeField] private float Interval = 1.0f;
    [SerializeField] private float TrueDamagePerTick = 15f;
    [SerializeField] private float EnemyDamageMultiplier = 1.0f;

    private List<EntityBase> entitiesWithin = new List<EntityBase>();

    private void Start()
    {
        StartCoroutine(DamageUnitsWithinRange());
        StartCoroutine(Pulse());
    }

    IEnumerator Pulse()
    {
        Tilemap tilemap = GetComponent<Tilemap>();

        float duration = 2f;
        while (true)
        {
            float c = 0;

            while (c < duration)
            {
                tilemap.color = Color.Lerp(Color.white, Color.black, c * 1.0f / duration);
                c += Time.deltaTime;
                yield return null;
            }

            c = 0;
            while (c < duration)
            {
                tilemap.color = Color.Lerp(Color.black, Color.white, c * 1.0f / duration);
                c += Time.deltaTime;
                yield return null;
            }

            tilemap.color = Color.white;
        }
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
                {
                    if (e is OriginiumSpider os)
                    {
                        os.Pollute();
                        os.InstaKill();
                    }
                    else e.TakeDamage(new(0, 0, damage), null);
                }
            });
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision) return;

        EntityBase entityBase = collision.GetComponent<EntityBase>();
        if (!entityBase || !entityBase.IsAlive() || entitiesWithin.Contains(entityBase)) return;
        if (collision.isTrigger && collision.GetComponent<PlayerBase>()) return; 

        entitiesWithin.Add(entityBase);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision) return;

        EntityBase entityBase = collision.GetComponent<EntityBase>();
        if (!entityBase || !entityBase.IsAlive() || !entitiesWithin.Contains(entityBase)) return;
        if (collision.isTrigger && collision.GetComponent<PlayerBase>()) return;

        entitiesWithin.Remove(entityBase);
    }
}