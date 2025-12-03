using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarkTile : EnvironmentalTileBase
{
    [SerializeField] private GameObject DarkZoneEffectPrefab;
    private Dictionary<EntityBase, DarkZoneEffect> activeEffects = new Dictionary<EntityBase, DarkZoneEffect>();

    [SerializeField] private float P_VisionReductionPercent = 0.4f;
    [SerializeField] private float E_VisionReductionPercent = 0.5f;

    public override StageManager.EnvironmentType GetEnvironmentType()
    {
        return StageManager.EnvironmentType.DARK_ZONE;
    }

    public override void OnEntityEnter(EntityBase entity)
    {
        base.OnEntityEnter(entity);

        float percentage = entity is PlayerBase ? P_VisionReductionPercent : E_VisionReductionPercent;
        float minRange = entity.b_attackRange < 100 ? entity.b_attackRange : 100;

        if (entity.b_attackRange * percentage < minRange)
        {
            percentage = 1.0f - minRange / entity.b_attackRange;
        }

        entity.ApplyEffect(Effect.AffectedStat.ARNG, "DARK_TILE_VISION_REDUCTION", -percentage * 100f, 9999f, true);

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
    }

    public override void OnEntityStay(EntityBase entity)
    {
        base.OnEntityStay(entity);


        float percentage = entity is PlayerBase ? P_VisionReductionPercent : E_VisionReductionPercent;
        float minRange = entity.b_attackRange < 100 ? entity.b_attackRange : 100;

        if (entity.b_attackRange * percentage < minRange)
        {
            percentage = 1.0f - minRange / entity.b_attackRange;
        }

        entity.ApplyEffect(Effect.AffectedStat.ARNG, "DARK_TILE_VISION_REDUCTION", -percentage * 100f, 9999f, true);

        if (activeEffects.ContainsKey(entity) && activeEffects[entity])
        {
            activeEffects[entity].gameObject.SetActive(true);
        }
    }

    public override void OnEntityExit(EntityBase entity)
    {
        base.OnEntityExit(entity);

        entity.RemoveEffect("DARK_TILE_VISION_REDUCTION");
        if (activeEffects.ContainsKey(entity))
        {
            activeEffects[entity].DisableEffect();
        }
    }
}
