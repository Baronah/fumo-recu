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
            switch (contactedEnvironmentType)
            {
                case EnvironmentType.ORIGINIUM_TILE:
                    DamageInstance.TrueDamage += (int)(Mathf.Max(1, DamageInstance.TotalDamage * 0.35f));
                    break;

                case EnvironmentType.MEDICAL_TILE:
                    ProjectileFirer.Heal(ProjectileFirer.mHealth * 0.04f);
                    break;

                case EnvironmentType.HEAT_PUMP_VENT:
                    ProjectileFirer.PushEntityFrom(target, ProjectileFirer.GetAttackPosition(), 1f, 0.1f);
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
                    _ => Color.white,
                };

                var trail = GetComponent<TrailRenderer>();

                trail.startColor = contactedEnvironmentType switch
                {
                    EnvironmentType.ORIGINIUM_TILE => new(0.72f, 0, 0, 0.6f),
                    EnvironmentType.MEDICAL_TILE => new(0, 0.72f, 0.13f, 0.6f),
                    EnvironmentType.HEAT_PUMP_VENT => new(Color.yellow.r, Color.yellow.g, Color.yellow.b, 0.6f),
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