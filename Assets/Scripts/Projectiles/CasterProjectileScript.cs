using System;
using UnityEngine;
using static StageManager;

public class CasterProjectileScript : ProjectileScript
{
    private bool hasHitEnvironmentalTile = false;
    private bool fieldExpertEnabled = false;

    private EnvironmentType contactedEnvironmentType;

    public override void ShootTowards(Vector3 targetPosition, ProjectileType projectileType, float ProjectileLifespan, params Type[] enemy)
    {
        PlayerRanged player = ProjectileFirer.GetComponent<PlayerRanged>();
        fieldExpertEnabled = player && player.Skills.Contains(SkillTree_Manager.SkillName.SPIRAL_FIELD_EXPERT);
        base.ShootTowards(targetPosition, projectileType, ProjectileLifespan, enemy);
    }

    public override void ShootTowards(Vector3 targetPosition, EntityBase enemy, ProjectileType projectileType, float ProjectileLifespan)
    {
        PlayerRanged player = ProjectileFirer.GetComponent<PlayerRanged>();
        fieldExpertEnabled = player && player.Skills.Contains(SkillTree_Manager.SkillName.SPIRAL_FIELD_EXPERT);
        base.ShootTowards(targetPosition, enemy, projectileType, ProjectileLifespan);
    }

    public override void OnHitEvent(EntityBase target)
    {
        if (ProjectileFirer)
        {
            bool ragingTerrain = CharacterPrefabsStorage.Skills.ContainsKey(SkillTree_Manager.SkillName.TERRAIN);
            float value;

            switch (contactedEnvironmentType)
            {
                case EnvironmentType.ORIGINIUM_TILE:
                    value = 0.3f;
                    if (ragingTerrain) value *= 2;

                    DamageInstance.TrueDamage += (int)(Mathf.Max(1, DamageInstance.TotalDamage * value));
                    break;

                case EnvironmentType.MEDICAL_TILE:
                    value = 0.04f;
                    if (ragingTerrain) value *= 2;

                    ProjectileFirer.Heal(ProjectileFirer.mHealth * value);
                    break;

                case EnvironmentType.HEAT_PUMP_VENT:
                    value = 1.5f;
                    if (ragingTerrain) value *= 2;

                    ProjectileFirer.PushEntityFrom(target, ProjectileFirer.GetAttackPosition(), value, 0.1f);
                    break;

                case EnvironmentType.DARK_ZONE:
                    value = 1.25f;
                    if (ragingTerrain) value *= 2;

                    target.ApplyEffect(Effect.AffectedStat.ARNG, "DARK_ZONE_CASTER_HIT_DEBUFF", -100f, value, true);
                    break;
            }
        }

        base.OnHitEvent(target);
    }

    public override void HandleHit(GameObject other)
    {
        if (!allowingUpdate) return;

        bool checkForEnvironmentalTile = !hasHitEnvironmentalTile && fieldExpertEnabled;
        if (checkForEnvironmentalTile)
        {
            EnvironmentalTileBase environmentalTileBase = other.GetComponent<EnvironmentalTileBase>();
            if (environmentalTileBase)
            {
                contactedEnvironmentType = environmentalTileBase.GetEnvironmentType();
                hasHitEnvironmentalTile = true;

                GetComponent<SpriteRenderer>().color = contactedEnvironmentType switch
                {
                    EnvironmentType.ORIGINIUM_TILE => new(0.72f, 0, 0),
                    EnvironmentType.MEDICAL_TILE => new(0, 0.72f, 0.13f),
                    EnvironmentType.HEAT_PUMP_VENT => Color.yellow,
                    EnvironmentType.DARK_ZONE => Color.black,
                    _ => Color.white,
                };

                var trail = GetComponent<TrailRenderer>();

                trail.startColor = contactedEnvironmentType switch
                {
                    EnvironmentType.ORIGINIUM_TILE => new(0.72f, 0, 0, 0.6f),
                    EnvironmentType.MEDICAL_TILE => new(0, 0.72f, 0.13f, 0.6f),
                    EnvironmentType.HEAT_PUMP_VENT => new(Color.yellow.r, Color.yellow.g, Color.yellow.b, 0.6f),
                    EnvironmentType.DARK_ZONE => new(Color.black.r, Color.black.g, Color.black.b, 0.6f),
                    _ => Color.white,
                };

                trail.endColor = new Color(
                    trail.startColor.r,
                    trail.startColor.g,
                    trail.startColor.b,
                    0f
                );

                trail.enabled = true;

                if (contactedEnvironmentType == EnvironmentType.HEAT_PUMP_VENT) TravelSpeed *= 1.35f;
            }
        }

        base.HandleHit(other);
    }
}