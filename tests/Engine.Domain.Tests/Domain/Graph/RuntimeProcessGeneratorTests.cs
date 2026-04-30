namespace Engine.Domain.Tests.Domain.Graph;

using Engine.Domain.Config;
using Engine.Domain.Graph;
using IceRemembers;
using Engine.Kinds;
using Engine.Processes;
using Engine.Resources;
using Engine.Wiring;

public class RuntimeProcessGeneratorTests
{
    [Fact]
    public void Validate_TransferWithoutPreconditionGate_Throws()
    {
        // Construct a process definition with a TransferAttributeMutation but NO
        // matching source-stock gate in its precondition. The invariant should fire.
        var config = IceRemembersDefinition.Create();
        var ungated = new ProcessDefinition(
            "ungated_transfer",
            "Ungated Transfer",
            IceRemembersKinds.Penguin,
            new TargetIsSelf(),
            Expr.Score(Expr.Lit(1f)));
        ungated.Mutations.Add(new TransferAttributeMutation(
            TransferEndpoint.Self, "vitality",
            TransferEndpoint.Self, "echo",
            5f));

        var broken = new DomainConfig
        {
            Kinds = config.Kinds,
            Terrains = config.Terrains,
            Processes = [.. config.Processes, ungated],
            Relationships = config.Relationships,
            RelationshipFacts = config.RelationshipFacts,
            Pressures = config.Pressures,
            Lifecycles = config.Lifecycles,
            Resources = config.Resources,
            Cultures = config.Cultures,
            Semantics = config.Semantics,
        };

        var ex = Assert.Throws<DefinitionGraphException>(
            () => Engine.Domain.Loader.DomainLoader.BuildDefinitionGraph(broken));
        Assert.Contains("ungated_transfer", ex.Message, StringComparison.Ordinal);
        Assert.Contains("no precondition gates the source", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void IceRemembers_DoesNotInstallLegacyGeneratedProcesses()
    {
        var config = IceRemembersDefinition.Create();

        Assert.Empty(config.Processes);
    }

    [Fact]
    public void IceRemembers_LowAltitudeVerbs_CoverMobileAndInfluencePhysics()
    {
        var config = IceRemembersDefinition.Create();
        var ownerKinds = config.LowAltitudeVerbs
            .Select(verb => verb.Kind)
            .Distinct()
            .ToHashSet();

        Assert.Equal(
            new HashSet<EntityKind>
            {
                IceRemembersKinds.Penguin,
                IceRemembersKinds.Artifact,
                IceRemembersKinds.Settlement,
                IceRemembersKinds.Faction,
            },
            ownerKinds);
        Assert.Equal(18, config.LowAltitudeVerbs.Count);
    }

    [Fact]
    public void IceRemembers_LowAltitudeVerbs_TargetKnownWorldAffordances()
    {
        var config = IceRemembersDefinition.Create();

        Assert.Contains(config.LowAltitudeVerbs, verb =>
            verb.Id == "penguin_explore" &&
            verb.TargetMode == LowAltitudeVerbTargetMode.Explore &&
            verb.GeneratorAffordanceWeight > 0f);
        Assert.Contains(config.LowAltitudeVerbs, verb =>
            verb.Id == "penguin_go_fishing" &&
            verb.TargetMode == LowAltitudeVerbTargetMode.Terrain &&
            verb.Terrains is not null &&
            verb.Terrains.Contains("ocean"));
        Assert.Contains(config.LowAltitudeVerbs, verb =>
            verb.Id == "penguin_follow_artifact" &&
            verb.TargetMode == LowAltitudeVerbTargetMode.EntityKind &&
            verb.TargetKind == IceRemembersKinds.Artifact &&
            verb.GeneratorAffordanceWeight > 1f);
        Assert.Contains(config.LowAltitudeVerbs, verb =>
            verb.Id == "artifact_seek_bearer" &&
            verb.TargetMode == LowAltitudeVerbTargetMode.EntityKind &&
            verb.TargetKind == IceRemembersKinds.Penguin &&
            verb.GeneratorAffordanceWeight > 1f);
        Assert.Contains(config.LowAltitudeVerbs, verb =>
            verb.Id == "settlement_build_the_rookery" &&
            verb.Operation == LowAltitudeVerbOperation.ExpandCoverage &&
            verb.TargetMode == LowAltitudeVerbTargetMode.OwnInfluence &&
            verb.CoverageGrowthChance > 0f);
        Assert.Contains(config.LowAltitudeVerbs, verb =>
            verb.Id == "faction_move_seat" &&
            verb.Operation == LowAltitudeVerbOperation.MoveHeadquarters &&
            verb.TargetMode == LowAltitudeVerbTargetMode.OwnInfluence &&
            verb.HeadquartersMoveChance > 0f);
    }
}
