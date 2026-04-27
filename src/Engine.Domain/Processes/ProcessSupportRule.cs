namespace Engine.Processes;

using Engine.Wiring;

/// <summary>
/// ProcessProgram metadata describing a support action that can improve a process's
/// downstream utility even when that process is not hard-blocked.
/// </summary>
public sealed class ProcessSupportRule
{
    public AttributeRef DesiredEffect { get; }
    public IExpression CurrentValueExpression { get; }
    public float DesiredMinimum { get; }
    public float UrgencyScale { get; }

    public ProcessSupportRule(
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
