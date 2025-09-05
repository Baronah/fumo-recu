using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMelee : PlayerBase
{
    [SerializeField] private GameObject IllusionPrefab;
    [SerializeField] private float DashSpeed = 3500f;
    [SerializeField] private float DashDuration = 0.5f;
    [SerializeField] private float DashCooldown = 6f;

    [SerializeField] private GameObject SkillEffect, SkillEffect_2, AftershockEffect;
    [SerializeField] private float SkillCooldown = 30f;
    [SerializeField] private float SkillDuration = 7f;
    [SerializeField] private float BurstHeal_HpPercentage = 0.35f;
    [SerializeField] private float HealPerSecond_HpPercentage = 0.05f;
    [SerializeField] private float DefBoost = 0.5f;
    [SerializeField] private float ResBoost = 10;
    [SerializeField] private float AtkBoost = 0.25f;
    [SerializeField] private float SpeedBoost = 0.35f;

    [SerializeField] float PullRadius = 400, DoTRadius = 220f, AoERadius = 300f;

    private bool IsSkillActive = false, IsDashing = false, CanUseSkill = true, CanUseDash = true;
    private short atkAdd, defAdd, resAdd, speedAdd;

    private HashSet<EntityBase> EnemyHitByDash = new HashSet<EntityBase>();

    short FUpdateCnt = 0;
    public override void FixedUpdate()
    {
        base.FixedUpdate();

        FUpdateCnt++;
        if (FUpdateCnt >= 3)
        {
            bool skillActive = IsSkillActive && IsAlive();

            SkillEffect.SetActive(skillActive);
            SkillEffect_2.SetActive(Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_IGNITE) && skillActive);
            FUpdateCnt = 0;
        }
    }

    public override void GetBonusSkill()
    {
        base.GetBonusSkill();
        if (Skills.Contains(SkillTree_Manager.SkillName.EQUIPMENT_RADIO))
        {
            DashCooldown *= 0.85f;
            SkillCooldown *= 0.85f;
        }
    }

    protected override void GetControlInputs()
    {
        if (!IsAlive()) return;

        if (Input.GetKeyDown(playerManager.AttackKey))
        {
            AttackCoroutine = StartCoroutine(Attack());
        }
        else if (Input.GetKeyDown(playerManager.SkillKey) && CanUseSkill && !IsSkillActive)
        {
            StartCoroutine(ActivateSkill());
        }
        else if (Input.GetKeyDown(playerManager.SpecialKey) && CanUseDash && !IsDashing)
        {
            StartCoroutine(Dash());
        }
        else
        {
            Move();
        }
    }

    IEnumerator DashLockout()
    {
        CanUseDash = false;
        StartCoroutine(playerManager.SpecialCooldown(DashCooldown));
        yield return new WaitForSeconds(DashCooldown);
        CanUseDash = true;
    }

    IEnumerator SkillLockout()
    {
        CanUseSkill = false;
        StartCoroutine(playerManager.SkillCooldown(SkillCooldown));
        yield return new WaitForSeconds(SkillCooldown);
        CanUseSkill = true;
    }

    public float GetDashDistance()
    {
        float distance = DashSpeed + moveSpeed * 5f;

        return distance;
    }

    IEnumerator Dash()
    {
        if (!CanUseDash) yield break;

        StartCoroutine(DashLockout());
        IsDashing = true;

        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        if (moveHorizontal == 0 && moveVertical == 0)
        {
            moveHorizontal = spriteRenderer.flipX ? -1 : 1;
        }

        if (sfxs[1]) sfxs[1].Play();
        
        bool checkForCollision = 
            Skills.Contains(SkillTree_Manager.SkillName.DASH_LETHAL) 
            ||
            Skills.Contains(SkillTree_Manager.SkillName.DASH_TOUCH);

        var movementInputs = new Vector2(moveHorizontal, moveVertical).normalized;
        
        bool allowDashes = true;
        while (allowDashes)
        {
            allowDashes = false;
            StartCoroutine(StartMovementLockout(DashDuration));
            StartCoroutine(StartAttackLockout(DashDuration));

            float invulDuration = DashDuration * 2f;
            if (Skills.Contains(SkillTree_Manager.SkillName.DASH_FAITH))
            {
                invulDuration += 0.5f;
            }

            SetInvulnerable(invulDuration);

            float dashTime = 0f;
            while (dashTime < DashDuration)
            {
                if (checkForCollision)
                {
                    var enemies = SearchForEntitiesAroundCertainPoint(typeof(EnemyBase), transform.position, 45f, true);

                    if (Skills.Contains(SkillTree_Manager.SkillName.DASH_TOUCH))
                    {
                        var enemy = enemies.FirstOrDefault(e => !EnemyHitByDash.Contains(e));
                        if (!allowDashes && enemy)
                        {
                            allowDashes = true;
                            EnemyHitByDash.Add(enemy);
                        }
                    }
                    else if (Skills.Contains(SkillTree_Manager.SkillName.DASH_LETHAL))
                    {
                        foreach (EntityBase enemy in enemies)
                        {
                            if (!enemy || !enemy.IsAlive() || EnemyHitByDash.Contains(enemy)) continue;

                            DealDamage(enemy, (int)(atk * 0.6f), 0, 0);
                            EnemyHitByDash.Add(enemy);
                        }
                    }
                }

                rb2d.velocity = CalculateMovement(movementInputs, GetDashDistance());

                animator.SetFloat("move", Mathf.Abs(moveHorizontal) + Mathf.Abs(moveVertical));

                GameObject Illusion = Instantiate(IllusionPrefab, transform.position, Quaternion.identity);
                SpriteRenderer IllusionSpriteRenderer = Illusion.GetComponentInChildren<SpriteRenderer>();
                IllusionSpriteRenderer.sprite = spriteRenderer.sprite;
                IllusionSpriteRenderer.flipX = spriteRenderer.flipX;
                IllusionSpriteRenderer.color = new Color(1, 1, 1, 0.5f);
                Destroy(Illusion, 0.2f);

                dashTime += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
        }

        yield return null;
        rb2d.velocity = Vector2.zero;
        IsDashing = false;
        if (EnemyHitByDash.Count > 0) EnemyHitByDash.Clear();
    }

    public override IEnumerator OnAttackComplete()
    {
        if (!CanAttack) yield break;
        var targets = SearchForEntitiesAroundCertainPoint(typeof(EnemyBase), AttackPosition.position, attackRange)
                    .Where(t => t && t.IsAlive());

        if (sfxs[0] && targets.Count() > 0) sfxs[0].Play();

        foreach (var target in targets)
        {
            DealDamage(target, atk);
        }
        yield return null;
    }

    bool extendSkillDuration = false;
    int damageTakenDuringSkill = 0;
    IEnumerator ActivateSkill()
    {
        if (!IsAlive() || IsSkillActive || !CanUseSkill) yield break;
        StartCoroutine(SkillLockout());

        if (sfxs[2]) sfxs[2].Play();

        IsSkillActive = true;
        bool CanPull = Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_PULL),
             CanDoT = Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_IGNITE);

        Heal(mHealth * BurstHeal_HpPercentage);
        atkAdd = (short) (bAtk * AtkBoost);
        atk += atkAdd;
        defAdd = (short) (bDef * DefBoost);
        def += defAdd;
        resAdd = (short) (ResBoost);
        res += resAdd;
        speedAdd = (short) (b_moveSpeed * SpeedBoost);
        moveSpeed += speedAdd;

        if (CanPull)
        {
            var enemies = SearchForEntitiesAroundCertainPoint(typeof(EnemyBase), transform.position, PullRadius, true);
            foreach (EntityBase enemy in enemies)
            {
                PullEntityTowards(enemy, transform, 2.5f, 0.1f);
                ApplyEffect(Effect.AffectedStat.MSPD, "JUGGERNAUNT_PULL_DEBUFF_MSPD", -40f, 1.25f, true);
            }
        }
        else if (CanDoT)
        {
            var enemies = SearchForEntitiesAroundCertainPoint(typeof(EnemyBase), transform.position, DoTRadius, true);
            foreach (EntityBase enemy in enemies)
            {
                DealDamage(enemy, (int)(atk * 0.25f), 0, 0);
            }
        }

        float c = 0, t = 0, d = SkillDuration;
        float durationAdded = 0;

        while (c < d)
        {
            c += Time.deltaTime;
            t += Time.deltaTime;

            if (extendSkillDuration && durationAdded < d)
            {
                float bonus = 0.5f;
                c -= bonus;
                durationAdded += bonus;
                extendSkillDuration = false;
            }

            if (t >= 1.0f)
            {
                Heal(mHealth * HealPerSecond_HpPercentage);
                if (CanPull)
                {
                    var enemies = SearchForEntitiesAroundCertainPoint(typeof(EnemyBase), transform.position, PullRadius, true);
                    foreach (EntityBase enemy in enemies)
                    {
                        PullEntityTowards(enemy, transform, 2.5f, 0.1f);
                        ApplyEffect(Effect.AffectedStat.MSPD, "JUGGERNAUNT_PULL_DEBUFF_MSPD", -40f, 1.25f, true);
                    }
                }
                else if (CanDoT)
                {
                    var enemies = SearchForEntitiesAroundCertainPoint(typeof(EnemyBase), transform.position, DoTRadius, true);
                    foreach (EntityBase enemy in enemies)
                    {
                        DealDamage(enemy, (int)(atk * 0.25f), 0, 0);
                    }
                }

                t = 0;
            }

            yield return null;
        }

        Heal(mHealth * HealPerSecond_HpPercentage);
        atk -= atkAdd;
        def -= defAdd;
        res -= resAdd;
        moveSpeed -= speedAdd;
        IsSkillActive = false;

        ReleaseAfterShock();
    }

    public void ReleaseAfterShock()
    {
        if (Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_AFTERSHOCK) && damageTakenDuringSkill > 0)
        {
            Instantiate(AftershockEffect, transform.position, Quaternion.identity);

            var enemies = SearchForEntitiesAroundCertainPoint(typeof(EnemyBase), transform.position, AoERadius, true);
            foreach (EntityBase enemy in enemies)
            {
                DealDamage(enemy, damageTakenDuringSkill / 2, 0, 0);
            }
        }
        damageTakenDuringSkill = 0;
    }

    public override void TakeDamage(DamageInstance damage, EntityBase source)
    {
        if (IsSkillActive && IsAlive() && damage.TotalDamage > 0)
        {
            extendSkillDuration = Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_DURATION);
            if (Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_AFTERSHOCK)) damageTakenDuringSkill += damage.TotalDamage;
        }
        base.TakeDamage(damage, source);
    }

    public override PlayerTooltipsInfo GetPlayerTooltipsInfo()
    {
        var info = base.GetPlayerTooltipsInfo();

        info.AttackText = $"Performs an attack that deals {atk} {damageType.ToString().ToLower()} damage to all enemies within range.";

        if (Skills.Contains(SkillTree_Manager.SkillName.DASH_TOUCH))
        {
            info.SpecialName = "Evasion - Gentle Touch";
            info.SpecialText =
                $"Dash a short distance toward the movement direction and briefly becomes invulnerable during the process, hitting an enemy extends the dash's duration. {DashCooldown}s cooldown.";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.DASH_LETHAL))
        {
            info.SpecialName = "Evasion - Lethal Tempo";
            info.SpecialText =
                $"Dash a short distance toward the movement direction, briefly becomes invulnerable during the process and damage all enemies self coming into contact with. {DashCooldown}s cooldown.";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.DASH_FAITH))
        {
            info.SpecialName = "Evasion - Leap of Faith";
            info.SpecialText =
                $"Dash a short distance toward the movement direction and becomes invulnerable during the process and for a brief moment afterward. {DashCooldown}s cooldown.";
        }
        else
        {
            info.SpecialName = "Evasion";
            info.SpecialText =
                $"Dash a short distance toward the movement direction and briefly becomes invulnerable during the process. {DashCooldown}s cooldown.";
        }

        bool hasUpgrade = true;
        if (Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_DURATION))
        {
            info.SkillName = "Juggernaunt - Persistence";
            info.SkillText =
                $"Immediately heals self for {BurstHeal_HpPercentage * 100}% max HP. In the next {SkillDuration} seconds: " +
                $"ATK +{AtkBoost * 100}%, DEF +{DefBoost * 100}%, RES +{ResBoost}, MSPD +{SpeedBoost * 100}% and " +
                $"regenerate {HealPerSecond_HpPercentage * 100}% max HP every second. Receiving damage during this period extends skill duration by additional 0.5s (up to +{SkillDuration}s). ";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_IGNITE))
        {
            info.SkillName = "Juggernaunt - Rim of the Sun";
            info.SkillText =
                $"Immediately heals self for {BurstHeal_HpPercentage * 100}% max HP. In the next {SkillDuration} seconds: " +
                $"ATK +{AtkBoost * 100}%, DEF +{DefBoost * 100}%, RES +{ResBoost}, MSPD +{SpeedBoost * 100}%. " +
                $"Every second, regenerates {HealPerSecond_HpPercentage * 100}% max HP and deals 25% ATK physical damage to all nearby enemies. ";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_PULL))
        {
            info.SkillName = "Juggernaunt - Swirling Vortex";
            info.SkillText =
                $"Immediately heals self for {BurstHeal_HpPercentage * 100}% max HP. In the next {SkillDuration} seconds: " +
                $"ATK +{AtkBoost * 100}%, DEF +{DefBoost * 100}%, RES +{ResBoost}, MSPD +{SpeedBoost * 100}%. " +
                $"Every second, regenerates {HealPerSecond_HpPercentage * 100}% max HP and reduces the MSPD of all nearby enemies by 30% and pulls them toward self. ";
        }
        else
        {
            hasUpgrade = false;
            info.SkillName = "Juggernaunt";
            info.SkillText =
                $"Immediately heals self for {BurstHeal_HpPercentage * 100}% max HP. In the next {SkillDuration} seconds: " +
                $"ATK +{AtkBoost * 100}%, DEF +{DefBoost * 100}%, RES +{ResBoost}, MSPD +{SpeedBoost * 100}% and " +
                $"regenerate {HealPerSecond_HpPercentage * 100}% max HP every second. ";
        }

        if (Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_AFTERSHOCK))
        {
            if (!hasUpgrade) info.SkillName = "Juggernaunt - Aftershock";
            info.SkillText += " After skill ends, deal physical damage equals to 50% damage taken during the duration to all nearby enemies.";
        }

        info.SkillText += $"{SkillCooldown}s cooldown.";

        return info;
    }
}