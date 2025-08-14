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

    IEnumerator Explode()
    {
        float targetRadius = explosionRadius * 2.025f;

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
            innerRect.sizeDelta = new Vector2(
                Mathf.Lerp(0, targetRadius, c * 1.0f / explosionDelay),
                Mathf.Lerp(0, targetRadius, c * 1.0f / explosionDelay)
            );

            if (c >= explosionDelay - 0.1f && !spawnedEffect)
            {
                GameObject effect = Instantiate(ExplosionEffect, transform.position + new Vector3(0, 60), Quaternion.identity);
                effect.transform.localScale = new Vector3(
                    targetRadius * 0.26f, 
                    targetRadius * 0.22f
                );

                Destroy(effect, 2.0f);
                spawnedEffect = true;
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

        Destroy(gameObject);
    }

    public override void WriteStats()
    {
        Description = "A spider-like creature that has been mutated by Originium.";
        Skillset = "";
        TooltipsDescription = "Explodes upon death, dealing true damage. " +
            "Will be instantly defeated upon taking damage from <color=#CC4000>Originium Pollution</color>, " +
            "causing a more violent explosion.";
        base.WriteStats();
    }
}