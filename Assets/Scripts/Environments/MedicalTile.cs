using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MedicalTile : EnvironmentalTileBase
{
    [SerializeField] private float HealPerTick = 15f;
    [SerializeField] private bool UsePercentageHeal = false;

    public override void OnEntityStay(EntityBase entity)
    {
        base.OnEntityStay(entity);
        float healAmount = UsePercentageHeal ? entity.mHealth * HealPerTick : HealPerTick;
        entity.Heal(healAmount);
    }
}
