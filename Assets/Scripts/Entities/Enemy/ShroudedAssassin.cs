using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ShroudedAssassin : EnemyBase
{
    [SerializeField] private GameObject shadowClonePrefab;
    [SerializeField] private Image feudBond;
    [SerializeField] private float dashCooldown = 30f;
    [SerializeField] private float dashDuration = 0.1f;
    [SerializeField] private float dashInterval = 0.1f;
    [SerializeField] private float dashDistance = 600f;
    public enum IllusionSpawnShape
    {
        STAR,
        TRIANGLE,
        LINE,
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

        ProcessFeudBond();
        if (dashCooldownTimer > 0) dashCooldownTimer -= Time.fixedDeltaTime;
        
        DashScanCnt++;
        if (DashScanCnt >= 25 && CanUseDash)
        {
            DashScanCnt = 0;
            var player = SearchForNearestEntityAroundCertainPoint(typeof(PlayerBase), transform.position, dashDistance * 0.7f);
            if (player) StartCoroutine(DashAttack());
        }
    }

    private PlayerBase PrevSpottedPlayer = null;
    private float SpottedTimer = 0f;
    void ProcessFeudBond()
    {
        if (SpottedPlayer && PrevSpottedPlayer == SpottedPlayer && SpottedPlayer.IsAlive())
        {
            if (SpottedPlayer)
            {
                feudBond.transform.position = SpottedPlayer.transform.position + new Vector3(0, 100, 0);
                feudBond.fillAmount = Mathf.Clamp01(SpottedTimer / MaxSpottedTimer);

                if (SpottedTimer < MaxSpottedTimer)
                {
                    SpottedTimer += Time.fixedDeltaTime;
                    feudBond.color = new Color(Color.red.a, Color.red.g, Color.red.b, 0.35f);
                    if (SpottedTimer >= MaxSpottedTimer)
                    {
                        SpottedPlayer.ApplyEffect(Effect.AffectedStat.MSPD, "ASSASSIN_SLOW", -90, 1.5f, true);
                    }
                }
                else
                {
                    feudBond.color = Color.red;
                    ApplyEffect(Effect.AffectedStat.MSPD, "ASSASSIN_MARK_MSPD_BUFF", 33, 0.2f, true);
                }
            }
            else SpottedTimer = 0f;
        }
        else
        {
            feudBond.fillAmount = 0f;
            SpottedTimer = 0f;
            PrevSpottedPlayer = SpottedPlayer;
        }
    }

    [SerializeField] private float MaxSpottedTimer = 10f;
    [SerializeField] private float MaxDamageReduction = 0.8f;
    public override void TakeDamage(DamageInstance damage, EntityBase source)
    {
        if (source == SpottedPlayer)
        {
            float ratio = Mathf.Lerp(1f, 0f, SpottedTimer / MaxSpottedTimer);
            float reduction = 1 - ratio * MaxDamageReduction;
            damage.Multiply(reduction);
        }
        else damage.SetTotal(1);

        if (damage.TotalDamage > 0) dashCooldownTimer--;
        base.TakeDamage(damage, source);
    }

    public override void Move()
    {
        if (IsDashing) return;
        base.Move();
    }

    public override IEnumerator Attack()
    {
        if (IsDashing) yield break;
        yield return StartCoroutine(base.Attack());
    }

    private bool dashDoesDamage = true;
    private List<GameObject> IllusionsTransforms = new();
    IEnumerator DashAttack()
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
        dashCooldownTimer = dashCooldown;
        IsDashing = true;
        IllusionSpawnShape shape = (IllusionSpawnShape) Random.Range(0, Enum.GetValues(typeof(IllusionSpawnShape)).Length);
        if (canRevive) shape = IllusionSpawnShape.LINE;

        animator.SetTrigger("skill_prep");

        Vector3 selfPos = transform.position;
        float range = Mathf.Clamp(Vector3.Distance(transform.position, SpottedPlayer.transform.position) * 2, dashDistance * 0.9f, dashDistance);

        Vector3 playerPos = SpottedPlayer.transform.position; 
        
        switch (shape)
        {
            case IllusionSpawnShape.STAR:
                GameObject o1, o2, o3, o4;

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
                        DealDamage(player, (int)(atk * 0.75f));
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
                    DealDamage(player, (int)(atk * 0.75f));
                }
            }
            transform.position = illusion.transform.position;
            Destroy(illusion);

            if (i + 1 < IllusionsTransforms.Count) FaceToward(IllusionsTransforms[i + 1].transform.position);
            yield return new WaitForSeconds(dashScaleInt);
        }

        dashDoesDamage = false;
        
        FaceToward(SpottedPlayer.transform.position);

        animator.SetTrigger("skill_end");
        yield return new WaitForSeconds(0.6f);

        animator.SetTrigger("skill_prep");
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

        IsDashing = false;
        IsFreezeImmune = IsStunImmune = false;
        IllusionsTransforms.Clear();
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
        foreach (var illusion in IllusionsTransforms)
        {
            if (illusion) Destroy(illusion);
        }
        IllusionsTransforms.Clear();
        IsDashing = false;
        IsFreezeImmune = IsStunImmune = false;
        dashCooldown = 12f;
        dashCooldownTimer = dashCooldown;
        animator.SetBool("attack", false);
        return base.Revive();
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
            "• [Feud Bonding] Gradually builds up \"Feud\" on the player unit (reset upon swapping). " +
            "Greatly reduces all damage taken, however this becomes less effective the more \"Feud\" the attacker has.\n\n" +
            "<b><color=red>FIRST PHASE</color></b>\n\n" +
            "• [Pack Up] Stops moving, then charges up and quickly dash toward the player, dealing physical damage to the player if comes into contact with.\n" +
            "• [Held Breath] Receiving damage reduces [Pack Up]'s cooldown. When \"Feud\" reaches max, slows the current player for a brief seconds.\n\n" +
            "<b><color=red>SECOND PHASE</color></b>\n\n" +
            "• [Toward Death] Stops moving, then creates multiple illusion of self. After a short delay, dashes toward these illusions in quick succession and damages the player if coming into contact with them. " +
            "Dashes' speed increases as HP decreases.";
        
        TooltipsDescription = "Assassin who has abandoned his name and covered his face. Behind that pall is a burning fanaticism and a destined fate.";
        base.WriteStats();
    }
}
