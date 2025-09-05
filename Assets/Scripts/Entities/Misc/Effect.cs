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
    }

    public AffectedStat affectedStat;
    
    protected EntityBase Helder;
    public float Value, Duration;
    public bool IsPercentage;
    public bool DecayOverDuration;

    private float InitValue, InitDuration;

    public bool IsInEffect => Helder != null && Value > 0f && Duration > 0f;

    public Effect(EntityBase helder, float value, float duration, bool isPercentage, bool decayOverDuration)
    {
        Helder = helder;
        InitValue = Value = value;
        InitDuration = Duration = duration;
        IsPercentage = isPercentage;
        DecayOverDuration = decayOverDuration;
    }

    public void Decay()
    {
        if (!DecayOverDuration) return;
        Value = Mathf.Lerp(0, InitValue, Duration * 1.0f / InitDuration);
    }

    public void Instantiate(EntityBase helder, float value, float duration, bool isPercentage, bool decayOverDuration = false)
    {
        Instantiate(new(helder, value, duration, isPercentage, decayOverDuration));
    }

    public void Instantiate(Effect effect)
    {
        if (IsGreaterThan(effect)) return;

        EndEffect();
        Helder = effect.Helder;
        InitValue = Value = effect.Value;
        InitDuration = Duration = effect.Duration;
        IsPercentage = effect.IsPercentage;
        DecayOverDuration = effect.DecayOverDuration;
    }

    public bool IsGreaterThan(Effect other)
    {
        if (!IsInEffect) return false;
        if (!other.IsInEffect) return true;
        if (Value > other.Value && IsPercentage == other.IsPercentage) return true;
        if (Value == other.Value && IsPercentage == other.IsPercentage) return Duration > other.Duration;

        float selfValue = IsPercentage ? Value * GetAffectAttributeValue() : Value, 
              otherValue = other.IsPercentage ? other.Value * other.GetAffectAttributeValue() : other.Value;

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