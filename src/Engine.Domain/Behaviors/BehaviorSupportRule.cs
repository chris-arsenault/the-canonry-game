namespace Engine.Behaviors;

using Engine.Wiring;

/// <summary>
/// BehaviorProgram metadata describing a support action that can improve a behavior's
/// downstream utility even when that behavior is not hard-blocked.
/// </summary>
public sealed class BehaviorSupportRule
{
    public AttributeRef DesiredEffect { get; }
    public IExpression CurrentValueExpression { get; }
    public float DesiredMinimum { get; }
    public float UrgencyScale { get; }

    public BehaviorSupportRule(
        AttributeRef desiredEffect,
        IExpression currentValueExpression,
        float desiredMinimum,
        float urgencyScale = 1f)
    {
        DesiredEffect = desiredEffect;
        CurrentValueExpression = currentValueExpression;
        DesiredMinimum = desiredMinimum;
        UrgencyScale = urgencyScale;
    }
}
