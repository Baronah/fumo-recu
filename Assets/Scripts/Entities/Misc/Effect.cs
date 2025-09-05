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

    public bool IsInEffect => Helder != null && Value > 0f && Duration > 0f;

    public Effect(EntityBase helder, float value, float duration, bool isPercentage)
    {
        Helder = helder;
        Value = value;
        Duration = duration;
        IsPercentage = isPercentage;
    }

    public void Instantiate(EntityBase helder, float value, float duration, bool isPercentage)
    {
        Instantiate(new(helder, value, duration, isPercentage));
    }

    public void Instantiate(Effect effect)
    {
        if (IsGreaterThan(effect)) return;

        Helder = effect.Helder;
        Value = effect.Value;
        Duration = effect.Duration;
        IsPercentage = effect.IsPercentage;
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
        Value = Duration = 0;
    }
}