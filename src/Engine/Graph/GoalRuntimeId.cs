namespace Engine.Graph;

/// <summary>Primitive runtime goal identity used inside the simulation kernel.</summary>
public readonly record struct GoalRuntimeId(int Value)
{
    public static readonly GoalRuntimeId None = new(0);
    public bool IsNone => Value == 0;
}
