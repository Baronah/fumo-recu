using DamageCalculation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static Cinemachine.CinemachineTargetGroup;
using static Effect;
using static ProjectileScript;
using static UnityEngine.GraphicsBuffer;

public class EntityBase : MonoBehaviour
{
    [SerializeField] public string Name;
    [SerializeField] public Sprite Icon;

    [SerializeField] public int mHealth;
    [SerializeField] public short bAtk, bDef, bRes;
    [SerializeField] public short defPen, defIgn, resPen, resIgn;
    [SerializeField] public float lifeSteal, b_moveSpeed, b_attackRange, b_attackWindupTime, b_attackInterval;
    public float MIN_PHYSICAL_DMG = 0.05F, MIN_MAGICAL_DMG = 0.1F;

    public int health;
    public short atk, def, res;
    public float ASPD = 100;
    public float moveSpeed, attackRange, attackWindupTime, attackInterval;

    public float hpRegenFlat = 0, hpRegenPercentage = 0;

    public short weight = 0;
    public short damageReduction = 0, damageAmplify = 0;

    public int GetMaxHealth() => mHealth;
    public short GetHealthPercentage() => (short)Mathf.Max(1, health * 100 / mHealth);
    public short GetMissinghealthPercentage() => (short)((mHealth - health) * 100 / mHealth);

    public bool IsFreezeImmune = false, IsStunImmune = false, IsPhysicalImmune = false, IsMagicalImmune = false, canRevive = false, isInvisible = false;

    public float InvulnerableTimer = 0f;
    public bool isInvulnerable => InvulnerableTimer > 0f;
    public void SetInvulnerable(float duration, bool stack = false)
    {
        if (stack)
        {
            InvulnerableTimer += duration;
            return;
        }

        InvulnerableTimer = Mathf.Max(InvulnerableTimer, duration);
    }

    public enum DamageType { PHYSICAL, MAGICAL, TRUE }
    public DamageType damageType;

    public enum AttackPattern { MELEE, RANGED, NONE }
    public AttackPattern attackPattern;
    [SerializeField] protected GameObject ProjectilePrefab;
    [SerializeField] protected ProjectileType ProjectileType = ProjectileType.CATCH_FIRST_TARGET_OF_TYPE;
    [SerializeField] public float ProjectileSpeed = 1000;
    [SerializeField] private GameObject DamagePopup;

    protected HealthBar healthBar;
    [SerializeField] protected GameObject ccBar;
    protected Slider ccSlider;

    [SerializeField] protected AnimationClip AttackAnimation;
    protected Transform AttackPosition;
    protected SpriteRenderer spriteRenderer;
    protected Animator animator;
    protected Rigidbody2D rb2d;
    protected Collider2D[] colliders;
    public AudioSource[] sfxs;

    private GameObject ShadowSprite;

    public SpriteRenderer GetSpriteRenderer() => spriteRenderer;

    protected bool useTransformAsAttackPosition = false;
    protected Vector3 PrevPosition;
    protected Color InitSpriteColor;

    protected float MovementLockout = 0, AttackLockout = 0;
    public bool IsMovementLocked => MovementLockout > 0 || IsFrozen || IsStunned;
    public bool IsAttackLocked => AttackLockout > 0 || IsFrozen || IsStunned;

    public bool IsBeingShifted = false;

    private bool TriggeredOnDeath = false;

    public float FreezeTimer = 0f, StunTimer = 0f;

    public bool IsFrozen => FreezeTimer > 0f;
    public bool IsStunned => StunTimer > 0f;

    [SerializeField] protected float preferredMoveAnimationPlaySpeed = 1.0f, preferredAttackAnimationSpeed = 1.0f;

    protected short UpdateCounter = 0;
    protected EntityManager EntityManager;

    protected Coroutine AttackCoroutine = null, LockoutMovementOnAttackCoroutine = null;
    protected Animation attackAnimation;

    protected bool IsComponentsInitialized = false;

    public Dictionary<string, Effect>
                    AtkBuffs = new(),
                    AtkDebuffs = new(),
                    DefBuffs = new(),
                    DefDebuffs = new(),
                    ResBuffs = new(),
                    ResDebuffs = new(),
                    MspdBuffs = new(),
                    MspdDebuffs = new(),
                    AspdBuffs = new(),
                    AspdDebuffs = new();

    private List<Effect> AllEffects()
    {
        return AtkBuffs.Values
            .Concat(AtkDebuffs.Values)
            .Concat(DefBuffs.Values)
            .Concat(DefDebuffs.Values)
            .Concat(ResBuffs.Values)
            .Concat(ResDebuffs.Values)
            .Concat(MspdBuffs.Values)
            .Concat(MspdDebuffs.Values)
            .Concat(AspdBuffs.Values)
            .Concat(AspdDebuffs.Values)
            .ToList();
    }

    public virtual bool CanAttack =>
        attackPattern != AttackPattern.NONE &&
        !IsFrozen &&
        !IsStunned &&
        IsAlive();

    public bool ViewOnlyMode => FindAnyObjectByType<StageManager>() == null;

    public Vector3 GetAttackPosition()
    {
        if (useTransformAsAttackPosition) return transform.position;
        return AttackPosition ? AttackPosition.position : transform.position;
    }

    public virtual void Start()
    {
        InitializeComponents();
    }

    public virtual void InitializeComponents()
    {
        Transform Sprite = transform.Find("Sprite");
        spriteRenderer = Sprite.GetComponent<SpriteRenderer>();
        animator = Sprite.GetComponent<Animator>();
        rb2d = GetComponent<Rigidbody2D>();
        colliders = GetComponents<Collider2D>();
        sfxs = GetComponents<AudioSource>();

        ShadowSprite = spriteRenderer.transform.Find("Shadow").gameObject;

        InitSpriteColor = Color.white;
        PrevPosition = transform.position;

        health = mHealth;
        atk = bAtk;
        def = bDef;
        res = bRes;
        moveSpeed = b_moveSpeed;
        attackRange = b_attackRange;
        attackWindupTime = b_attackWindupTime;
        attackInterval = b_attackInterval;

        AttackPosition = transform.Find("AttackPosition");
        if (!AttackPosition)
        {
            AttackPosition = transform;
            useTransformAsAttackPosition = true;
        }
        if (spriteRenderer.flipX) FlipAttackPosition();

        healthBar = GetComponentInChildren<HealthBar>();
        healthBar.SetMaxHealth(mHealth);

        ccSlider = ccBar.GetComponentInChildren<Slider>();

        if (ViewOnlyMode) return;

        EntityManager = FindObjectOfType<EntityManager>();
        if (EntityManager)
        {
            EntityManager.OnEntitySpawn(this.gameObject);
        }

        StartCoroutine(OnStartCoroutine());
    }

    IEnumerator OnStartCoroutine()
    {
        Color transparentBlack = new Color(0, 0, 0, 0);
        spriteRenderer.color = transparentBlack;

        float c = 0, d = 0.25f;
        while (c < d)
        {
            spriteRenderer.color = Color.Lerp(transparentBlack, Color.black, c * 1.0f / d);
            c += Time.deltaTime;
            yield return null;
        }

        c = 0; d = 0.5f;
        while (c < d)
        {
            spriteRenderer.color = Color.Lerp(Color.black, InitSpriteColor, c * 1.0f / d);
            c += Time.deltaTime;
            yield return null;
        }

        spriteRenderer.color = InitSpriteColor;
        yield return null;
    }

    short BDB_Cnt = 0;
    public virtual void FixedUpdate()
    {
        if (!IsAlive() && !TriggeredOnDeath && !canRevive) OnDeath();

        Regen();
        UpdateCooldowns();
        UpdateEffectDurations();

        BDB_Cnt++;
        if (BDB_Cnt > 25) CalculateBuffsAndDebuffs();
        
        HandleSpriteFlipping();
        HandleAnimationSpeed();
    }

    // use negative values for debuffs
    public enum EffectPersistType
    {
        PERSIST,
        DECAY
    };

    public void ApplyEffect(AffectedStat affectedStat, string Key, float Value, float Duration, bool IsPercentageBased, EffectPersistType persistType = EffectPersistType.PERSIST)
    {
        bool IsDebuff = Value < 0;
        bool DecayOverDuration = persistType == EffectPersistType.DECAY;

        if (IsDebuff)
        {
            Value *= -1;

            switch (affectedStat)
            {
                case AffectedStat.ATK:
                    if (AtkDebuffs.ContainsKey(Key))
                        AtkDebuffs[Key].Instantiate(this, Value, Duration, IsPercentageBased, DecayOverDuration);
                    else
                        AtkDebuffs.Add(Key, new(this, Value, Duration, IsPercentageBased, DecayOverDuration)); 
                    break;

                case AffectedStat.DEF:
                    if (DefDebuffs.ContainsKey(Key))
                        DefDebuffs[Key].Instantiate(this, Value, Duration, IsPercentageBased, DecayOverDuration);
                    else
                        DefDebuffs.Add(Key, new(this, Value, Duration, IsPercentageBased, DecayOverDuration));
                    break;

                case AffectedStat.RES:
                    if (ResDebuffs.ContainsKey(Key))
                        ResDebuffs[Key].Instantiate(this, Value, Duration, IsPercentageBased, DecayOverDuration);
                    else
                        ResDebuffs.Add(Key, new(this, Value, Duration, IsPercentageBased, DecayOverDuration));
                    break;

                case AffectedStat.MSPD:
                    if (MspdDebuffs.ContainsKey(Key))
                        MspdDebuffs[Key].Instantiate(this, Value, Duration, IsPercentageBased, DecayOverDuration);
                    else
                        MspdDebuffs.Add(Key, new(this, Value, Duration, IsPercentageBased, DecayOverDuration));
                    break;

                case AffectedStat.ASPD:
                    if (AspdDebuffs.ContainsKey(Key))
                        AspdDebuffs[Key].Instantiate(this, Value, Duration, IsPercentageBased, DecayOverDuration);
                    else
                        AspdDebuffs.Add(Key, new(this, Value, Duration, IsPercentageBased, DecayOverDuration));
                    break;
            }
        }
        else
        {
            switch (affectedStat)
            {
                case AffectedStat.ATK:
                    if (AtkBuffs.ContainsKey(Key))
                        AtkBuffs[Key].Instantiate(this, Value, Duration, IsPercentageBased, DecayOverDuration);
                    else
                        AtkBuffs.Add(Key, new(this, Value, Duration, IsPercentageBased, DecayOverDuration));
                    break;

                case AffectedStat.DEF:
                    if (DefBuffs.ContainsKey(Key))
                        DefBuffs[Key].Instantiate(this, Value, Duration, IsPercentageBased, DecayOverDuration);
                    else
                        DefBuffs.Add(Key, new(this, Value, Duration, IsPercentageBased, DecayOverDuration));
                    break;

                case AffectedStat.RES:
                    if (ResBuffs.ContainsKey(Key))
                        ResBuffs[Key].Instantiate(this, Value, Duration, IsPercentageBased, DecayOverDuration);
                    else
                        ResBuffs.Add(Key, new(this, Value, Duration, IsPercentageBased, DecayOverDuration));
                    break;

                case AffectedStat.MSPD:
                    if (MspdBuffs.ContainsKey(Key))
                        MspdBuffs[Key].Instantiate(this, Value, Duration, IsPercentageBased, DecayOverDuration);
                    else
                        MspdBuffs.Add(Key, new(this, Value, Duration, IsPercentageBased, DecayOverDuration));
                    break;

                case AffectedStat.ASPD:
                    if (AspdBuffs.ContainsKey(Key))
                        AspdBuffs[Key].Instantiate(this, Value, Duration, IsPercentageBased, DecayOverDuration);
                    else
                        AspdBuffs.Add(Key, new(this, Value, Duration, IsPercentageBased, DecayOverDuration));
                    break;
            }
        }

        CalculateBuffsAndDebuffs();
    }

    public void UpdateEffectDurations()
    {
        var effects = AllEffects();
        foreach (var effect in effects)
        {
            if (!effect.IsInEffect) continue;
            effect.Duration -= Time.deltaTime;
            if (effect.DecayOverDuration) effect.Decay();

            if (!effect.IsInEffect) effect.EndEffect();
        }
    }

    float prevAtkAdd = 0, prevDefAdd = 0, prevResAdd = 0;
    float prevMspdAdd = 0, prevAspdAdd = 0;
    public void CalculateBuffsAndDebuffs()
    {
        BDB_Cnt = 0;
        
        atk -= (short) prevAtkAdd;
        def -= (short) prevDefAdd;
        res -= (short) prevResAdd;
        moveSpeed -= prevMspdAdd;
        ASPD -= prevAspdAdd;

        prevAtkAdd = prevDefAdd = prevResAdd = 0;
        prevMspdAdd = prevAspdAdd = 0;

        // Buffs
        List<Effect> atkBuffsList = new(AtkBuffs.Values.Where(a => a.IsInEffect).ToList());
        atkBuffsList.ForEach(a =>
        {
            if (a.IsPercentage)
            {
                prevAtkAdd += (bAtk * a.Value / 100);
            }
            else
            {
                prevAtkAdd += a.Value;
            }
        });

        List<Effect> defBuffsList = new(DefBuffs.Values.Where(a => a.IsInEffect).ToList());
        defBuffsList.ForEach(a =>
        {
            if (a.IsPercentage)
            {
                prevDefAdd += (bDef * a.Value / 100);
            }
            else
            {
                prevDefAdd += a.Value;
            }
        });

        List<Effect> resBuffsList = new(ResBuffs.Values.Where(a => a.IsInEffect).ToList());
        resBuffsList.ForEach(a =>
        {
            if (a.IsPercentage)
            {
                prevResAdd += (bRes * a.Value / 100);
            }
            else
            {
                prevResAdd += a.Value;
            }
        });

        List<Effect> mspdBuffsList = new(MspdBuffs.Values.Where(a => a.IsInEffect).ToList());
        mspdBuffsList.ForEach(a =>
        {
            if (a.IsPercentage)
            {
                prevMspdAdd += b_moveSpeed * a.Value / 100;
            }
            else
            {
                prevMspdAdd += a.Value;
            }
        });

        List<Effect> aspdBuffsList = new(AspdBuffs.Values.Where(a => a.IsInEffect).ToList());
        aspdBuffsList.ForEach(a =>
        {
            prevAspdAdd += a.Value;
        });

        float simAtk = (atk + prevAtkAdd),
            simDef = (def + prevDefAdd),
            simRes = (res + prevResAdd),
             simMspd = moveSpeed + prevMspdAdd,
             simAspd = ASPD + prevAspdAdd;

        // Debuffs
        List<Effect> sortedAtkDebuffs = new(AtkDebuffs.Values.Where(a => a.IsInEffect).ToList());
        sortedAtkDebuffs.Sort((a1, a2) => (int) (a2.Value - a1.Value));
        sortedAtkDebuffs.ForEach(a =>
        {
            if (a.IsPercentage)
            {
                prevAtkAdd -= (simAtk * a.Value / 100);

                simAtk -= (simAtk * a.Value / 100);
            }
            else
            {
                prevAtkAdd -= a.Value;

                simAtk -= a.Value;
            }
        });

        List<Effect> sortedDefDebuffs = new(DefDebuffs.Values.Where(a => a.IsInEffect).ToList());
        sortedDefDebuffs.Sort((a1, a2) => (int)(a2.Value - a1.Value));
        sortedDefDebuffs.ForEach(a =>
        {
            if (a.IsPercentage)
            {
                prevDefAdd -= (simDef * a.Value / 100);

                simDef -= (simDef * a.Value / 100);
            }
            else
            {
                prevDefAdd -= a.Value;
                simDef -= a.Value;
            }
        });

        List<Effect> sortedResDebuffs = new(ResDebuffs.Values.Where(a => a.IsInEffect).ToList());
        sortedResDebuffs.Sort((a1, a2) => (int)(a2.Value - a1.Value));
        sortedResDebuffs.ForEach(a =>
        {
            if (a.IsPercentage)
            {
                prevResAdd -= (simRes * a.Value / 100);
                simRes -= (simRes * a.Value / 100);
            }
            else
            {
                prevResAdd -= a.Value;
                simRes -= a.Value;
            }
        });

        List<Effect> sortedMspdDebuffs = new(MspdDebuffs.Values.Where(a => a.IsInEffect).ToList());
        sortedMspdDebuffs.Sort((a1, a2) => (int)(a2.Value - a1.Value));
        sortedMspdDebuffs.ForEach(a =>
        {
            if (a.IsPercentage)
            {
                prevMspdAdd -= (simMspd * a.Value / 100);
                simMspd -= (simMspd * a.Value / 100);
            }
            else
            {
                prevMspdAdd -= a.Value;
                simMspd -= a.Value;
            }
        });

        List<Effect> sortedAspdDebuffs = new(AspdDebuffs.Values.Where(a => a.IsInEffect).ToList());
        sortedAspdDebuffs.ForEach(a =>
        {
            if (a.IsPercentage)
            {
                prevAspdAdd -= (simAspd * a.Value / 100);
                simAspd -= (simAspd * a.Value / 100);
            }
            else
            {
                prevAspdAdd -= a.Value;
                simAspd -= a.Value;
            }
        });

        if (prevMspdAdd * -1 >= moveSpeed) prevMspdAdd = moveSpeed * -1;
        if (prevAtkAdd * -1 >= atk) prevAtkAdd = atk * -1;
        if (prevDefAdd * -1 >= def) prevDefAdd = def * -1;
        if (prevResAdd * -1 >= res) prevResAdd = res * -1;
        if (simAspd < 20) prevAspdAdd = ASPD - 20; 

        atk += (short) prevAtkAdd;
        def += (short) prevDefAdd;
        res += (short) prevResAdd;
        moveSpeed += prevMspdAdd;
        ASPD += prevAspdAdd;
    }

    private float regenTimer = 0;
    public void Regen()
    {
        regenTimer += Time.deltaTime;
        if (regenTimer < 1.0f) return;
        regenTimer = 0;
        
        if (!IsAlive()) return;

        float regenAmount = Mathf.Ceil(hpRegenFlat + mHealth * hpRegenPercentage);
        if (regenAmount <= 0) return;

        Heal(regenAmount, this, false, true);
    }

    public virtual void UpdateCooldowns()
    {
        bool PrevFrozen = FreezeTimer > 0f;
        FreezeTimer -= Time.deltaTime;
        if (FreezeTimer > 0f) OnFreezeMaintain();
        else if (PrevFrozen && FreezeTimer <= 0f) OnFreezeExit();

        bool PrevStunned = StunTimer > 0f;
        StunTimer -= Time.deltaTime;
        if (StunTimer > 0f) OnStunMaintain();
        else if (PrevStunned && StunTimer <= 0f) OnStunExit();

        AttackLockout -= Time.deltaTime;
        MovementLockout -= Time.deltaTime;

        InvulnerableTimer -= Time.deltaTime;
    }

    public virtual void HandleSpriteFlipping()
    {
        Vector3 CurrentPos = transform.position;
        float deltaX = CurrentPos.x - PrevPosition.x;

        bool PrevFlipX = spriteRenderer.flipX;
        if (Mathf.Abs(deltaX) > 0.25f)
        {
            spriteRenderer.flipX = deltaX <= 0;
        }

        if (PrevFlipX != spriteRenderer.flipX)
            FlipAttackPosition();

        PrevPosition = CurrentPos;
    }

    public virtual void FlipAttackPosition()
    {
        if (useTransformAsAttackPosition) return;

        AttackPosition.localPosition = new Vector3(
            -AttackPosition.localPosition.x,
            AttackPosition.localPosition.y,
            AttackPosition.localPosition.z
        );
    }

    public virtual void FaceToward(Vector2 position)
    {
        if (IsFrozen || IsStunned) return;

        float deltaX = position.x - transform.position.x;

        bool PrevFlipX = spriteRenderer.flipX;
        if (Mathf.Abs(deltaX) > Mathf.Epsilon)
        {
            spriteRenderer.flipX = deltaX <= 0;
        }

        if (PrevFlipX != spriteRenderer.flipX)
            FlipAttackPosition();
    }

    public virtual void HandleAnimationSpeed()
    {
        float MIN_MSPEED = preferredMoveAnimationPlaySpeed * 0.2f,
               MAX_MSPEED = preferredMoveAnimationPlaySpeed * 2,
               X_MSPD_MULTIPLIER = preferredMoveAnimationPlaySpeed - MIN_MSPEED;
        animator.SetFloat("speed_value", Mathf.Lerp(MIN_MSPEED, MAX_MSPEED, moveSpeed * (X_MSPD_MULTIPLIER / (MAX_MSPEED - MIN_MSPEED)) / b_moveSpeed));

        float MIN_ASPEED = preferredAttackAnimationSpeed * 0.2f,
           MAX_ASPEED = preferredAttackAnimationSpeed * 5,
           X_ASPD_MULTIPLIER = preferredAttackAnimationSpeed - MIN_MSPEED;
        animator.SetFloat("a_speed_value", Mathf.Lerp(MIN_ASPEED, MAX_ASPEED, ASPD * (X_ASPD_MULTIPLIER / (MAX_ASPEED - MIN_ASPEED)) / 100));
    }

    public virtual IEnumerator StartMovementLockout(float m)
    {
        StopMovement();
        MovementLockout = Mathf.Max(MovementLockout, m);
        yield return null;
    }

    public virtual IEnumerator StartAttackLockout(float m)
    {
        AttackLockout = Mathf.Max(AttackLockout, m);
        yield return null;
    }

    public virtual DamageInstance DamageOutput(EntityBase target, int pDmg, int mDmg, int tDmg)
    {
        DamagePipeline pipeline = new DamagePipeline
        {
            attacker = this,
            target = target,
            instance = new DamageInstance(pDmg, mDmg, tDmg)
        };

        pipeline.Add(new ModifyRawDamage());
        pipeline.Add(new CalculateDefense());
        pipeline.Calculate();

        return pipeline.instance;
    }

    public DamageInstance GetInstanceBasedOnDamagetype()
    {
        return GetInstanceBasedOnDamagetype(atk);
    }

    public DamageInstance GetInstanceBasedOnDamagetype(int atk)
    {
        DamageInstance instance = new DamageInstance();
        if (damageType == DamageType.PHYSICAL) instance.PhysicalDamage = atk;
        else if (damageType == DamageType.MAGICAL) instance.MagicalDamage = atk;
        else instance.TrueDamage = atk;
        return instance;
    }

    public virtual void DealDamage(EntityBase target, int damage)
    {
        if (damageType == DamageType.PHYSICAL) DealDamage(target, damage, 0, 0);
        else if (damageType == DamageType.MAGICAL) DealDamage(target, 0, damage, 0);
        else DealDamage(target, 0, 0, damage);
    }

    public virtual void DealDamage(EntityBase target, DamageInstance damage)
    {
        DealDamage(target, damage.PhysicalDamage, damage.MagicalDamage, damage.TrueDamage);
    }

    public virtual void DealDamage(EntityBase target, int pDmg, int mDmg, int tDmg, bool allowWhenDisabled = false)
    {
        if ((!allowWhenDisabled && (IsFrozen || IsStunned)) || !target || !target.IsAlive() || target.isInvulnerable) return;

        var calcDamage = DamageOutput(target, pDmg, mDmg, tDmg);

        target.TakeDamage(calcDamage, this);

        if (calcDamage.TotalDamage <= 0) return;
        OnSuccessfulAttack(target, calcDamage);
    }

    public virtual void OnSuccessfulAttack(EntityBase target, DamageInstance damage)
    {
        if (lifeSteal > 0)
        {
            Heal(damage.TotalDamage * lifeSteal);
        }
    }

    public virtual void TakeDamage(DamageInstance damage, EntityBase source)
    {
        if (!this || !this.IsAlive() || this.isInvulnerable) return;

        OnAttackReceive(source);
        ShowDamageDealt(damage);
        AdjustHealthOnDamageReceive(damage);
        if (damage.TotalDamage > 0) StartCoroutine(PulseSprite());
    }

    public void InstaKill()
    {
        if (!IsAlive()) return;
        canRevive = false;
        health = 0;
        healthBar.SetHealth(0);
        OnDeath();
    }

    public virtual void OnAttackReceive(EntityBase source)
    {

    }

    public void ShowDamageDealt(DamageInstance damage)
    {
        if (damage.TotalDamage == 0) return;

        string dmgTxt = string.Empty;

        bool hasMoreThanOneDamageType = false;
        if (damage.PhysicalDamage > 0)
        {
            dmgTxt += $"<color=red>{damage.PhysicalDamage}";
            hasMoreThanOneDamageType = true;
        }

        if (damage.MagicalDamage > 0)
        {
            if (hasMoreThanOneDamageType) dmgTxt += '\n';
            dmgTxt += $"<color=#ff00ff>{damage.MagicalDamage}</color>";
            hasMoreThanOneDamageType = true;
        }

        if (damage.TrueDamage > 0)
        {
            if (hasMoreThanOneDamageType) dmgTxt += '\n';
            dmgTxt += $"<color=#b1b1b1>{damage.TrueDamage}</color>";
        }

        DisplayDamage(dmgTxt, new(0, 55));
    }

    public void AdjustHealthOnDamageReceive(DamageInstance damage)
    {
        health -= damage.TotalDamage;
        if (health < 0) health = 0;
        healthBar.SetHealth(health);

        if (health <= 0)
        {
            if (!canRevive) OnDeath();
            else
            {
                StopAllCoroutines();
                StartCoroutine(Revive());
            }
        }
    }

    public void SetHealth(int health)
    {
        this.health = health;
        if (healthBar) healthBar.SetHealth(health);
    }

    public IEnumerator PulseSprite()
    {
        spriteRenderer.color = Color.red;

        float c = 0, d = 0.5f;
        while (c < d)
        {
            spriteRenderer.color = Color.Lerp(Color.red, InitSpriteColor, c * 1.0f / d);
            c += Time.deltaTime;

            yield return null;
        }

        spriteRenderer.color = InitSpriteColor;
    }

    public void DisplayDamage(string msg)
    {
        DisplayDamage(msg, Vector3.zero);
    }

    public void DisplayDamage(string msg, Vector3 offset)
    {
        if (!DamagePopup) return;

        GameObject popup = Instantiate(DamagePopup, transform.position + offset, Quaternion.identity);
        popup.GetComponent<DamagePopup>().text.text = msg;
    }

    public virtual bool IsAlive() => health > 0;

    public virtual void Move()
    {
        // base example
        if (rb2d.velocity.magnitude != 0) animator.SetBool("attack", false);
    }

    public virtual void StopMovement()
    {
        rb2d.velocity = Vector2.zero;
        animator.SetFloat("move", 0);
    }

    public virtual void ApplyFreeze(EntityBase target, float duration)
    {
        if (target.IsFreezeImmune || !target.IsAlive()) return;

        target.animator.speed = 0f;
        target.FreezeTimer = Mathf.Max(target.FreezeTimer, duration);
        target.StopMovement();
        target.CancelAttack();

        target.ccBar.SetActive(true);
        target.ccSlider.value = target.ccSlider.maxValue = target.FreezeTimer;
    }

    public virtual void ApplyStun(EntityBase target, float duration)
    {
        if (target.IsStunImmune || !target.IsAlive()) return;

        target.animator.speed = 0f;
        target.StunTimer = Mathf.Max(target.StunTimer, duration);
        target.StopMovement();
        target.CancelAttack();

        target.ccBar.SetActive(true);
        target.ccSlider.value = target.ccSlider.maxValue = target.StunTimer;
    }

    public virtual void OnFreezeMaintain()
    {
        spriteRenderer.color = Color.blue;
        ccSlider.value = FreezeTimer;
    }

    public virtual void OnFreezeExit()
    {
        if (FreezeTimer > 0f) return;

        animator.SetBool("attack", false);
        FreezeTimer = 0f;
        spriteRenderer.color = InitSpriteColor;
        animator.speed = 1f;
        ccSlider.value = 0;   
        ccBar.SetActive(false);
    }

    public void EndFreeze()
    {
        FreezeTimer = 0f;
        OnFreezeExit();
    }

    public virtual void OnStunMaintain()
    {
        ccSlider.value = StunTimer;
    }

    public virtual void OnStunExit()
    {
        if (StunTimer > 0f) return;

        animator.SetBool("attack", false);
        StunTimer = 0f;
        animator.speed = 1f;
        ccSlider.value = 0;
        ccBar.SetActive(false);
    }

    public void EndStun()
    {
        StunTimer = 0f;
        OnStunExit();
    }

    public virtual Vector2 CalculateMovement(Vector2 normalizedMovementVector) => CalculateMovement(normalizedMovementVector, moveSpeed);
    public virtual Vector2 CalculateMovement(Vector2 normalizedMovementVector, float speed) => normalizedMovementVector * speed;

    public virtual void OnDeath()
    {
        ccBar.SetActive(false);
        ShadowSprite.SetActive(false);
        animator.speed = 1f;

        TriggeredOnDeath = true;
        animator.SetTrigger("die");
        healthBar.SetHealth(0);
        healthBar.gameObject.SetActive(false);
        foreach (var c in colliders)
        {
            c.enabled = false;
        }
        rb2d.velocity = Vector2.zero;
        StopAllCoroutines();
        StartCoroutine(StartMovementLockout(999));
        StartCoroutine(StartAttackLockout(999));

        if (EntityManager)
        {
            EntityManager.OnEntityDeath(this.gameObject);
        }
        Destroy(this.gameObject, 4);
        StartCoroutine(SpriteFadeOutOnDeath());
    }

    IEnumerator SpriteFadeOutOnDeath()
    {
        yield return new WaitForSeconds(0.8f);
        float c = 0, d = 0.25f;
        while (c < d)
        {
            spriteRenderer.color = Color.Lerp(InitSpriteColor, Color.black, c * 1.0f / d);
            c += Time.deltaTime;
            yield return null;
        }
        spriteRenderer.color = Color.black;

        c = 0; d = 0.5f;
        while (c < d)
        {
            spriteRenderer.color = Color.Lerp(Color.black, new Color(0, 0, 0, 0), c * 1.0f / d);
            c += Time.deltaTime;
            yield return null;
        }
        spriteRenderer.color = new Color(0, 0, 0, 0);
    }

    public virtual IEnumerator Attack()
    {
        if (!CanAttack || IsAttackLocked) yield break;

        animator.SetBool("attack", true);
        LockoutMovementOnAttackCoroutine = StartCoroutine(LockoutMovementsOnAttack());
    }

    // Called by the animation event
    public virtual IEnumerator OnAttackComplete()
    {
        yield break;
    }

    public virtual IEnumerator LockoutMovementsOnAttack()
    {
        // base example
        if (IsAttackLocked) yield break;

        StartCoroutine(StartAttackLockout(GetAttackLockoutTime()));

        StartCoroutine(StartMovementLockout(GetWindupTime() * 1.5f));

        yield return new WaitForSeconds(GetAttackAnimationLength());
        if (!IsFrozen && !IsStunned) animator.SetBool("attack", false);

        yield return null;
    }

    public float GetWindupTime() => attackWindupTime * (100 / Mathf.Max(20, ASPD));

    public float GetAttackInterval() => attackInterval * (100 / Mathf.Max(20, ASPD));

    public float GetAttackAnimationLength() => 
        AttackAnimation 
            ? AttackAnimation.length / preferredAttackAnimationSpeed / animator.GetFloat("a_speed_value")
            : 0;

    public float GetAttackLockoutTime() 
        => Mathf.Max(
                GetWindupTime(),
                GetAttackInterval(),
                GetAttackAnimationLength()
            );

    public virtual void CancelAttack()
    {
        if (!IsFrozen && !IsStunned) animator.SetBool("attack", false);

        if (AttackCoroutine != null)
        {
            StopCoroutine(AttackCoroutine);
            AttackCoroutine = null;
            AttackLockout = (short)Mathf.Max(AttackLockout, 0);
        }

        if (LockoutMovementOnAttackCoroutine != null)
        {
            StopCoroutine(LockoutMovementOnAttackCoroutine);
            LockoutMovementOnAttackCoroutine = null;
        }
    }

    public virtual void Heal(float amount, bool healThroughDead = false)
    {
        Heal(amount, this, healThroughDead);
    }

    public virtual void Heal(float amount, EntityBase target, bool healThroughDead = false, bool displayMsg = true)
    {
        if (amount <= 0 || (!target.IsAlive() && !healThroughDead)) return;
        if (displayMsg) target.DisplayDamage("<color=green>+" + (int)amount + "</color>", new Vector3(0, 55));
        target.health += (int)amount;
        if (target.health > target.mHealth) target.health = target.mHealth;
        target.healthBar.SetHealth(target.health);
    }

    [SerializeField] protected float reviveDuration = 5;
    [SerializeField] protected float postReviveDuration = 0;
    public virtual IEnumerator Revive()
    {
        if (!canRevive) yield break;

        float lockoutDuration = reviveDuration + postReviveDuration + 1f;
        animator.SetTrigger("die");
        health = 1;
        StartCoroutine(StartMovementLockout(lockoutDuration));
        StartCoroutine(StartAttackLockout(lockoutDuration));
        SetInvulnerable(lockoutDuration);

        yield return new WaitForSeconds(1f);

        float c = 0;
        while (c < reviveDuration)
        {
            health = (int) Mathf.Lerp(1, mHealth, c * 1.0f / reviveDuration);
            SetHealth(health);
            c += Time.deltaTime;
            yield return null;
        }

        animator.SetTrigger("revive");
        health = mHealth;
        SetHealth(health);
        canRevive = false;
    }

    public void PullEntityTowards(EntityBase targetEntity, Transform targetPosition, float pullForce, float duration, bool hasReferencePosition = true)
        => StartCoroutine(ApplyForceCoroutine(targetEntity, targetPosition.position, pullForce, duration, true, hasReferencePosition));

    public void PushEntityFrom(EntityBase targetEntity, Transform sourcePosition, float pushForce, float duration, bool hasReferencePosition = true)
        => StartCoroutine(ApplyForceCoroutine(targetEntity, sourcePosition.position, pushForce, duration, false, hasReferencePosition));

    public void PullEntityTowards(EntityBase targetEntity, Vector3 targetPosition, float pullForce, float duration, bool hasReferencePosition = true)
    => StartCoroutine(ApplyForceCoroutine(targetEntity, targetPosition, pullForce, duration, true, hasReferencePosition));

    public void PushEntityFrom(EntityBase targetEntity, Vector3 sourcePosition, float pushForce, float duration, bool hasReferencePosition = true)
        => StartCoroutine(ApplyForceCoroutine(targetEntity, sourcePosition, pushForce, duration, false, hasReferencePosition));

    private IEnumerator ApplyForceCoroutine(EntityBase targetEntity, Vector3 referencePosition, float force, float duration, bool isPull, bool hasReferencePosition = true)
    {
        float ForceValue = force * duration / 0.03f;
        float ForceValueAfterWeight = ForceValue - targetEntity.weight;
        if (ForceValueAfterWeight <= 0.5f) yield break;

        float multiplier = ForceValueAfterWeight / ForceValue;
        force *= multiplier;

        targetEntity.StopMovement();
        targetEntity.CancelAttack();
        targetEntity.IsBeingShifted = true;

        StartCoroutine(targetEntity.StartMovementLockout(duration + 0.1f));
        float elapsedTime = 0f;
        float initialDistance = Vector3.Distance(targetEntity.transform.position, referencePosition);
        float minDistanceForPull = 20f;

        if (hasReferencePosition && initialDistance <= minDistanceForPull && isPull)
        {
            yield break;
        }

        float lastDistance = initialDistance;

        while (elapsedTime < duration)
        {
            if (!targetEntity.IsAlive())
            {
                targetEntity.rb2d.velocity = Vector2.zero;
                yield break;
            }

            float currentDistance = Vector3.Distance(targetEntity.transform.position, referencePosition);

            if (isPull)
            {
                // Stop if reached minimum distance
                if (hasReferencePosition && currentDistance <= minDistanceForPull)
                {
                    targetEntity.rb2d.velocity = Vector2.zero;
                    break;
                }

                // Stop if distance is increasing (overshot the target)
                if (currentDistance > lastDistance && elapsedTime > 0.05f) // Small grace period
                {
                    targetEntity.rb2d.velocity = Vector2.zero;
                    break;
                }
            }

            Vector3 directionVector;
            if (hasReferencePosition)
            {
                directionVector = isPull
                            ? (referencePosition - targetEntity.transform.position).normalized
                            : (targetEntity.transform.position - referencePosition).normalized;
            }
            else directionVector = referencePosition;

            if (isPull)
            {
                float progress = 1f - (currentDistance / initialDistance);
                float easedForce = force * (1f - Mathf.Pow(1f - progress, 3f));
                float finalForce = Mathf.Max(0.3f * force, easedForce);
                targetEntity.rb2d.AddForce(directionVector * finalForce, ForceMode2D.Force);
            }
            else
            {
                float timeProgress = elapsedTime / duration;
                float decayedForce = force * (1f - timeProgress * 0.5f);
                targetEntity.rb2d.AddForce(directionVector * decayedForce, ForceMode2D.Force);
            }

            targetEntity.IsBeingShifted = true;
            lastDistance = currentDistance;
            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // Ensure entity stops at the end
        targetEntity.rb2d.velocity = Vector2.zero;
        targetEntity.IsBeingShifted = false;
    }

    public void StopForceEffects(EntityBase targetEntity)
    {
        if (targetEntity != null && targetEntity.rb2d != null)
        {
            targetEntity.rb2d.velocity = Vector2.zero;
            StopAllCoroutines(); // Note: This stops ALL coroutines, consider a more targeted approach
        }
    }

    public void CreateProjectileAndShootToward(EntityBase target, ProjectileScript.ProjectileType projectileType, float acceleration = 0)
    {
        CreateProjectileAndShootToward(ProjectilePrefab, GetInstanceBasedOnDamagetype(), target, AttackPosition.position, target.transform.position, projectileType, ProjectileSpeed, acceleration);
    }

    public void CreateProjectileAndShootToward(EntityBase target, ProjectileScript.ProjectileType projectileType, float travelSpeed, float acceleration)
    {
        CreateProjectileAndShootToward(ProjectilePrefab, GetInstanceBasedOnDamagetype(), target, AttackPosition.position, target.transform.position, projectileType, travelSpeed, acceleration);
    }

    public void CreateProjectileAndShootToward(EntityBase target, Vector3 targetPosition, ProjectileScript.ProjectileType projectileType, float acceleration = 0)
    {
        CreateProjectileAndShootToward(ProjectilePrefab, GetInstanceBasedOnDamagetype(), target, AttackPosition.position, targetPosition, projectileType, ProjectileSpeed, acceleration);
    }

    public void CreateProjectileAndShootToward(EntityBase target, Vector3 spawnPosition, Vector3 targetPosition, ProjectileScript.ProjectileType projectileType, float acceleration = 0)
    {
        CreateProjectileAndShootToward(ProjectilePrefab, GetInstanceBasedOnDamagetype(), target, spawnPosition, targetPosition, projectileType, ProjectileSpeed, acceleration);
    }

    public void CreateProjectileAndShootToward(EntityBase target, Vector3 spawnPosition, Vector3 targetPosition, ProjectileScript.ProjectileType projectileType, float travelSpeed, float acceleration = 0)
    {
        CreateProjectileAndShootToward(ProjectilePrefab, GetInstanceBasedOnDamagetype(), target, spawnPosition, targetPosition, projectileType, travelSpeed, acceleration);
    }

    public void CreateProjectileAndShootToward(EntityBase target, DamageInstance damageInstance, ProjectileScript.ProjectileType projectileType, float acceleration = 0)
    {
        CreateProjectileAndShootToward(ProjectilePrefab, damageInstance, target, AttackPosition.position,target.transform.position, projectileType, ProjectileSpeed, acceleration);
    }

    public void CreateProjectileAndShootToward(EntityBase target, DamageInstance damageInstance, Vector3 targetPosition, ProjectileScript.ProjectileType projectileType, float acceleration = 0)
    {
        CreateProjectileAndShootToward(ProjectilePrefab, damageInstance, target, AttackPosition.position, targetPosition, projectileType, ProjectileSpeed, acceleration);
    }

    public void CreateProjectileAndShootToward(GameObject ProjectilePref, DamageInstance damageInstance, EntityBase target, Vector3 spawnPosition, Vector3 preferPosition, ProjectileScript.ProjectileType projectileType, float travelSpeed = 1000, float acceleration = 0, float lifeSpan = 8)
    {
        if (!ProjectilePref) return;

        GameObject projectile = Instantiate(ProjectilePref, spawnPosition, Quaternion.identity);
        ProjectileScript projectileScript = projectile.GetComponent<ProjectileScript>();
        if (!projectileScript) return;

        projectileScript.ProjectileFirer = this;
        projectileScript.DamageInstance = damageInstance;
        projectileScript.TravelSpeed = travelSpeed;
        projectileScript.Acceleration = acceleration;
        projectileScript.ShootTowards(preferPosition, target, projectileType, lifeSpan);
    }

    public void CreateProjectileAndShootToward(GameObject ProjectilePref, DamageInstance damageInstance, Vector3 spawnPosition, Vector3 preferPosition, ProjectileScript.ProjectileType projectileType, float travelSpeed = 1000, float acceleration = 0, float lifeSpan = 8, params Type[] targetType)
    {
        if (!ProjectilePref) return;

        GameObject projectile = Instantiate(ProjectilePref, spawnPosition, Quaternion.identity);
        ProjectileScript projectileScript = projectile.GetComponent<ProjectileScript>();
        if (!projectileScript) return;

        projectileScript.ProjectileFirer = this;
        projectileScript.DamageInstance = damageInstance;
        projectileScript.TravelSpeed = travelSpeed;
        projectileScript.Acceleration = acceleration;
        projectileScript.ShootTowards(preferPosition, projectileType, lifeSpan, targetType);
    }

    public virtual List<EntityBase> SearchForEntitiesAroundSelf(Type type = null, bool catchInvisibles = false, short take = -1)
    {
        return SearchForEntitiesAroundCertainPoint(type, transform.position, attackRange, catchInvisibles, take);
    }

    public virtual List<EntityBase> SearchForEntitiesAroundSelf(Type type, float range, bool catchInvisibles = false, short take = -1)
    {
        return SearchForEntitiesAroundCertainPoint(type, transform.position, range, catchInvisibles, take);
    }

    public virtual List<EntityBase> SearchForEntitiesAroundSelf(float r, Type type = null, bool catchInvisibles = false, short take = -1)
    {
        return SearchForEntitiesAroundCertainPoint(type, transform.position, r, catchInvisibles, take);
    }

    public static List<EntityBase> SearchForEntitiesAroundCertainPoint(Type type, Vector2 pos, float r, bool catchInvisibles = false, short take = -1)
    {
        Collider2D[] collider2Ds = Physics2D.OverlapCircleAll(pos, r);
        List<EntityBase> entityBases = new List<EntityBase>();

        foreach (Collider2D collider in collider2Ds)
        {
            EntityBase entity = collider.GetComponent<EntityBase>();
            if (!entity 
                || !entity.IsAlive() 
                    || (entity.isInvisible && !catchInvisibles) 
                        || entityBases.Contains(entity)
                            || (type != null && !type.IsAssignableFrom(entity.GetType()))) 
                continue;
            
            entityBases.Add(entity);
        }

        if (entityBases.Count == 1 || entityBases.Count <= take) return entityBases;

        entityBases = entityBases.OrderBy(e => Vector2.Distance(e.transform.position, pos)).ToList();
        return take == -1 ? entityBases : entityBases.Take(take).ToList();
    }

    public virtual EntityBase SearchForNearestEntityAroundSelf(Type type = null, bool catchInvisible = false)
    {
        return SearchForNearestEntityAroundCertainPoint(type, AttackPosition.position, attackRange, catchInvisible);
    }

    public static EntityBase SearchForNearestEntityAroundCertainPoint(Type type, Vector2 pos, float r, bool catchInvisibles = false)
    {
        var targets = SearchForEntitiesAroundCertainPoint(type, pos, r, catchInvisibles, 1);
        if (targets == null || targets.Count <= 0) return null;

        return targets[0];
    }
}
