using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBase : EntityBase
{
    public Sprite AttackSprite, SkillSprite, SpecialSprite;
    public string AttackDes, SkillName, SkillDes, SpecialName, SpecialDes;
    protected PlayerManager playerManager;
    protected StageManager StageManager;

    private Transform TransformFeetposition;
    public Vector3 Feetposition => TransformFeetposition.position;

    private void Update()
    {
        GetControlInputs();
    }

    public override void InitializeComponents()
    {
        StageManager = FindObjectOfType<StageManager>();
        StageManager.OnPlayerSpawn(this);

        base.InitializeComponents();
        TransformFeetposition = transform.Find("Feetposition");

        playerManager = FindObjectOfType<PlayerManager>();
        playerManager.Register(this);

        StartCoroutine(InvulnerableOnSpawn());
    }

    IEnumerator InvulnerableOnSpawn()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(1f);
        isInvulnerable = false;
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
}