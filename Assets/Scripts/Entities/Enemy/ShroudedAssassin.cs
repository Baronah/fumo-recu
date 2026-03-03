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

    public float WrappedShroudHpTriggerThreshold = 0.3f;

    AssassinFeudBondObject feudBondScript;
    bool reviving = false;

    public enum IllusionSpawnShape
    {
        RANDOM,
        STAR,
        TRIANGLE,
        LINE,
        LINE_EXACT,
        FIREWORK,
        X,
        Z,
    };
    private float dashCooldownTimerCountdown = 20f;
    private int CurrentPhase => canRevive || reviving ? 1 : 2;

    private bool IsDashing = false;
    private bool CanUseDash 
        => SpottedPlayer 
            && dashCooldownTimerCountdown <= 0f 
            && !IsAttackLocked 
            && CanAttack 
            && !IsDashing 
            && IsAlive();

    private short DashScanCnt = 0;
    CanvasGroup canvasGroup;
    SpriteRenderer shadowSpriteRend;
    Color shadowInitColor;

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (dashCooldownTimerCountdown > 0)
        {
            float reduce = Time.fixedDeltaTime;
            if (CurrentPhase == 2) reduce *= (1 + Mathf.Lerp(2f, 0f, health * 1.0f / mHealth));
            if (WarppedShroudedTriggered) reduce *= 1.5f;
            dashCooldownTimerCountdown -= reduce;
        }

        ProcessFeudBond();

        CheckForWrappedShrouded();
        if (WarppedShroudedTriggered) return;

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

        canvasGroup = GetComponent<CanvasGroup>();
        shadowSpriteRend = ShadowSprite.GetComponent<SpriteRenderer>();
        shadowInitColor = shadowSpriteRend.color;
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
            ApplyEffect(Effect.AffectedStat.MSPD, "ASSASSIN_MARK_ATK_BUFF", MaxMspdBuff * 100, 0.2f, true);
            ApplyEffect(Effect.AffectedStat.ATK, "ASSASSIN_MARK_MSPD_BUFF", MaxAtkBuff * 100, 0.2f, true);
            if (!canRevive) SpottedPlayer.ApplyEffect(Effect.AffectedStat.MSPD, "ASSASSIN_MARKED_DEBUFF", -20f, 0.2f, true); 
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
    public override void TakeDamage(DamageInstance damage, EntityBase source, ProjectileScript projectileInfo = null)
    {
        if (source && source == SpottedPlayer)
        {
            float ratio = Mathf.Lerp(0f, 1f, FeudLevel * 1.00f / MaxFeudLevel);
            float reduction = 1 - ratio * MaxDamageReduction;
            damage.Multiply(reduction);

            AddFeud(source);
        }
        else if (damage.TotalDamage > 0) damage.SetTotal(1);

        if (damage.TotalDamage > 0) dashCooldownTimerCountdown--;
        base.TakeDamage(damage, source);
    }

    public override void Move()
    {
        if (IsDashing || reviving || WarppedShroudedTriggered) return;
        base.Move();
    }

    public override IEnumerator Attack()
    {
        if (WarppedShroudedTriggered || IsDashing || reviving || !CanAttack || IsAttackLocked) yield break;
        yield return StartCoroutine(base.Attack());
        if (sfxs[0]) sfxs[0].Play();
    }

    private bool dashDoesDamage = true;
    private List<GameObject> IllusionsTransforms = new();
    IEnumerator DashAttack(IllusionSpawnShape shape = IllusionSpawnShape.RANDOM)
    {
        if (WarppedShroudedTriggered)
        {
            foreach (var item in colliders)
            {
                item.enabled = true;
            }

            spriteRenderer.color = InitSpriteColor;
            canvasGroup.alpha = 1;
        }

        IsShiftImmune = true;
        EndFreeze();
        EndStun();
        StopMovement();
        CancelAttack();

        if (shape == IllusionSpawnShape.RANDOM)
        {
            if (CurrentPhase == 1)
            {
                yield return StartCoroutine(UseDash(IllusionSpawnShape.LINE));
                yield return StartCoroutine(UseDash(IllusionSpawnShape.LINE_EXACT));
            }
            else if (WarppedShroudedTriggered)
            {
                if (Vector2.Distance(transform.position, SpottedPlayer.transform.position) >= 350f)
                {
                    yield return StartCoroutine(UseDash(IllusionSpawnShape.LINE));
                    yield return StartCoroutine(UseDash(IllusionSpawnShape.RANDOM));
                }
                else
                {
                    yield return StartCoroutine(UseDash(IllusionSpawnShape.RANDOM));
                    yield return StartCoroutine(UseDash(IllusionSpawnShape.LINE));
                }
            }
            else
            {
                yield return StartCoroutine(UseDash(IllusionSpawnShape.RANDOM));
                yield return StartCoroutine(UseDash(IllusionSpawnShape.LINE_EXACT));
            }
        }
        else
            yield return StartCoroutine(UseDash(shape));

        IsDashing = false;
        IsFreezeImmune = IsStunImmune = false;
        IllusionsTransforms.Clear();
        dashCooldownTimerCountdown = dashCooldown;
        IsShiftImmune = false;
    }

    IEnumerator UseDash(IllusionSpawnShape shape = IllusionSpawnShape.RANDOM)
    {
        if (!SpottedPlayer) yield break;

        if (shape == IllusionSpawnShape.RANDOM) 
            shape = (IllusionSpawnShape) Random.Range(1, Enum.GetValues(typeof(IllusionSpawnShape)).Length);

        float healthRatio = CurrentPhase == 1 ? 1 : health * 1.0f / mHealth, minThreshold = WrappedShroudHpTriggerThreshold;
        healthRatio = Mathf.InverseLerp(minThreshold, 1f, healthRatio);

        float dashScaleDuration = Mathf.Lerp(dashDuration * 0.4f, dashDuration, healthRatio),
              dashScaleInt = Mathf.Lerp(dashInterval * 0.4f, dashInterval, healthRatio),
              prepTime = Mathf.Lerp(0.5f, 1f, healthRatio),
              prepAdditionalTime = Mathf.Lerp(0.35f, 0.7f, healthRatio);

        if (FeudLevel >= MaxFeudLevel)
        {
            dashScaleDuration *= 0.7f;
            dashScaleInt *= 0.7f;
        }

        if (shape == IllusionSpawnShape.FIREWORK)
        {
            dashScaleDuration /= 2;
        }

        IllusionsTransforms = new();
        IsFreezeImmune = IsStunImmune = true;
        IsDashing = true;

        animator.SetTrigger("skill_prep");
        if (sfxs[1]) sfxs[1].Play();

        Vector3 selfPos = transform.position;
        float range = Mathf.Clamp(Vector3.Distance(transform.position, SpottedPlayer.transform.position) * 2, dashDistance * 0.9f, dashDistance);

        Vector3 playerPos = SpottedPlayer.transform.position;

        GameObject o1, o2, o3, o4, o5, o6, o7, o8, o9, o10, o11, o12, o13, o14, o15, o16;
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

                o5 = Instantiate(shadowClonePrefab, selfPos, Quaternion.identity);
                IllusionsTransforms.Add(o5);
                break;

            case IllusionSpawnShape.TRIANGLE:
                float Dist = Vector2.Distance(selfPos, playerPos);
                if (playerPos.y > selfPos.y)
                {
                    if (Dist >= range / 2)
                    {
                        if (playerPos.x > selfPos.x)
                        {
                            o1 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range, 0), Quaternion.identity);
                            o2 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 2, range), Quaternion.identity);
                        }
                        else
                        {
                            o1 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 2 * -1, range), Quaternion.identity);
                            o2 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range * -1, 0), Quaternion.identity);
                        }
                    }
                    else
                    {
                        o1 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 2, range), Quaternion.identity);
                        o2 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 2 * -1, range), Quaternion.identity);
                    }
                }
                else
                {
                    if (Dist >= range / 2)
                    {
                        if (playerPos.x > selfPos.x)
                        {
                            o1 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range * -1, 0), Quaternion.identity);
                            o2 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 2 * -1, range * -1), Quaternion.identity);
                        }
                        else
                        {
                            o1 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 2, range * -1), Quaternion.identity);
                            o2 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range, 0), Quaternion.identity);
                        }
                    }
                    else
                    {
                        o1 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 2 * -1, range * -1), Quaternion.identity);
                        o2 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 2, range * -1), Quaternion.identity);
                    }
                }

                o3 = Instantiate(shadowClonePrefab, selfPos, Quaternion.identity);
                IllusionsTransforms.AddRange(new List<GameObject>()
                {
                    o1, o2, o3
                });
                break;

            case IllusionSpawnShape.FIREWORK:
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

            case IllusionSpawnShape.X:
                o1 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 2 * -1, range / 2), Quaternion.identity);
                o2 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 2, range / 2 * -1), Quaternion.identity);
                o3 = Instantiate(shadowClonePrefab, selfPos, Quaternion.identity);
                o4 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 2 * -1, range / 2 * -1), Quaternion.identity);
                o5 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range / 2, range / 2), Quaternion.identity);
                o6 = Instantiate(shadowClonePrefab, selfPos, Quaternion.identity);

                IllusionsTransforms.AddRange(new List<GameObject>()
                {
                    o1, o2, o3, o4, o5, o6
                });

                break;

            case IllusionSpawnShape.Z:
                if (playerPos.y < selfPos.y)
                {
                    if (playerPos.x <= selfPos.x)
                    {
                        o1 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range * -1, 0), Quaternion.identity);
                        o2 = Instantiate(shadowClonePrefab, selfPos + new Vector3(0, range * -1), Quaternion.identity);
                        o3 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range * -1, range * -1), Quaternion.identity);
                    }
                    else
                    {
                        o1 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range, 0), Quaternion.identity);
                        o2 = Instantiate(shadowClonePrefab, selfPos + new Vector3(0, range * -1), Quaternion.identity);
                        o3 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range, range * -1), Quaternion.identity);
                    }
                }
                else
                {
                    if (playerPos.x <= selfPos.x)
                    {
                        o1 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range, 0), Quaternion.identity);
                        o2 = Instantiate(shadowClonePrefab, selfPos + new Vector3(0, range * -1), Quaternion.identity);
                        o3 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range, range * -1), Quaternion.identity);
                    }
                    else
                    {
                        o1 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range, 0), Quaternion.identity);
                        o2 = Instantiate(shadowClonePrefab, selfPos + new Vector3(0, range), Quaternion.identity);
                        o3 = Instantiate(shadowClonePrefab, selfPos + new Vector3(range, range), Quaternion.identity);
                    }
                }

                IllusionsTransforms.AddRange(new List<GameObject>()
                {
                    o1, o2, o3
                });

                break;

            case IllusionSpawnShape.LINE:
                o1 = Instantiate(shadowClonePrefab, playerPos + (transform.position - playerPos).normalized * -250f, Quaternion.identity);
                IllusionsTransforms.Add(o1);
                break;

            case IllusionSpawnShape.LINE_EXACT:
                o1 = Instantiate(shadowClonePrefab, playerPos, Quaternion.identity);
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

                if (Time.deltaTime > 0) SpawnIllusion();

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
    }

    [HideInInspector] public bool WarppedShroudedTriggered = false;
    IEnumerator ActivateVanishAndDashTowardPlayer()
    {
        if (WarppedShroudedTriggered) yield break;

        StopMovement();
        EndFreeze();
        EndStun();

        WarppedShroudedTriggered = true;
        
        dashCooldownTimerCountdown = dashCooldown;
        dashAtkScale += 0.3f;
        defPen += 25;

        if (sfxs[2]) sfxs[2].Play();

        yield return StartCoroutine(Vanish(3f));
        dashCooldownTimerCountdown = dashCooldown - 3f;

        float ReappearTime = 0.5f, minReappearTime = 0.35f;
        while (true)
        {
            yield return new WaitUntil(() => dashCooldownTimerCountdown <= 0 && SpottedPlayer);

            float minOffset = Random.Range(100f, 400f);
            Vector3 randomDirection = new Vector3(Random.Range(-1000, 1000), Random.Range(-1000, 1000)).normalized;
            Vector3 appearSpot = SpottedPlayer.transform.position
                    + randomDirection * minOffset;
            transform.position = appearSpot;
            if (SpottedPlayer) FaceToward(SpottedPlayer.transform.position);

            Color appearColor = new(InitSpriteColor.r, InitSpriteColor.g, InitSpriteColor.b, 0.25f);
            
            float c = 0, d = Mathf.Max(ReappearTime, minReappearTime);
            ReappearTime -= 0.05f;

            while (c < d)
            {
                shadowSpriteRend.color = Color.Lerp(Color.clear, shadowInitColor, c * 1.0f / d);
                spriteRenderer.color = Color.Lerp(spriteRenderer.color, appearColor, c * 1.0f / d);
                c += Time.deltaTime;
                yield return null;
            }

            shadowSpriteRend.color = shadowInitColor;

            if (SpottedPlayer) yield return StartCoroutine(DashAttack());
            else dashCooldownTimerCountdown = dashCooldown;

            yield return StartCoroutine(Vanish());
        }
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
        EndFreeze();
        EndStun();
        ClearAllEffects();
        canRevive = false;
        reviving = true;

        foreach (var item in colliders)
        {
            item.enabled = false;
        }

        foreach (var illusion in IllusionsTransforms)
        {
            if (illusion) Destroy(illusion);
        }
        IllusionsTransforms.Clear();
        IsDashing = false;
        dashCooldown = 11f;
        dashCooldownTimerCountdown = 0f;
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
            }
            yield return null;
        }

        reviving = false;
        animator.SetTrigger("revive");
        health = mHealth;
        SetHealth(health);

        def = bDef = (short) (bDef * 0.25f);
        res = bRes = 0;
        StartCoroutine(AutoBuildupFeud());

        foreach (var item in colliders)
        {
            item.enabled = true;
        }
    }

    void CheckForWrappedShrouded()
    {
        if (WarppedShroudedTriggered) return;
        if (IsDashing || animator.GetBool("attack") || CurrentPhase != 2 || health >= mHealth * WrappedShroudHpTriggerThreshold) return;

        StartCoroutine(ActivateVanishAndDashTowardPlayer());
    }

    IEnumerator Vanish(float d = 1f)
    {
        StopMovement();
        foreach (var cld in colliders) cld.enabled = false;
        IsStunImmune = IsFreezeImmune = IsShiftImmune = true;

        Color end = new(InitSpriteColor.r, InitSpriteColor.g, InitSpriteColor.b, 0);
        float c = 0;

        while (c < d)
        {
            spriteRenderer.color = Color.Lerp(InitSpriteColor, end, c * 1.0f / d);
            shadowSpriteRend.color = Color.Lerp(shadowInitColor, Color.clear, c * 1.0f / d);

            canvasGroup.alpha = Mathf.Lerp(1, 0, c * 1.0f / d);
            c += Time.deltaTime;
            yield return null;
        }

        spriteRenderer.color = end;
        shadowSpriteRend.color = Color.clear;
        canvasGroup.alpha = 0;
    }

    IEnumerator AutoBuildupFeud()
    {
        float interval = 1f;
        while (isActiveAndEnabled && IsAlive())
        {
            yield return new WaitForSeconds(interval);
            if (SpottedPlayer && CurrentPhase == 2)
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
            $"• [Feud Bonding] Receiving damage from the player unit builds up \"Feud\" on them (has a limit). Self takes reduced damage from the attacker based on how much \"Feud\" they have built up, up to {MaxDamageReduction * 100}%. " +
            "Gains greatly increased ATK and MSPD when the target has their \"Feud\" maxed out.\n\n" +
            "<b><color=red>FIRST PHASE</color></b>\n\n" +
            "• [Pack Up] Stops moving, becomes immune to freeze and stun, then charges up and quickly dash toward your position, dealing physical damage if comes into contact with. Receving damage shortens the cool-down of the next use.\n\n" +
            "• [Held Breath] When HP reaches 0, enters a revival state and rapidly builds up \"Feud\" on the presenting player throughout the duration. Enters the second phase afterward.\n\n" +
            
            "<b><color=red>SECOND PHASE</color></b>\n\n" +
            "• DEF and RES are reduced.\n\n" +
            "• [Overflowing Hatred] \"Feud\" is now gradually builds up overtime, and 'slows' the player at max stacks.\n\n" +
            "• [Toward Death] Stops moving, becomes immune to freeze and stun, then creates multiple illusion of self. After a short delay, dashes toward these illusions in quick succession and damages the player if comes into contact with. " +
            "Dash speed increases as HP decreases. Receving damage shortens the cool-down of the next use.\n\n" +
            $"• [Wrapped Shroud] When HP falls below {WrappedShroudHpTriggerThreshold * 100}% for the first time: Stops attacking, becomes invisible and invulnerable. " +
            "Periodically reappears to cast [Toward Death], disables 'Invisibility' during the process and resumes them after finished.";
        
        TooltipsDescription = "Assassin who has abandoned his name and covered his face. Behind that pall is a burning fanaticism and a destined fate.";
        base.WriteStats();
    }
}
