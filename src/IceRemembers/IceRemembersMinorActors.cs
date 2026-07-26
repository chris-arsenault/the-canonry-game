namespace IceRemembers;

using Engine.Constants;
using Engine.Domain.Config;

using K = IceRemembers.IceRemembersKinds;

public static partial class IceRemembersDefinition
{
    private static readonly Dictionary<EntityKind, float> CulturalFrontStrengths = new()
    {
        [K.Settlement] = 2.8f,
        [K.Faction] = 2.6f,
        [K.Region] = 2.4f,
        [K.PointOfInterest] = 2.2f,
        [K.Transport] = 1.8f,
        [K.Orca] = 1.7f,
        [K.Penguin] = 1.6f,
        [K.Artifact] = 1.6f,
        [K.Ability] = 1.5f,
        [K.Occurrence] = 1.7f,
    };

    private static List<MinorActorRuleConfig> CreateMinorActorRules()
    {
        var rules = new List<MinorActorRuleConfig>();
        foreach (var entity in IceRemembersEntities.All)
        {
            rules.Add(CulturalFront(
                entity.Kind,
                NonMobilePlaneCode(entity.PrimaryPlane),
                CulturalFrontStrength(entity.Kind)));
        }

        rules.Add(RoadCrew(K.Transport));
        rules.Add(CampaignColumn(K.Faction));
        return rules;
    }

    private static MinorActorRuleConfig CulturalFront(
        EntityKind sourceKind,
        int planeCode,
        float strength) =>
        new()
        {
            SourceKind = sourceKind,
            RoleCode = MinorActorRoleCodes.CulturalFront,
            PlaneCode = planeCode,
            AssignmentRadius = sourceKind == K.Faction ? 6 : 5,
            Stride = planeCode == PlaneCodes.Semantic ? 3 : 2,
            CommitmentTicks = 8,
            ReplanTicks = 12,
            BaseStrength = strength,
        };

    private static MinorActorRuleConfig RoadCrew(EntityKind sourceKind) =>
        new()
        {
            SourceKind = sourceKind,
            RoleCode = MinorActorRoleCodes.RoadCrew,
            PlaneCode = PlaneCodes.Physical,
            AssignmentRadius = 6,
            Stride = 3,
            CommitmentTicks = 10,
            ReplanTicks = 15,
            BaseStrength = 1.6f,
        };

    private static MinorActorRuleConfig CampaignColumn(EntityKind sourceKind) =>
        new()
        {
            SourceKind = sourceKind,
            RoleCode = MinorActorRoleCodes.CampaignColumn,
            PlaneCode = PlaneCodes.Physical,
            AssignmentRadius = 7,
            Stride = 2,
            CommitmentTicks = 6,
            ReplanTicks = 8,
            BaseStrength = 1.6f,
        };

    private static int NonMobilePlaneCode(PrimaryPlane primaryPlane) =>
        primaryPlane == PrimaryPlane.Physical
            ? PlaneCodes.Semantic
            : PlaneCodes.Physical;

    private static float CulturalFrontStrength(EntityKind sourceKind) =>
        CulturalFrontStrengths.TryGetValue(sourceKind, out var strength)
            ? strength
            : 1.2f;
}
