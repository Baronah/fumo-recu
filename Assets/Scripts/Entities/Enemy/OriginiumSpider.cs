using System.Collections;
using UnityEngine;

public class OriginiumSpider : EnemyBase
{
    [SerializeField] private float explosionDelay = 0.5f;
    [SerializeField] private float explosionAtkScale = 2.0f;
    [SerializeField] private float explosionRadius = 250f;

    [SerializeField] private float pollutedExplosionAtkScaleMultiplier = 2.0f;
    [SerializeField] private float pollutedExplosionRadiusMultiplier = 2.0f;

    [SerializeField] private GameObject ExplosionRangeIndicator_Outer, ExplosionRangeIndicator_Inner, ExplosionEffect;

    private bool IsPolluted = false;

    public void Pollute()
    {
        if (IsPolluted) return;
        IsPolluted = true;
        explosionAtkScale *= pollutedExplosionAtkScaleMultiplier;
        explosionRadius *= pollutedExplosionRadiusMultiplier;
    }

    public override void OnDeath()
    {
        base.OnDeath();
        StartCoroutine(Explode());
    }

    public override IEnumerator OnAttackComplete()
    {
        if (sfxs[0]) sfxs[0].Play();
        return base.OnAttackComplete();
    }

    IEnumerator Explode()
    {
        float targetRadius = explosionRadius * 2.15f;

        ExplosionRangeIndicator_Outer.GetComponent<RectTransform>().sizeDelta = new Vector2(
            targetRadius, 
            targetRadius
        );

        ExplosionRangeIndicator_Outer.SetActive(true);
        ExplosionRangeIndicator_Inner.SetActive(true);

        float c = 0;
        bool spawnedEffect = false;

        RectTransform innerRect = ExplosionRangeIndicator_Inner.GetComponent<RectTransform>();
        while (c < explosionDelay)
        {
            float size = Mathf.Lerp(0, targetRadius, c * 1.0f / explosionDelay);
            innerRect.sizeDelta = new Vector2(
                size,
                size
            );

            if (c >= explosionDelay - 0.05f && !spawnedEffect)
            {
                GameObject effect = Instantiate(ExplosionEffect, transform.position + new Vector3(0, 60), Quaternion.identity);
                effect.transform.localScale = new Vector3(
                    targetRadius * 0.26f,
                    targetRadius * 0.22f
                );

                Destroy(effect, 2.0f);
                spawnedEffect = true;
                sfxs[1].Play();
            }

            c += Time.deltaTime;
            yield return null;
        }

        innerRect.sizeDelta = new Vector2(targetRadius, targetRadius);
        yield return null;

        var player = DetectPlayer(explosionRadius, true);
        if (player)
        {
            DealDamage(player, new DamageInstance(0, 0, (int)(atk * explosionAtkScale)));
        }

        ExplosionRangeIndicator_Outer.SetActive(false);
        ExplosionRangeIndicator_Inner.SetActive(false);
    }

    public override void WriteStats()
    {
        Description = "A spider-like creature that has been mutated by Originium.";
        Skillset = 
            "• Explodes upon death, dealing true damage in an area around self.\n" +
            "• Will be instantly defeated when coming into contact with an <color=#CC4000>Originium Pollution</color>, " +
            "causing a far stronger explosion.";
        TooltipsDescription = "Explodes upon death, dealing true damage. " +
            "Will be instantly defeated upon taking damage from <color=#CC4000>Originium Pollution</color>, " +
            "causing a more violent explosion.";
        base.WriteStats();
    }
}