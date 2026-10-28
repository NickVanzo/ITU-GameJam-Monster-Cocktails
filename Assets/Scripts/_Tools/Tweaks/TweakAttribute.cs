using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
public class TweakAttribute : Attribute
{
    public readonly string sCategory;
    public readonly float fMin;
    public readonly float fMax;
    public bool HasRange => fMin < fMax;

    public TweakAttribute(string sCategory = null)
    {
        this.sCategory = sCategory;
    }

    public TweakAttribute(float fMin, float fMax) : this(null, fMin, fMax)
    {   
    }

    public TweakAttribute(string sCategory, float fMin, float fMax)
    {
        this.sCategory = sCategory;
        this.fMin = fMin;
        this.fMax = fMax;
    }
}
