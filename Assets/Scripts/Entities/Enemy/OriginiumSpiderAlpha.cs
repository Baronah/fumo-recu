using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class OriginiumSpiderAlpha : EnemyBase
{
    [SerializeField] private float explosionDelay = 0.5f;
    [SerializeField] private float explosionAtkScale = 2.0f;
    [SerializeField] private float explosionRadius = 250f;

    [SerializeField] private float pollutedExplosionAtkScaleMultiplier = 2.0f;
    [SerializeField] private float pollutedExplosionRadiusMultiplier = 2.0f;

    [SerializeField] private GameObject ExplosionRangeIndicator_Outer, ExplosionRangeIndicator_Inner, ExplosionEffect;
    
    [SerializeField] private Tile OriginiumTile;
    private Tilemap OriginiumTilemap;
    private bool IsPolluted = false;

    public override void InitializeComponents()
    {
        GameObject TilemapObj = GameObject.Find("OriginiumTiles");
        if (TilemapObj) OriginiumTilemap = TilemapObj.GetComponent<Tilemap>();
        base.InitializeComponents();
    }

    public override IEnumerator OnAttackComplete()
    {
        if (sfxs[0]) sfxs[0].Play();
        return base.OnAttackComplete();
    }

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
            innerRect.sizeDelta = new Vector2(
                Mathf.Lerp(0, targetRadius, c * 1.0f / explosionDelay),
                Mathf.Lerp(0, targetRadius, c * 1.0f / explosionDelay)
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

        var position = FeetPosition.position;

        OriginiumTilemap.SetTile(
            OriginiumTilemap.WorldToCell(position),
            OriginiumTile
        );

        if (IsPolluted)
        {
            float offset = 75f;

            // adjacent tiles
            OriginiumTilemap.SetTile(
                OriginiumTilemap.WorldToCell(position + new Vector3(offset, 0)),
                OriginiumTile
            );

            OriginiumTilemap.SetTile(
                OriginiumTilemap.WorldToCell(position - new Vector3(offset, 0)),
                OriginiumTile
            );

            OriginiumTilemap.SetTile(
                OriginiumTilemap.WorldToCell(position + new Vector3(0, offset)),
                OriginiumTile
            );

            OriginiumTilemap.SetTile(
                OriginiumTilemap.WorldToCell(position - new Vector3(0, offset)),
                OriginiumTile
            );

            // diagonal tiles
            OriginiumTilemap.SetTile(
                OriginiumTilemap.WorldToCell(position + new Vector3(offset, offset)),
                OriginiumTile
            );

            OriginiumTilemap.SetTile(
                OriginiumTilemap.WorldToCell(position + new Vector3(-offset, offset)),
                OriginiumTile
            );

            OriginiumTilemap.SetTile(
                OriginiumTilemap.WorldToCell(position + new Vector3(offset, -offset)),
                OriginiumTile
            );

            OriginiumTilemap.SetTile(
                OriginiumTilemap.WorldToCell(position + new Vector3(-offset, -offset)),
                OriginiumTile
            );
        }

    }

    public override void WriteStats()
    {
        Description = "A spider-like creature that has assimilated into the Originium.";
        Skillset =
            "• Explodes upon death, dealing true damage in an area around self and creates an <color=#CC4000>Originium Pollution</color> on the spot.\n" +
            "• Will be instantly defeated when coming into contact with an <color=#CC4000>Originium Pollution</color>, " +
            "causing a stronger explosion and spreading the pollution to its nearby tiles.";
        TooltipsDescription = "Explodes upon death, dealing true damage " +
            "and creates an <color=#CC4000>Originium Pollution</color> on the spot. " +
            "Will be instantly defeated upon taking damage from <color=#CC4000>Originium Pollution</color>, " +
            "causing a more violent explosion and spreading the pollution.";
        base.WriteStats();
    }
}