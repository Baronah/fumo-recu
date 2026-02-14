using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMelee : PlayerBase
{
    [SerializeField] private GameObject IllusionPrefab, AfterimagePrefabs;
    [SerializeField] private float DashSpeed = 3500f;
    [SerializeField] private float DashDuration = 0.5f;
    [SerializeField] private float DashCooldown = 6f;

    [SerializeField] private GameObject SkillEffect, SkillEffect_2, AftershockEffect, AftershockEffect_2, BlackflashEffect, SwirlEffect, CounterEffect;
    [SerializeField] private float SkillCooldown = 30f;
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

    [SerializeField] float AfterShockDamageConversionRatio = 0.75f;
    [SerializeField] TMP_Text AftershockDmgCounter;

    [SerializeField] AudioSource Ambient, Vortex, Shock, Counter;
    
    private bool IsSkillActive = false, IsDashing = false, CanUseSkill = true, CanUseDash = true;

    private HashSet<EntityBase> EnemyHitByDash = new HashSet<EntityBase>();
    bool Debut = false;

    private PlayerMeleeAfterimage DashAfterImages = null;

    public override PlayerManager.PlayerType GetPlayerType()
    {
        return PlayerManager.PlayerType.MELEE;
    }

    public override void InitializeComponents()
    {
        Ambient.volume = PlayerPrefs.GetFloat("SFX", 1.0f);
        SkillBar = SkillBarObj.GetComponentInChildren<Slider>();
        SkillEffectColor = SkillEffect.GetComponent<SpriteRenderer>().color;

        dashCooldownTimer = DashCooldown;
        skillCooldownTimer = SkillCooldown;

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
        if (Skills.Contains(SkillTree_Manager.SkillName.WINGED_STEPS_C))
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

        if (Skills.Contains(SkillTree_Manager.SkillName.BEYOND_NIGHT))
        {
            SpeedBoost += 0.25f;
            AtkBoost = -1f;
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
            UseSkill();
        }
        else if (Input.GetKeyDown(InputManager.Instance.SpecialKey))
        {
            UseSpecial();
        }
        else
        {
            Move();
        }
    }

    float dashCooldownTimer = 0f;
    IEnumerator DashLockout()
    {
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

    void ReduceSpecialCooldown(float amount)
    {
        if (dashCooldownTimer >= DashCooldown) return;
        dashCooldownTimer += amount;
        if (dashCooldownTimer > DashCooldown) dashCooldownTimer = DashCooldown;
        StartCoroutine(playerManager.SpecialCooldown(DashCooldown, dashCooldownTimer));
    }

    float skillCooldownTimer = 0;
    IEnumerator SkillLockout()
    {
        skillCooldownTimer = 0;
        CanUseSkill = false;
        StartCoroutine(playerManager.SkillCooldown(SkillCooldown, skillCooldownTimer));
        while (skillCooldownTimer < SkillCooldown)
        {
            skillCooldownTimer += Time.deltaTime;
            yield return null;
        }
        CanUseSkill = true;
    }

    void ReduceSkillCooldown(float amount)
    {
        if (skillCooldownTimer >= SkillCooldown) return;
        skillCooldownTimer += amount;
        if (skillCooldownTimer > SkillCooldown) skillCooldownTimer = SkillCooldown;
        StartCoroutine(playerManager.SkillCooldown(SkillCooldown, skillCooldownTimer));
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
        if (!CanUseDash || DashAfterImages) yield break;

        if (Skills.Contains(SkillTree_Manager.SkillName.WIND_ANTHEM)) ReduceSkillCooldown(IsWindAnthemMaxed ? 6f : 4f);
        StartCoroutine(DashLockout());
        IsDashing = true;
        Debut = false;
        healedOnThisDash = false;

        if (Skills.Contains(SkillTree_Manager.SkillName.DASH_AFTERIMAGES))
        {
            GameObject o = Instantiate(AfterimagePrefabs, transform.position, Quaternion.identity);
            DashAfterImages = o.GetComponent<PlayerMeleeAfterimage>();
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

                            DealDamage(enemy, atk, 0, 0);
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
                ApplyEffect(Effect.AffectedStat.MSPD, "WINGED_STEPS_A_MSPD_BUFF", 30f, 2f, true, EffectPersistType.DECAY);
            }
            if (Skills.Contains(SkillTree_Manager.SkillName.WIND_ANTHEM)) ReduceSpecialCooldown(IsWindAnthemMaxed ? 1.5f : 0.75f);
        }

        float atk = this.atk;
        if (Skills.Contains(SkillTree_Manager.SkillName.HEAVY_HITTER))
        {
            atk *= GetHeavyHitterMultiplier();
        }

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
            else DealDamage(target, (int) atk);
        }

        timerSinceLastAttack = 0f;
        yield return null;
    }

    public override void UseSkill()
    {
        if (!CanUseSkill || IsSkillActive) return;

        base.UseSkill();
        StartCoroutine(ActivateSkill(SkillDuration));
    }

    bool extendSkillDuration = false;
    float damageTakenDuringSkill = 0;
    float juggernauntCurrentDuration = 0;
    IEnumerator ActivateSkill(float duration, bool fromInherit = false)
    {
        if (!IsAlive() || IsSkillActive || !CanUseSkill) yield break;
        if (!fromInherit) StartCoroutine(SkillLockout());

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
            ApplyEffect(Effect.AffectedStat.ATK, "BEYOND_THE_NIGHT", -100f, 999f, true, EffectPersistType.PERSIST, false);
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
                float bonus = 0.75f;
                juggernauntCurrentDuration -= bonus;
                durationAdded += bonus;
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
            enemy.ApplyEffect(Effect.AffectedStat.MSPD, "JUGGERNAUNT_PULL_DEBUFF_MSPD", -40f, 1.25f, true);
            enemy.ApplyEffect(Effect.AffectedStat.ASPD, "JUGGERNAUNT_PULL_DEBUFF_ASPD", -33f, 1.25f, false);
        }
    }

    void ProcessDoT()
    {
        var enemies = SearchForEntitiesAroundCertainPoint(typeof(EnemyBase), transform.position, DoTRadius, true);
        string Key = "JUGGERNAUT_IGNITE_DOT";

        foreach (EntityBase enemy in enemies)
        {
            DealDamage(enemy, (int)(atk * 0.34f), 0, 0);
            if (enemy.DefDebuffs.ContainsKey(Key))
                enemy.ApplyEffect(Effect.AffectedStat.DEF, Key, -(15f + enemy.DefDebuffs[Key].Value * 0.85f), 1.5f, true);
            else
                enemy.ApplyEffect(Effect.AffectedStat.DEF, Key, -15f, 1.5f, true);
        }
    }

    public override void OnFieldSwapOut(PlayerBase swapInPlayer)
    {
        base.OnFieldSwapOut(swapInPlayer);
        if (swapInPlayer && Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_SHINDOUKAKU)
            && IsSkillActive)
        {
            PlayerRanged ranged = swapInPlayer.GetComponent<PlayerRanged>();
            ranged.SetJuggernauntInherit(SkillDuration - juggernauntCurrentDuration,
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
                enemy.ApplyEffect(Effect.AffectedStat.MSPD, "AFTERSHOCK_MSPD_DEBUFF", -99f, 1f, true, EffectPersistType.DECAY);
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
                Heal(mHealth * 0.2f);
                healedOnThisDash = true;
            }
        }

        if (IsSkillActive && IsAlive() && damage.TotalDamage > 0)
        {
            extendSkillDuration = Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_DURATION);
            if (Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_AFTERSHOCK))
            {
                float postDamage = damageTakenDuringSkill + damage.TotalDamage * AfterShockDamageConversionRatio;
                if (damageTakenDuringSkill <= 0)
                {
                    SkillEffect.GetComponent<SpriteRenderer>().color = new(1, 0.6f, 0, 0.25f);
                }

                StartCoroutine(CountUpAftershockDmg(damageTakenDuringSkill, postDamage));
                damageTakenDuringSkill = postDamage;
            }
        }
        
        if (!IsDashing) base.TakeDamage(damage, source);
    }

    protected override void MintRevive()
    {
        dashCooldownTimer = DashCooldown;
        skillCooldownTimer = SkillCooldown;
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
            info.SpecialName = "Evasion - Gentle Touch";
            info.SpecialText =
                $"Dash a short distance toward the movement direction and briefly becomes invulnerable during the process, hitting an enemy extends the dash's duration.";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.DASH_LETHAL))
        {
            info.SpecialName = "Evasion - Lethal Tempo";
            info.SpecialText =
                $"Dash a short distance toward the movement direction, briefly becomes invulnerable during the process, damage all enemies self coming into contact with and push them aside.";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.DASH_FAITH))
        {
            info.SpecialName = "Evasion - Breath of the Wind";
            info.SpecialText =
                $"Dash a short distance toward the movement direction. During the process, self becomes invulnerable while reflects all incoming attacks. " +
                $"The first successful deflection also heals self.";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.DASH_AFTERIMAGES))
        {
            info.SpecialName = "Evasion - 'Yesterday Once More'";
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
            info.SkillName = "Juggernaut - Persistence";
            info.SkillText =
                $"Immediately heals self for {BurstHeal_HpPercentage * 100}% max HP. In the next {SkillDuration} seconds: " +
                $"ATK +{AtkBoost * 100}%, DEF +{DefBoost * 100}%, RES +{ResBoost}, MSPD +{SpeedBoost * 100}% and " +
                $"regenerate {HealPerSecond_HpPercentage * 100}% max HP every second. Receiving damage during this period extends skill duration by additional 0.5s (up to +{SkillDuration}s). ";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_IGNITE))
        {
            info.SkillName = "Juggernaut - Rim of the Sun";
            info.SkillText =
                $"Immediately heals self for {BurstHeal_HpPercentage * 100}% max HP. In the next {SkillDuration} seconds: " +
                $"ATK +{AtkBoost * 100}%, DEF +{DefBoost * 100}%, RES +{ResBoost}, MSPD +{SpeedBoost * 100}%. " +
                $"Every second, regenerates {HealPerSecond_HpPercentage * 100}% max HP and deals 25% ATK physical damage to all nearby enemies. ";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_PULL))
        {
            info.SkillName = "Juggernaut - Swirling Vortex";
            info.SkillText =
                $"Immediately heals self for {BurstHeal_HpPercentage * 100}% max HP. In the next {SkillDuration} seconds: " +
                $"ATK +{AtkBoost * 100}%, DEF +{DefBoost * 100}%, RES +{ResBoost}, MSPD +{SpeedBoost * 100}%. " +
                $"Every second, regenerates {HealPerSecond_HpPercentage * 100}% max HP while reduces the MSPD and ASPD of all nearby enemies by 33% and pulls them toward self. ";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_SHINDOUKAKU))
        {
            info.SkillName = "Juggernaut - Resonance";
            info.SkillText =
                $"Immediately heals self for {BurstHeal_HpPercentage * 100}% max HP. In the next {SkillDuration} seconds: " +
                $"ATK +{AtkBoost * 100}%, DEF +{DefBoost * 100}%, RES +{ResBoost}, MSPD +{SpeedBoost * 100}%. " +
                $"Upon swapping, transfers this effect to the swapped in character and extends its duration for 2 more seconds. ";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.BEYOND_NIGHT))
        {
            info.SkillName = "Juggernaut - Beyond the Night";
            info.SkillText =
                $"Immediately heals self for {BurstHeal_HpPercentage * 100}% max HP and removes all enemy aggro. In the next {SkillDuration} seconds: " +
                $"Becomes invisible, but ATK becomes 0, DEF +{DefBoost * 100}%, RES +{ResBoost}, MSPD +{SpeedBoost * 100}%. ";
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.JUGGERNAUNT_AFTERSHOCK))
        {
            info.SkillName = "Juggernaut - Aftershock";
            info.SkillText =
                $"Immediately heals self for {BurstHeal_HpPercentage * 100}% max HP. In the next {SkillDuration} seconds: " +
                $"ATK +{AtkBoost * 100}%, DEF +{DefBoost * 100}%, RES +{ResBoost}, MSPD +{SpeedBoost * 100}% and " +
                $"regenerate {HealPerSecond_HpPercentage * 100}% max HP every second. " +
                $"After skill ends or upon swapping, deal physical damage equals to {AfterShockDamageConversionRatio * 100}% damage taken during the duration to all nearby enemies.";
        }
        else
        {
            hasUpgrade = false;
            info.SkillName = "Juggernaut";
            info.SkillText =
                $"Immediately heals self for {BurstHeal_HpPercentage * 100}% max HP. In the next {SkillDuration} seconds: " +
                $"ATK +{AtkBoost * 100}%, DEF +{DefBoost * 100}%, RES +{ResBoost}, MSPD +{SpeedBoost * 100}% and " +
                $"regenerate {HealPerSecond_HpPercentage * 100}% max HP every second. ";
        }

        info.SkillText += $"{SkillCooldown}s cooldown.";

        return info;
    }
}