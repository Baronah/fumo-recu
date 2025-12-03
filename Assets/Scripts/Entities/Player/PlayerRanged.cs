using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerRanged : PlayerBase
{
    [SerializeField] GameObject ProjectileSkill_2, ProjectileSkill_3, ProjectileSkill_4;
    [SerializeField] private GameObject AttackRangeIndicator, Warning, SkillEffect, FreezeEffect, JuggEffect;

    [SerializeField] private Transform SkillPosition;
    [SerializeField] private float SkillCooldown = 30f;
    [SerializeField] private float SkillDuration = 7f;
    [SerializeField] private float Skill_DamageMulitplier = 0.25f;
    [SerializeField] private float Skill_AtkInterval = 0.25f;
    [SerializeField] private GameObject SkillBarObj;
    private Slider SkillBar;

    [SerializeField] private float FreezeRange = 800f;
    [SerializeField] private float FreezeDurationMin = 1f, FreezeDurationMax = 4f, MinDistanceForFreezeDuration = 150f;
    [SerializeField] private float FreezeCooldown = 11.5f;
    [SerializeField] private float FreezeCastDuration = 0.25f;

    [SerializeField] private GameObject IllusionPrefab;

    private bool CanUseSkill = true, IsSkillActive = false, CanUseFreeze = true;
    private RectTransform AttackRangeIndicatorRect;

    private EntityBase target;
    private float skillCurrentDuration;

    private bool Debut = false;

    public override void Start()
    {
        base.Start();
        AttackRangeIndicatorRect = AttackRangeIndicator.GetComponent<RectTransform>();
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        if (!IsAlive())
        {
            AttackRangeIndicator.SetActive(false);
        }

        if (AttackRangeIndicatorRect)
        {
            AttackRangeIndicatorRect.sizeDelta = new Vector2(
                attackRange * 2,
                attackRange * 2
            );
        }

        bool SkillActive = IsSkillActive && IsAlive();
        SkillEffect.SetActive(SkillActive);
        SkillBarObj.SetActive(SkillActive);
    }

    public override PlayerManager.PlayerType GetPlayerType()
    {
        return PlayerManager.PlayerType.RANGED;
    }

    public override void InitializeComponents()
    {
        SkillBar = SkillBarObj.GetComponentInChildren<Slider>();
        base.InitializeComponents();
    }

    public override void OnFieldSwapOut(PlayerBase swapInPlayer)
    {
        base.OnFieldSwapOut(swapInPlayer);

        if (!Skills.Contains(SkillTree_Manager.SkillName.SPIRAL_SHADOW) || !IsSkillActive) return;

        // inherits stats and skill duration and start casting skill on time for remaining duration
        GameObject o = Instantiate(IllusionPrefab, transform.position, Quaternion.identity);
        PlayerCasterIllusion playerCasterIllusion = o.GetComponent<PlayerCasterIllusion>();
        playerCasterIllusion.InitializeComponents();
        playerCasterIllusion.SetInherit(
            ATK: (short)(atk * 0.4f),
            maxDuration: SkillDuration,
            duration: skillCurrentDuration,
            Skill_DamageMulitplier, 
            Skill_AtkInterval, 
            flipX: spriteRenderer.flipX);
    }

    public override void FlipAttackPosition()
    {
        base.FlipAttackPosition();
        SkillPosition.localPosition = new Vector3(
            -SkillPosition.localPosition.x,
            SkillPosition.localPosition.y,
            SkillPosition.localPosition.z
        );

        SkillEffect.transform.localPosition = new Vector3(
            -SkillEffect.transform.localPosition.x,
            SkillEffect.transform.localPosition.y,
            SkillEffect.transform.localPosition.z
        );
    }

    public override void GetSkillTreeEffects()
    {
        base.GetSkillTreeEffects();
        if (Skills.Contains(SkillTree_Manager.SkillName.EQUIPMENT_RADIO))
        {
            FreezeCooldown *= 0.85f;
            SkillCooldown *= 0.85f;
        }

        if (Skills.Contains(SkillTree_Manager.SkillName.JUST_A_NICE_LOOKING_ROCK))
        {
            FreezeCooldown *= 0.948f;
            SkillCooldown *= 0.948f;
        }

        if (Skills.Contains(SkillTree_Manager.SkillName.FREEZE_ICEAGE))
            FreezeDurationMin += 1f;

        if (Skills.Contains(SkillTree_Manager.SkillName.WINDBLOW_SOUTH))
            FreezeDurationMin = Mathf.Max(FreezeDurationMin, 2f);

        if (Skills.Contains(SkillTree_Manager.SkillName.SPIRAL_MORE))
            Skill_DamageMulitplier *= 0.7f;
    }

    public override void OnFieldEnter()
    {
        base.OnFieldEnter();

        if (Skills.Contains(SkillTree_Manager.SkillName.MAJOR_DEBUT))
        {
            Debut = true;
            UseSpecial();
        }
    }

    protected override void GetControlInputs()
    {
        if (!IsAlive() || IsSkillActive) return;

        if (Input.GetKeyDown(InputManager.Instance.AttackKey))
        {
            AttackCoroutine = StartCoroutine(Attack());
        }
        else if (Input.GetKeyDown(InputManager.Instance.SkillKey))
        {
            UseSkill();
        }
        else if (Input.GetKeyDown(InputManager.Instance.SpecialKey))
        {
            UseSpecial();
        }
        else 
            Move();
    }

    public override void UseSkill()
    {
        if (!CanUseSkill) return;

        base.UseSkill();
        StartCoroutine(CastSkill());
    }

    public override void UseSpecial()
    {
        if (!CanUseFreeze) return;

        base.UseSpecial();
        StartCoroutine(CastFreeze());
    }

    IEnumerator SkillLockout()
    {
        CanUseSkill = false;
        StartCoroutine(playerManager.SkillCooldown(SkillCooldown));
        yield return new WaitForSeconds(SkillCooldown);
        CanUseSkill = true;
    }

    IEnumerator FreezeLockout(float d)
    {
        CanUseFreeze = false;
        StartCoroutine(playerManager.SpecialCooldown(d));
        yield return new WaitForSeconds(d);
        CanUseFreeze = true;
    }

    public override IEnumerator Attack()
    {
        if (!CanAttack || IsAttackLocked) yield break;

        AttackRangeIndicator.SetActive(true);
        target = SearchForNearestEntityAroundCertainPoint(typeof(EnemyBase), transform.position, attackRange);
        if (!target)
        {
            Warning.SetActive(true);
            yield return new WaitForSeconds(1f);
            Warning.SetActive(false);
            AttackRangeIndicator.SetActive(false);
            yield break;
        }

        MovementLockout = Mathf.Max(MovementLockout, GetWindupTime() * 1.5f);
        animator.SetBool("attack", true);
        LockoutMovementOnAttackCoroutine = StartCoroutine(LockoutMovementsOnAttack());

        yield return new WaitForSeconds(GetWindupTime());

        AttackRangeIndicator.SetActive(false);
        yield return null;
    }

    public override IEnumerator OnAttackComplete()
    {
        if (!target) yield break;

        if (sfxs[0]) sfxs[0].Play();

        int currentAtk = atk;
        float atkPostBonus = atk;
        if (Skills.Contains(SkillTree_Manager.SkillName.HEAVY_HITTER))
        {
            atkPostBonus *= GetHeavyHitterMultiplier();
            timerSinceLastAttack = 0f;
        }

        atk = (short)atkPostBonus;
        CreateProjectileAndShootToward(target, ProjectileType, ProjectileSpeed);
        target = null;

        atk = (short)currentAtk;
    }

    public IEnumerator CastFreeze()
    {
        if (!IsAlive() || !CanUseFreeze || IsFrozen || IsStunned) yield break;

        CanUseFreeze = false;

        StartCoroutine(StartMovementLockout(FreezeCastDuration));
        StartCoroutine(StartAttackLockout(FreezeCastDuration));
        
        animator.SetBool("attack", false);
        animator.SetTrigger("skill");
        IsSkillActive = true;

        if (sfxs[1]) sfxs[1].Play();

        Instantiate(FreezeEffect, SkillPosition.position, Quaternion.identity);
        yield return new WaitForSeconds(FreezeCastDuration - Time.fixedDeltaTime);

        Dictionary<EntityBase, float> InitialHitDictionary = new();
        var hitEnemies = SearchForEntitiesAroundCertainPoint(typeof(EnemyBase), SkillPosition.position, FreezeRange, true);
        
        foreach (EntityBase e in hitEnemies)
        {
            EnemyBase enemy = e as EnemyBase;
            float distance = Vector3.Distance(SkillPosition.position, enemy.transform.position);
            float freezeDuration = distance >= FreezeRange * 0.8f
                ?
                FreezeDurationMin
                : 
                Mathf.Lerp(FreezeDurationMin, FreezeDurationMax, MinDistanceForFreezeDuration * 1.0f / distance);
            ApplyFreeze(enemy, freezeDuration);

            if (Skills.Contains(SkillTree_Manager.SkillName.FREEZE_SUPERCONDUCT))
            {
                enemy.ApplyEffect(Effect.AffectedStat.DEF, "FREEZE_SUPERCONDUCT_DEF_DEBUFF", -50, freezeDuration, true);
                enemy.ApplyEffect(Effect.AffectedStat.RES, "FREEZE_SUPERCONDUCT_RES_DEBUFF", -50, freezeDuration, true);
            }

            if (Skills.Contains(SkillTree_Manager.SkillName.WINDBLOW_NORTH))
            {
                float pushDuration = distance >= FreezeRange * 0.8f
                    ?
                    0.1f
                    :
                    Mathf.Lerp(0.12f, 0.23f, MinDistanceForFreezeDuration * 1.0f / distance);

                PushEntityFrom(enemy, AttackPosition.transform, 1.5f, pushDuration);
            }
            else if (Skills.Contains(SkillTree_Manager.SkillName.WINDBLOW_SOUTH))
                PullEntityTowards(enemy, AttackPosition.transform, 2f, 0.25f);

            InitialHitDictionary.Add(e, freezeDuration);
        }

        float cooldown = Debut ? 0 : FreezeCooldown;
        if (Skills.Contains(SkillTree_Manager.SkillName.FREEZE_CHARGE))
        {
            for (int i = 1; i <= hitEnemies.Count; i++) cooldown *= 0.85f;
        }

        StartCoroutine(FreezeLockout(cooldown));
        Debut = false;

        animator.SetTrigger("skill_end");
        IsSkillActive = false;
        yield return null;

        Dictionary<EntityBase, float> HitDictionary = new(InitialHitDictionary);
        if (Skills.Contains(SkillTree_Manager.SkillName.FREEZE_ICEAGE))
        {
            while (HitDictionary.Count > 0)
            {
                yield return new WaitForSeconds(0.1f);
                if (sfxs[1]) sfxs[1].Play();
                yield return new WaitForSeconds(0.1f);

                Dictionary<EntityBase, float> HitThisRound = new();
                foreach (var pair in HitDictionary)
                {
                    EntityBase InitHitEnemyHit = pair.Key;
                    Instantiate(FreezeEffect, InitHitEnemyHit.transform.position, Quaternion.identity);

                    var nearbyHits = SearchForEntitiesAroundCertainPoint(typeof(EnemyBase), InitHitEnemyHit.transform.position, FreezeRange, true)
                                    .Where(s => !InitialHitDictionary.ContainsKey(s) && !HitDictionary.ContainsKey(s));

                    foreach (EntityBase nearby in nearbyHits)
                    {
                        EnemyBase enemy = nearby as EnemyBase;
                        float distance = Vector3.Distance(InitHitEnemyHit.transform.position, enemy.transform.position);
                        float freezeDuration = distance >= FreezeRange * 0.8f
                            ?
                            FreezeDurationMin
                            :
                            Mathf.Lerp(FreezeDurationMin, pair.Value, MinDistanceForFreezeDuration * 1.0f / distance);
                        ApplyFreeze(enemy, freezeDuration);
                        HitThisRound.Add(nearby, freezeDuration);
                        InitialHitDictionary.Add(nearby, freezeDuration);

                        if (Skills.Contains(SkillTree_Manager.SkillName.WINDBLOW_NORTH))
                        {
                            float pushDuration = distance >= FreezeRange * 0.8f
                                ?
                                0.1f
                                :
                                Mathf.Lerp(0.12f, 0.23f, MinDistanceForFreezeDuration * 1.0f / distance);

                            PushEntityFrom(enemy, InitHitEnemyHit.transform, 1.5f, pushDuration);
                        }
                        else if (Skills.Contains(SkillTree_Manager.SkillName.WINDBLOW_SOUTH))
                            PullEntityTowards(enemy, InitHitEnemyHit.transform, 2f, 0.25f);
                    }
                }

                HitDictionary = new(HitThisRound);
            }
        }
    }

    public IEnumerator CastSkill()
    {
        if (!IsAlive()) yield break;

        StartCoroutine(StartAttackLockout(SkillDuration));
        StartCoroutine(StartMovementLockout(SkillDuration));
        StartCoroutine(SkillLockout());

        animator.SetTrigger("skill");
        IsSkillActive = true;
        skillCurrentDuration = 0;
        flowerCount = 0;
        float angleOffset = 0;

        bool shootAdditionalBullets = Skills.Contains(SkillTree_Manager.SkillName.SPIRAL_MORE),
             homingOnFirstTarget = Skills.Contains(SkillTree_Manager.SkillName.SPIRAL_TRAVEL);

        EntityBase lockInTarget = homingOnFirstTarget 
            ? SearchForNearestEntityAroundSelf(typeof(EnemyBase))
            : null;

        SkillBar.maxValue = SkillBar.value = SkillDuration;
        float intervalCounter = Skill_AtkInterval;

        float lockInTargetDamageMul = 1.0f;
        int yellowBulletAngle = 90;

        while (skillCurrentDuration < SkillDuration)
        {
            SkillBar.value = SkillDuration - skillCurrentDuration;

            if (intervalCounter >= Skill_AtkInterval)
            {
                Vector3 sourcePosition = SkillPosition.position;
                intervalCounter = 0;
                if (sfxs[2]) sfxs[2].Play();

                float lifeSpan = 1.5f;
                float speed = ProjectileSpeed * 0.25f;
                for (int i = 0; i < 360; i += 30)
                {
                    float currentAngle = i + angleOffset;

                    float angleInRadians = currentAngle * Mathf.Deg2Rad;

                    float circleRadius = 30f + (skillCurrentDuration * 5f);
                    Vector3 targetPosition = new Vector3(
                        sourcePosition.x + Mathf.Cos(angleInRadians) * circleRadius,
                        sourcePosition.y + Mathf.Sin(angleInRadians) * circleRadius,
                        sourcePosition.z
                    );

                    if (lockInTarget && lockInTarget.IsAlive())
                    {
                        CreateProjectileAndShootToward(
                            lockInTarget,
                            new DamageInstance(0, (int)(atk * Skill_DamageMulitplier * lockInTargetDamageMul), 0),
                            sourcePosition,
                            projectileType: ProjectileScript.ProjectileType.HOMING_TO_SPECIFIC_TARGET
                            );

                        if (lockInTargetDamageMul > 0.2f) lockInTargetDamageMul -= 0.01f;
                    }
                    else
                    {
                        CreateProjectileAndShootToward(
                            ProjectilePrefab,
                            new DamageInstance(0, (int)(atk * Skill_DamageMulitplier), 0),
                            sourcePosition,
                            targetPosition,
                            projectileType: ProjectileScript.ProjectileType.CATCH_FIRST_TARGET_OF_TYPE,
                            travelSpeed: speed,
                            acceleration: speed,
                            lifeSpan: lifeSpan,
                            targetType: typeof(EnemyBase));
                    }

                    if (shootAdditionalBullets)
                    {
                        currentAngle = i - angleOffset - 15;
                        angleInRadians = currentAngle * Mathf.Deg2Rad;

                        targetPosition = new Vector3(
                            sourcePosition.x + Mathf.Cos(angleInRadians) * circleRadius,
                            sourcePosition.y + Mathf.Sin(angleInRadians) * circleRadius,
                            sourcePosition.z
                        );

                        CreateProjectileAndShootToward(
                            ProjectileSkill_2,
                            new DamageInstance(0, (int)(atk * Skill_DamageMulitplier), 0),
                            sourcePosition,
                            targetPosition,
                            projectileType: ProjectileScript.ProjectileType.CATCH_FIRST_TARGET_OF_TYPE,
                            travelSpeed: speed,
                            acceleration: speed,
                            lifeSpan: lifeSpan,
                            targetType: typeof(EnemyBase));
                    }
                }
                angleOffset += 6;

                if (shootAdditionalBullets)
                {
                    int angle = yellowBulletAngle;
                    float angleInRadians = angle * Mathf.Deg2Rad;
                    float circleRadius = 30f + (skillCurrentDuration * 5f);
                    Vector3 targetPosition = new Vector3(
                        sourcePosition.x + Mathf.Cos(angleInRadians) * circleRadius,
                        sourcePosition.y + Mathf.Sin(angleInRadians) * circleRadius,
                        sourcePosition.z
                    );

                    CreateProjectileAndShootToward(
                        ProjectileSkill_3,
                        new DamageInstance(0, (int)(atk * Skill_DamageMulitplier * 2), 0),
                        sourcePosition,
                        targetPosition,
                        projectileType: ProjectileScript.ProjectileType.CATCH_FIRST_TARGET_OF_TYPE,
                        travelSpeed: speed * 2,
                        acceleration: speed * 2,
                        lifeSpan: lifeSpan * 0.6f,
                        targetType: typeof(EnemyBase));

                    angle *= -1;
                    angleInRadians = angle * Mathf.Deg2Rad;
                    targetPosition = new Vector3(
                            sourcePosition.x + Mathf.Cos(angleInRadians) * circleRadius,
                            sourcePosition.y + Mathf.Sin(angleInRadians) * circleRadius,
                            sourcePosition.z
                        );

                    CreateProjectileAndShootToward(
                        ProjectileSkill_3,
                        new DamageInstance(0, (int)(atk * Skill_DamageMulitplier * 2), 0),
                        sourcePosition,
                        targetPosition,
                        projectileType: ProjectileScript.ProjectileType.CATCH_FIRST_TARGET_OF_TYPE,
                        travelSpeed: speed * 2,
                        acceleration: speed * 2,
                        lifeSpan: lifeSpan * 0.6f,
                        targetType: typeof(EnemyBase));

                    angle += 180;
                    angleInRadians = angle * Mathf.Deg2Rad;
                    targetPosition = new Vector3(
                        sourcePosition.x + Mathf.Cos(angleInRadians) * circleRadius,
                        sourcePosition.y + Mathf.Sin(angleInRadians) * circleRadius,
                        sourcePosition.z
                    );

                    CreateProjectileAndShootToward(
                        ProjectileSkill_3,
                        new DamageInstance(0, (int)(atk * Skill_DamageMulitplier * 2), 0),
                        sourcePosition,
                        targetPosition,
                        projectileType: ProjectileScript.ProjectileType.CATCH_FIRST_TARGET_OF_TYPE,
                        travelSpeed: speed * 2,
                        acceleration: speed * 2,
                        lifeSpan: lifeSpan * 0.6f,
                        targetType: typeof(EnemyBase));

                    angle *= -1;
                    angleInRadians = angle * Mathf.Deg2Rad;
                    targetPosition = new Vector3(
                            sourcePosition.x + Mathf.Cos(angleInRadians) * circleRadius,
                            sourcePosition.y + Mathf.Sin(angleInRadians) * circleRadius,
                            sourcePosition.z
                        );

                    CreateProjectileAndShootToward(
                        ProjectileSkill_3,
                        new DamageInstance(0, (int)(atk * Skill_DamageMulitplier * 2), 0),
                        sourcePosition,
                        targetPosition,
                        projectileType: ProjectileScript.ProjectileType.CATCH_FIRST_TARGET_OF_TYPE,
                        travelSpeed: speed * 2,
                        acceleration: speed * 2,
                        lifeSpan: lifeSpan * 0.6f,
                        targetType: typeof(EnemyBase));

                    yellowBulletAngle += 12;
                }
            }
            
            yield return null;
            skillCurrentDuration += Time.deltaTime;
            intervalCounter += Time.deltaTime;
        }

        animator.SetTrigger("skill_end");
        IsSkillActive = false;
        skillCurrentDuration = SkillDuration;
        yield return null;
    }

    short flowerCount = 0;
    public override void DealDamage(EntityBase target, int pDmg, int mDmg, int tDmg, bool allowWhenDisabled = false)
    {
        base.DealDamage(target, pDmg, mDmg, tDmg, allowWhenDisabled);
        if (IsSkillActive 
            && Skills.Contains(SkillTree_Manager.SkillName.SPIRAL_PHANTOM) 
            && !target.IsAlive()
            && flowerCount <= 3)
        {
            StartCoroutine(CreateExtraFlowers(target.transform.position, SkillDuration - skillCurrentDuration));
            flowerCount++;
        }
    }

    public IEnumerator CreateExtraFlowers(Vector3 position, float duration)
    {
        float angleOffset = 0;

        float intervalCounter = Skill_AtkInterval;
        float count = 0;
        duration = Mathf.Clamp(duration, 0.5f, 1.5f);

        GameObject E_SkillEffect = Instantiate(SkillEffect, position, Quaternion.identity);
        E_SkillEffect.GetComponent<SpriteRenderer>().color = Color.green;
        E_SkillEffect.transform.localScale = new(30, 30, 30);
        E_SkillEffect.SetActive(true);
        Destroy(E_SkillEffect, duration + 0.2f);

        yield return new WaitForSeconds(0.2f);

        while (count < duration)
        {
            if (intervalCounter >= Skill_AtkInterval)
            {
                Vector3 sourcePosition = position;
                intervalCounter = 0;
                if (sfxs[2]) sfxs[2].Play();

                float lifeSpan = 1.5f;
                float speed = ProjectileSpeed * 0.25f;
                for (int i = 0; i < 360; i += 30)
                {
                    float currentAngle = (i + angleOffset) * -1;

                    float angleInRadians = currentAngle * Mathf.Deg2Rad;

                    float circleRadius = 30f + (count * 5f);
                    Vector3 targetPosition = new Vector3(
                        sourcePosition.x + Mathf.Cos(angleInRadians) * circleRadius,
                        sourcePosition.y + Mathf.Sin(angleInRadians) * circleRadius,
                        sourcePosition.z
                    );

                    CreateProjectileAndShootToward(
                        ProjectileSkill_4,
                        new DamageInstance(0, (int)(atk * Skill_DamageMulitplier * 0.7f), 0),
                        sourcePosition,
                        targetPosition,
                        projectileType: ProjectileScript.ProjectileType.CATCH_FIRST_TARGET_OF_TYPE,
                        travelSpeed: speed,
                        acceleration: speed,
                        lifeSpan: lifeSpan,
                        targetType: typeof(EnemyBase));
                }
                angleOffset += 6;
            }

            yield return null;
            count += Time.deltaTime;
            intervalCounter += Time.deltaTime;
        }
    }

    private float BurstHeal_HpPercentage = 0.35f;
    private float HealPerSecond_HpPercentage = 0.05f;
    private float DefBoost = 0.5f;
    private float ResBoost = 10;
    private float AtkBoost = 0.25f;
    private float SpeedBoost = 0.35f;
    public void SetJuggernauntInherit(float duration, float BurstHeal_HpPercentage, float HPS_Percentage, float DefBoost, float ResBoost, float AtkBoost, float SpeedBoost)
    {
        this.BurstHeal_HpPercentage = BurstHeal_HpPercentage;
        this.HealPerSecond_HpPercentage = HPS_Percentage;
        this.DefBoost = DefBoost;
        this.ResBoost = ResBoost;
        this.AtkBoost = AtkBoost;
        this.SpeedBoost = SpeedBoost;
        StartCoroutine(ActivateJuggernaunt(duration + 2));
    }

    float juggernauntCurrentDuration = 0;
    private short atkAdd, defAdd, resAdd, speedAdd;
    protected IEnumerator ActivateJuggernaunt(float duration)
    {
        yield return new WaitUntil(() => IsComponentsInitialized);
        JuggEffect.SetActive(true);
        juggernauntCurrentDuration = 0;

        atkAdd = (short)(bAtk * AtkBoost);
        atk += atkAdd;
        defAdd = (short)(bDef * DefBoost);
        def += defAdd;
        resAdd = (short)(ResBoost);
        res += resAdd;
        speedAdd = (short)(b_moveSpeed * SpeedBoost);
        moveSpeed += speedAdd;

        float t = 1.0f, d = duration;

        while (juggernauntCurrentDuration < d)
        {
            juggernauntCurrentDuration += Time.deltaTime;
            t += Time.deltaTime;

            if (t >= 1.0f)
            {
                Heal(mHealth * HealPerSecond_HpPercentage);
                t = 0;
            }

            yield return null;
        }

        Heal(mHealth * HealPerSecond_HpPercentage);
        atk -= atkAdd;
        def -= defAdd;
        res -= resAdd;
        moveSpeed -= speedAdd;
        JuggEffect.SetActive(false);
    }

    public override PlayerTooltipsInfo GetPlayerTooltipsInfo()
    {
        var info = base.GetPlayerTooltipsInfo();

        info.AttackText = $"Lauches a projectile toward the nearest enemy within range, " +
            $"dealing {atk} {damageType.ToString().ToLower()} damage.";

        if (Skills.Contains(SkillTree_Manager.SkillName.SPIRAL_MORE))
        {
            info.SkillName = "Der Tag neigt sich - Flowering Night";
            info.SkillText =
                $"In the next {SkillDuration} seconds: becomes unable to move and attack, continuously unleashes multiple waves of projectiles " +
                $"spreading in all direction around self. Each projectile hits the first enemy it comes into contact with, dealing {Skill_DamageMulitplier * 100}% ATK damage each. " +
                $"{SkillCooldown}s cooldown.";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.SPIRAL_TRAVEL))
        {
            info.SkillName = "Der Tag neigt sich - Ghost Lead";
            info.SkillText =
                $"In the next {SkillDuration} seconds: becomes unable to move and attack, and lock-on to the nearest enemy within attack range. " +
                $"Continuously unleashes waves of projectiles spreading in all direction around self " +
                $". Each projectile hits the first enemy it comes into contact with (if there is a locked enemy, all projectiles homing toward them instead), dealing {Skill_DamageMulitplier * 100}% ATK damage each. " +
                $"{SkillCooldown}s cooldown.";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.SPIRAL_PHANTOM))
        {
            info.SkillName = "Der Tag neigt sich - Phantom Bullets";
            info.SkillText =
                $"In the next {SkillDuration} seconds: becomes unable to move and attack, continuously unleashes waves of projectiles " +
                $"spreading in all direction around self. Each projectile hits the first enemy it comes into contact with, dealing {Skill_DamageMulitplier * 100}% ATK damage each. " +
                $"If an enemy is defeated while the skill is active, another waves of projectiles will be created at their position, lasting up to 1.5 seconds (triggers up to 3 times). " +
                $"{SkillCooldown}s cooldown.";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.SPIRAL_SHADOW))
        {
            info.SkillName = "Der Tag neigt sich - Twilight of Wolumonde";
            info.SkillText =
                $"In the next {SkillDuration} seconds: becomes unable to move and attack, continuously unleashes waves of projectiles " +
                $"spreading in all direction around self. Each projectile hits the first enemy it comes into contact with, dealing {Skill_DamageMulitplier * 100}% ATK damage each. " +
                $"Swapping during this skill leaves behind a phantom that maintains the same effect for the remaining duration." +
                $"{SkillCooldown}s cooldown.";
        }
        else
        {
            info.SkillName = "Der Tag neigt sich";
            info.SkillText =
                $"In the next {SkillDuration} seconds: becomes unable to move and attack, continuously unleashes waves of projectiles " +
                $"spreading in all direction around self. Each projectile hits the first enemy it comes into contact with, dealing {Skill_DamageMulitplier * 100}% ATK damage each. " +
                $"{SkillCooldown}s cooldown.";
        }

        if (Skills.Contains(SkillTree_Manager.SkillName.FREEZE_ICEAGE))
        {
            info.SpecialName = "Zeropoint Burst - Snow Blossom";
            info.SpecialText =
                $"After a short delay, inflicts freeze to all enemies within attack range for {FreezeDurationMin} - {FreezeDurationMax} seconds, inversely based on distance. Frozen enemies continues to " +
                $"create an extra freeze ring around their position (trigger once per enemy) ";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.FREEZE_CHARGE))
        {
            info.SpecialName = "Zeropoint Burst - Hypercharge";
            info.SpecialText =
                $"After a short delay, inflicts freeze to all enemies within attack range for {FreezeDurationMin} - {FreezeDurationMax} seconds, inversely based on distance. Every enemy hit " +
                $"shortens the cool-down of the next usage by 15%";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.FREEZE_SUPERCONDUCT))
        {
            info.SpecialName = "Zeropoint Burst - Superconduct";
            info.SpecialText =
                $"After a short delay, inflicts freeze to all enemies within attack range for {FreezeDurationMin} - {FreezeDurationMax} seconds, inversely based on distance " +
                $"and reduce their DEF and RES by 50% for equivalent duration";
        }
        else
        {
            info.SpecialName = "Zeropoint Burst";
            info.SpecialText =
                $"After a short delay, inflicts freeze to all enemies within attack range for {FreezeDurationMin} - {FreezeDurationMax} seconds (closer enemies are frozen for longer)";
        }

        if (Skills.Contains(SkillTree_Manager.SkillName.WINDBLOW_NORTH))
        {
            info.SpecialText += " and pushes them away from self";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.WINDBLOW_SOUTH))
        {
            info.SpecialText += " and pulls them towards self";
        }

        info.SpecialText += $". {FreezeCooldown}s cool-down.";

            return info;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}