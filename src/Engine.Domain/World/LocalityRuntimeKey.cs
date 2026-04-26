namespace Engine.Graph;

using Engine.Domain.Config;

public readonly record struct LocalityVisitId(int Value)
{
    public static readonly LocalityVisitId None = new(0);
    public bool IsNone => Value == 0;
}

public readonly record struct LocalityRuntimeKey(PrimaryPlane Plane, int Q, int R, int S)
{
    public static LocalityRuntimeKey Create(PrimaryPlane plane, int q, int r, int? s = null) =>
        new(plane, q, r, s ?? (-q - r));
}

public enum LocalitySelectionReason
{
    None = 0,
    BestBundle = 1,
    KeptCommitment = 2,
    ContinuedCommitment = 3,
    SwitchedCommitment = 4,
}
