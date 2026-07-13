namespace Engine.Kernel;

internal readonly record struct RelationshipDecision(
    bool Active,
    int ContactTicks,
    int LastContactTick,
    float Strength,
    bool StrengthChanged,
    bool Cull,
    bool FollowSource);
