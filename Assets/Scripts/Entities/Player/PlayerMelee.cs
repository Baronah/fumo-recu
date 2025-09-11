using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMelee : PlayerBase
{
    [SerializeField] private GameObject IllusionPrefab, AfterimagePrefabs;
    [SerializeField] private float DashSpeed = 3500f;
    [SerializeField] private float DashDuration = 0.5f;
    [SerializeField] private float DashCooldown = 6f;

    [SerializeField] private GameObject SkillEffect, SkillEffect_2, AftershockEffect, BlackflashEffect, SwirlEffect;
    [SerializeField] private float SkillCooldown = 30f;
    [SerializeField] private float SkillDuration = 7f;
    [SerializeField] private float BurstHeal_HpPercentage = 0.35f;
    [SerializeField] private float HealPerSecond_HpPercentage = 0.05f;
    [SerializeField] private float DefBoost = 0.5f;
    [SerializeField] private float ResBoost = 10;
    [SerializeField] private float AtkBoost = 0.25f;
    [SerializeField] private float SpeedBoost = 0.35f;
    [SerializeField] private GameObject SkillBarObj;
    private Slider SkillBar;

    [SerializeField] float PullRadius = 400, DoTRadius = 220f, AoERadius = 300f;
    [SerializeField] float AfterShockDamageConversionRatio = 0.75f;

    private bool IsSkillActive = false, IsDashing = false, CanUseSkill = true, CanUseDash = true;
    private short atkAdd, defAdd, resAdd, speedAdd;

    private HashSet<EntityBase> EnemyHitByDash = new HashSet<EntityBase>();
    bool Debut = false;

    private PlayerMeleeAfterimage DashAfterImages = null;

    public override void InitializeComponents()
    {
        SkillBar = SkillBarObj.GetComponentInChildren<Slider>();
        base.InitializeComponents();
    }

    short FUpdateCnt = 0;
    public override void FixedUpdate()
    {
        base.FixedUpdate();

        FUpdateCnt++;
        if (FUpdateCnt >= 5)
        {
            bool skillActive = IsSkillActive && IsAlive();

            SkillEffect.SetActive(skillActive);
            SkillEffect_2.SetActive(Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_IGNITE) && skillActive);
            BlackflashEffect.SetActive(IsAlive() && AtkBuffs.ContainsKey("BLACKFLASH_ATK_BUFF") && AtkBuffs["BLACKFLASH_ATK_BUFF"].IsInEffect);
            SkillBarObj.SetActive(skillActive);

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

        if (Input.GetKeyDown(playerManager.AttackKey))
        {
            AttackCoroutine = StartCoroutine(Attack());
        }
        else if (Input.GetKeyDown(playerManager.SkillKey))
        {
            UseSkill();
        }
        else if (Input.GetKeyDown(playerManager.SpecialKey))
        {
            UseSpecial();
        }
        else
        {
            Move();
        }
    }

    IEnumerator DashLockout()
    {
        float CD = Debut ? 0 : DashCooldown;

        CanUseDash = false;
        StartCoroutine(playerManager.SpecialCooldown(CD));
        yield return new WaitForSeconds(CD);
        CanUseDash = true;
    }

    IEnumerator SkillLockout()
    {
        CanUseSkill = false;
        StartCoroutine(playerManager.SkillCooldown(SkillCooldown));
        yield return new WaitForSeconds(SkillCooldown);
        CanUseSkill = true;
    }

    public override void UseSpecial()
    {
        if (IsDashing) return;

        if (CanUseDash && !DashAfterImages)
        {
            base.UseSpecial();
            StartCoroutine(Dash());
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.DASH_AFTERIMAGES) && DashAfterImages)
        {
            StartCoroutine(DashBackToAfterImages());
        }
    }
    
    public float GetDashDistance()
    {
        float distance = DashSpeed + moveSpeed * 5f;

        return distance;
    }

    IEnumerator Dash()
    {
        if (!CanUseDash || DashAfterImages) yield break;

        StartCoroutine(DashLockout());
        IsDashing = true;
        Debut = false;

        if (Skills.Contains(SkillTree_Manager.SkillName.DASH_AFTERIMAGES))
        {
            GameObject o = Instantiate(AfterimagePrefabs, transform.position, Quaternion.identity);
            DashAfterImages = o.GetComponent<PlayerMeleeAfterimage>();
            DashAfterImages.GetComponentInChildren<SpriteRenderer>().flipX = spriteRenderer.flipX;
        }

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

                dashTime += Time.deltaTime;
                yield return null;
            }

            yield return null;
        }

        yield return null;
        rb2d.velocity = Vector2.zero;
        IsDashing = false;
        if (EnemyHitByDash.Count > 0) EnemyHitByDash.Clear();
    }

    IEnumerator DashBackToAfterImages()
    {
        if (!DashAfterImages || !Skills.Contains(SkillTree_Manager.SkillName.DASH_AFTERIMAGES)) yield break;

        DashAfterImages.OnPlayerDashCallback();
        IsDashing = true;

        if (sfxs[1]) sfxs[1].Play();

        StartCoroutine(StartMovementLockout(DashDuration));

        float invulDuration = DashDuration;
        SetInvulnerable(invulDuration);

        bool prevFlipX = spriteRenderer.flipX;
        spriteRenderer.flipX = DashAfterImages.GetComponentInChildren<SpriteRenderer>().flipX;

        if (spriteRenderer.flipX != prevFlipX) FlipAttackPosition();

        transform.position = PrevPosition = DashAfterImages.transform.position;

        Destroy(DashAfterImages.gameObject);
        yield return null;
        rb2d.velocity = Vector2.zero;
        IsDashing = false;
    }

    public override IEnumerator OnAttackComplete()
    {
        if (!CanAttack) yield break;
        var targets = SearchForEntitiesAroundCertainPoint(typeof(EnemyBase), AttackPosition.position, attackRange)
                    .Where(t => t && t.IsAlive());

        if (sfxs[0] && targets.Count() > 0) sfxs[0].Play();

        foreach (var target in targets)
        {
            if (IsDashing && Skills.Contains(SkillTree_Manager.SkillName.BLACKFLASH))
            {
                defPen += 50;
                ApplyEffect(Effect.AffectedStat.ATK, "BLACKFLASH_ATK_BUFF", 100, 5, true, EffectPersistType.DECAY);
                DealDamage(target, (int)(atk * 2.5f), 0, 0);
                defPen -= 50;
                DisplayDamage("<color=black><size=60>BLACKFLASH!</size></color>", new(0, 50));
            }
            else DealDamage(target, atk);
        }
        yield return null;
    }

    public override void UseSkill()
    {
        if (!CanUseSkill || IsSkillActive) return;

        base.UseSkill();
        StartCoroutine(ActivateSkill());
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

        SkillBar.value = SkillBar.maxValue = SkillDuration;

        if (CanPull)
        {
            GameObject o = Instantiate(SwirlEffect, transform.position, Quaternion.identity);
            Destroy(o, 1.5f);
            
            var enemies = SearchForEntitiesAroundCertainPoint(typeof(EnemyBase), transform.position, PullRadius, true);
            foreach (EntityBase enemy in enemies)
            {
                PullEntityTowards(enemy, transform, 2.5f, 0.1f);
                enemy.ApplyEffect(Effect.AffectedStat.MSPD, "JUGGERNAUNT_PULL_DEBUFF_MSPD", -40f, 1.25f, true);
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

            SkillBar.value = d - c;

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
                    GameObject o = Instantiate(SwirlEffect, transform.position, Quaternion.identity);
                    Destroy(o, 1.5f);
                    var enemies = SearchForEntitiesAroundCertainPoint(typeof(EnemyBase), transform.position, PullRadius, true);
                    foreach (EntityBase enemy in enemies)
                    {
                        PullEntityTowards(enemy, transform, 2.5f, 0.1f);
                        enemy.ApplyEffect(Effect.AffectedStat.MSPD, "JUGGERNAUNT_PULL_DEBUFF_MSPD", -40f, 1.25f, true);
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

    public override void OnFieldSwapOut()
    {
        base.OnFieldSwapOut();
        ReleaseAfterShock();
        if (DashAfterImages) Destroy(DashAfterImages.gameObject);
    }

    private void ReleaseAfterShock()
    {
        if (Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_AFTERSHOCK) && damageTakenDuringSkill > 0)
        {
            Instantiate(AftershockEffect, transform.position, Quaternion.identity);

            var enemies = SearchForEntitiesAroundCertainPoint(typeof(EnemyBase), transform.position, AoERadius, true);
            foreach (EntityBase enemy in enemies)
            {
                DealDamage(enemy, (int)(damageTakenDuringSkill * AfterShockDamageConversionRatio), 0, 0);
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
                $"Dash a short distance toward the movement direction and briefly becomes invulnerable during the process, hitting an enemy extends the dash's duration.";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.DASH_LETHAL))
        {
            info.SpecialName = "Evasion - Lethal Tempo";
            info.SpecialText =
                $"Dash a short distance toward the movement direction, briefly becomes invulnerable during the process and damage all enemies self coming into contact with.";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.DASH_FAITH))
        {
            info.SpecialName = "Evasion - Leap of Faith";
            info.SpecialText =
                $"Dash a short distance toward the movement direction and becomes invulnerable during the process and for a brief moment afterward.";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.DASH_AFTERIMAGES))
        {
            info.SpecialName = "Evasion - Time Traveler";
            info.SpecialText =
                $"Dash a short distance toward the movement direction, leaving behind an afterimage and briefly becomes invulnerable during the process. The afterimage lasts " +
                $"3 seconds, casting this skill again during that period will teleport you back to the its location.";
        }
        else
        {
            info.SpecialName = "Evasion";
            info.SpecialText =
                $"Dash a short distance toward the movement direction and briefly becomes invulnerable during the process.";
        }

        if (Skills.Contains(SkillTree_Manager.SkillName.BLACKFLASH))
        {
            info.SpecialText += " Landing an attack during this period increases its damage to 250% and grants you a decaying ATK buff.";
        }
        info.SpecialText += $" {DashCooldown}s cooldown.";

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
                $"Every second, regenerates {HealPerSecond_HpPercentage * 100}% max HP and reduces the MSPD of all nearby enemies by 40% and pulls them toward self. ";
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
            info.SkillText += $" After skill ends, deal physical damage equals to {AfterShockDamageConversionRatio * 100}% damage taken during the duration to all nearby enemies.";
        }

        info.SkillText += $"{SkillCooldown}s cooldown.";

        return info;
    }
}