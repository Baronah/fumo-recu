using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DarkTile : EnvironmentalTileBase
{
    [SerializeField] private GameObject DarkZoneEffectPrefab;
    private Dictionary<EntityBase, DarkZoneEffect> activeEffects = new Dictionary<EntityBase, DarkZoneEffect>();

    private Tilemap tilemap;

    [SerializeField] private float P_VisionReductionPercent = 0.4f;
    [SerializeField] private float E_VisionReductionPercent = 0.5f;

    private void Start()
    {
        tilemap = GetComponent<Tilemap>();
    }

    public override StageManager.EnvironmentType GetEnvironmentType()
    {
        return StageManager.EnvironmentType.DARK_ZONE;
    }

    bool IsFullyInsideTilemapColliders(Collider2D[] boxes)
    {
        BoxCollider2D box = boxes.FirstOrDefault(b => b.isTrigger) as BoxCollider2D;
        if (box == null) return false;

        // Get the bounds of the BoxCollider2D in world space
        Bounds bounds = box.bounds;

        // Get all four corners of the box
        Vector3[] corners = new Vector3[4]
        {
            new Vector3(bounds.min.x, bounds.min.y, 0), // Bottom-left
            new Vector3(bounds.max.x, bounds.min.y, 0), // Bottom-right
            new Vector3(bounds.min.x, bounds.max.y, 0), // Top-left
            new Vector3(bounds.max.x, bounds.max.y, 0)  // Top-right
        };

        // Check if all corners are inside tiles
        foreach (Vector3 corner in corners)
        {
            Vector3Int cellPosition = tilemap.WorldToCell(corner);

            // Check if there's a tile at this position
            if (!tilemap.HasTile(cellPosition))
            {
                return false; // One corner is not inside a tile
            }
        }

        return true; // All corners are inside tiles
    }

    public override void OnEntityEnter(EntityBase entity)
    {
        ApplyBlindnessEffect(entity);

        if (activeEffects.ContainsKey(entity))
        {
            activeEffects[entity].Initialize(entity);
        }
        else
        {
            DarkZoneEffect effectInstance = Instantiate(DarkZoneEffectPrefab, entity.transform.position + Vector3.up * 100f, Quaternion.identity).GetComponent<DarkZoneEffect>();
            effectInstance.Initialize(entity);
            activeEffects.Add(entity, effectInstance);
        }

        if (entity is HibernatorKnight h) h.OnShroudedZoneEnter();
        else if (entity is Gloompincer g) g.OnShroudedZoneEnter();

        base.OnEntityEnter(entity);
    }

    public override void OnEntityStay(EntityBase entity)
    {
        ApplyBlindnessEffect(entity);

        if (activeEffects.ContainsKey(entity) && activeEffects[entity])
        {
            activeEffects[entity].gameObject.SetActive(true);
        }

        base.OnEntityStay(entity);
    }

    void ApplyBlindnessEffect(EntityBase entity)
    {
        if (entity as Gloompincer) return;

        float percentage = entity is PlayerBase ? P_VisionReductionPercent : E_VisionReductionPercent;
        float minRange = entity.b_attackRange < 100 ? entity.b_attackRange : 100;

        if (entity.b_attackRange * percentage < minRange)
        {
            percentage = 1.0f - minRange / entity.b_attackRange;
        }

        entity.ApplyEffect(Effect.AffectedStat.ARNG, "DARK_TILE_VISION_REDUCTION", -percentage * 100f, 9999f, true);
    }

    public override void OnEntityExit(EntityBase entity)
    {
        base.OnEntityExit(entity);

        entity.RemoveEffect("DARK_TILE_VISION_REDUCTION");
        if (activeEffects.ContainsKey(entity))
        {
            activeEffects[entity].DisableEffect();
        }

        if (entity is HibernatorKnight h) h.OnShroudedZoneExit();
        else if (entity is Gloompincer g) g.OnShroudedZoneExit();
    }
}
