namespace Engine.Domain.Compilation.KernelPrograms;

public sealed class MinorActorProgram
{
    public bool EnabledByDefault { get; init; }

    public IReadOnlyList<MinorActorRoleRule> RoleRules { get; init; } = [];

    /// <summary>Relationship kind whose living edges aim campaign-labor cohorts (0 = none).</summary>
    public int CampaignRelationshipKindId { get; init; }

    /// <summary>Relationship kind that marks a laden carrier for the movement road-speed bonus (0 = none).</summary>
    public int CarrierRelationshipKindId { get; init; }
}

public sealed record MinorActorRoleRule(
    int SourceKindId,
    int SourceSubtypeId,
    int RoleCode,
    int PlaneCode,
    int AssignmentRadius,
    int Stride,
    int CommitmentTicks,
    int ReplanTicks,
    FixedScalar BaseStrength);
