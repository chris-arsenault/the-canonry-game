namespace IceRemembers;

using Engine.Domain.Config;

using Attr = IceRemembers.IceRemembersAttributeId;
using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;

public static class IceRemembersDegeneracyDefinition
{
    private const string SettlementDepopulationId = "settlement_depopulation";
    private const string PoliticalMonocultureId = "political_monoculture";
    private const string NpcExtinctionId = "npc_extinction";
    private const string SettlementDepopulationReason = "settlement_depopulation_transition";
    private const string PoliticalMonocultureReason = "political_monoculture_transition";
    private const string NpcExtinctionReason = "npc_extinction_transition";

    public static DegeneracyConfig Create() => new()
    {
        Detectors =
        [
            new DegeneracyDetectorConfig
            {
                Id = SettlementDepopulationId,
                DisplayName = "Settlement Depopulation",
                Description = "Too many settlements have declining resident populations.",
                ReasonCode = SettlementDepopulationReason,
                Metric = DegeneracyMetric.AttributeDeclineRatio,
                ThresholdDirection = DegeneracyThresholdDirection.HighIsBad,
                NoSubjectsSeverity = DegeneracySeverity.Critical,
                PrimaryKind = K.Settlement,
                Attribute = Attr.Residents.ToDomainName(),
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
                Id = NpcExtinctionId,
                DisplayName = "NPC Extinction",
                Description = "Too few penguins remain active.",
                ReasonCode = NpcExtinctionReason,
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
