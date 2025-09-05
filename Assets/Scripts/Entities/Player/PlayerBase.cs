using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SkillTree_Manager;

public class PlayerBase : EntityBase
{
    public Sprite AttackSprite, SkillSprite, SpecialSprite;
    public string AttackDes, SkillName, SkillDes, SpecialName, SpecialDes;
    protected PlayerManager playerManager;
    protected StageManager StageManager;

    private Transform TransformFeetposition;
    public Vector3 Feetposition => TransformFeetposition.position;

    public List<SkillName> Skills = new();

    private void Update()
    {
        GetControlInputs();
    }

    public override void InitializeComponents()
    {
        if (IsComponentsInitialized) return;
        StageManager = FindObjectOfType<StageManager>();
        StageManager.OnPlayerSpawn(this);

        GetBonusSkill();

        base.InitializeComponents();
        TransformFeetposition = transform.Find("Feetposition");

        playerManager = FindObjectOfType<PlayerManager>();
        playerManager.Register(this);

        SetInvulnerable(1f);
        IsComponentsInitialized = true;
    }

    public virtual void GetBonusSkill()
    {
        foreach (var skill in CharacterPrefabsStorage.Skills)
        {
            var key = skill.Key;
            Skills.Add(key);

            switch (key)
            {
                case SkillTree_Manager.SkillName.WINGED_STEPS_A:
                    b_moveSpeed += b_moveSpeed * 0.2f;
                    break;
                case SkillTree_Manager.SkillName.WINGED_STEPS_B:
                    ASPD += 25;
                    break;
                case SkillTree_Manager.SkillName.EQUIPMENT_BLADE:
                    defPen += 10;
                    break;
                case SkillTree_Manager.SkillName.EQUIPMENT_SCOPE:
                    b_attackRange *= 1.2f;
                    break;
                case SkillTree_Manager.SkillName.EQUIPMENT_PROVISIONS:
                    mHealth += (int)(mHealth * 0.1f);
                    hpRegenPercentage += 0.005f;
                    break;
            }
        }
    }

    protected virtual void GetControlInputs()
    {
        if (!IsAlive()) return;

        if (Input.GetKeyDown(KeyCode.Z))
        {
            StartCoroutine(Attack());
        }
        else Move();
    }

    public override void Move()
    {
        if (IsMovementLocked) return;
        
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        var movementInputs = new Vector2(moveHorizontal, moveVertical).normalized;

        rb2d.velocity = CalculateMovement(movementInputs);

        animator.SetFloat("move", Mathf.Abs(moveHorizontal) + Mathf.Abs(moveVertical));

        base.Move();
    }

    public override IEnumerator Attack()
    {
        if (!IsAlive() || IsAttackLocked) yield break;
        
        StartCoroutine(base.Attack());

        yield return new WaitForSeconds(GetWindupTime());
        yield return null;
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
        StartCoroutine(playerManager.AttackCooldown(GetAttackLockoutTime()));
        return base.LockoutMovementsOnAttack();
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
            bDef= bDef,
            def = def,
            bRes = bRes,
            res = res,
            attackPattern = attackPattern,
            damageType = damageType,
            mHealth = mHealth,
            health = health,
            moveSpeed = moveSpeed,
            SkillName = SkillName,
            SkillText = SkillDes,
            SpecialName = SpecialName,
            SpecialText = SpecialDes,
            AttackText = "Perform an attack that deals",
        };
    }

    public override void TakeDamage(DamageInstance damage, EntityBase source)
    {
        base.TakeDamage(damage, source); 
        
        if (source)
            playerManager.OnPlayerAttacked(damage.TotalDamage * 1.0f / (mHealth * 0.5f));
    }

    public override void DealDamage(EntityBase target, int pDmg, int mDmg, int tDmg, bool allowWhenDisabled = false)
    {
        base.DealDamage(target, pDmg, mDmg, tDmg, allowWhenDisabled);
        if (!target.IsAlive())
        {
            if (Skills.Contains(SkillTree_Manager.SkillName.VICTORY_ATK))
                ApplyEffect(Effect.AffectedStat.ATK, "VICTORY_ATK_BUFF", 50, 5, true, true);
            else if (Skills.Contains(SkillTree_Manager.SkillName.VICTORY_MSPD))
                ApplyEffect(Effect.AffectedStat.MSPD, "VICTORY_MSPD_BUFF", 50, 5, true, true);
        }
    }

    public override void OnDeath()
    {
        base.OnDeath();
        playerManager.OnPlayerDeath();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision || !collision.gameObject) return;

        if (collision.gameObject.CompareTag("Fumo"))
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