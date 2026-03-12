using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageInstance
{ 
    public float PhysicalDamage { get; set; }
    private float BasePhysicalDamage { get; set; }

    public float MagicalDamage { get; set; }
    private float BaseMagicalDamage { get; set; }

    public float TrueDamage { get; set; }
    private float BaseTrueDamage { get; set; }

    public bool IsDodged = false;

    public float TotalDamage => PhysicalDamage + MagicalDamage + TrueDamage;

    public DamageInstance() => Set(0);

    public DamageInstance(float amount) => Set(amount);

    public DamageInstance(DamageInstance damageInstance)
        => Set(damageInstance.PhysicalDamage, damageInstance.MagicalDamage, damageInstance.TrueDamage);

    public DamageInstance(float physical, float magical, float trueDamage)
        => Set(physical, magical, trueDamage);


    public void Multiply(float percentage) => Multiply(percentage, percentage, percentage);

    public void Multiply(float pPercentage, float mPercentage, float tPercentage)
    {
        PhysicalDamage = Mathf.CeilToInt(PhysicalDamage * pPercentage);
        MagicalDamage = Mathf.CeilToInt(MagicalDamage * mPercentage);
        TrueDamage = Mathf.CeilToInt(TrueDamage * tPercentage);
    }

    public void MultiplyBase(float percentage) => MultiplyBase(percentage, percentage, percentage);

    public void MultiplyBase(float pPercentage, float mPercentage, float tPercentage)
    {
        PhysicalDamage = Mathf.CeilToInt(BasePhysicalDamage * pPercentage);
        MagicalDamage = Mathf.CeilToInt(BaseMagicalDamage * mPercentage);
        TrueDamage = Mathf.CeilToInt(BaseTrueDamage * tPercentage);
    }

    public void Set(float physical, float magical, float trueDamage)
    {
        PhysicalDamage = BasePhysicalDamage = physical;
        MagicalDamage = BaseMagicalDamage = magical;
        TrueDamage = BaseTrueDamage = trueDamage;
    }

    public void Set(float amount) => Set(amount, amount, amount);

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

    public void Add(float amount) => Add(amount, amount, amount);

    public void Add(float pAmount, float mAmount, float tAmount)
    {
        PhysicalDamage += pAmount;
        MagicalDamage += mAmount;
        TrueDamage += tAmount;
    }

    public void AddByPercentage(float percentage) => AddByPercentage(percentage, percentage, percentage);

    public void AddByPercentage(float pPercentage, float mPercentage, float tPercentage)
    {
        PhysicalDamage += BasePhysicalDamage * pPercentage;
        MagicalDamage += BaseMagicalDamage * mPercentage;
        TrueDamage += BaseTrueDamage * tPercentage;
    }

    public override string ToString()
    {
        return 
            $"(Base) Physical: {BasePhysicalDamage} / {PhysicalDamage}\n" +
            $"(Base) Magical: {BaseMagicalDamage} / {MagicalDamage}\n" +
            $"(Base) True: {BaseTrueDamage} / {TrueDamage}\n" +
            $"Total: {TotalDamage}";
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