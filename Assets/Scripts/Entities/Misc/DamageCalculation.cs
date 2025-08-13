using UnityEngine;

namespace DamageCalculation 
{
    public class ModifyRawDamage : IDamageStep
    {
        public void Process(EntityBase attacker, EntityBase target, DamageInstance instance)
        {

        }
    }

    public class CalculateDefense : IDamageStep
    {

        public void Process(EntityBase attacker, EntityBase target, DamageInstance instance)
        {
            float MIN_PHYSICALDMG = attacker.MIN_PHYSICAL_DMG,
                  MIN_MAGICALDMG = attacker.MIN_MAGICAL_DMG;

            int physicalDamage = 
                Mathf.FloorToInt(
                    Mathf.Max(
                        instance.PhysicalDamage * MIN_PHYSICALDMG, 
                        instance.PhysicalDamage * (100 - ((target.def - attacker.defIgn) * (100 - attacker.defPen) / 100)) / 100
                    )
                );
            int magicalDamage = 
                Mathf.FloorToInt(
                    Mathf.Max(
                        instance.MagicalDamage * MIN_MAGICALDMG, 
                        instance.MagicalDamage * (100 - ((target.res - attacker.resIgn) * (100 - attacker.resPen) / 100)) / 100
                    )
                );

            instance.PhysicalDamage = instance.PhysicalDamage > 0 && physicalDamage <= 0 ? 1 : physicalDamage;
            instance.MagicalDamage = instance.MagicalDamage > 0 && magicalDamage <= 0 ? 1 : magicalDamage;
        }
    }
}