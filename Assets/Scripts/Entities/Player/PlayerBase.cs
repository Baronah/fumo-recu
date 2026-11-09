using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LevelDifficultyModifier;
using static PlayerManager;
using static SkillTree_Manager;
using static UnityEngine.EventSystems.EventTrigger;
using SkillType = PlayerManager.SkillType;

public class PlayerBase : EntityBase
{
    private static readonly int MoveHash = Animator.StringToHash("move");
    private static WaitForSeconds _waitForSeconds1 = new WaitForSeconds(1f);
    private static WaitForSeconds _waitForSeconds0_5 = new WaitForSeconds(0.5f);
    public bool SettleSwappedInPlayer = false;
    public LayerMask ObstacleLayers;
    public Sprite AttackSprite, SkillSprite, SpecialSprite;
    public string AttackDes, SkillName, SkillDes, SpecialName, SpecialDes;
    
    protected static PlayerManager playerManager => PlayerManager._instance;

    [SerializeField] protected GameObject HH_Effect_parent;
    [SerializeField] private Material HH_Fill_Material;
    [SerializeField] protected Image HH_Effect_fill;

    [SerializeField] protected GameObject RockEffect, VowEffect;
    
    [SerializeField] protected GameObject WindanthemBar, WindanthemMaxEffect;
    protected Slider WindanthemSlider;
    protected TMP_Text WindanthemCounter;

    public HashSet<UpgradeData> Skills = new();
    public bool HasSkill(SkillName skillName) => Skills.Any(s => s.skillName == skillName);
    public UpgradeData GetSkill(SkillName skillName) => Skills.FirstOrDefault(s => s.skillName == skillName);
    public float[] GetSkillValues(SkillName skillName)
    {
        if (HasSkill(skillName)) 
            return Skills.FirstOrDefault(s => s.skillName == skillName).values;
        return UpgradesManager._instance.GetUpgradeValues(skillName);
    }

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

        StageManager.OnPlayerSpawn(this);

        playerManager.Register(this);

        PreInitialize_GetSkillTreeAttributes();
        GetSkillTreeEffects();
        StageManager.ProcessPlayerSkillTree(this);

        HH_Effect_parent.SetActive(HasSkill(SkillTree_Manager.SkillName.HEAVY_HITTER));
        WindanthemSlider = WindanthemBar.GetComponentInChildren<Slider>();
        WindanthemCounter = WindanthemBar.GetComponentInChildren<TMP_Text>();

        base.InitializeComponents();
        FeetPosition = transform.Find("Feetposition");

        SetInvulnerable(1f);

        IsComponentsInitialized = true;
    }

    protected virtual void PreInitialize_GetSkillTreeAttributes() { }

    public float blade_LowHpThreshold, blade_MaxDamageUp;
    public float scope_MaxRangeThreshold, scope_MaxDamageUp;
    float idea_UltimateRefundBase, idea_SpecialRefundBase;
    float idea_ContinuousBuff, idea_ContinuousBuffCap;
    short book_AttentionMaxReduction;
    float book_effectThreshold, book_atkBuff, book_aspdBuff;
    float device_effectThreshold,
          device_attentionMinThreshold,
          device_attentionMinLifesteal,
          device_defBuff,
          device_resBuff;
    protected override void OnInitialize_GetSkillTreeAttributes()
    {
        base.OnInitialize_GetSkillTreeAttributes();

        float[] bladeValues = GetSkillValues(SkillTree_Manager.SkillName.EQUIPMENT_BLADE);
        blade_MaxDamageUp = bladeValues[1];
        blade_LowHpThreshold = bladeValues[2];

        float[] scopeValues = GetSkillValues(SkillTree_Manager.SkillName.EQUIPMENT_SCOPE);
        scope_MaxRangeThreshold = scopeValues[1];
        scope_MaxDamageUp = scopeValues[2];

        float[] ideasValues = GetSkillValues(SkillTree_Manager.SkillName.WINGED_STEPS_C);
        idea_SpecialRefundBase = ideasValues[0];
        idea_UltimateRefundBase = ideasValues[1];
        idea_ContinuousBuff = ideasValues[2];
        idea_ContinuousBuffCap = ideasValues[3];

        float[] bookValues = GetSkillValues(SkillTree_Manager.SkillName.ATTENTION_BOOK);
        book_effectThreshold = bookValues[0];
        book_atkBuff = bookValues[1];
        book_aspdBuff = bookValues[2];
        book_AttentionMaxReduction = (short)bookValues[3];

        float[] deviceValues = GetSkillValues(SkillTree_Manager.SkillName.ATTENTION_DEVICE);
        device_effectThreshold = deviceValues[0];
        device_defBuff = deviceValues[1];
        device_resBuff = deviceValues[2];
        device_attentionMinThreshold = deviceValues[3];
        device_attentionMinLifesteal = deviceValues[4];

        if (HasSkill(SkillTree_Manager.SkillName.WINGED_STEPS_A)
        || HasSkill(SkillTree_Manager.SkillName.WINGED_STEPS_B)
        || HasSkill(SkillTree_Manager.SkillName.WINGED_STEPS_C))
            AccelerationBuffPerSec *= GetSkillValues(SkillTree_Manager.SkillName.ACCELERATION)[4];

        if (HasSkill(SkillTree_Manager.SkillName.ATTENTION_DEVICE))
            StatisRequiredTimer = GetSkillValues(SkillTree_Manager.SkillName.STATIS)[2];

        HungerDamageFlatPerSec /= 2;
    }

    [SerializeField] GameObject AllowVow;
    public override void FixedUpdate()
    {
        if (Time.timeScale <= 0) return;
        base.FixedUpdate();

        bool alive = IsAlive();

        if (alive)
        {
            IdeasBuff();
            AttentionBuff();
            WindBladeBuff();
            ProcessPlayerGeologistUpgrade();
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

    public override void OnEnvironmentalTileEnter(StageManager.EnvironmentType environmentType)
    {
        base.OnEnvironmentalTileEnter(environmentType);
        ProcessPlayerGeologistUpgrade();
    }

    float geologistCountUp = 0;
    public virtual void ProcessPlayerGeologistUpgrade()
    {
        geologistCountUp += Time.fixedDeltaTime;
        if (geologistCountUp <= 0.15f) return;

        if (environmentalTilesStandingOn.Count <= 0)
        {
            geologistCountUp = 0;
            return;
        }

        if (HasSkill(SkillTree_Manager.SkillName.GEOLOGIST_OBSERVE))
        {
            ReduceUltimateCooldown(geologistCountUp * GetSkillValues(SkillTree_Manager.SkillName.GEOLOGIST_OBSERVE)[0], PlayerBase.CooldownReductionType.FLAT);
        }

        if (HasSkill(SkillTree_Manager.SkillName.GEOLOGIST_STUDY))
        {
            ApplyEffect(Effect.AffectedStat.ATK, "GEOLOGIST_ATK_BUFF", GetSkillValues(SkillTree_Manager.SkillName.GEOLOGIST_STUDY)[0], 0.5f, true);
        }

        if (HasSkill(SkillTree_Manager.SkillName.GEOLOGIST_EXPLORE))
        {
            ApplyEffect(Effect.AffectedStat.MSPD, "GEOLOGIST_MSPD_BUFF", GetSkillValues(SkillTree_Manager.SkillName.GEOLOGIST_EXPLORE)[0], 0.5f, true);
        }

        geologistCountUp = 0;
    }

    public override void OnEnvironmentalTileExit(StageManager.EnvironmentType environmentType)
    {
        base.OnEnvironmentalTileExit(environmentType);
        if (environmentalTilesStandingOn.Count <= 0)
        {
            RemoveEffect("GEOGOLIST_ATK_BUFF");
            RemoveEffect("GEOGOLIST_MSPD_BUFF");
        }
    }

    protected override bool TriggerHunger()
    {
        return base.TriggerHunger() && IsMoving() && !HasSkill(SkillTree_Manager.SkillName.EQUIPMENT_PROVISIONS);
    }

    float w_countUp = 0;
    void WindBladeBuff()
    {
        w_countUp += Time.fixedDeltaTime;
        if (w_countUp < 0.15f) return;
        w_countUp = 0;

        if (!HasSkill(SkillTree_Manager.SkillName.WINGED_STEPS_B)) return;

        float amount = b_moveSpeed == 0
            ? 0 
            : (moveSpeed - b_moveSpeed) / (b_moveSpeed * 0.01f);

        if (amount > 0) ApplyEffect(Effect.AffectedStat.ASPD, "WIND_BLADE_BUFF", amount * 0.7f, 0.15f, false);
    }

    float i_countUp = 0;
    bool previouslyMoving = true;
    short i_ContiuousCounter = 0;
    void IdeasBuff()
    {
        i_countUp += Time.fixedDeltaTime;
        if (i_countUp < 1f) return;
        float interval = i_countUp;
        i_countUp = 0;

        if (!HasSkill(SkillTree_Manager.SkillName.WINGED_STEPS_C)) return;
        
        bool isMoving = rb2d.velocity.magnitude > 0.1f;
        if (isMoving == previouslyMoving) i_ContiuousCounter = (short) Mathf.Min(i_ContiuousCounter + 1, idea_ContinuousBuffCap);
        else i_ContiuousCounter = 0;

        if (isMoving)
        {
            float reduceAmount = interval * (idea_UltimateRefundBase * (1f + idea_ContinuousBuff * i_ContiuousCounter));
            ReduceUltimateCooldown(reduceAmount, CooldownReductionType.PERCENTAGE_CURRENT);
        }
        else
        {
            float reduceAmount = interval * (idea_SpecialRefundBase * (1f + idea_ContinuousBuff * i_ContiuousCounter));
            ReduceSpecialCooldown(reduceAmount, CooldownReductionType.PERCENTAGE_CURRENT);
        }

        previouslyMoving = isMoving;
    }

    float a_countUp = 0;
    bool hasLifestealBuffFromAttentionMin = false;
    void AttentionBuff()
    {
        a_countUp += Time.fixedDeltaTime;
        if (a_countUp < 0.2f) return;
        a_countUp = 0;

        if (health > mHealth * device_attentionMinThreshold && hasLifestealBuffFromAttentionMin)
        {
            hasLifestealBuffFromAttentionMin = false;
            lifeSteal -= device_attentionMinLifesteal;
        }

        if (health >= mHealth * book_effectThreshold && HasSkill(SkillTree_Manager.SkillName.ATTENTION_BOOK))
        {
            ApplyEffect(Effect.AffectedStat.ATK, "ATTENTION_BUFF", book_atkBuff, 0.22f, true);
            ApplyEffect(Effect.AffectedStat.ASPD, "ATTENTION_BUFF", book_aspdBuff, 0.22f, true);
        }
        
        if (health <= mHealth * device_effectThreshold && HasSkill(SkillTree_Manager.SkillName.ATTENTION_DEVICE))
        {
            ApplyEffect(Effect.AffectedStat.DEF, "ATTENTION_BUFF", device_defBuff, 0.22f, false);
            ApplyEffect(Effect.AffectedStat.RES, "ATTENTION_BUFF", device_resBuff, 0.22f, false);

            if (health <= mHealth * device_attentionMinThreshold && !hasLifestealBuffFromAttentionMin)
            {
                hasLifestealBuffFromAttentionMin = true;
                lifeSteal += device_attentionMinLifesteal;
            }
        }
    }

    public override void UpdateCooldowns()
    {
        base.UpdateCooldowns();
        if (HasSkill(SkillTree_Manager.SkillName.HEAVY_HITTER))
        {
            timerSinceLastAttack += Time.fixedDeltaTime;
            HH_Effect_fill.fillAmount = Mathf.Lerp(0, 1f, timerSinceLastAttack / heavyHitterMaxTimer);
            HH_Effect_fill.color = IsHeavyHitterMaxed ? Color.white : new(0.81f, 0.12f, 0.12f);
            HH_Effect_fill.material = IsHeavyHitterMaxed ? null : HH_Fill_Material;
        }

        leverCDTimer += Time.fixedDeltaTime;
    }

    readonly string StartMspdBuffKey = "SWAP_START_MSPDBUFF";
    public virtual void OnFieldEnter()
    {
        short diff = (short)(CharacterPrefabsStorage.DifficultyLevel - 1);
        if (diff >= (int) DiffType.PlayerFieldDebuff_1) StartCoroutine(DifficultModifierNegativeStatus(diff));

        if (diff >= (int) DiffType.Player1HP && playerManager.IsFirstTimeStageEnter)
        {
            SetHealth(1);
            StartCoroutine(HealingEffectivenessDebuff());
            playerManager.IsFirstTimeStageEnter = false;
        }

        GetVow();
        if (HasSkill(SkillTree_Manager.SkillName.SWAP_START_ATK))
        {
            Heal((mHealth - health) * 0.3f);
            ApplyEffect(Effect.AffectedStat.ATK, "SWAP_START_ATKBUFF", 75f, 5f, true, EffectPersistType.PERSIST);
        }

        if (HasSkill(SkillTree_Manager.SkillName.SWAP_START_MSPD))
        {
            ApplyEffect(Effect.AffectedStat.MSPD, StartMspdBuffKey, 50f, 9999f, true, EffectPersistType.PERSIST, false);
        }
    }

    IEnumerator HealingEffectivenessDebuff()
    {
        HealingEffectiveness -= 0.9f;
        
        int loop = 80;
        float add = 0.9f / loop;

        for (int i = 0; i < loop; i++)
        {
            yield return _waitForSeconds0_5;
            HealingEffectiveness += add;
        }
    }

    IEnumerator DifficultModifierNegativeStatus(short diff)
    {
        if (diff < (int)DiffType.PlayerFieldDebuff_1) yield break;

        if (diff == (int)DiffType.PlayerFieldDebuff_1) yield return new WaitForSeconds(10f);
        else yield return new WaitForSeconds(5f);

        float c = 0, d = 40f;
        while (c < d)
        {
            float Strength_Scale80To0 = Mathf.Lerp(0, 80f, c * 1.0f / d),
                  Strength_Scale50To0 = Mathf.Lerp(0, 50f, c * 1.0f / d);

            ApplyEffect(Effect.AffectedStat.ATK, "DIFFICULT_HANDICAP_ATK", Strength_Scale80To0 * -1, 9999f, true);

            if (diff >= (int) DiffType.PlayerFieldDebuff_2)
                ApplyEffect(Effect.AffectedStat.MSPD, "DIFFICULT_HANDICAP_MSPD", Strength_Scale50To0 * -1, 9999f, true);

            c += 1;
            yield return _waitForSeconds1;
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
        if (!Debut && !FireWorkStarted && HasSkill(SkillTree_Manager.SkillName.SWAP_START_SPECIAL))
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
        SkillTree_Manager.SkillName.SWAP_START_SPECIAL,
        SkillTree_Manager.SkillName.SWAP_START_MSPD,
        SkillTree_Manager.SkillName.BREAK_THE_ICE,
        SkillTree_Manager.SkillName.CERTAIN_FATES,
        SkillTree_Manager.SkillName.BUBBLE_ARTS,
        SkillTree_Manager.SkillName.HEAVY_HITTER,
        SkillTree_Manager.SkillName.SPECIAL_MSPD,
        SkillTree_Manager.SkillName.ULTIMATE_BUFF,
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
        var SelectedSkills = CharacterPrefabsStorage.Skills.ToList();
        
        var ExtraSkill = StageManager.GetExtraSkills();
        if (ExtraSkill != null) SelectedSkills.AddRange(ExtraSkill);
        
        SelectedSkills = SelectedSkills.Distinct().ToList();

        RockGachaSkill rockGacha = RockPickEffect.GetComponent<RockGachaSkill>();
        if (SelectedSkills.Contains(SkillTree_Manager.SkillName.A_NICE_LOOKING_ROCK))
        {
            RockBonusSkill.RemoveAll(s => SelectedSkills.Contains(s));

            if (RockBonusSkill.Count > 0)
            {
                SkillName bonusSkill = RockBonusSkill[Random.Range(0, RockBonusSkill.Count)];
                SelectedSkills.Add(bonusSkill);

                GameObject o = Instantiate(RockPickEffect, transform.position + new Vector3(0, 100), Quaternion.identity, transform);
                RockGachaSkill thisRock = o.GetComponent<RockGachaSkill>();
                thisRock.SetSkill(bonusSkill);

                playerManager.PlayerInvoke_SetSkillUI(
                    bonusSkill,
                    thisRock.GetSkillImage(bonusSkill),
                    new Color(0f, 1f, 0.67f));
            }
        }

        float[] values;
        foreach (var skillName in SelectedSkills)
        {
            Skills.Add(UpgradesManager._instance.GetUpgradeData(skillName));

            switch (skillName)
            {
                case SkillTree_Manager.SkillName.WINGED_STEPS_A:
                    values = GetSkillValues(SkillTree_Manager.SkillName.WINGED_STEPS_A);
                    ASPD += values[0];
                    break;

                case SkillTree_Manager.SkillName.WINGED_STEPS_B:
                    values = GetSkillValues(SkillTree_Manager.SkillName.WINGED_STEPS_B);
                    ApplyEffect(Effect.AffectedStat.MSPD, "WINGED_STEPS_B_BUFF", values[0], 9999f, true, EffectPersistType.PERSIST);
                    break;

                case SkillTree_Manager.SkillName.EQUIPMENT_BLADE:
                    values = GetSkillValues(SkillTree_Manager.SkillName.EQUIPMENT_BLADE);
                    defPen += (short) values[0];
                    break;

                case SkillTree_Manager.SkillName.EQUIPMENT_SCOPE:
                    values = GetSkillValues(SkillTree_Manager.SkillName.EQUIPMENT_SCOPE);
                    b_attackRange += b_attackRange * values[0];
                    break;

                case SkillTree_Manager.SkillName.EQUIPMENT_PROVISIONS:
                    values = GetSkillValues(SkillTree_Manager.SkillName.EQUIPMENT_PROVISIONS);
                    HealingEffectiveness += values[0];
                    break;

                case SkillTree_Manager.SkillName.HEAVY_HITTER:
                    values = GetSkillValues(SkillTree_Manager.SkillName.HEAVY_HITTER);
                    ASPD -= values[0];
                    bAtk += (short)(bAtk * values[1]);
                    heavyHitterMaxTimer = values[3];
                    break;

                case SkillTree_Manager.SkillName.A_NICE_LOOKING_ROCK:
                    values = GetSkillValues(SkillTree_Manager.SkillName.A_NICE_LOOKING_ROCK);
                    mHealth += (mHealth * values[0]);
                    bAtk = (short)(bAtk * (1 + values[0]));
                    b_moveSpeed += b_moveSpeed * values[0];
                    break;

                case SkillTree_Manager.SkillName.HAIR_RIBBON:
                    values = GetSkillValues(SkillTree_Manager.SkillName.HAIR_RIBBON);
                    PlayerType playerType = GetPlayerType();

                    if (CharacterPrefabsStorage.startingPlayer == playerType)
                    {
                        bAtk = (short)(bAtk * (1 + values[0]));

                        if (playerType == PlayerType.MELEE)
                        {
                            mHealth += (int)(mHealth * values[1]);
                        }
                        else if (playerType == PlayerType.RANGED)
                        {
                            b_moveSpeed += b_moveSpeed * values[2];
                        }
                    }
                    break;

                case SkillTree_Manager.SkillName.CERTAIN_FATES:
                    values = GetSkillValues(SkillTree_Manager.SkillName.CERTAIN_FATES);
                    weight += (short)values[0];
                    break;
            }
        }

        canVow = HasSkill(SkillTree_Manager.SkillName.KNOTS);

        if (playerManager && playerManager.MintBlessing)
        {
            playerManager.PlayerInvoke_SetSkillUI(
                SkillTree_Manager.SkillName.AMULET, 
                rockGacha.GetSkillImage(SkillTree_Manager.SkillName.AMULET),
                new Color(0, 0.8f, 1f));
        }
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

        foreach (var skill in Skills)
        {
            playerManager.PlayerInvoke_RemoveSkillUI(skill.skillName);
        }
    }

    protected override float GetRegenAmount()
    {
        float regenAmount = base.GetRegenAmount();
        float provisionAdd = 0;
        if (HasSkill(SkillTree_Manager.SkillName.EQUIPMENT_PROVISIONS))
        {
            float[] values = GetSkillValues(SkillTree_Manager.SkillName.EQUIPMENT_PROVISIONS);
            provisionAdd = mHealth * values[1] + (mHealth - health) * values[2];
        }

        return regenAmount + provisionAdd;
    }

    protected void MakeVow(PlayerManager.SkillType skillType)
    {
        if (!HasSkill(SkillTree_Manager.SkillName.KNOTS) || playerManager.hasVowed || skillType == SkillType.NONE) return;

        SkillType seal;

        if (skillType == SkillType.SPECIAL)
            seal = SkillType.ULTIMATE;
        else
            seal = SkillType.SPECIAL;

        playerManager.SetSealSkill(this, seal);
        GameObject vowEffect = Instantiate(VowEffect, transform.position + new Vector3(0, 100), Quaternion.identity, transform);
        SpriteRenderer vowEffectSp = vowEffect.GetComponent<SpriteRenderer>();

        Color vowColor = GetVowEffectColor(skillType);
        vowEffectSp.color = vowColor;
        vowEffect.GetComponentInChildren<Image>().color = new Color(vowColor.r, vowColor.g, vowColor.b, 0.7f);

        playerManager.PlayerInvoke_SetSkillUI(SkillTree_Manager.SkillName.KNOTS, vowEffectSp.sprite, vowColor); 

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

        if (Input.GetKeyDown(GlobalStageManager.AttackKey))
        {
            Action_Attack();
        }
        else if (Input.GetKeyDown(GlobalStageManager.SkillKey))
        {
            Action_Skill();
        }
        else if (Input.GetKeyDown(GlobalStageManager.SpecialKey))
        {
            Action_Special();
        }
        else
        {
            Move();
        }
    }

    public virtual void Action_Attack()
    {
        if (!IsAlive()) return;
        AttackCoroutine = StartCoroutine(Attack());
    }

    public virtual void Action_Skill()
    {
        if (!IsAlive()) return;

        if (HasSkill(SkillTree_Manager.SkillName.KNOTS) && !playerManager.hasVowed)
        {
            MakeVow(PlayerManager.SkillType.ULTIMATE);
        }

        UseSkill();
    }

    public virtual void Action_Special()
    {
        if (!IsAlive()) return;

        if (HasSkill(SkillTree_Manager.SkillName.KNOTS) && !playerManager.hasVowed)
        {
            MakeVow(PlayerManager.SkillType.SPECIAL);
        }

        UseSpecial();
    }

    short specialCastCount = 0;
    public virtual void UseSpecial()
    {
        if (HasSkill(SkillTree_Manager.SkillName.SPECIAL_MSPD))
        {
            float[] values = GetSkillValues(SkillTree_Manager.SkillName.SPECIAL_MSPD);
            float duration = Mathf.Min(values[3], values[1] + specialCastCount * values[2]);
            ApplyEffect(Effect.AffectedStat.MSPD, "SPECIAL_MSPD_BUFF", values[0], duration, true, EffectPersistType.PERSIST);
            specialCastCount++;
        }
    }

    [SerializeField] GameObject UltimateShockwaveEffect;
    public virtual void UseSkill()
    {
        if (HasSkill(SkillTree_Manager.SkillName.ULTIMATE_BUFF))
        {
            ActivateLever();
        }
    }

    readonly float leverCD = 5f;
    float leverCDTimer = 9999f;
    void ActivateLever()
    {
        float[] values = GetSkillValues(SkillTree_Manager.SkillName.ULTIMATE_BUFF);
        ApplyEffect(Effect.AffectedStat.ATK, "ULTIMATE_ATK_BUFF", values[0], values[1], true, EffectPersistType.DECAY);
        ApplyEffect(Effect.AffectedStat.ASPD, "ULTIMATE_ASPD_BUFF", values[0], values[1], true, EffectPersistType.DECAY);
        ApplyEffect(Effect.AffectedStat.MSPD, "ULTIMATE_MSPD_BUFF", values[0], values[1], true, EffectPersistType.DECAY);

        if (leverCDTimer < leverCD) return;

        float range = Mathf.Max(attackRange, 300f);

        var enemies = SearchForEntitiesAroundSelf(typeof(EnemyBase), range, true);
        foreach (var enemy in enemies)
        {
            if (!enemy || !enemy.IsAlive()) continue;
            enemy.ApplyEffect(Effect.AffectedStat.MSPD, "ULTIMATE_ENEMY_DEBUFF", values[2] * -1, values[3], true, EffectPersistType.DECAY);
            PushEntityFrom(enemy, transform.position, 3.6f, 0.15f, true);
        }

        GameObject vfx = Instantiate(UltimateShockwaveEffect, transform.position, Quaternion.identity, transform);
        vfx.transform.localScale *= range / 300f;
        Destroy(vfx, 0.5f);

        leverCDTimer = 0f;
    }

    public override void Move()
    {
        if (IsMovementLocked || IsBound) return;

        Vector2 movementInputs = InputManager.Instance.GetMovementInput();
        rb2d.velocity = CalculateMovement(movementInputs);

        // Calculate movement magnitude for animator
        float moveMagnitude = Mathf.Abs(movementInputs.x) + Mathf.Abs(movementInputs.y);
        animator.SetFloat(MoveHash, moveMagnitude);

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

    public override void TakeDamage(DamageInstance damage, EntityBase source, ProjectileScript projectileInfo = null, bool IgnoreInvulnerability = false, bool CalculateDamage = false)
    {
        base.TakeDamage(damage, source, projectileInfo, IgnoreInvulnerability, CalculateDamage);

        if (damage.TotalDamage > 0 && !isInvulnerable) RemoveEffect(StartMspdBuffKey);

        if (source && !isInvulnerable && !IgnoreInvulnerability)
        {
            float strengthLevel = damage.TotalDamage * 1.0f / (mHealth * 0.5f);
            playerManager.OnPlayerAttacked(strengthLevel);
        }
    }

    public float heavyHitterMaxTimer;
    protected float timerSinceLastAttack = 0f;
    protected float GetHeavyHitterMultiplier()
    {
        if (!HasSkill(SkillTree_Manager.SkillName.HEAVY_HITTER)) return 1f;
        float[] values = GetSkillValues(SkillTree_Manager.SkillName.HEAVY_HITTER);
        float multiplier = 1f + Mathf.Lerp(0f, values[2], timerSinceLastAttack / heavyHitterMaxTimer);
        return multiplier;
    }

    protected void GetWingedStepMspdBuff()
    {
        float[] values = GetSkillValues(SkillTree_Manager.SkillName.WINGED_STEPS_A);
        float baseValue = values[1], jumpValue = values[3];
        string key = "WINGED_STEPS_A_MSPD_BUFF";
        if (MspdBuffs.ContainsKey(key) && MspdBuffs[key].IsInEffect)
        {
            float newValue = MspdBuffs[key].Value + jumpValue;

            ApplyEffect(
                Effect.AffectedStat.MSPD, 
                key,
                Mathf.Max(baseValue, newValue),
                values[2], 
                true, 
                EffectPersistType.PERSIST);
        }
        else
        {
            ApplyEffect(Effect.AffectedStat.MSPD, key, baseValue, values[2], true, EffectPersistType.PERSIST);
        }
    }

    protected bool IsHeavyHitterMaxed =>
        HasSkill(SkillTree_Manager.SkillName.HEAVY_HITTER)
        &&
        timerSinceLastAttack >= heavyHitterMaxTimer;

    readonly HashSet<EntityBase> Levitated = new();
    public override void DealDamage(EntityBase target, float pDmg, float mDmg, float tDmg, bool allowWhenDisabled = false, ProjectileScript projectileInfo = null)
    {
        if (!target.IsAlive()) return;

        if (HasSkill(SkillTree_Manager.SkillName.BUBBLE_ARTS) && !Levitated.Contains(target))
        {
            float[] values = GetSkillValues(SkillTree_Manager.SkillName.BUBBLE_ARTS);
            mDmg += (int)(atk * values[0]);
            ApplyLevitate(target, values[1]);
            Levitated.Add(target);
        }

        if (HasSkill(SkillTree_Manager.SkillName.BREAK_THE_ICE) && target.IsFrozen)
        {
            float[] values = GetSkillValues(SkillTree_Manager.SkillName.BREAK_THE_ICE);
            float freezeDuration = target.FreezeTimer;
            target.EndFreeze();

            int bonusDmg = (int)(target.mHealth * values[0]);
            tDmg += bonusDmg;

            int bonusAdaptiveDmg = (int)Mathf.Min(atk * values[1] * freezeDuration, bAtk * values[2]);
            if (damageType == DamageType.PHYSICAL) pDmg += bonusAdaptiveDmg;
            else if (damageType == DamageType.MAGICAL) mDmg += bonusAdaptiveDmg;
            else tDmg += bonusAdaptiveDmg;
        }

        if (HasSkill(SkillTree_Manager.SkillName.ATTENTION_BOOK) && health >= mHealth)
        {
            float convertRatio = GetSkillValues(SkillTree_Manager.SkillName.ATTENTION_BOOK)[3];
            tDmg += (int)((pDmg + mDmg + tDmg) * convertRatio);
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
        if (HasSkill(SkillTree_Manager.SkillName.VICTORY_ATK))
        {
            float[] values = GetSkillValues(SkillTree_Manager.SkillName.VICTORY_ATK);
            float strength = Mathf.Min(values[3], values[0] + values[1] * enemyDefeatCount), duration = values[2];
            ApplyEffect(Effect.AffectedStat.ATK, "VICTORY_ATK_BUFF", strength, duration, true, EffectPersistType.DECAY);
            ApplyEffect(Effect.AffectedStat.MSPD, "VICTORY_MSPD_BUFF", strength, duration, true, EffectPersistType.DECAY);
        }

        if (HasSkill(SkillTree_Manager.SkillName.VICTORY_REFRESH))
        {
            float[] values = GetSkillValues(SkillTree_Manager.SkillName.VICTORY_REFRESH);
            ReduceSpecialCooldown(Mathf.Min(values[2], values[0] + values[1] * enemyDefeatCount), CooldownReductionType.PERCENTAGE_FULL);
        }

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
        if (!gameObject.activeSelf) return;

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
        float[] values = GetSkillValues(SkillTree_Manager.SkillName.AMULET);
        Heal(mHealth * values[0], healThroughDead: true);

        playerManager.MintBlessingRevival();
        playerManager.PlayerInvoke_RemoveSkillUI(SkillTree_Manager.SkillName.AMULET);
        SetInvulnerable(values[1]);

        Instantiate(RockEffect, transform.position, Quaternion.identity);
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
            attackSpeed = GetWindupTime(),
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

    public void ResumeStageBGM()
    {
        StageManager.StageBGM.Play();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision || !collision.gameObject) return;

        FumoScript fumoScript = collision.gameObject.GetComponent<FumoScript>();
        if (fumoScript && fumoScript.ObjectiveType == FumoScript.FumoObjectiveType.PICK_UP && collision.gameObject.CompareTag("Fumo"))
        {
            StageManager.OnPlayerFumoPickup(this, collision);
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