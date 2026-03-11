using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageInstance
{ 
    public float PhysicalDamage { get; set; }
    
    public float MagicalDamage { get; set; }
    
    public float TrueDamage { get; set; }

    public bool IsDodged = false;

    public float TotalDamage => PhysicalDamage + MagicalDamage + TrueDamage;

    public DamageInstance()
    {
        PhysicalDamage = 0;
        MagicalDamage = 0;
        TrueDamage = 0;
    }

    public DamageInstance(float amount)
    {
        PhysicalDamage = amount;
        MagicalDamage = amount;
        TrueDamage = amount;
    }

    public DamageInstance(DamageInstance damageInstance)
    {   
        PhysicalDamage = damageInstance.PhysicalDamage;
        MagicalDamage = damageInstance.MagicalDamage;
        TrueDamage = damageInstance.TrueDamage;
    }

    public DamageInstance(float physical, float magical, float trueDamage)
    {
        PhysicalDamage = physical;
        MagicalDamage = magical;
        TrueDamage = trueDamage;
    }

    public void Multiply(float percentage)
    {
        Multiply(percentage, percentage, percentage);
    }

    public void Multiply(float pPercentage, float mPercentage, float tPercentage)
    {
        PhysicalDamage = Mathf.CeilToInt(PhysicalDamage * pPercentage);
        MagicalDamage = Mathf.CeilToInt(MagicalDamage * mPercentage);
        TrueDamage = Mathf.CeilToInt(TrueDamage * tPercentage);
    }

    public void Set(float physical, float magical, float trueDamage)
    {
        PhysicalDamage = physical;
        MagicalDamage = magical;
        TrueDamage = trueDamage;
    }

    public void Set(float amount)
    {
        Set(amount, amount, amount);
    }

    public void SetTotal(float amount)
    {
        if (amount == 1)
        {
            Set(0, 0, 1);
            return;
        }
        float split = amount / 3;
        Set(split, split, amount - (split * 2));
    }
}

public interface IDamageStep
{
    void Process(EntityBase attacker, EntityBase target, DamageInstance instance);
}

public class DamagePipeline
{
    public EntityBase attacker, target;
    public DamageInstance instance;
    private readonly List<IDamageStep> steps = new();

    public void Add(IDamageStep step) => steps.Add(step);
    
    public DamageInstance Calculate()
    {
        foreach (var step in steps)
        {
            step.Process(attacker, target, instance);
        }

        return instance;
    }
}