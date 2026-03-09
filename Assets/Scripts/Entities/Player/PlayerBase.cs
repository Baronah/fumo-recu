using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static PlayerManager;
using static SkillTree_Manager;
using SkillType = PlayerManager.SkillType;

public class PlayerBase : EntityBase
{
    public bool SettleSwappedInPlayer = false;
    public LayerMask ObstacleLayers;
    public Sprite AttackSprite, SkillSprite, SpecialSprite;
    public string AttackDes, SkillName, SkillDes, SpecialName, SpecialDes;
    protected PlayerManager playerManager;
    protected StageManager stageManager;

    [SerializeField] protected GameObject HH_Effect_parent;
    [SerializeField] private Material HH_Fill_Material;
    [SerializeField] protected Image HH_Effect_fill;

    [SerializeField] protected GameObject RockEffect, VowEffect;
    
    [SerializeField] protected GameObject WindanthemBar, WindanthemMaxEffect;
    protected Slider WindanthemSlider;
    protected TMP_Text WindanthemCounter;

    private Transform TransformFeetposition;
    public Vector3 Feetposition => TransformFeetposition.position;

    public List<SkillName> Skills = new();

    protected Coroutine SkillCoroutine = null;

    protected string WindAnthemKey = "WIND_ANTHEM_BUFF";
    [SerializeField] protected float WindAnthemAspdBuffAmount = 15f, WindAnthemAspdBuffDuration = 15f, WindAnthemAspdBuffCap = 75f;
    protected bool IsWindAnthemMaxed => AspdBuffs.ContainsKey(WindAnthemKey) && AspdBuffs[WindAnthemKey].IsInEffect && AspdBuffs[WindAnthemKey].Value >= WindAnthemAspdBuffCap;

    public virtual PlayerType GetPlayerType()
    {
        return playerManager.PlayerStartType;
    }

    public override System.Type GetGenericType() => typeof(PlayerBase);

    private void Update()
    {
        GetControlInputs();
    }

    public override void InitializeComponents()
    {
        if (IsComponentsInitialized) return;
        ObstacleLayers = LayerMask.GetMask("Obstacle", "OnedirectionalPassage", "Border");
        stageManager = FindObjectOfType<StageManager>();
        stageManager.OnPlayerSpawn(this);

        GetSkillTreeEffects();
        HH_Effect_parent.SetActive(Skills.Contains(SkillTree_Manager.SkillName.HEAVY_HITTER));
        WindanthemSlider = WindanthemBar.GetComponentInChildren<Slider>();
        WindanthemCounter = WindanthemBar.GetComponentInChildren<TMP_Text>();

        base.InitializeComponents();
        TransformFeetposition = transform.Find("Feetposition");

        playerManager = FindObjectOfType<PlayerManager>();
        playerManager.Register(this);

        SetInvulnerable(1f);

        OnFieldEnter();
        IsComponentsInitialized = true;
    }

    [SerializeField] GameObject AllowVow;
    public override void FixedUpdate()
    {
        base.FixedUpdate();

        bool alive = IsAlive();

        if (alive)
        {
            IdeasBuff();
            AttentionBuff();
            WindBladeBuff();
        }

        WindanthemBar.SetActive(alive && AspdBuffs.ContainsKey(WindAnthemKey) && AspdBuffs[WindAnthemKey].IsInEffect);
        if (WindanthemBar.activeSelf)
        {
            WindanthemSlider.maxValue = WindAnthemAspdBuffDuration;
            WindanthemSlider.value = AspdBuffs[WindAnthemKey].Duration;

            WindanthemCounter.text = ((int)(AspdBuffs[WindAnthemKey].Value / WindAnthemAspdBuffAmount)).ToString();
        }

        WindanthemMaxEffect.SetActive(alive && IsWindAnthemMaxed);

        AllowVow.SetActive(alive && canVow && !playerManager.hasVowed);
    }

    float w_countUp = 0;
    void WindBladeBuff()
    {
        w_countUp += Time.fixedDeltaTime;
        if (w_countUp < 0.2f) return;
        w_countUp = 0;

        if (!Skills.Contains(SkillTree_Manager.SkillName.WINGED_STEPS_B)) return;

        float amount = b_moveSpeed == 0
            ? 0 
            : (moveSpeed - b_moveSpeed) / (b_moveSpeed * 0.01f);

        if (amount > 0) ApplyEffect(Effect.AffectedStat.ASPD, "WIND_BLADE_BUFF", amount * 0.7f, 0.2f, false);
    }

    float i_countUp = 0;
    void IdeasBuff()
    {
        i_countUp += Time.fixedDeltaTime;
        if (i_countUp < 1f) return;
        float interval = i_countUp;
        i_countUp = 0;

        if (!Skills.Contains(SkillTree_Manager.SkillName.WINGED_STEPS_C)) return;
        
        bool isMoving = rb2d.velocity.magnitude > 0.1f;
        if (isMoving)
        {
            ReduceUltimateCooldown(interval * 0.03f, CooldownReductionType.PERCENTAGE_FULL);
        }
        else
        {
            ReduceSpecialCooldown(interval * 0.05f, CooldownReductionType.PERCENTAGE_FULL);
        }
    }

    float a_countUp = 0;
    void AttentionBuff()
    {
        a_countUp += Time.fixedDeltaTime;
        if (a_countUp < 0.2f) return;
        a_countUp = 0;

        if (health > mHealth * 0.8f && Skills.Contains(SkillTree_Manager.SkillName.ATTENTION_BOOK))
        {
            ApplyEffect(Effect.AffectedStat.ATK, "ATTENTION_BUFF", 25, 0.25f, true);
            ApplyEffect(Effect.AffectedStat.ASPD, "ATTENTION_BUFF", 30, 0.25f, true);
        }
        
        if (health <= mHealth * 0.6f && Skills.Contains(SkillTree_Manager.SkillName.ATTENTION_DEVICE))
        {
            ApplyEffect(Effect.AffectedStat.DEF, "ATTENTION_BUFF", 35, 0.25f, false);
            ApplyEffect(Effect.AffectedStat.RES, "ATTENTION_BUFF", 25, 0.25f, false);
        }
    }

    public override void UpdateCooldowns()
    {
        base.UpdateCooldowns();
        if (Skills.Contains(SkillTree_Manager.SkillName.HEAVY_HITTER))
        {
            timerSinceLastAttack += Time.deltaTime;
            HH_Effect_fill.fillAmount = Mathf.Lerp(0, 1f, timerSinceLastAttack / heavyHitterMaxTimer);
            HH_Effect_fill.color = IsHeavyHitterMaxed ? Color.white : new(0.81f, 0.12f, 0.12f);
            HH_Effect_fill.material = IsHeavyHitterMaxed ? null : HH_Fill_Material;
        }
    }

    public virtual void OnFieldEnter()
    {
        GetVow();
        if (Skills.Contains(SkillTree_Manager.SkillName.SWAP_START_ATK))
        {
            Heal((mHealth - health) * 0.3f);
            ApplyEffect(Effect.AffectedStat.ATK, "SWAP_START_ATKBUFF", 75f, 5f, true, EffectPersistType.PERSIST);
        }
    }

    protected bool FireWorkStarted = false;
    protected IEnumerator FireWork_Special()
    {
        FireWorkStarted = true;

        float duration = 5f, c = 0f, intervalCount = 0, interval = 0.5f;
        while (c < duration)
        {
            if (intervalCount >= interval)
            {
                ReduceSpecialCooldown(interval * 2f, CooldownReductionType.FLAT);
                intervalCount = 0;
            }

            intervalCount += Time.deltaTime;
            c += Time.deltaTime;
            yield return null;
        }
    }

    protected bool Debut = false;
    protected virtual IEnumerator SpecialLockout()
    {
        if (!Debut && !FireWorkStarted && Skills.Contains(SkillTree_Manager.SkillName.MAJOR_DEBUT))
            StartCoroutine(FireWork_Special());
        yield return null;
    }

    protected virtual IEnumerator UltimateLockout()
    {
        yield return null;
    }

    protected bool canVow = false;
    private List<SkillTree_Manager.SkillName> RockBonusSkill = new()
    {
        SkillTree_Manager.SkillName.WINGED_STEPS_A,
        SkillTree_Manager.SkillName.WINGED_STEPS_B,
        SkillTree_Manager.SkillName.WINGED_STEPS_C,
        SkillTree_Manager.SkillName.SWAP_START_ATK,
        SkillTree_Manager.SkillName.MAJOR_DEBUT,
        SkillTree_Manager.SkillName.BREAK_THE_ICE,
        SkillTree_Manager.SkillName.CERTAIN_FATES,
        SkillTree_Manager.SkillName.BUBBLE_ARTS,
        SkillTree_Manager.SkillName.HEAVY_HITTER,
        SkillTree_Manager.SkillName.SPECIAL_MSPD,
        SkillTree_Manager.SkillName.EQUIPMENT_SCOPE,
        SkillTree_Manager.SkillName.EQUIPMENT_PROVISIONS,
        SkillTree_Manager.SkillName.EQUIPMENT_BLADE,
        SkillTree_Manager.SkillName.ATTENTION_BOOK,
        SkillTree_Manager.SkillName.ATTENTION_DEVICE,
        SkillTree_Manager.SkillName.VICTORY_ATK,
        SkillTree_Manager.SkillName.VICTORY_REFRESH,
    };

    [SerializeField] private GameObject RockPickEffect;

    public virtual void GetSkillTreeEffects()
    {
        var SelectedSkills = CharacterPrefabsStorage.Skills.Keys.ToList();

        if (SelectedSkills.Contains(SkillTree_Manager.SkillName.A_NICE_LOOKING_ROCK))
        {
            RockBonusSkill.RemoveAll(s => SelectedSkills.Contains(s));

            if (RockBonusSkill.Count > 0)
            {
                SkillName bonusSkill = RockBonusSkill[Random.Range(0, RockBonusSkill.Count)];
                SelectedSkills.Add(bonusSkill);

                GameObject o = Instantiate(RockPickEffect, transform.position + new Vector3(0, 100), Quaternion.identity, transform);
                o.GetComponent<RockGachaSkill>().SetSkill(bonusSkill);
            }
        }

        foreach (var skill in SelectedSkills)
        {
            Skills.Add(skill);

            switch (skill)
            {
                case SkillTree_Manager.SkillName.WINGED_STEPS_A:
                    ASPD += 20;
                    break;

                case SkillTree_Manager.SkillName.WINGED_STEPS_B:
                    b_moveSpeed += b_moveSpeed * 0.1f;
                    break;

                case SkillTree_Manager.SkillName.EQUIPMENT_BLADE:
                    defIgn += 10;
                    break;

                case SkillTree_Manager.SkillName.EQUIPMENT_SCOPE:
                    b_attackRange *= 1.2f;
                    break;

                case SkillTree_Manager.SkillName.EQUIPMENT_PROVISIONS:
                    HealingEffectiveness += 0.25f;
                    break;

                case SkillTree_Manager.SkillName.HEAVY_HITTER:
                    ASPD -= 40;
                    bAtk += (short)(bAtk * 0.2f);
                    break;

                case SkillTree_Manager.SkillName.A_NICE_LOOKING_ROCK:
                    mHealth += (mHealth * 0.052f);
                    bAtk = (short)(bAtk * 1.052f);
                    b_moveSpeed += b_moveSpeed * 0.052f;
                    break;

                case SkillTree_Manager.SkillName.HAIR_RIBBON:
                    PlayerType playerType = GetPlayerType();

                    if (CharacterPrefabsStorage.startingPlayer == playerType)
                    {
                        bAtk = (short)(bAtk * 1.3f);

                        if (playerType == PlayerType.MELEE)
                        {
                            mHealth += (int)(mHealth * 0.15f);
                        }
                        else if (playerType == PlayerType.RANGED)
                        {
                            b_moveSpeed += b_moveSpeed * 0.2f;
                        }
                    }
                    break;

                case SkillTree_Manager.SkillName.CERTAIN_FATES:
                    weight++;
                    break;
            }
        }

        canVow = Skills.Contains(SkillTree_Manager.SkillName.KNOTS);
    }

    public virtual void OnFieldSwapOut(PlayerBase swapInPlayer)
    {
        swapInPlayer.timerSinceLastAttack = timerSinceLastAttack;
        swapInPlayer.environmentalTilesStandingOn = new(this.environmentalTilesStandingOn);

        List<Dictionary<string, Effect>> allBuffs = AllBuffs();
        foreach (var dictionary in allBuffs)
        {
            foreach (var kvp in dictionary)
            {
                Effect buff = kvp.Value;
                if (!buff.TransferOnSwap) continue;
                swapInPlayer.ApplyEffect(buff.affectedStat, kvp.Key, buff.Value, buff.Duration, buff.IsPercentage, buff.DecayOverDuration ? EffectPersistType.DECAY : EffectPersistType.PERSIST);
            }
        }

        swapInPlayer.enemyDefeatCount = enemyDefeatCount;
        swapInPlayer.specialCastCount = specialCastCount;

        swapInPlayer.SettleSwappedInPlayer = true;
    }

    protected override float GetRegenAmount()
    {
        float regenAmount = base.GetRegenAmount();
        float provisionAdd = 0;
        if (Skills.Contains(SkillTree_Manager.SkillName.EQUIPMENT_PROVISIONS))
        {
            provisionAdd = mHealth * 0.01f + (mHealth - health) * 0.025f;
        }

        return regenAmount + provisionAdd;
    }

    protected void MakeVow(PlayerManager.SkillType skillType)
    {
        if (!Skills.Contains(SkillTree_Manager.SkillName.KNOTS) || playerManager.hasVowed || skillType == SkillType.NONE) return;

        SkillType seal;

        if (skillType == SkillType.SPECIAL)
            seal = SkillType.ULTIMATE;
        else
            seal = SkillType.SPECIAL;

        playerManager.SetSealSkill(this, seal);
        GameObject vowEffect = Instantiate(VowEffect, transform.position + new Vector3(0, 100), Quaternion.identity, transform);
        
        Color vowColor = GetVowEffectColor(skillType);
        vowEffect.GetComponent<SpriteRenderer>().color = vowColor;
        vowEffect.GetComponentInChildren<Image>().color = new Color(vowColor.r, vowColor.g, vowColor.b, 0.7f);

        GetVow();
    }

    Color GetVowEffectColor(PlayerManager.SkillType skillType)
    {
        if (GetPlayerType() == PlayerType.MELEE)
        {
            return skillType switch
            {
                PlayerManager.SkillType.SPECIAL => new Color(0.83f, 0.1f, 0.1f),
                PlayerManager.SkillType.ULTIMATE => new Color(0.112f, 0.79f, 0.42f),
                _ => Color.white,
            };
        }
        else
        {
            return skillType switch
            {
                PlayerManager.SkillType.SPECIAL => new Color(0.13f, 0.52f, 1f),
                PlayerManager.SkillType.ULTIMATE => new Color(0.79f, 0.12f, 1f),
                _ => Color.white,
            };
        }
    }

    protected virtual void GetVow()
    {

    }

    protected virtual void GetControlInputs()
    {
        if (!IsAlive()) return;

        if (Input.GetKeyDown(InputManager.Instance.AttackKey))
        {
            StartCoroutine(Attack());
        }
        else Move();
    }

    short specialCastCount = 0;
    public virtual void UseSpecial()
    {
        if (Skills.Contains(SkillTree_Manager.SkillName.SPECIAL_MSPD))
        {
            float duration = Mathf.Min(3f, 1f + specialCastCount * 0.5f);
            ApplyEffect(Effect.AffectedStat.MSPD, "SPECIAL_MSPD_BUFF", 100f, duration, true, EffectPersistType.PERSIST);
            specialCastCount++;
        }
    }

    public virtual void UseSkill()
    {

    }

    public override void Move()
    {
        if (IsMovementLocked || IsBound) return;

        Vector2 movementInputs = InputManager.Instance.GetMovementInput();
        rb2d.velocity = CalculateMovement(movementInputs);

        // Calculate movement magnitude for animator
        float moveMagnitude = Mathf.Abs(movementInputs.x) + Mathf.Abs(movementInputs.y);
        animator.SetFloat("move", moveMagnitude);

        base.Move();
    }

    public override IEnumerator Attack()
    {
        if (!CanAttack || IsAttackLocked) yield break;

        MovementLockout = Mathf.Max(MovementLockout, GetWindupTime() * 1.5f);

        animator.SetBool("attack", true);
        LockoutMovementOnAttackCoroutine = StartCoroutine(LockoutMovementsOnAttack());
    }

    public override IEnumerator OnAttackComplete()
    {
        var targets = SearchForEntitiesAroundCertainPoint(typeof(EnemyBase), AttackPosition.position, attackRange);
        foreach (var target in targets)
        {
            if (!target || !target.IsAlive()) continue;
            DealDamage(target, atk);
        }

        yield return null;
    }

    public override IEnumerator LockoutMovementsOnAttack()
    {
        StartCoroutine(base.LockoutMovementsOnAttack());
        StartCoroutine(playerManager.AttackCooldown(GetAttackLockoutTime()));
        yield return null;
    }

    public void ClearAllAggro()
    {
        var enemies = EntityManager.Enemies;
        foreach (var enemy in enemies)
        {
            enemy.ChangeAggro(null);
        }
    }

    public void SetInvisible(float duration)
    {
        ClearAllAggro();
        StartCoroutine(SetInvisibleCoroutine(duration));
    }

    IEnumerator SetInvisibleCoroutine(float duration)
    {
        isInvisible = true;
        float c = 0;
        while (c < duration)
        {
            spriteRenderer.color = new Color(1, 1, 1, 0.3f);
            c += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        spriteRenderer.color = Color.white;
        isInvisible = false;
    }

    public virtual PlayerTooltipsInfo GetPlayerTooltipsInfo()
    {
        return new PlayerTooltipsInfo
        {
            Icon = Icon,
            AttackSprite = AttackSprite,
            SkillSprite = SkillSprite,
            SpecialSprite = SpecialSprite,
            attackRange = attackRange,
            attackSpeed = attackWindupTime,
            attackInterval = attackInterval,
            atk = atk,
            bAtk = bAtk,
            bDef = bDef,
            def = def,
            bRes = bRes,
            res = res,
            attackPattern = attackPattern,
            damageType = damageType,
            mHealth = Mathf.FloorToInt(mHealth),
            health = Mathf.FloorToInt(health),
            moveSpeed = moveSpeed,
            SkillName = SkillName,
            SkillText = SkillDes,
            SpecialName = SpecialName,
            SpecialText = SpecialDes,
            AttackText = "Perform an attack that deals",
        };
    }

    public override void TakeDamage(DamageInstance damage, EntityBase source, ProjectileScript projectileInfo = null)
    {
        base.TakeDamage(damage, source, projectileInfo);

        if (source && !isInvulnerable)
            playerManager.OnPlayerAttacked(damage.TotalDamage * 1.0f / (mHealth * 0.5f));
    }

    public float heavyHitterMaxTimer = 10f;
    protected float timerSinceLastAttack = 0f;
    protected float GetHeavyHitterMultiplier()
    {
        if (!Skills.Contains(SkillTree_Manager.SkillName.HEAVY_HITTER)) return 1f;
        float multiplier = 1f + Mathf.Lerp(0f, 2.5f, timerSinceLastAttack / heavyHitterMaxTimer);
        return multiplier;
    }

    protected bool IsHeavyHitterMaxed =>
        Skills.Contains(SkillTree_Manager.SkillName.HEAVY_HITTER)
        &&
        timerSinceLastAttack >= heavyHitterMaxTimer;


    readonly HashSet<EntityBase> Levitated = new();
    public override void DealDamage(EntityBase target, float pDmg, float mDmg, float tDmg, bool allowWhenDisabled = false, ProjectileScript projectileInfo = null)
    {
        if (Skills.Contains(SkillTree_Manager.SkillName.BUBBLE_ARTS) && !Levitated.Contains(target))
        {
            mDmg += (int)(atk * 0.1f);
            ApplyLevitate(target, 2.5f);
            Levitated.Add(target);
        }

        if (Skills.Contains(SkillTree_Manager.SkillName.BREAK_THE_ICE) && target.IsFrozen)
        {
            float freezeDuration = target.FreezeTimer;
            target.EndFreeze();

            int bonusDmg = (int)(target.mHealth * 0.1f + Mathf.Min(atk * 0.5f * freezeDuration, bAtk * 4f));
            tDmg += bonusDmg;
        }

        base.DealDamage(target, pDmg, mDmg, tDmg, allowWhenDisabled);
        if (!target.IsAlive())
        {
            OnEnemyDefeat(target);
        }
    }

    int enemyDefeatCount = 0;
    public virtual void OnEnemyDefeat(EntityBase enemy)
    {
        if (Skills.Contains(SkillTree_Manager.SkillName.VICTORY_ATK))
        {
            float strength = Mathf.Min(100f, 50f + 5f * enemyDefeatCount), duration = 5f;
            ApplyEffect(Effect.AffectedStat.ATK, "VICTORY_ATK_BUFF", strength, duration, true, EffectPersistType.DECAY);
            ApplyEffect(Effect.AffectedStat.MSPD, "VICTORY_MSPD_BUFF", strength, duration, true, EffectPersistType.DECAY);
        }
        else if (Skills.Contains(SkillTree_Manager.SkillName.VICTORY_REFRESH))
            ReduceSpecialCooldown(Mathf.Min(1.0f, 0.3f + 0.07f * enemyDefeatCount), CooldownReductionType.PERCENTAGE_FULL);

        enemyDefeatCount++;
    }

    public enum CooldownReductionType
    {
        FLAT,
        PERCENTAGE_FULL,
        PERCENTAGE_CURRENT,
    }

    public virtual void ReduceUltimateCooldown(float amount, CooldownReductionType reductionType = CooldownReductionType.FLAT)
    {
    }

    public virtual void ReduceSpecialCooldown(float amount, CooldownReductionType reductionType = CooldownReductionType.FLAT)
    {
    }

    public override void OnDeath()
    {
        if (!IsAlive() && playerManager.MintBlessing)
        {
            MintRevive();
            return;
        }

        base.OnDeath();
        playerManager.OnPlayerDeath();
    }

    protected virtual void MintRevive()
    {
        Heal(mHealth * 0.52f, healThroughDead: true);

        playerManager.MintBlessingRevival();
        SetInvulnerable(1.52f);

        Instantiate(RockEffect, transform.position, Quaternion.identity);
    }

    public void ResumeStageBGM()
    {
        stageManager.StageBGM.Play();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision || !collision.gameObject) return;

        FumoScript fumoScript = collision.gameObject.GetComponent<FumoScript>();
        if (fumoScript && fumoScript.ObjectiveType == FumoScript.FumoObjectiveType.PICK_UP && collision.gameObject.CompareTag("Fumo"))
        {
            stageManager.OnPlayerFumoPickup(this, collision);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(AttackPosition ? AttackPosition.position : transform.position, attackRange);
    }

    private void OnDisable()
    {
        StopMovement();
    }
}