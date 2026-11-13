using UnityEngine;

public class Effect
{
    public enum AffectedStat
    {
        ATK,
        DEF,
        RES,
        HP,
        ARNG,
        MSPD,
        ASPD,
        WEIGHT,
    }

    public enum ValueType
    {
        FLAT,
        PERCENTAGE,
    }

    public AffectedStat affectedStat;
    
    protected EntityBase Helder;
    public float Value, Duration;
    public ValueType valueType;
    public bool DecayOverDuration;
    public bool TransferOnSwap = true;

    private float InitValue, InitDuration;

    public bool IsInEffect => Helder != null && Value > 0f && Duration > 0f;

    public Effect(EntityBase helder, AffectedStat affectedStat, float value, float duration, ValueType valueType, bool decayOverDuration, bool transferOnSwap)
    {
        Helder = helder;
        InitValue = Value = value;
        InitDuration = Duration = duration;
        this.valueType = valueType;
        DecayOverDuration = decayOverDuration;
        TransferOnSwap = transferOnSwap;
        this.affectedStat = affectedStat;
    }

    public void Decay()
    {
        if (!DecayOverDuration) return;
        Value = Mathf.Lerp(0, InitValue, Duration * 1.0f / InitDuration);
    }

    public void Instantiate(EntityBase helder, AffectedStat affectedStat, float value, float duration, ValueType valueType, bool decayOverDuration = false, bool transferOnSwap = true)
    {
        Instantiate(new(helder, affectedStat, value, duration, valueType, decayOverDuration, transferOnSwap));
    }

    public void Instantiate(Effect effect)
    {
        if (IsGreaterThan(effect)) return;

        EndEffect();
        Helder = effect.Helder;
        InitValue = Value = effect.Value;
        InitDuration = Duration = effect.Duration;
        valueType = effect.valueType;
        DecayOverDuration = effect.DecayOverDuration;
        TransferOnSwap = effect.TransferOnSwap;
        affectedStat = effect.affectedStat;
    }

    public bool IsGreaterThan(Effect other)
    {
        if (!IsInEffect) return false;
        if (!other.IsInEffect) return true;
        if (Value > other.Value && valueType == other.valueType) return true;
        if (Value == other.Value && valueType == other.valueType) return Duration > other.Duration;

        float selfValue = valueType == ValueType.PERCENTAGE ? Value * GetAffectAttributeValue() : Value, 
              otherValue = other.valueType == ValueType.PERCENTAGE ? other.Value * other.GetAffectAttributeValue() : other.Value;

        return selfValue > otherValue;
    }

    public float GetAffectAttributeValue()
    {
        return affectedStat switch
        {
            AffectedStat.ATK => Helder.bAtk,
            AffectedStat.DEF => Helder.bDef,
            AffectedStat.RES => Helder.bRes,
            AffectedStat.ARNG => Helder.b_attackRange,
            AffectedStat.HP => Helder.mHealth,
            AffectedStat.MSPD => Helder.b_moveSpeed,
            AffectedStat.ASPD => 100,
            _ => 0,
        };
    }

    public void EndEffect()
    {
        Helder = null;
        InitValue = Value = InitDuration = Duration = 0;
        DecayOverDuration = false;
    }
}