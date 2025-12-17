using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ShroudedAssassin : EnemyBase
{
    [SerializeField] private GameObject shadowClonePrefab;
    [SerializeField] private GameObject feudBond;
    [SerializeField] private float dashCooldown = 30f;
    [SerializeField] private float dashDuration = 0.1f;
    [SerializeField] private float dashInterval = 0.1f;
    [SerializeField] private float dashDistance = 600f;
    [SerializeField] private float dashAtkScale = 1f;

    AssassinFeudBondObject feudBondScript;
    bool reviving = false;

    public enum IllusionSpawnShape
    {
        STAR,
        TRIANGLE,
        LINE,
        FIREWORK,
    };
    private float dashCooldownTimer = 20f;

    private bool IsDashing = false;
    private bool CanUseDash 
        => SpottedPlayer 
            && dashCooldownTimer <= 0f 
            && !IsAttackLocked 
            && CanAttack 
            && !IsDashing 
            && IsAlive();

    private short DashScanCnt = 0;
    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (dashCooldownTimer > 0)
        {
            float reduce = Time.fixedDeltaTime;
            if (!canRevive) reduce *= (1 + Mathf.Lerp(1.5f, 0f, health * 1.0f / mHealth));
            dashCooldownTimer -= reduce;
        }

        ProcessFeudBond();
        DashScanCnt++;
        if (DashScanCnt >= 25 && CanUseDash && IsAlive() && !reviving)
        {
            DashScanCnt = 0;
            var player = SearchForNearestEntityAroundCertainPoint(typeof(PlayerBase), transform.position, dashDistance * 0.7f);
            if (player) StartCoroutine(DashAttack());
        }
    }

    public override void InitializeComponents()
    {
        GameObject feudBondObj = Instantiate(feudBond, transform.position + new Vector3(0, 100, 0), Quaternion.identity);
        feudBondScript = feudBondObj.GetComponent<AssassinFeudBondObject>();

        base.InitializeComponents();
    }

    private PlayerBase PrevSpottedPlayer = null;
    private int FeudLevel = 0;
    void ProcessFeudBond()
    {
        if (!SpottedPlayer)
        {
            return;
        }

        if (SpottedPlayer != PrevSpottedPlayer) FeudLevel = 0;
        PrevSpottedPlayer = SpottedPlayer;

        if (feudBondScript)
        {
            feudBondScript.transform.position = SpottedPlayer.transform.position + new Vector3(0, 100, 0);
            feudBondScript.SetFeud(FeudLevel, MaxFeudLevel);
        }

        if (FeudLevel >= MaxFeudLevel)
        {
            ApplyEffect(Effect.AffectedStat.MSPD, "ASSASSIN_MARK_MSPD_BUFF", MaxAtkBuff * 100, 0.2f, true);
            ApplyEffect(Effect.AffectedStat.MSPD, "ASSASSIN_MARK_ATK_BUFF", MaxMspdBuff * 100, 0.2f, true);
        }
    }

    void AddFeud(EntityBase source)
    {
        if (!source || source != SpottedPlayer || FeudLevel >= MaxFeudLevel) return;
        FeudLevel++;

        ProcessFeudBond();
    }

    public int MaxFeudLevel = 20;
    public float MaxDamageReduction = 1f, MaxAtkBuff = 0.3f, MaxMspdBuff = 0.3f;
    public override void TakeDamage(DamageInstance damage, EntityBase source)
    {
        if (source && source == SpottedPlayer)
        {
            float ratio = Mathf.Lerp(0f, 1f, FeudLevel * 1.00f / MaxFeudLevel);
            float reduction = 1 - ratio * MaxDamageReduction;
            damage.Multiply(reduction);

            AddFeud(source);
        }
        else damage.SetTotal(1);

        if (damage.TotalDamage > 0) dashCooldownTimer--;
        base.TakeDamage(damage, source);
    }

    public override void Move()
    {
        if (IsDashing || reviving) return;
        base.Move();
    }

    public override IEnumerator Attack()
    {
        if (IsDashing || reviving || !CanAttack || IsAttackLocked) yield break;
        yield return StartCoroutine(base.Attack());
        if (sfxs[0]) sfxs[0].Play();
    }

    private bool dashDoesDamage = true;
    private List<GameObject> IllusionsTransforms = new();
    IEnumerator DashAttack()
    {
        EndFreeze();
        EndStun();
        StopMovement();
        CancelAttack();

        IllusionSpawnShape shape = (IllusionSpawnShape)Random.Range(0, Enum.GetValues(typeof(IllusionSpawnShape)).Length);
        if (canRevive) shape = IllusionSpawnShape.LINE;

        float healthRatio = canRevive ? 1 : health * 1.0f / mHealth, minThreshold = 0.35f;
        healthRatio = Mathf.InverseLerp(minThreshold, 1f, healthRatio);

        float dashScaleDuration = Mathf.Lerp(dashDuration * 0.4f, dashDuration, healthRatio),
              dashScaleInt = Mathf.Lerp(dashInterval * 0.4f, dashInterval, healthRatio), 
              prepTime = Mathf.Lerp(0.5f, 1f, healthRatio), 
              prepAdditionalTime = Mathf.Lerp(0.35f, 0.7f, healthRatio);

        if (FeudLevel >= MaxFeudLevel)
        {
            dashScaleDuration *= 0.6f;
            dashScaleInt *= 0.6f;
        }

        float duration = dashScaleDuration * 4 + dashScaleInt * 4;
        if (shape == IllusionSpawnShape.FIREWORK) duration /= 2;

        IsFreezeImmune = IsStunImmune = true;
        IsDashing = true;

        animator.SetTrigger("skill_prep");
        if (sfxs[1]) sfxs[1].Play();

        Vector3 selfPos = transform.position;
        float range = Mathf.Clamp(Vector3.Distance(transform.position, SpottedPlayer.transform.position) * 2, dashDistance * 0.9f, dashDistance);

        Vector3 playerPos = SpottedPlayer.transform.position;

        GameObject o1, o2, o3, o4;
        switch (shape)
        {
            case IllusionSpawnShape.STAR:

                //starts as left up corner
                if (playerPos.x >= transform.position.x && playerPos.y <= transform.position.y)
                {
                    // right
                    o1 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range, 0), Quaternion.identity);
                    // down left
                    o2 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 4, range / 2 * -1, 0), Quaternion.identity);
                    // down right
                    o3 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range * 3 / 4, range / 2 * -1, 0), Quaternion.identity);
                    // up
                    o4 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 2, range / 2, 0), Quaternion.identity);

                    IllusionsTransforms.Add(o1);
                    IllusionsTransforms.Add(o2);
                    IllusionsTransforms.Add(o4);
                    IllusionsTransforms.Add(o3);
                }
                // starts as right up corner
                else if (playerPos.x < transform.position.x && playerPos.y <= transform.position.y)
                {
                    // left
                    o1 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range * -1, 0), Quaternion.identity);
                    // down right
                    o2 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 4 * -1, range / 2 * -1), Quaternion.identity);
                    // down left
                    o3 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range * 3 / 4 * -1, range / 2 * -1, 0), Quaternion.identity);
                    // up
                    o4 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 2 * -1, range / 2, 0), Quaternion.identity);

                    IllusionsTransforms.Add(o1);
                    IllusionsTransforms.Add(o2);
                    IllusionsTransforms.Add(o4);
                    IllusionsTransforms.Add(o3);
                }
                // starts in left down corner
                else if (playerPos.x >= transform.position.x && playerPos.y > transform.position.y)
                {
                    // left up
                    o1 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 4 * -1, range / 2, 0), Quaternion.identity);
                    // right up
                    o2 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range * 3 / 4, range / 2, 0), Quaternion.identity);
                    // top
                    o3 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 4, range), Quaternion.identity);
                    // right corner
                    o4 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 2, 0, 0), Quaternion.identity);

                    IllusionsTransforms.Add(o2);
                    IllusionsTransforms.Add(o1);
                    IllusionsTransforms.Add(o4);
                    IllusionsTransforms.Add(o3);
                }
                else
                {
                    // left up
                    o1 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range * 3 / 4 * -1, range / 2, 0), Quaternion.identity);
                    // right up
                    o2 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 4, range / 2, 0), Quaternion.identity);
                    // top
                    o3 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 4 * -1, range), Quaternion.identity);
                    // left corner
                    o4 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 2 * -1, 0, 0), Quaternion.identity);

                    IllusionsTransforms.Add(o1);
                    IllusionsTransforms.Add(o2);
                    IllusionsTransforms.Add(o4);
                    IllusionsTransforms.Add(o3);
                }

                GameObject o5 = Instantiate(shadowClonePrefab, selfPos, Quaternion.identity);
                IllusionsTransforms.Add(o5);
                break;
                
            case IllusionSpawnShape.TRIANGLE:
                if (playerPos.y < transform.position.y)
                {
                    // starts as top
                    if (Mathf.Abs(playerPos.x - transform.position.x) < range / 4)
                    {
                        o1 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 2 * -1, range), Quaternion.identity);
                        o2 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 2, range), Quaternion.identity);
                    }
                    // starts as right
                    else if (playerPos.x <= transform.position.x)
                    {
                        o1 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range * -1, 0), Quaternion.identity);
                        o2 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 2 * -1, range * -1), Quaternion.identity);
                    }
                    else
                    {
                        o1 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range, 0), Quaternion.identity);
                        o2 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 2, range * -1), Quaternion.identity);
                    }
                }
                else
                {
                    // starts as top
                    if (Mathf.Abs(playerPos.x - transform.position.x) < range / 4)
                    {
                        o1 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 2 * -1, range * -1), Quaternion.identity);
                        o2 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 2, range * -1), Quaternion.identity);
                    }
                    // starts as right
                    else if (playerPos.x <= transform.position.x)
                    {
                        o1 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range * -1, 0), Quaternion.identity);
                        o2 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 2 * -1, range), Quaternion.identity);
                    }
                    else
                    {
                        o1 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range, 0), Quaternion.identity);
                        o2 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 2, range), Quaternion.identity);
                    }
                }

                o3 = Instantiate(shadowClonePrefab, selfPos, Quaternion.identity);
                IllusionsTransforms.Add(o1);
                IllusionsTransforms.Add(o2);
                IllusionsTransforms.Add(o3);
                break;

            case IllusionSpawnShape.FIREWORK:
                GameObject o6, o7, o8, o9, o10, o11, o12, o13, o14, o15, o16;
                
                o1 = Instantiate(shadowClonePrefab, selfPos + new Vector3(-range, 0), Quaternion.identity);
                o2 = Instantiate(shadowClonePrefab, selfPos, Quaternion.identity);
                o3 = Instantiate(shadowClonePrefab, selfPos + new Vector3(-range / 2, range / 2), Quaternion.identity);
                o4 = Instantiate(shadowClonePrefab, selfPos, Quaternion.identity);
                o5 = Instantiate(shadowClonePrefab, selfPos + new Vector3(0, range), Quaternion.identity);
                o6 = Instantiate(shadowClonePrefab, selfPos, Quaternion.identity);
                o7 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 2, range / 2), Quaternion.identity);
                o8 = Instantiate(shadowClonePrefab, selfPos, Quaternion.identity);
                o9 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range, 0), Quaternion.identity);
                o10 = Instantiate(shadowClonePrefab, selfPos, Quaternion.identity);
                o11 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 2, -range / 2), Quaternion.identity);
                o12 = Instantiate(shadowClonePrefab, selfPos, Quaternion.identity);
                o13 = Instantiate(shadowClonePrefab, selfPos + new Vector3(0, -range), Quaternion.identity);
                o14 = Instantiate(shadowClonePrefab, selfPos, Quaternion.identity);
                o15 = Instantiate(shadowClonePrefab, selfPos + new Vector3(-range / 2, -range / 2), Quaternion.identity);
                o16 = Instantiate(shadowClonePrefab, selfPos, Quaternion.identity);

                IllusionsTransforms.AddRange(new List<GameObject>()
                {
                    o1, o2, o3, o4, o5, o6, o7, o8,
                    o9, o10, o11, o12, o13, o14, o15, o16
                });

                break;

            case IllusionSpawnShape.LINE:
                o1 = Instantiate(shadowClonePrefab, playerPos + (transform.position - playerPos).normalized * -250f, Quaternion.identity);
                IllusionsTransforms.Add(o1); 
                break;
        }
        CreateIllusionsTrails(prepTime + 0.3f);

        yield return new WaitForSeconds(prepTime);
        animator.SetTrigger("skill_cast");
        yield return new WaitForSeconds(0.32f);

        float c = 0;
        for (int i = 0; i < IllusionsTransforms.Count; ++i)
        {
            dashDoesDamage = true;
            GameObject illusion = IllusionsTransforms[i];
            selfPos = transform.position;
            c = 0;
            while (c < dashScaleDuration)
            {
                if (dashDoesDamage)
                {
                    var player = SearchForNearestEntityAroundCertainPoint(typeof(PlayerBase), transform.position, 60f, true);
                    if (player)
                    {
                        dashDoesDamage = false;
                        DealDamage(player, (int)(atk * dashAtkScale));
                    }
                }
                transform.position = Vector3.Lerp(selfPos, illusion.transform.position, c * 1.0f / dashScaleDuration);

                SpawnIllusion();

                c += Time.deltaTime;
                yield return null;
            }

            if (dashDoesDamage)
            {
                var player = SearchForNearestEntityAroundCertainPoint(typeof(PlayerBase), transform.position, 60f, true);
                if (player)
                {
                    dashDoesDamage = false;
                    DealDamage(player, (int)(atk * dashAtkScale));
                }
            }
            transform.position = illusion.transform.position;
            Destroy(illusion);

            if (i + 1 < IllusionsTransforms.Count) FaceToward(IllusionsTransforms[i + 1].transform.position);
            yield return new WaitForSeconds(dashScaleInt);
        }

        dashDoesDamage = false;
        
        if (SpottedPlayer) FaceToward(SpottedPlayer.transform.position);

        animator.SetTrigger("skill_end");
        yield return new WaitForSeconds(0.4f);

        if (SpottedPlayer && Vector3.Distance(SpottedPlayer.transform.position, AttackPosition.position) > attackRange * DangerRange_RatioOfAttackRange)
        {
            yield return new WaitForSeconds(0.2f);
            animator.SetTrigger("skill_prep");
            if (sfxs[1]) sfxs[1].Play();
            GameObject playerO = Instantiate(shadowClonePrefab, SpottedPlayer.transform.position, Quaternion.identity);
            IllusionsTransforms.Add(playerO);
            CreateIllusionsTrails(playerO, prepAdditionalTime + 0.3f);

            yield return new WaitForSeconds(prepAdditionalTime);

            animator.SetTrigger("skill_cast");
            yield return new WaitForSeconds(0.32f);

            dashDoesDamage = true;
            c = 0;
            selfPos = transform.position;
            while (c < dashDuration)
            {
                if (dashDoesDamage)
                {
                    var player = SearchForNearestEntityAroundCertainPoint(typeof(PlayerBase), transform.position, 75f, true);
                    if (player)
                    {
                        dashDoesDamage = false;
                        DealDamage(player, (int)(atk * 0.75f));
                    }
                }
                transform.position = Vector3.Lerp(selfPos, playerO.transform.position, c * 1.0f / dashDuration);

                SpawnIllusion();

                c += Time.deltaTime;
                yield return null;
            }
            transform.position = playerO.transform.position;
            Destroy(playerO);

            yield return new WaitForSeconds(0.1f);
            animator.SetTrigger("skill_end");
            yield return new WaitForSeconds(0.4f);
        }

        IsDashing = false;
        IsFreezeImmune = IsStunImmune = false;
        IllusionsTransforms.Clear();
        dashCooldownTimer = dashCooldown;
    }

    IEnumerator DashToPlayer()
    {
        EndFreeze();
        EndStun();

        float healthRatio = canRevive ? 1 : health * 1.0f / mHealth, minThreshold = 0.2f;
        healthRatio = Mathf.InverseLerp(minThreshold, 1f, healthRatio);

        float dashScaleDuration = Mathf.Lerp(dashDuration * 0.4f, dashDuration, healthRatio),
              dashScaleInt = Mathf.Lerp(dashInterval * 0.4f, dashInterval, healthRatio),
              prepTime = Mathf.Lerp(0.5f, 1f, healthRatio),
              prepAdditionalTime = Mathf.Lerp(0.35f, 0.7f, healthRatio);

        float duration = dashScaleDuration * 4 + dashScaleInt * 4;

        SetInvulnerable(duration);
        StartCoroutine(StartAttackLockout(duration));
        StartCoroutine(StartMovementLockout(duration));

        IsFreezeImmune = IsStunImmune = true;
        IsDashing = true;

        animator.SetTrigger("skill_prep");

        Vector3 selfPos = transform.position;
        float range = Mathf.Clamp(Vector3.Distance(transform.position, SpottedPlayer.transform.position) * 2, dashDistance * 0.9f, dashDistance);

        Vector3 playerPos = SpottedPlayer.transform.position;

        GameObject o1 = Instantiate(shadowClonePrefab, playerPos, Quaternion.identity);
        IllusionsTransforms.Add(o1);

        CreateIllusionsTrails(prepTime + 0.3f);

        yield return new WaitForSeconds(prepTime);
        animator.SetTrigger("skill_cast");
        yield return new WaitForSeconds(0.32f);

        float c = 0;
        for (int i = 0; i < IllusionsTransforms.Count; ++i)
        {
            dashDoesDamage = true;
            GameObject illusion = IllusionsTransforms[i];
            selfPos = transform.position;
            c = 0;
            while (c < dashScaleDuration)
            {
                if (dashDoesDamage)
                {
                    var player = SearchForNearestEntityAroundCertainPoint(typeof(PlayerBase), transform.position, 60f, true);
                    if (player)
                    {
                        dashDoesDamage = false;
                        DealDamage(player, (int)(atk * dashAtkScale));
                    }
                }
                transform.position = Vector3.Lerp(selfPos, illusion.transform.position, c * 1.0f / dashScaleDuration);

                SpawnIllusion();

                c += Time.deltaTime;
                yield return null;
            }

            if (dashDoesDamage)
            {
                var player = SearchForNearestEntityAroundCertainPoint(typeof(PlayerBase), transform.position, 60f, true);
                if (player)
                {
                    dashDoesDamage = false;
                    DealDamage(player, (int)(atk * dashAtkScale));
                }
            }
            transform.position = illusion.transform.position;
            Destroy(illusion);

            if (i + 1 < IllusionsTransforms.Count) FaceToward(IllusionsTransforms[i + 1].transform.position);
            yield return new WaitForSeconds(dashScaleInt);
        }

        dashDoesDamage = false;
        yield return new WaitForSeconds(0.4f);

        IsDashing = false;
        IsFreezeImmune = IsStunImmune = false;
        IllusionsTransforms.Clear();
        dashCooldownTimer = dashCooldown;
    }

    [SerializeReference] GameObject trailPrefab;
    void CreateIllusionsTrails(float persist)
    {
        List<GameObject> TargetPoints = new(IllusionsTransforms);
        TargetPoints.Insert(0, this.gameObject);

        for (int i = 0; i < TargetPoints.Count - 1; ++i)
        {
            Vector3 currentIllusionPos = TargetPoints[i].transform.position;
            Vector3 nextIllusionPos = TargetPoints[i + 1].transform.position;

            // Correct midpoint
            Vector3 trailSpawnPos = (currentIllusionPos + nextIllusionPos) / 2f;

            // Correct rotation (for UI, aligning up-axis to the direction)
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, nextIllusionPos - currentIllusionPos);

            GameObject trail = Instantiate(trailPrefab, trailSpawnPos, rotation, TargetPoints[i].transform);
            float scale = 1.0f / TargetPoints[i].transform.localScale.x;
            trail.transform.localScale = new(scale, scale);

            Image trailImg = trail.GetComponentInChildren<Image>();

            // Resize trail length
            float distance = Vector3.Distance(currentIllusionPos, nextIllusionPos);
            trailImg.GetComponent<RectTransform>().sizeDelta = new Vector2(85, distance);

            if (trail) Destroy(trail, persist);
        }
    }

    void CreateIllusionsTrails(GameObject target, float persist)
    {
        Vector3 currentIllusionPos = transform.position;
        Vector3 nextIllusionPos = target.transform.position;

        // Correct midpoint
        Vector3 trailSpawnPos = (currentIllusionPos + nextIllusionPos) / 2f;

        // Correct rotation (for UI, aligning up-axis to the direction)
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, nextIllusionPos - currentIllusionPos);

        GameObject trail = Instantiate(trailPrefab, trailSpawnPos, rotation, transform);
        float scale = 1.0f / transform.localScale.x;
        trail.transform.localScale = new(scale, scale);

        Image trailImg = trail.GetComponentInChildren<Image>();

        // Resize trail length
        float distance = Vector3.Distance(currentIllusionPos, nextIllusionPos);
        trailImg.GetComponent<RectTransform>().sizeDelta = new Vector2(85, distance);

        if (trail) Destroy(trail, persist);
    }

    public override void OnDeath()
    {
        foreach (var illusion in IllusionsTransforms)
        {
            if (illusion) Destroy(illusion);
        }
        IllusionsTransforms.Clear();
        base.OnDeath();
    }

    public override IEnumerator Revive()
    {
        ClearAllEffects();
        canRevive = false;
        reviving = true;

        foreach (var illusion in IllusionsTransforms)
        {
            if (illusion) Destroy(illusion);
        }
        IllusionsTransforms.Clear();
        IsDashing = false;
        IsFreezeImmune = IsStunImmune = false;
        dashCooldown = 10f;
        dashCooldownTimer = dashCooldown;
        animator.SetBool("attack", false);

        float lockoutDuration = reviveDuration + postReviveDuration + 1f;
        animator.SetTrigger("die");
        health = 1;
        StartCoroutine(StartMovementLockout(lockoutDuration));
        StartCoroutine(StartAttackLockout(lockoutDuration));
        SetInvulnerable(lockoutDuration);

        yield return new WaitForSeconds(1f);

        float c = 0, fillInterval = 0;
        while (c < reviveDuration)
        {
            health = (int)Mathf.Lerp(1, mHealth, c * 1.0f / reviveDuration);
            SetHealth(health);
            c += Time.deltaTime;
            fillInterval += Time.deltaTime;

            if (fillInterval >= 0.15f && SpottedPlayer)
            {
                fillInterval = 0;
                AddFeud(SpottedPlayer);
                SpottedPlayer.ApplyEffect(Effect.AffectedStat.MSPD, "ASSASSIN_HELD_BREATH_SLOW", -30f, 0.4f, true);
            }
            yield return null;
        }

        reviving = false;
        animator.SetTrigger("revive");
        health = mHealth;
        SetHealth(health);

        def = bDef = (short) (bDef * 0.5f);
        res = bRes = (short) (bRes * 0.3f);
        StartCoroutine(AutoBuildupFeud());
    }

    IEnumerator AutoBuildupFeud()
    {
        float interval = 0.75f;
        while (isActiveAndEnabled && IsAlive())
        {
            yield return new WaitForSeconds(interval);
            if (SpottedPlayer && !canRevive)
            {
                AddFeud(SpottedPlayer);
            }
        }
    }

    void SpawnIllusion()
    {
        GameObject Illusion = Instantiate(shadowClonePrefab, transform.position, Quaternion.identity);
        SpriteRenderer IllusionSpriteRenderer = Illusion.GetComponentInChildren<SpriteRenderer>();
        IllusionSpriteRenderer.sprite = spriteRenderer.sprite;
        IllusionSpriteRenderer.flipX = spriteRenderer.flipX;
        IllusionSpriteRenderer.color = new Color(1, 1, 1, 0.5f);
        Destroy(Illusion, 0.2f);
    }

    public override void WriteStats()
    {
        Description = "Assassin who has abandoned his name and covered his face. Behind that pall is a burning fanaticism and a destined fate.";
        
        Skillset = 
            "• [Feud Bonding] Receiving damage from the player unit builds up \"Feud\" on them (stacks up to a limit). Self takes reduced damage from the attacker based on how much \"Feud\" they have built up. " +
            "Gains increased ATK and MSPD when the target has their \"Feud\" maxed out.\n\n" +
            "<b><color=red>FIRST PHASE</color></b>\n\n" +
            "• [Pack Up] Stops moving, becomes immune to freeze and stun, then charges up and quickly dash toward your position, dealing physical damage if comes into contact with. Receving damage shortens the cool-down of the next use.\n\n" +
            "• [Held Breath] When HP reaches 0, enters a revival state and rapidly builds up \"Feud\" on the presenting player and slows them throughout the duration. Enters the second phase afterward.\n\n" +
            
            "<b><color=red>SECOND PHASE</color></b>\n\n" +
            "• DEF and RES are reduced.\n\n" +
            "• [Overflowing Hatred] \"Feud\" is now gradually builds up overtime.\n\n" +
            "• [Toward Death] Stops moving, becomes immune to freeze and stun, then creates multiple illusion of self. After a short delay, dashes toward these illusions in quick succession and damages the player if comes into contact with. " +
            "Dash speed increases as HP decreases. Receving damage shortens the cool-down of the next use.";
        
        TooltipsDescription = "Assassin who has abandoned his name and covered his face. Behind that pall is a burning fanaticism and a destined fate.";
        base.WriteStats();
    }
}
