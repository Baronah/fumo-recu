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
    [SerializeField] private GameObject AttackRangeIndicator, Warning, EffectsParent, SkillEffect, FreezeEffect, FreezeRing, FreezeMaintRing, JuggEffect;

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

    short fixedUpdateCnt = 0;
    public override void FixedUpdate()
    {
        base.FixedUpdate();

        fixedUpdateCnt++;
        if (fixedUpdateCnt < 5) return;
        fixedUpdateCnt = 0;

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

        bool alive = IsAlive();
        bool SkillActive = IsSkillActive && alive, FreezeActive = IsFreezeActive && alive;
        SkillEffect.SetActive(SkillActive);
        FreezeEffect.SetActive(FreezeActive);
        SkillBarObj.SetActive(SkillActive);

        SkillBarObj.transform.localPosition =
            WindanthemBar.activeSelf ? new Vector3(-0.08f, -3.2f, 0) : new Vector3(-0.08f, -2.3f, 0);

        ccBar.transform.localPosition =
            SkillBarObj.activeSelf ? SkillBarObj.transform.localPosition + new Vector3(0, -0.9f, 0) : new Vector3(-0.08f, -2.3f, 0);
    }

    public override PlayerManager.PlayerType GetPlayerType()
    {
        return PlayerManager.PlayerType.RANGED;
    }

    public override void InitializeComponents()
    {
        SkillBar = SkillBarObj.GetComponentInChildren<Slider>();

        freezeCooldownTimer = FreezeCooldown;
        skillCooldownTimer = SkillCooldown;

        base.InitializeComponents();
    }

    public override void OnFieldSwapOut(PlayerBase swapInPlayer)
    {
        base.OnFieldSwapOut(swapInPlayer);
        SpawnIllusion();
        if (freezeMaintRing) Destroy(freezeMaintRing);
    }

    void SpawnIllusion()
    {
        if (!Skills.Contains(SkillTree_Manager.SkillName.SPIRAL_SHADOW) || !IsSkillActive) return;

        // inherits stats and skill duration and start casting skill on time for remaining duration
        GameObject o = Instantiate(IllusionPrefab, transform.position, Quaternion.identity);
        PlayerCasterIllusion playerCasterIllusion = o.GetComponent<PlayerCasterIllusion>();
        playerCasterIllusion.InitializeComponents();
        playerCasterIllusion.SetInherit(
            ATK: (short)(atk * 0.5f),
            maxDuration: SkillDuration,
            duration: skillCurrentDuration,
            Skill_DamageMulitplier,
            GetSkillFiringInterval(),
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

        EffectsParent.transform.localPosition = new Vector3(
            -EffectsParent.transform.localPosition.x,
            EffectsParent.transform.localPosition.y,
            EffectsParent.transform.localPosition.z
        );
    }

    public override void GetSkillTreeEffects()
    {
        base.GetSkillTreeEffects();
        if (Skills.Contains(SkillTree_Manager.SkillName.EQUIPMENT_RADIO))
        {
            FreezeCooldown *= 0.9f;
            SkillCooldown *= 0.9f;
        }

        if (Skills.Contains(SkillTree_Manager.SkillName.JUST_A_NICE_LOOKING_ROCK))
        {
            FreezeCooldown *= 0.948f;
            SkillCooldown *= 0.948f;
        }

        if (Skills.Contains(SkillTree_Manager.SkillName.FREEZE_BLOOM))
        {
            FreezeDurationMin += 1f;
            FreezeDurationMax += 1f;
        }

        if (Skills.Contains(SkillTree_Manager.SkillName.WINDBLOW_SOUTH))
            FreezeDurationMin = Mathf.Max(FreezeDurationMin, 2f);

        if (Skills.Contains(SkillTree_Manager.SkillName.SPIRAL_MORE))
            Skill_DamageMulitplier *= 0.75f;
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
        if (!IsAlive()) return;

        if (Input.GetKeyDown(InputManager.Instance.AttackKey))
        {
            AttackCoroutine = StartCoroutine(Attack());
        }
        else if (Input.GetKeyDown(InputManager.Instance.SkillKey))
        {
            if (SkillCoroutine == null) UseSkill();
            else CancelSkill();
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
        if (!CanUseSkill || IsFreezeActive) return;

        base.UseSkill();
        SkillCoroutine = StartCoroutine(CastSkill());
    }

    void CancelSkill()
    {
        if (!IsSkillActive || SkillCoroutine == null) return;

        if (Skills.Contains(SkillTree_Manager.SkillName.SPIRAL_SHADOW)) SpawnIllusion();
        else if (Skills.Contains(SkillTree_Manager.SkillName.SPIRAL_READ)) RefundSkill();

        StopCoroutine(SkillCoroutine);
        SkillCoroutine = null;

        animator.SetTrigger("skill_end");
        IsSkillActive = false;
    }

    void RefundSkill()
    {
        float refundPercentage,
              spMaxRefund = 0.85f, minWinForSpMax = 0.25f, 
              maxRefund = 0.8f, minWindowForMaxRefund = 0.5f;
        
        float currentDuration = skillCurrentDuration;
        if (currentDuration < minWinForSpMax) refundPercentage = spMaxRefund;
        else if (currentDuration <= minWindowForMaxRefund) refundPercentage = maxRefund;
        else
        {
            refundPercentage = Mathf.Lerp(maxRefund, 0f, currentDuration * 1.0f / SkillDuration);
        }

        float refundTime = SkillCooldown * refundPercentage;
        skillCooldownTimer += refundTime;

        StartCoroutine(playerManager.SkillCooldown(SkillCooldown, skillCooldownTimer));
    }

    public override void UseSpecial()
    {
        if (!CanUseFreeze) return; 
        if (IsSkillActive) CancelSkill();

        base.UseSpecial();
        StartCoroutine(CastFreeze());
    }

    public override void Move()
    {
        if (IsMovementLocked || IsFreezeActive) return;
        if (IsSkillActive && InputManager.Instance.GetMovementInput().magnitude > 0) CancelSkill();

        base.Move();
    }

    float skillCooldownTimer = 0f;
    IEnumerator SkillLockout(float d)
    {
        CanUseSkill = false;
        StartCoroutine(playerManager.SkillCooldown(d));
        skillCooldownTimer = 0f;
        while (skillCooldownTimer < d)
        {
            skillCooldownTimer += Time.deltaTime;
            yield return null;
        }
        CanUseSkill = true;
    }

    float freezeCooldownTimer = 0f;
    IEnumerator FreezeLockout(float d)
    {
        CanUseFreeze = false;
        StartCoroutine(playerManager.SpecialCooldown(d));
        freezeCooldownTimer = 0f;
        while (freezeCooldownTimer < d)
        {
            freezeCooldownTimer += Time.deltaTime;
            yield return null;
        }
        CanUseFreeze = true;
    }

    public override IEnumerator Attack()
    {
        if (!CanAttack || IsAttackLocked || IsFreezeActive) yield break;
        if (IsSkillActive) CancelSkill();

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

    bool IsFreezeActive = false;
    GameObject freezeMaintRing = null;
    public IEnumerator CastFreeze()
    {
        if (!IsAlive() || !CanUseFreeze || IsFrozen || IsStunned) yield break;

        CanUseFreeze = false;
        IsFreezeActive = true;

        StartCoroutine(StartMovementLockout(FreezeCastDuration));
        StartCoroutine(StartAttackLockout(FreezeCastDuration));
        
        animator.SetBool("attack", false);
        animator.SetTrigger("skill");

        if (sfxs[1]) sfxs[1].Play();

        Instantiate(FreezeRing, SkillPosition.position, Quaternion.identity);
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
        
        if (Skills.Contains(SkillTree_Manager.SkillName.FREEZE_HOLD))
        {
            float bonusDuration = 0, maxBonus = 5f;
            freezeMaintRing = null;
            bool initiatedRing = false;

            var currentHitEnemies = SearchForEntitiesAroundCertainPoint(typeof(EnemyBase), SkillPosition.position, FreezeRange, true);
            short frameCount = 0;
            while (bonusDuration < maxBonus && Input.GetKey(InputManager.Instance.SpecialKey))
            {
                if (frameCount >= 3)
                {
                    currentHitEnemies = SearchForEntitiesAroundCertainPoint(typeof(EnemyBase), SkillPosition.position, FreezeRange, true);
                    frameCount = 0;
                }

                if (!freezeMaintRing && !initiatedRing)
                {
                    freezeMaintRing = Instantiate(FreezeMaintRing, SkillPosition.position + new Vector3(0, 15, 0), Quaternion.identity, SkillPosition);
                    initiatedRing = true;
                }

                foreach (var enemy in currentHitEnemies)
                {
                    if (InitialHitDictionary.ContainsKey(enemy)) 
                        ApplyFreeze(enemy, InitialHitDictionary[enemy]);
                    else
                        ApplyFreeze(enemy, FreezeDurationMin);
                }

                bonusDuration += Time.deltaTime;
                frameCount++;
                yield return null;
            }

            if (freezeMaintRing) Destroy(freezeMaintRing);
        }

        Debut = false;
        IsFreezeActive = false;

        animator.SetTrigger("skill_end");
        yield return null;

        Dictionary<EntityBase, float> HitDictionary = new(InitialHitDictionary);
        if (Skills.Contains(SkillTree_Manager.SkillName.FREEZE_BLOOM))
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
                    Instantiate(FreezeRing, InitHitEnemyHit.transform.position, Quaternion.identity);

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

    public float GetSkillFiringInterval()
    {
        float ASPD_Dif = ASPD - 100;
        float ScaleFactor = 100 / (100 + ASPD_Dif * 0.33f);
        return Skill_AtkInterval * ScaleFactor;
    }

    public IEnumerator CastSkill()
    {
        if (!IsAlive()) yield break;

        StartCoroutine(StartMovementLockout(0.15f));
        StartCoroutine(SkillLockout(SkillCooldown));

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
        float intervalCounter = GetSkillFiringInterval();

        float lockInTargetDamageMul = 1.0f;
        int yellowBulletAngle = 90;

        while (skillCurrentDuration < SkillDuration)
        {
            SkillBar.value = SkillDuration - skillCurrentDuration;

            if (intervalCounter >= GetSkillFiringInterval())
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

        SkillCoroutine = null;
    }

    short flowerCount = 0;
    public override void DealDamage(EntityBase target, int pDmg, int mDmg, int tDmg, bool allowWhenDisabled = false, ProjectileScript projectileScript = null)
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

    public override void OnDeath()
    {
        base.OnDeath();
        if (!IsAlive() && freezeMaintRing) Destroy(freezeMaintRing);
    }

    protected override void MintRevive()
    {
        freezeCooldownTimer = FreezeCooldown;
        skillCooldownTimer = SkillCooldown;
        base.MintRevive();
    }

    public override PlayerTooltipsInfo GetPlayerTooltipsInfo()
    {
        var info = base.GetPlayerTooltipsInfo();

        info.AttackText = $"Lauches a projectile toward the nearest enemy within range, " +
            $"dealing {atk} {damageType.ToString().ToLower()} damage.";

        if (Skills.Contains(SkillTree_Manager.SkillName.SPIRAL_MORE))
        {
            info.SkillName = "Der Tag neigt Sich - Flowering Night";
            info.SkillText =
                $"Continuously unleashes multiple waves of projectiles spreading in all direction around self, lasts up to {SkillDuration} seconds (can be cancelled via recast or perform other action). " +
                $"Each projectile hits the first enemy it comes into contact with, dealing {Skill_DamageMulitplier * 100}% ATK damage each, firing interval scales with ASPD. " +
                $"{SkillCooldown}s cooldown.";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.SPIRAL_TRAVEL))
        {
            info.SkillName = "Der Tag neigt Sich - Ghost Lead";
            info.SkillText =
                $"Locks-on to the nearest enemy within range and continuously unleashes waves of projectiles, lasts up to {SkillDuration} seconds (can be cancelled via recast or perform other action). " +
                $". Each projectile hits the first enemy it comes into contact with (if there is a locked enemy, all projectiles homing toward them instead), dealing {Skill_DamageMulitplier * 100}% ATK damage each, firing interval scales with ASPD. " +
                $"{SkillCooldown}s cooldown.";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.SPIRAL_PHANTOM))
        {
            info.SkillName = "Der Tag neigt Sich - Phantom Bullets";
            info.SkillText =
                $"Continuously unleashes waves of projectiles spreading in all direction around self, lasts up to {SkillDuration} seconds (can be cancelled via recast or perform other action). " +
                $"Each projectile hits the first enemy it comes into contact with, dealing {Skill_DamageMulitplier * 100}% ATK damage each, firing interval scales with ASPD. " +
                $"If an enemy is defeated while the skill is active, another waves of projectiles will be created at their position, lasting up to 1.5 seconds (triggers up to 3 times). " +
                $"{SkillCooldown}s cooldown.";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.SPIRAL_SHADOW))
        {
            info.SkillName = "Der Tag neigt Sich - Twilight of Wolumonde";
            info.SkillText =
                $"Continuously unleashes waves of projectiles spreading in all direction around self, lasts up to {SkillDuration} seconds (can be cancelled via recast or perform other action). " +
                $"Each projectile hits the first enemy it comes into contact with, dealing {Skill_DamageMulitplier * 100}% ATK damage each, firing interval scales with ASPD. " +
                $"Cancelling or swapping during the skill leaves behind a phantom that maintains the same effect for the remaining duration." +
                $"{SkillCooldown}s cooldown.";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.SPIRAL_READ))
        {
            info.SkillName = "Der Tag neigt Sich - Widely Read";
            info.SkillText =
                $"Continuously unleashes waves of projectiles spreading in all direction around self, lasts up to {SkillDuration} seconds (can be cancelled via recast or perform other action, and refunds upon doing so). " +
                $"Each projectile hits the first enemy it comes into contact with, dealing {Skill_DamageMulitplier * 100}% ATK damage each, firing interval scales with ASPD. " +
                $"{SkillCooldown}s cooldown.";
        }
        else
        {
            info.SkillName = "Der Tag neigt Sich";
            info.SkillText =
                $"Continuously unleashes waves of projectiles spreading in all direction around self, lasts up to {SkillDuration} seconds (can be cancelled via recast or perform other action). " +
                $"Each projectile hits the first enemy it comes into contact with, dealing {Skill_DamageMulitplier * 100}% ATK damage each, firing interval scales with ASPD. " +
                $"{SkillCooldown}s cooldown.";
        }

        if (Skills.Contains(SkillTree_Manager.SkillName.FREEZE_BLOOM))
        {
            info.SpecialName = "Zeropoint Burst - Snow Blossom";
            info.SpecialText =
                $"After a short delay, inflicts freeze to all enemies within attack range for {FreezeDurationMin} - {FreezeDurationMax} seconds, inversely based on distance. Frozen enemies continues to " +
                $"create an extra freeze ring around their position (trigger once per enemy) ";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.FREEZE_HOLD))
        {
            info.SpecialName = "Zeropoint Burst - Focused Suppression";
            info.SpecialText =
                $"After a short delay, inflicts freeze to all enemies within attack range for {FreezeDurationMin} - {FreezeDurationMax} seconds, inversely based on distance. The freeze can be maintained by holding the skill's key for up to an additional 5 seconds (can not act while maintaining, release the key to end its effect).";
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