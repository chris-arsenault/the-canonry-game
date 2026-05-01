namespace IceRemembers;

using Engine.Domain.Config;

using Attr = IceRemembers.IceRemembersAttributeId;
using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;

public static class IceRemembersDegeneracyDefinition
{
    private const string SettlementDepopulationId = "settlement_collapse";
    private const string PoliticalMonocultureId = "political_monoculture";
    private const string PenguinExtinctionId = "penguin_extinction";
    private const string SettlementDepopulationReason = "settlement_collapse_transition";
    private const string PoliticalMonocultureReason = "political_monoculture_transition";
    private const string PenguinExtinctionReason = "penguin_extinction_transition";

    public static DegeneracyConfig Create() => new()
    {
        Detectors =
        [
            new DegeneracyDetectorConfig
            {
                Id = SettlementDepopulationId,
                DisplayName = "Settlement Collapse",
                Description = "Too many settlements have declining temporary health.",
                ReasonCode = SettlementDepopulationReason,
                Metric = DegeneracyMetric.AttributeDeclineRatio,
                ThresholdDirection = DegeneracyThresholdDirection.HighIsBad,
                NoSubjectsSeverity = DegeneracySeverity.Critical,
                PrimaryKind = K.Settlement,
                Attribute = Attr.TempHp.ToDomainName(),
                WarningThreshold = 0.5f,
                CriticalThreshold = 0.8f,
            },
            new DegeneracyDetectorConfig
            {
                Id = PoliticalMonocultureId,
                DisplayName = "Political Monoculture",
                Description = "One faction controls too much of the world.",
                ReasonCode = PoliticalMonocultureReason,
                Metric = DegeneracyMetric.RelationshipEndpointDominanceRatio,
                ThresholdDirection = DegeneracyThresholdDirection.HighIsBad,
                NoSubjectsSeverity = DegeneracySeverity.Normal,
                PrimaryKind = K.Settlement,
                SecondaryKind = K.Faction,
                RelationshipKind = Rel.ControlledBy.ToDomainName(),
                DominantRelationshipEndpoint = DegeneracyRelationshipEndpoint.Target,
                WarningThreshold = 0.6f,
                CriticalThreshold = 0.8f,
            },
            new DegeneracyDetectorConfig
            {
                Id = PenguinExtinctionId,
                DisplayName = "Penguin Extinction",
                Description = "Too few penguins remain active.",
                ReasonCode = PenguinExtinctionReason,
                Metric = DegeneracyMetric.ActiveKindCount,
                ThresholdDirection = DegeneracyThresholdDirection.LowIsBad,
                NoSubjectsSeverity = DegeneracySeverity.Critical,
                PrimaryKind = K.Penguin,
                WarningThreshold = 5f,
                CriticalThreshold = 2f,
            },
        ],
    };
}
