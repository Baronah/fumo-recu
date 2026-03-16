using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCasterIllusion : EntityBase
{
    [SerializeField] private AudioSource Debut;
    [SerializeField] private Transform SkillPosition;
    [SerializeField] private GameObject SkillEffect, SkillBarObj;
    private Slider SkillBar;

    private float SkillDuration = 7f;
    private float SkillCurrentDuration = 0;
    
    private float Skill_DamageMulitplier = 0.25f;
    private float Skill_AtkInterval = 0.25f;

    bool isComponentsInitialized = false;
    public override void InitializeComponents()
    {
        isComponentsInitialized = true;
        SkillBar = SkillBarObj.GetComponentInChildren<Slider>();
        base.InitializeComponents();
        isInvisible = true;
        SetInvulnerable(9999f);
    }

    public bool FieldExpert = false;
    public PlayerManager playerManager = null;
    public void SetInherit(short ATK, float maxDuration, float duration, float multiplier, float interval, bool FieldExpert, PlayerManager playerManager, float lifeSpan, bool flipX)
    {
        InitSpriteColor = new(1, 0, 0.15f, 0.75f);
        spriteRenderer.color = InitSpriteColor;
        bAtk = atk = ATK;
        SkillDuration = maxDuration;
        SkillCurrentDuration = duration;
        Skill_DamageMulitplier = multiplier;
        Skill_AtkInterval = interval;

        this.FieldExpert = FieldExpert; 
        this.playerManager = playerManager;

        spriteRenderer.flipX = flipX;
        HandleSpriteFlipping();

        this.lifeSpan = lifeSpan;

        if (Debut) Debut.Play();
        StartCoroutine(CastSkill());
    }

    public override void Move()
    {
        
    }

    public override IEnumerator Attack()
    {
        yield return null;
    }

    public override void FlipAttackPosition()
    {
        base.FlipAttackPosition();
        SkillPosition.localPosition = new Vector3(
            -SkillPosition.localPosition.x,
            SkillPosition.localPosition.y,
            SkillPosition.localPosition.z
        );

        SkillEffect.transform.localPosition = new Vector3(
            -SkillEffect.transform.localPosition.x,
            SkillEffect.transform.localPosition.y,
            SkillEffect.transform.localPosition.z
        );
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        InitSpriteColor = new(1, 0, 0.15f, 0.4f);
        spriteRenderer.color = InitSpriteColor;
    }

    float lifeSpan;
    public IEnumerator CastSkill()
    {
        yield return null;
        if (!IsAlive()) yield break;

        SkillBarObj.SetActive(true);
        animator.SetTrigger("skill");
        float count = SkillCurrentDuration, 
              intervalCount = Skill_AtkInterval;
        float angleOffset = 0;

        SkillBar.maxValue = SkillDuration;
        SkillBar.value = SkillDuration - count;

        float speed = ProjectileSpeed * 0.25f;

        while (count < SkillDuration)
        {
            SkillBar.value = SkillDuration - count;
            if (intervalCount >= Skill_AtkInterval)
            {
                Vector3 sourcePosition = SkillPosition.position;
                intervalCount = 0;
                if (sfxs[2]) sfxs[2].Play();

                for (int i = 0; i < 360; i += 30)
                {
                    float currentAngle = i + angleOffset;

                    float angleInRadians = currentAngle * Mathf.Deg2Rad;

                    float circleRadius = 30f + (count * 5f);
                    Vector3 targetPosition = new Vector3(
                        sourcePosition.x + Mathf.Cos(angleInRadians) * circleRadius,
                        sourcePosition.y + Mathf.Sin(angleInRadians) * circleRadius,
                        sourcePosition.z
                    );

                    CreateProjectileAndShootToward(
                        ProjectilePrefab,
                        new DamageInstance(0, (int)(atk * Skill_DamageMulitplier), 0),
                        sourcePosition,
                        targetPosition,
                        projectileType: ProjectileScript.ProjectileType.CATCH_FIRST_TARGET_OF_TYPE,
                        travelSpeed: speed,
                        acceleration: speed,
                        lifeSpan: lifeSpan,
                        targetType: typeof(EnemyBase));
                }
                angleOffset += 6;
            }

            yield return null;
            count += Time.deltaTime;
            intervalCount += Time.deltaTime;
        }

        animator.SetTrigger("skill_end");
        yield return null;

        Destroy(this.gameObject);
    }
}