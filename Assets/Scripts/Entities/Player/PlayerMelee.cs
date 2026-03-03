using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static PlayerManager;

public class PlayerMelee : PlayerBase
{
    [SerializeField] private GameObject IllusionPrefab, AfterimagePrefabs;
    [SerializeField] private float DashSpeed = 3500f;
    [SerializeField] private float DashDuration = 0.5f;
    [SerializeField] private float DashCooldown = 6f;
    [SerializeField] private float Up_DashDamageScale = 1.0f;
    [SerializeField] private float Up_DashDeflectHealPercentage = 0.2f;
    [SerializeField] private float Up_DashAfterImagePersistTime = 3f;
    [SerializeField] private float Up_DashSlowPercentage = 40f, Up_DashSlowDuration = 3f;

    [SerializeField] private float BL_AtkScale = 2.5f, BL_AtkBuff = 100f, BL_BuffDur = 5f;

    [SerializeField] private GameObject SkillEffect, SkillEffect_2, AftershockEffect, AftershockEffect_2, BlackflashEffect, SwirlEffect, CounterEffect;
    [SerializeField] private float UltCooldown = 30f;
    [SerializeField] private float SkillDuration = 7f;
    [SerializeField] private float BurstHeal_HpPercentage = 0.35f;
    [SerializeField] private float HealPerSecond_HpPercentage = 0.05f;
    [SerializeField] private float DefBoost = 0.5f;
    [SerializeField] private float ResBoost = 10;
    [SerializeField] private float AtkBoost = 0.25f;
    [SerializeField] private float SpeedBoost = 0.35f;

    [SerializeField] private GameObject SkillBarObj;
    private Color SkillEffectColor;
    private Slider SkillBar;

    [SerializeField] float PullRadius = 400, DoTRadius = 220f, AoERadius = 250f;

    [SerializeField] float RimDoTAtkScale = 0.34f,
                           RimDefShredValue = 15f,
                           PullDebuffValue = 33f,
                           AfterShockDamageConversionRatio = 0.75f,
                           AfterShockExplodeSlowTime = 1f, 
                           TransferBonusDuration = 2f,
                           ExtendDurationOfHit = 0.75f;
    [SerializeField] TMP_Text AftershockDmgCounter;

    [SerializeField] AudioSource Ambient, Vortex, Shock, Counter;
    
    private bool IsSkillActive = false, IsDashing = false, CanUseSkill = true, CanUseDash = true;

    private HashSet<EntityBase> EnemyHitByDash = new HashSet<EntityBase>();

    private PlayerMeleeAfterimage DashAfterImages = null;

    public override PlayerManager.PlayerType GetPlayerType()
    {
        return PlayerManager.PlayerType.MELEE;
    }

    [SerializeField] Collider2D DashCollider;
    public override void InitializeComponents()
    {
        Ambient.volume = PlayerPrefs.GetFloat("SFX", 1.0f);
        SkillBar = SkillBarObj.GetComponentInChildren<Slider>();
        SkillEffectColor = SkillEffect.GetComponent<SpriteRenderer>().color;

        dashCooldownTimer = DashCooldown;
        ultCooldownTimer = UltCooldown;

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
            AftershockDmgCounter.gameObject.SetActive(skillActive && Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_AFTERSHOCK) && damageTakenDuringSkill > 0);

            SkillBarObj.transform.localPosition =
                WindanthemBar.activeSelf ? new Vector3(-0.08f, -3.2f, 0) : new Vector3(-0.08f, -2.3f, 0);

            ccBar.transform.localPosition =
                SkillBarObj.activeSelf ? SkillBarObj.transform.localPosition + new Vector3(0, -0.9f, 0) : new Vector3(-0.08f, -2.3f, 0);

            FUpdateCnt = 0;
        }
    }

    public override void GetSkillTreeEffects()
    {
        base.GetSkillTreeEffects();

        if (Skills.Contains(SkillTree_Manager.SkillName.HAIR_RIBBON) && CharacterPrefabsStorage.startingPlayer == PlayerManager.PlayerType.MELEE)
        {
            DashCooldown *= 0.9f;
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

        if (Skills.Contains(SkillTree_Manager.SkillName.BEYOND_NIGHT))
        {
            SpeedBoost += 0.25f;
        }

        if (!playerManager.FirstBlackFlash) UpgradeBlackflash();
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
            if (Skills.Contains(SkillTree_Manager.SkillName.KNOTS) && !playerManager.hasVowed)
            {
                MakeVow(SkillType.ULTIMATE);
            }

            UseSkill();
        }
        else if (Input.GetKeyDown(InputManager.Instance.SpecialKey))
        {
            if (Skills.Contains(SkillTree_Manager.SkillName.KNOTS) && !playerManager.hasVowed)
            {
                MakeVow(SkillType.SPECIAL);
            }

            UseSpecial();
        }
        else
        {
            Move();
        }
    }

    protected override void GetVow()
    {
        var skill = playerManager.GetVowSkill(this);
        if (skill == SkillType.NONE) return;
        
        if (skill == SkillType.SPECIAL)
        {
            DashCooldown *= 0.55f;
            DashDuration *= 1.5f;

            ApplyEffect(Effect.AffectedStat.ATK, "VOW_ATK_BUFF", 15f, 9999f, true, EffectPersistType.PERSIST, false);
            ApplyEffect(Effect.AffectedStat.MSPD, "VOW_MSPD_BUFF", 20f, 9999f, true, EffectPersistType.PERSIST, false);
            ApplyEffect(Effect.AffectedStat.DEF, "VOW_DEF_BUFF", 10f, 9999f, false, EffectPersistType.PERSIST, false);

            Up_DashSlowPercentage += 20f;

            Up_DashDamageScale += 1f;
            Up_DashDeflectHealPercentage += 0.2f;
            Up_DashAfterImagePersistTime = DashCooldown;

            BL_AtkBuff += 50f;
            BL_BuffDur += 1f;
        }
        else
        {
            UltCooldown *= 0.7f;
            SkillDuration += 2;
            BurstHeal_HpPercentage += 0.1f;
            HealPerSecond_HpPercentage += 0.02f;
            AtkBoost += 0.2f;
            ResBoost += 10;
            SpeedBoost += 0.15f;

            ApplyEffect(Effect.AffectedStat.DEF, "VOW_DEF_BUFF", 10f, 9999f, true, EffectPersistType.PERSIST, false);
            ApplyEffect(Effect.AffectedStat.RES, "VOW_RES_BUFF", 10f, 9999f, true, EffectPersistType.PERSIST, false);
            ApplyEffect(Effect.AffectedStat.ASPD, "VOW_ASPD_BUFF", 15f, 9999f, false, EffectPersistType.PERSIST, false);

            PullRadius += 100f;
            DoTRadius += 100f;
            AoERadius += 50f;

            RimDoTAtkScale += 0.125f;
            RimDefShredValue += 5f;

            PullDebuffValue += 17f;

            AfterShockDamageConversionRatio += 0.25f;
            AfterShockExplodeSlowTime += 0.5f;

            TransferBonusDuration += 2f;
        }
    }

    float dashCooldownTimer = 0f;
    protected override IEnumerator SpecialLockout()
    {
        StartCoroutine(base.SpecialLockout());

        dashCooldownTimer = Debut ? DashCooldown : 0f;

        CanUseDash = false;
        StartCoroutine(playerManager.SpecialCooldown(DashCooldown, dashCooldownTimer));
        while (dashCooldownTimer < DashCooldown)
        {
            dashCooldownTimer += Time.deltaTime;
            yield return null;
        }
        CanUseDash = true;
    }

    public override void ReduceSpecialCooldown(float amount, CooldownReductionType reductionType = CooldownReductionType.FLAT)
    {
        if (!IsAlive()) return;
        if (dashCooldownTimer >= DashCooldown) return;

        float reductionAmount = reductionType switch
        {
            CooldownReductionType.FLAT => amount,
            CooldownReductionType.PERCENTAGE_FULL => DashCooldown * amount,
            CooldownReductionType.PERCENTAGE_CURRENT => (DashCooldown - dashCooldownTimer) * amount,
            _ => amount,
        };

        dashCooldownTimer += reductionAmount;
        if (dashCooldownTimer > DashCooldown) dashCooldownTimer = DashCooldown;
        StartCoroutine(playerManager.SpecialCooldown(DashCooldown, dashCooldownTimer));
    }

    float ultCooldownTimer = 0f;
    protected override IEnumerator UltimateLockout()
    {
        StartCoroutine(base.UltimateLockout());

        ultCooldownTimer = 0;
        CanUseSkill = false;
        StartCoroutine(playerManager.SkillCooldown(UltCooldown, ultCooldownTimer));
        while (ultCooldownTimer < UltCooldown)
        {
            ultCooldownTimer += Time.deltaTime;
            yield return null;
        }
        CanUseSkill = true;
    }

    public override void ReduceUltimateCooldown(float amount, CooldownReductionType reductionType = CooldownReductionType.FLAT)
    {
        if (!IsAlive()) return;
        if (ultCooldownTimer >= UltCooldown) return;

        float reductionAmount = reductionType switch
        { 
            CooldownReductionType.FLAT => amount,
            CooldownReductionType.PERCENTAGE_FULL => UltCooldown * amount,
            CooldownReductionType.PERCENTAGE_CURRENT => (UltCooldown - ultCooldownTimer) * amount,
            _ => amount,
        };

        ultCooldownTimer += reductionAmount;

        if (ultCooldownTimer > UltCooldown) ultCooldownTimer = UltCooldown;
        StartCoroutine(playerManager.SkillCooldown(UltCooldown, ultCooldownTimer));
    }

    public override void UseSpecial()
    {
        if (IsDashing || playerManager.MeleeSealSkill == SkillType.SPECIAL) return;

        if (CanUseDash)
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
        float distance = DashSpeed + moveSpeed * 2f;

        return distance;
    }

    public override void Move()
    {
        if (IsDashing) return;
        base.Move();
    }

    IEnumerator Dash()
    {
        if (!CanUseDash) yield break;

        if (Skills.Contains(SkillTree_Manager.SkillName.WIND_ANTHEM)) ReduceUltimateCooldown(IsWindAnthemMaxed ? 6f : 4f);
        StartCoroutine(SpecialLockout());
        IsDashing = true;
        Debut = false;
        healedOnThisDash = false;

        DashCollider.enabled = true;

        if (Skills.Contains(SkillTree_Manager.SkillName.DASH_AFTERIMAGES))
        {
            if (DashAfterImages) Destroy(DashAfterImages.gameObject);

            GameObject o = Instantiate(AfterimagePrefabs, transform.position, Quaternion.identity);
            DashAfterImages = o.GetComponent<PlayerMeleeAfterimage>();
            DashAfterImages.SetPersist(Up_DashAfterImagePersistTime);
            DashAfterImages.GetComponentInChildren<SpriteRenderer>().flipX = spriteRenderer.flipX;
        }

        var movementInputs = InputManager.Instance.GetMovementInput();

        if (movementInputs == Vector2.zero)
        {
            movementInputs = spriteRenderer.flipX ? Vector2.left : Vector2.right;
        }

        if (sfxs[1]) sfxs[1].Play();
        
        bool checkForCollision = 
            Skills.Contains(SkillTree_Manager.SkillName.DASH_LETHAL) 
            ||
            Skills.Contains(SkillTree_Manager.SkillName.DASH_TOUCH);

        bool allowDashes = true;

        while (allowDashes)
        {
            allowDashes = false;
            StartCoroutine(StartMovementLockout(DashDuration));

            float invulDuration = DashDuration * 2f;

            Vector2 dashVelocity = CalculateMovement(movementInputs, GetDashDistance());
            rb2d.velocity = dashVelocity;

            float dashTime = 0f;
            while (dashTime < DashDuration)
            {
                if (checkForCollision)
                {
                    var enemies = SearchForEntitiesAroundCertainPoint(typeof(EnemyBase), transform.position, 60f, true);

                    if (Skills.Contains(SkillTree_Manager.SkillName.DASH_TOUCH))
                    {
                        foreach (EntityBase enemy in enemies)
                        {
                            if (enemy == null || !enemy.IsAlive()) continue;

                            enemy.ApplyEffect(Effect.AffectedStat.MSPD, "DASH_TOUCH_SLOW", -Up_DashSlowPercentage, Up_DashSlowDuration, true, EffectPersistType.DECAY);
                        }

                        var enemyForDashExtend = enemies.FirstOrDefault(e => !EnemyHitByDash.Contains(e));
                        if (!allowDashes && enemyForDashExtend)
                        {
                            allowDashes = true;
                            EnemyHitByDash.Add(enemyForDashExtend);
                        }
                    }
                    else if (Skills.Contains(SkillTree_Manager.SkillName.DASH_LETHAL))
                    {
                        foreach (EntityBase enemy in enemies)
                        {
                            if (!enemy || !enemy.IsAlive() || EnemyHitByDash.Contains(enemy)) continue;

                            DealDamage(enemy, (int)(atk * Up_DashDamageScale), 0, 0);
                            PushEntityFrom(enemy, movementInputs, 5f, DashDuration - dashTime, false);
                            EnemyHitByDash.Add(enemy);
                        }
                    }
                }

                animator.SetFloat("move", movementInputs.magnitude);

                SpawnIllusion();

                dashTime += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            if (Skills.Contains(SkillTree_Manager.SkillName.DASH_LETHAL))
            {
                var enemies = SearchForEntitiesAroundCertainPoint(typeof(EnemyBase), transform.position, 60f, true);

                foreach (EntityBase enemy in enemies)
                {
                    if (!enemy || !enemy.IsAlive() || EnemyHitByDash.Contains(enemy)) continue;

                    DealDamage(enemy, atk, 0, 0);
                    PushEntityFrom(enemy, transform, 3f, 0.1f);
                    EnemyHitByDash.Add(enemy);
                }
            }
            yield return null;
        }

        SpawnIllusion();

        yield return null;
        DashCollider.enabled = false;
        rb2d.velocity = Vector2.zero;
        IsDashing = false;
        if (EnemyHitByDash.Count > 0) EnemyHitByDash.Clear();
    }

    void SpawnIllusion()
    {
        GameObject Illusion = Instantiate(IllusionPrefab, transform.position, Quaternion.identity);
        SpriteRenderer IllusionSpriteRenderer = Illusion.GetComponentInChildren<SpriteRenderer>();
        IllusionSpriteRenderer.sprite = spriteRenderer.sprite;
        IllusionSpriteRenderer.flipX = spriteRenderer.flipX;
        IllusionSpriteRenderer.color = new Color(1, 1, 1, 0.5f);
        Destroy(Illusion, 0.2f);
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

        bool hasTargets = targets.Count() > 0;

        if (hasTargets)
        {
            if (sfxs[0]) sfxs[0].Play();
            if (Skills.Contains(SkillTree_Manager.SkillName.WINGED_STEPS_A))
            {
                ApplyEffect(Effect.AffectedStat.MSPD, "WINGED_STEPS_A_MSPD_BUFF", 30f, 2f + GetWindupTime(), true, EffectPersistType.DECAY);
            }
            if (Skills.Contains(SkillTree_Manager.SkillName.WIND_ANTHEM)) ReduceSpecialCooldown(IsWindAnthemMaxed ? 1.5f : 0.75f);
        }

        float atk = this.atk;
        if (Skills.Contains(SkillTree_Manager.SkillName.HEAVY_HITTER))
        {
            atk *= GetHeavyHitterMultiplier();
        }

        bool BlackFlash = IsDashing && Skills.Contains(SkillTree_Manager.SkillName.BLACKFLASH) && hasTargets;
        foreach (var target in targets)
        {
            if (BlackFlash)
                ProcessBlackFlash(target);
            else 
                DealDamage(target, (int) atk);
        }

        timerSinceLastAttack = 0f;
        yield return null;

        if (BlackFlash && playerManager.FirstBlackFlash)
        {
            playerManager.FirstBlackFlash = false;
            UpgradeBlackflash();
        }
    }

    void UpgradeBlackflash()
    {
        ApplyEffect(Effect.AffectedStat.ATK, "BLACKFLASH_PERMA_BUFF", 20f, 9999f, true, EffectPersistType.PERSIST);
        BL_AtkBuff *= 1.2f;
        BL_BuffDur *= 1.2f;
    }

    void ProcessBlackFlash(EntityBase target)
    {
        bool upgraded = !playerManager.FirstBlackFlash;
        float finalAtk = atk * BL_AtkScale;

        if (upgraded) defPen += 60;

        ApplyEffect(Effect.AffectedStat.ATK, "BLACKFLASH_ATK_BUFF", BL_AtkBuff, BL_BuffDur, true, EffectPersistType.DECAY);
        DealDamage(target, (int) finalAtk, 0, 0);
        DisplayDamage("<color=black><size=60>BLACKFLASH!</size></color>", new(0, 55));
        
        if (upgraded) defPen -= 60;
    }

    public override void UseSkill()
    {
        if (!CanUseSkill || IsSkillActive || playerManager.MeleeSealSkill == SkillType.ULTIMATE) return;

        base.UseSkill();
        StartCoroutine(ActivateSkill(SkillDuration));
    }

    bool extendSkillDuration = false;
    float damageTakenDuringSkill = 0;
    float juggernauntCurrentDuration = 0;
    IEnumerator ActivateSkill(float duration, bool fromInherit = false)
    {
        if (!IsAlive() || IsSkillActive || !CanUseSkill) yield break;
        if (!fromInherit) StartCoroutine(UltimateLockout());

        if (sfxs[2]) sfxs[2].Play();

        juggernauntCurrentDuration = 0;
        IsSkillActive = true;
        bool CanPull = Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_PULL),
             CanDoT = Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_IGNITE);
        
        Heal(mHealth * BurstHeal_HpPercentage);

        if (Skills.Contains(SkillTree_Manager.SkillName.WIND_ANTHEM))
        {
            if (AspdBuffs.ContainsKey(WindAnthemKey))
            {
                ApplyEffect(Effect.AffectedStat.ASPD, WindAnthemKey, Mathf.Min(WindAnthemAspdBuffCap, AspdBuffs[WindAnthemKey].Value + WindAnthemAspdBuffAmount), WindAnthemAspdBuffDuration, false);
                if (IsWindAnthemMaxed)
                {
                    ApplyEffect(Effect.AffectedStat.ASPD, "WIND_ANTHEM_MAXED_ASPD_BUFF", WindAnthemAspdBuffCap, WindAnthemAspdBuffDuration, false);
                    ApplyEffect(Effect.AffectedStat.MSPD, "WIND_ANTHEM_MAXED_MSPD_BUFF", WindAnthemAspdBuffCap / 2, WindAnthemAspdBuffDuration, true);
                }
            }
            else
                ApplyEffect(Effect.AffectedStat.ASPD, WindAnthemKey, WindAnthemAspdBuffAmount, WindAnthemAspdBuffDuration, false);
        }
        
        ApplyEffect(Effect.AffectedStat.ATK, "JUGGERNAUNT_SKILL_ATK_BUFF", AtkBoost * 100, 999f, true, EffectPersistType.PERSIST, false);
        ApplyEffect(Effect.AffectedStat.DEF, "JUGGERNAUNT_SKILL_DEF_BUFF", DefBoost * 100, 999f, true, EffectPersistType.PERSIST, false);
        ApplyEffect(Effect.AffectedStat.RES, "JUGGERNAUNT_SKILL_RES_BUFF", ResBoost, 999f, false, EffectPersistType.PERSIST, false);
        ApplyEffect(Effect.AffectedStat.MSPD, "JUGGERNAUNT_SKILL_MSPD_BUFF", SpeedBoost * 100, 999f, true, EffectPersistType.PERSIST, false);

        if (Skills.Contains(SkillTree_Manager.SkillName.BEYOND_NIGHT))
        {
            playerManager.ClearStageBGM(duration);
            Ambient.Play();
            SetInvisible(duration);
            ApplyEffect(Effect.AffectedStat.ATK, "BEYOND_THE_NIGHT", -200f, 9999f, true, EffectPersistType.PERSIST);
        }

        SkillBar.value = SkillBar.maxValue = duration;

        if (CanPull)
        {
            if (Vortex) Vortex.Play();
            ProcessPull();
        }
        else if (CanDoT)
        {
            ProcessDoT();
        }

        float t = 0, d = duration;
        float durationAdded = 0;

        while (juggernauntCurrentDuration < d)
        {
            juggernauntCurrentDuration += Time.deltaTime;
            t += Time.deltaTime;

            SkillBar.value = d - juggernauntCurrentDuration;
            
            if (extendSkillDuration && durationAdded < d)
            {
                juggernauntCurrentDuration -= ExtendDurationOfHit;
                durationAdded += ExtendDurationOfHit;
                extendSkillDuration = false;
            }

            if (t >= 1.0f)
            {
                ProcessJuggernautTick(CanPull, CanDoT);
                t = 0;
            }

            yield return null;
        }

        if (Vortex && Vortex.isPlaying) Vortex.Stop();

        ProcessJuggernautTick(CanPull, CanDoT);

        IsSkillActive = false;

        RemoveEffect("JUGGERNAUNT_SKILL_ATK_BUFF");
        RemoveEffect("JUGGERNAUNT_SKILL_DEF_BUFF");
        RemoveEffect("JUGGERNAUNT_SKILL_RES_BUFF");
        RemoveEffect("JUGGERNAUNT_SKILL_MSPD_BUFF");
        if (Skills.Contains(SkillTree_Manager.SkillName.BEYOND_NIGHT))
        {
            RemoveEffect("BEYOND_THE_NIGHT");
        }

        ReleaseAfterShock();
    }

    void ProcessJuggernautTick(bool CanPull, bool CanDoT)
    {
        Heal(mHealth * HealPerSecond_HpPercentage);
        if (CanPull)
        {
            ProcessPull();
        }
        else if (CanDoT)
        {
            ProcessDoT();
        }
    }

    void ProcessPull()
    {
        GameObject o = Instantiate(SwirlEffect, transform.position, Quaternion.identity);
        Destroy(o, 1.5f);
        var enemies = SearchForEntitiesAroundCertainPoint(typeof(EnemyBase), transform.position, PullRadius, true);
        foreach (EntityBase enemy in enemies)
        {
            PullEntityTowards(enemy, transform, 3f, 0.12f);
            enemy.ApplyEffect(Effect.AffectedStat.MSPD, "JUGGERNAUNT_PULL_DEBUFF_MSPD", -PullDebuffValue, 1.25f, true);
            enemy.ApplyEffect(Effect.AffectedStat.ASPD, "JUGGERNAUNT_PULL_DEBUFF_ASPD", -PullDebuffValue, 1.25f, false);
        }
    }

    void ProcessDoT()
    {
        var enemies = SearchForEntitiesAroundCertainPoint(typeof(EnemyBase), transform.position, DoTRadius, true);
        string Key = "JUGGERNAUT_IGNITE_DOT";

        foreach (EntityBase enemy in enemies)
        {
            DealDamage(enemy, (int)(atk * RimDoTAtkScale), 0, 0);
            if (enemy.DefDebuffs.ContainsKey(Key))
                enemy.ApplyEffect(Effect.AffectedStat.DEF, Key, -(RimDefShredValue + enemy.DefDebuffs[Key].Value * (100f - RimDefShredValue) / 100f), 2f, true);
            else
                enemy.ApplyEffect(Effect.AffectedStat.DEF, Key, -RimDefShredValue, 2f, true);
        }
    }

    public override void OnFieldSwapOut(PlayerBase swapInPlayer)
    {
        base.OnFieldSwapOut(swapInPlayer);
        if (swapInPlayer && Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_SHINDOUKAKU)
            && IsSkillActive)
        {
            float transferDuration = SkillDuration - juggernauntCurrentDuration + TransferBonusDuration;

            PlayerRanged ranged = swapInPlayer.GetComponent<PlayerRanged>();
            ranged.SetJuggernauntInherit(transferDuration,
                BurstHeal_HpPercentage, 
                HealPerSecond_HpPercentage,
                DefBoost,
                ResBoost,
                AtkBoost, 
                SpeedBoost
            );

        }

        ReleaseAfterShock();
        if (DashAfterImages) Destroy(DashAfterImages.gameObject);
    }

    private void ReleaseAfterShock()
    {
        if (Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_AFTERSHOCK) && damageTakenDuringSkill > 0)
        {
            if (Shock) Shock.Play();
            SkillEffect.GetComponent<SpriteRenderer>().color = SkillEffectColor;
            Instantiate(AftershockEffect, transform.position, Quaternion.identity);
            Instantiate(AftershockEffect_2, transform.position, Quaternion.identity);

            defPen += 50;
            var enemies = SearchForEntitiesAroundCertainPoint(typeof(EnemyBase), transform.position, AoERadius, true);
            foreach (EntityBase enemy in enemies)
            {
                DealDamage(enemy, (int)(damageTakenDuringSkill), 0, 0);
                enemy.ApplyEffect(Effect.AffectedStat.MSPD, "AFTERSHOCK_MSPD_DEBUFF", -99f, AfterShockExplodeSlowTime, true, EffectPersistType.DECAY);
            }
            defPen -= 50;
        }
        damageTakenDuringSkill = 0;
    }

    bool healedOnThisDash = false;
    [SerializeField] Material ColorOverlayMat;
    public override void TakeDamage(DamageInstance damage, EntityBase source, ProjectileScript projectileInfo = null)
    {
        if (IsDashing && Skills.Contains(SkillTree_Manager.SkillName.DASH_FAITH))
        {
            CounterAttack(damage, source, projectileInfo);
        }

        if (IsSkillActive && IsAlive() && damage.TotalDamage > 0)
        {
            extendSkillDuration = Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_DURATION);
            if (Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_AFTERSHOCK))
            {
                StoreDamage(damage);
            }
        }
        
        if (!IsDashing) base.TakeDamage(damage, source);
    }

    void StoreDamage(DamageInstance damage)
    {
        float postDamage =
            Mathf.Max(1, damageTakenDuringSkill + damage.TotalDamage * AfterShockDamageConversionRatio);

        if (damageTakenDuringSkill <= 0)
        {
            SkillEffect.GetComponent<SpriteRenderer>().color = new(1, 0.6f, 0, 0.25f);
        }

        StartCoroutine(CountUpAftershockDmg(damageTakenDuringSkill, postDamage));
        damageTakenDuringSkill = postDamage;
    }

    void CounterAttack(DamageInstance damage, EntityBase source, ProjectileScript projectileInfo = null)
    {
        // reflect projectile if catches one
        if (source && projectileInfo != null)
        {
            SpriteRenderer projectileRenderer = projectileInfo.GetComponent<SpriteRenderer>();
            projectileRenderer.material = ColorOverlayMat;
            projectileRenderer.color = Color.yellow;
            CreateProjectileAndShootToward(
                projectileInfo.gameObject,
                projectileInfo.DamageInstance,
                transform.position,
                source.transform.position,
                ProjectileScript.ProjectileType.CATCH_FIRST_TARGET_OF_TYPE,
                projectileInfo.TravelSpeed,
                projectileInfo.Acceleration,
                8,
                typeof(EnemyBase));
        }
        // otherwise reflect incoming attack as AOE
        else
        {
            var enemies = SearchForEntitiesAroundCertainPoint(typeof(EnemyBase), transform.position, 150f, true);
            foreach (EntityBase enemy in enemies)
            {
                DealDamage(enemy, damage.PhysicalDamage, damage.MagicalDamage, damage.TrueDamage);
            }
        }

        Instantiate(CounterEffect, transform.position, Quaternion.identity);
        if (Counter && !Counter.isPlaying) Counter.Play();
        if (!healedOnThisDash)
        {
            Heal(mHealth * Up_DashDeflectHealPercentage);
            healedOnThisDash = true;
        }
    }

    protected override void MintRevive()
    {
        dashCooldownTimer = DashCooldown;
        ultCooldownTimer = UltCooldown;
        base.MintRevive();
    }

    IEnumerator CountUpAftershockDmg(float init, float end)
    {
        float c = 0, d = 0.25f;
        while (c < d)
        {
            int displayValue = (int) Mathf.Lerp(init, end, c * 1.0f / d);
            AftershockDmgCounter.text = $"{displayValue}";

            float sizeValue = displayValue * 1.0f / (mHealth * 0.8f),
                  colorValue = displayValue * 1.0f / mHealth;
            AftershockDmgCounter.fontSize = Mathf.Lerp(24, 48, sizeValue);
            AftershockDmgCounter.color = Color.Lerp(Color.white, Color.red, colorValue);

            c += Time.deltaTime;
            yield return null;
        }

        AftershockDmgCounter.text = $"{(int) end}";
    }

    public override PlayerTooltipsInfo GetPlayerTooltipsInfo()
    {
        var info = base.GetPlayerTooltipsInfo();

        info.AttackText = $"Performs an attack that deals {atk} {damageType.ToString().ToLower()} damage to all enemies within range.";

        if (Skills.Contains(SkillTree_Manager.SkillName.DASH_TOUCH))
        {
            info.SpecialName = CharacterPrefabsStorage.GetSkillName(SkillTree_Manager.SkillName.DASH_TOUCH);
            info.SpecialText =
                $"Dash a short distance toward the movement direction and briefly becomes invulnerable during the process, " +
                $"hitting an enemy extends the dash while slowing them by {Up_DashSlowPercentage}% over {Up_DashSlowDuration} seconds.";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.DASH_LETHAL))
        {
            info.SpecialName = CharacterPrefabsStorage.GetSkillName(SkillTree_Manager.SkillName.DASH_LETHAL);
            info.SpecialText =
                $"Dash a short distance toward the movement direction, briefly becomes invulnerable during the process, deals {Up_DashDamageScale * 100}% ATK physical damage to all enemies self coming into contact with and push them alongside with the dash.";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.DASH_FAITH))
        {
            info.SpecialName = CharacterPrefabsStorage.GetSkillName(SkillTree_Manager.SkillName.DASH_FAITH);
            info.SpecialText =
                $"Dash a short distance toward the movement direction. During the process, self becomes invulnerable while reflects all incoming attacks. " +
                $"The first successful deflection also heals self for {Up_DashDeflectHealPercentage * 100}% max HP.";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.DASH_AFTERIMAGES))
        {
            info.SpecialName = CharacterPrefabsStorage.GetSkillName(SkillTree_Manager.SkillName.DASH_AFTERIMAGES);
            info.SpecialText =
                $"Dash a short distance toward the movement direction, leaving behind an afterimage and briefly becomes invulnerable during the process. The afterimage lasts " +
                $"{Up_DashAfterImagePersistTime} seconds, casting this skill again during that period will teleport you back to the its location.";
        }
        else
        {
            info.SpecialName = "Evasion";
            info.SpecialText =
                $"Dash a short distance toward the movement direction and briefly becomes invulnerable during the process.";
        }

        if (Skills.Contains(SkillTree_Manager.SkillName.BLACKFLASH))
        {
            info.SpecialText += $" Landing an attack during this period increases its damage to {BL_AtkScale * 100}% " +
                $"and grants you {BL_AtkBuff}% decaying ATK over {BL_BuffDur} seconds.";
        }
        info.SpecialText += $" {Math.Round(DashCooldown, 1)}s cooldown.";

        if (Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_DURATION))
        {
            info.SkillName = CharacterPrefabsStorage.GetSkillName(SkillTree_Manager.SkillName.JUGGERNAUNT_DURATION);
            info.SkillText =
                $"Immediately heals self for {BurstHeal_HpPercentage * 100}% max HP. In the next {SkillDuration} seconds: " +
                $"ATK +{AtkBoost * 100}%, DEF +{DefBoost * 100}%, RES +{ResBoost}, MSPD +{SpeedBoost * 100}% and " +
                $"regenerate {HealPerSecond_HpPercentage * 100}% max HP every second. Receiving damage during this period extends skill duration by additional {ExtendDurationOfHit}s (up to +{SkillDuration}s). ";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_IGNITE))
        {
            info.SkillName = CharacterPrefabsStorage.GetSkillName(SkillTree_Manager.SkillName.JUGGERNAUNT_IGNITE);
            info.SkillText =
                $"Immediately heals self for {BurstHeal_HpPercentage * 100}% max HP. In the next {SkillDuration} seconds: " +
                $"ATK +{AtkBoost * 100}%, DEF +{DefBoost * 100}%, RES +{ResBoost}, MSPD +{SpeedBoost * 100}%. " +
                $"Every second, regenerates {HealPerSecond_HpPercentage * 100}% max HP while dealing {RimDoTAtkScale * 100}% ATK physical damage to all nearby enemies and " +
                $"inflict a stacking DEF shred on them. ";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_PULL))
        {
            info.SkillName = CharacterPrefabsStorage.GetSkillName(SkillTree_Manager.SkillName.JUGGERNAUNT_PULL);
            info.SkillText =
                $"Immediately heals self for {BurstHeal_HpPercentage * 100}% max HP. In the next {SkillDuration} seconds: " +
                $"ATK +{AtkBoost * 100}%, DEF +{DefBoost * 100}%, RES +{ResBoost}, MSPD +{SpeedBoost * 100}%. " +
                $"Every second, regenerates {HealPerSecond_HpPercentage * 100}% max HP while reducing the MSPD and ASPD of all nearby enemies by {PullDebuffValue}% and pulls them toward self. ";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_SHINDOUKAKU))
        {
            info.SkillName = CharacterPrefabsStorage.GetSkillName(SkillTree_Manager.SkillName.JUGGERNAUNT_SHINDOUKAKU);
            info.SkillText =
                $"Immediately heals self for {BurstHeal_HpPercentage * 100}% max HP. In the next {SkillDuration} seconds: " +
                $"ATK +{AtkBoost * 100}%, DEF +{DefBoost * 100}%, RES +{ResBoost}, MSPD +{SpeedBoost * 100}%. " +
                $"Upon swapping, transfers this effect to the swapped in character and extends its duration for {TransferBonusDuration} more seconds. ";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.BEYOND_NIGHT))
        {
            info.SkillName = CharacterPrefabsStorage.GetSkillName(SkillTree_Manager.SkillName.BEYOND_NIGHT);
            info.SkillText =
                $"Immediately heals self for {BurstHeal_HpPercentage * 100}% max HP and removes all enemy aggro. In the next {SkillDuration} seconds: " +
                $"Becomes invisible, but ATK becomes 0, DEF +{DefBoost * 100}%, RES +{ResBoost}, MSPD +{SpeedBoost * 100}%. ";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_AFTERSHOCK))
        {
            info.SkillName = CharacterPrefabsStorage.GetSkillName(SkillTree_Manager.SkillName.JUGGERNAUNT_AFTERSHOCK);
            info.SkillText =
                $"Immediately heals self for {BurstHeal_HpPercentage * 100}% max HP. In the next {SkillDuration} seconds: " +
                $"ATK +{AtkBoost * 100}%, DEF +{DefBoost * 100}%, RES +{ResBoost}, MSPD +{SpeedBoost * 100}% and " +
                $"regenerate {HealPerSecond_HpPercentage * 100}% max HP every second. " +
                $"After skill ends or upon swapping, deal physical damage equals to {AfterShockDamageConversionRatio * 100}% damage taken during the duration to all nearby enemies.";
        }
        else
        {
            info.SkillName = "Juggernaut";
            info.SkillText =
                $"Immediately heals self for {BurstHeal_HpPercentage * 100}% max HP. In the next {SkillDuration} seconds: " +
                $"ATK +{AtkBoost * 100}%, DEF +{DefBoost * 100}%, RES +{ResBoost}, MSPD +{SpeedBoost * 100}% and " +
                $"regenerate {HealPerSecond_HpPercentage * 100}% max HP every second. ";
        }

        info.SkillText += $"{Math.Round(UltCooldown, 1)}s cooldown.";

        return info;
    }
}