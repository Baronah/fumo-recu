using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MedicalTile : EnvironmentalTileBase
{
    [SerializeField] private float HealPerTick = 15f;
    [SerializeField] private bool UsePercentageHeal = false;

    public override StageManager.EnvironmentType GetEnvironmentType()
    {
        return StageManager.EnvironmentType.MEDICAL_TILE;
    }

    public override void OnStageStart()
    {
        if (CharacterPrefabsStorage.Skills.ContainsKey(SkillTree_Manager.SkillName.TERRAIN))
        {
            bool hasGeologist = CharacterPrefabsStorage.Skills.ContainsKey(SkillTree_Manager.SkillName.GEOGOLIST_OBSERVE)
                || CharacterPrefabsStorage.Skills.ContainsKey(SkillTree_Manager.SkillName.GEOGOLIST_EXPLORE)
                || CharacterPrefabsStorage.Skills.ContainsKey(SkillTree_Manager.SkillName.GEOGOLIST_STUDY);
            float multiplier = hasGeologist ? 3f : 2f;
            HealPerTick *= multiplier;
        }

        StartCoroutine(Pulse());
        base.OnStageStart();
    }

    public override void OnEntityStay(EntityBase entity)
    {
        base.OnEntityStay(entity);
        float healAmount = UsePercentageHeal ? entity.mHealth * HealPerTick : HealPerTick;
        
        if (entity is SaintStatue ss) ss.OnMedicalTileHealingReceive(healAmount);
        else entity.Heal(healAmount);
    }

    IEnumerator Pulse()
    {
        Tilemap tilemap = GetComponent<Tilemap>();

        Color init = tilemap.color; 
        Color target = Color.green;
        target.a = 0.8f;

        float duration = 3f;
        while (true)
        {
            float c = 0;

            while (c < duration)
            {
                tilemap.color = Color.Lerp(init, target, c * 1.0f / duration);
                c += Time.deltaTime;
                yield return null;
            }

            c = 0;
            while (c < duration)
            {
                tilemap.color = Color.Lerp(target, init, c * 1.0f / duration);
                c += Time.deltaTime;
                yield return null;
            }

            tilemap.color = init;
        }
    }
}
