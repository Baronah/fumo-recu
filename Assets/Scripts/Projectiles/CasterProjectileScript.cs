using System;
using System.Collections.Generic;
using UnityEngine;
using static StageManager;

public class CasterProjectileScript : ProjectileScript
{
    [SerializeField] private AudioSource bounceSFX, enviSFX;
    private bool fieldExpertEnabled = false;

    private HashSet<EnvironmentType> contactedEnvironmentType = new();

    public override void ShootTowards(Vector3 targetPosition, ProjectileType projectileType, float ProjectileLifespan, bool useAbsoluteDirection = false, params Type[] enemy)
    {
        PlayerRanged player = ProjectileFirer ? ProjectileFirer.GetComponent<PlayerRanged>() : null;
        fieldExpertEnabled = player && player.Skills.Contains(SkillTree_Manager.SkillName.SPIRAL_FIELD_EXPERT);
        base.ShootTowards(targetPosition, projectileType, ProjectileLifespan, useAbsoluteDirection, enemy);
    }

    public override void ShootTowards(Vector3 targetPosition, EntityBase enemy, ProjectileType projectileType, float ProjectileLifespan, bool useAbsoluteDirection = false)
    {
        PlayerRanged player = ProjectileFirer ? ProjectileFirer.GetComponent<PlayerRanged>() : null;
        fieldExpertEnabled = player && player.Skills.Contains(SkillTree_Manager.SkillName.SPIRAL_FIELD_EXPERT);
        base.ShootTowards(targetPosition, enemy, projectileType, ProjectileLifespan, useAbsoluteDirection);
    }

    short bounceCount = 0;
    public override void OnHitEvent(EntityBase target)
    {
        if (!target || target == excludedTarget) return;

        if (ProjectileFirer)
        {
            ApplyContactedEnvironmentalEffects(target);
        }

        bool resetOnHit = false;
        if (fieldExpertEnabled && bounceCount < 5)
        {
            resetOnHit = true;
            Bounce(target);
        }

        if (doesDamage)
        {
            ProjectileFirer.DealDamage(target, DamageInstance.PhysicalDamage, DamageInstance.MagicalDamage, DamageInstance.TrueDamage, true, this);
            if (!resetOnHit) allowingUpdate = false;
        }

        if (displayMsg != string.Empty) ProjectileFirer.DisplayDamage(displayMsg, msgDisplayOffset);

        if (!resetOnHit)
        {
            gameObject.SetActive(false);
            Destroy(this.gameObject, 0.5f);
        }
    }

    bool appliedDamage = false;
    void ApplyContactedEnvironmentalEffects(EntityBase target)
    {
        bool ragingTerrain = CharacterPrefabsStorage.Skills.ContainsKey(SkillTree_Manager.SkillName.TERRAIN);

        foreach (var contactedEnvironmentType in contactedEnvironmentType)
        {
            float value;
            switch (contactedEnvironmentType)
            {
                case EnvironmentType.ORIGINIUM_TILE:
                    if (appliedDamage) break;

                    value = 0.3f;
                    if (ragingTerrain) value *= 2;

                    DamageInstance.TrueDamage += (int)(Mathf.Max(1, DamageInstance.TotalDamage * value));
                    appliedDamage = true;
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

                    target.ApplyEffect(Effect.AffectedStat.ARNG, "DARK_ZONE_CASTER_HIT_DEBUFF", -99f, value, true);
                    break;
            }
        }
    }

    void Bounce(EntityBase initTarget)
    {
        bounceCount++;

        Collider2D col = initTarget.selfColliders[0];

        Vector2 contact = col.ClosestPoint(rb2d.position);
        Vector2 normal = (rb2d.position - contact).normalized;
        if (normal.magnitude < 0.1f)
        {
            normal = -rb2d.velocity.normalized;
        }

        Vector2 reflected = Vector2.Reflect(rb2d.velocity, normal).normalized;

        excludedTarget = initTarget;
        Acceleration = 0;

        ShootTowards(reflected, ProjectileType.CATCH_FIRST_TARGET_OF_TYPE, Mathf.Max(Lifespan, 5f), true, initTarget.GetGenericType());
        if (bounceSFX && b_audioPlayLockout <= 0f)
        {
            bounceSFX.Play();
            b_audioPlayLockout = 0.5f;
        }
    }

    private static float b_audioPlayLockout = 0f, e_audioPlayLockout = 0f;
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (b_audioPlayLockout > 0) b_audioPlayLockout -= Time.fixedUnscaledDeltaTime;
        if (e_audioPlayLockout > 0) e_audioPlayLockout -= Time.fixedUnscaledDeltaTime;
    }

    public override void HandleHit(GameObject other)
    {
        if (!allowingUpdate) return;

        bool checkForEnvironmentalTile = fieldExpertEnabled;
        if (checkForEnvironmentalTile)
        {
            EnvironmentalTileBase environmentalTileBase = other.GetComponent<EnvironmentalTileBase>();
            if (environmentalTileBase) AddContactedEnvironmental(environmentalTileBase.GetEnvironmentType());
        }

        base.HandleHit(other);
    }

    SpriteRenderer spriteRenderer;
    void AddContactedEnvironmental(EnvironmentType environmentType)
    {
        if (!fieldExpertEnabled) return;
        if (contactedEnvironmentType.Contains(environmentType)) return;
        
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();

        contactedEnvironmentType.Add(environmentType);

        Color finalColor = Color.black;

        foreach (var envType in contactedEnvironmentType)
        {
            finalColor += envType switch
            {
                EnvironmentType.ORIGINIUM_TILE => new(0.72f, 0, 0),
                EnvironmentType.MEDICAL_TILE => new(0, 0.72f, 0.13f),
                EnvironmentType.HEAT_PUMP_VENT => Color.yellow,
                EnvironmentType.DARK_ZONE => Color.black,
                _ => Color.white,
            };
        }

        finalColor /= contactedEnvironmentType.Count;
        spriteRenderer.color = finalColor;

        var trail = GetComponent<TrailRenderer>();

        Color finalTrailColor = Color.black;

        foreach (var envType in contactedEnvironmentType)
        {
            finalTrailColor += envType switch
            {
                EnvironmentType.ORIGINIUM_TILE => new(0.72f, 0, 0),
                EnvironmentType.MEDICAL_TILE => new(0, 0.72f, 0.13f),
                EnvironmentType.HEAT_PUMP_VENT => Color.yellow,
                EnvironmentType.DARK_ZONE => Color.black,
                _ => Color.white,
            };
        }

        finalTrailColor /= contactedEnvironmentType.Count;

        trail.startColor = new Color(
            finalTrailColor.r,
            finalTrailColor.g,
            finalTrailColor.b,
            0.7f
        );

        trail.endColor = new Color(
            trail.startColor.r,
            trail.startColor.g,
            trail.startColor.b,
            0f
        );

        trail.enabled = true;

        if (enviSFX && e_audioPlayLockout <= 0f)
        {
            enviSFX.Play();
            e_audioPlayLockout = 0.5f;
        }
    }
}