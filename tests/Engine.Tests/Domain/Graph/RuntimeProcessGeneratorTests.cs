namespace Engine.Tests.Domain.Graph;

using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Domain.IceRemembers;
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
            DefinitionRole.Process,
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
    public void Generate_FromIceRemembers_ProducesOneProcessPerEntityVerb()
    {
        var config = IceRemembersDefinition.Create();
        var entityVerbs = config.Semantics!.Verbs
            .Where(v => v.OwnerType == SemanticOwnerType.Entity)
            .ToList();

        Assert.Equal(entityVerbs.Count, config.Processes.Count);
        foreach (var verb in entityVerbs)
        {
            Assert.Single(config.Processes, p => p.Id == verb.Id);
        }
    }

    [Fact]
    public void Generate_MobilityVerbs_OneProcessPerKind_OnOwnerPlane()
    {
        var config = IceRemembersDefinition.Create();
        var processesById = config.Processes.ToDictionary(p => p.Id);

        var physicalMobility = new[] { "penguin_travel", "artifact_compel" };
        foreach (var id in physicalMobility)
        {
            var proc = processesById[id];
            Assert.Contains(proc.Mutations, m => m is MoveTowardTargetMutation);
        }

        var semanticMobility = new[] { "settlement_deliberate", "faction_preach", "region_commune" };
        foreach (var id in semanticMobility)
        {
            var proc = processesById[id];
            Assert.Contains(proc.Mutations, m => m is MoveTowardTargetOnSemanticPlaneMutation);
        }
    }

    [Fact]
    public void Generate_DriftVerbs_OneProcessPerKind_OnOwnerPlane()
    {
        var config = IceRemembersDefinition.Create();
        var processesById = config.Processes.ToDictionary(p => p.Id);

        foreach (var id in new[] { "penguin_wander", "artifact_circulate" })
        {
            var proc = processesById[id];
            Assert.IsType<AlwaysTrue>(proc.Precondition);
            Assert.Contains(proc.Mutations, m => m is WanderMutation);
        }

        foreach (var id in new[] { "settlement_gossip", "faction_quarrel", "region_dream" })
        {
            var proc = processesById[id];
            Assert.IsType<AlwaysTrue>(proc.Precondition);
            Assert.Contains(proc.Mutations, m => m is WanderOnSemanticPlaneMutation);
        }
    }

    [Fact]
    public void Generate_SelfSacrifice_UsesConservationTransfer_SelfToSelf()
    {
        var config = IceRemembersDefinition.Create();
        // artifact_channel: Condition → Power (self-sac).
        var channel = config.Processes.Single(p => p.Id == "artifact_channel");

        var transfer = Assert.Single(channel.Mutations.OfType<TransferAttributeMutation>());
        Assert.Equal(TransferEndpoint.Self, transfer.Source);
        Assert.Equal("condition", transfer.SourceAttribute);
        Assert.Equal(TransferEndpoint.Self, transfer.Destination);
        Assert.Equal("power", transfer.DestinationAttribute);
        Assert.Equal(5f, transfer.Amount);
    }

    [Fact]
    public void Generate_HexToHealth_UsesHexToHealthMutation_NoDrain()
    {
        var config = IceRemembersDefinition.Create();
        // penguin_warm: hex.food -> Penguin.vitality.
        var eat = config.Processes.Single(p => p.Id == "penguin_warm");

        var mutation = Assert.Single(eat.Mutations.OfType<HexToHealthMutation>());
        Assert.Equal("vitality", mutation.AttributeName);
        Assert.Equal(ResourceType.Food, mutation.Resource);
        Assert.Equal(2f, mutation.RatePerHexValue);
        Assert.True(mutation.TargetLocation);
        // No location-resource mutation (hex is possibility space, not stock).
        Assert.DoesNotContain(eat.Mutations, m => m is ModifyLocationResourceMutation);
    }

    [Fact]
    public void Generate_HexToHealth_TargetsConfiguredRecoveryHosts()
    {
        var config = IceRemembersDefinition.Create();
        var eat = config.Processes.Single(p => p.Id == "penguin_warm");
        var precondition = Assert.IsType<AndPrecondition>(eat.Precondition);
        Assert.Contains(precondition.Children, p => p is TargetIsNotSelf);
        Assert.DoesNotContain(precondition.Children, p => p is HasColocatedNamedEntityPrecondition);
        Assert.Contains(precondition.Children, p => p is WithinPlaneDistancePrecondition);
        Assert.Contains(
            eat.ProviderSelectors,
            selector => selector.Scope == RefScope.Structural &&
                        selector.Kind == IceRemembersKinds.Settlement &&
                        selector.Name == "coordinates");
    }

    [Fact]
    public void Generate_UntouchedToHex_DebitsFiveFromAttr_BumpsHexByOne()
    {
        var config = IceRemembersDefinition.Create();
        // penguin_witness: Penguin.echo -> hex.population.
        var inspire = config.Processes.Single(p => p.Id == "penguin_witness");

        var attrMut = Assert.Single(inspire.Mutations.OfType<ModifySelfAttributeMutation>());
        Assert.Contains("echo", attrMut.Describe(), StringComparison.Ordinal);
        Assert.Contains("delta=-5", attrMut.Describe(), StringComparison.Ordinal);

        var resMut = Assert.Single(inspire.Mutations.OfType<ModifyLocationResourceMutation>());
        Assert.Contains("population", resMut.Describe(), StringComparison.Ordinal);
        Assert.Contains("delta=1", resMut.Describe(), StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_ConversionVerb_UsesConservationTransfer_TargetToSelf()
    {
        var config = IceRemembersDefinition.Create();
        // penguin_wield: Artifact.power -> Penguin.echo. Owner = Penguin (destination).
        var wield = config.Processes.Single(p => p.Id == "penguin_wield");

        Assert.Equal(IceRemembersKinds.Penguin, wield.TargetKind);
        var transfer = Assert.Single(wield.Mutations.OfType<TransferAttributeMutation>());
        Assert.Equal(TransferEndpoint.Target, transfer.Source);
        Assert.Equal("power", transfer.SourceAttribute);
        Assert.Equal(TransferEndpoint.Self, transfer.Destination);
        Assert.Equal("echo", transfer.DestinationAttribute);
        Assert.Equal(5f, transfer.Amount);
    }

    [Fact]
    public void Generate_ConversionVerb_RequiresOwnerPlaneProximity()
    {
        var config = IceRemembersDefinition.Create();

        // Owner on physical plane → physical proximity check.
        var wield = config.Processes.Single(p => p.Id == "penguin_wield");
        var wieldPre = Assert.IsType<AndPrecondition>(wield.Precondition);
        Assert.Contains(wieldPre.Children,
            p => p is WithinPlaneDistancePrecondition w && w.Plane == PrimaryPlane.Physical && w.MaxDistance == 1);

        // Owner on semantic plane → semantic proximity check.
        var employ = config.Processes.Single(p => p.Id == "settlement_claims_echo");
        var employPre = Assert.IsType<AndPrecondition>(employ.Precondition);
        Assert.Contains(employPre.Children,
            p => p is WithinPlaneDistancePrecondition w && w.Plane == PrimaryPlane.Semantic && w.MaxDistance == 1);
    }

    [Fact]
    public void Generate_ConversionVerb_SoftGateOfFiveOnOutflowSide()
    {
        var config = IceRemembersDefinition.Create();
        // settlement_claims_echo: penguin.echo → settlement.dominion. Owner = settlement (dest).
        // Source stock is on the target (penguin.echo); precondition checks target has >= 5.
        var employ = config.Processes.Single(p => p.Id == "settlement_claims_echo");
        var pre = Assert.IsType<AndPrecondition>(employ.Precondition);
        Assert.Contains(pre.Children,
            p => p is TargetAttributeThreshold t && t.AttributeName == "echo" && t.Min == 5f);

        // faction_starve: penguin.warmth -> faction.provisions. Owner = faction (dest).
        var starve = config.Processes.Single(p => p.Id == "faction_starve");
        var starvePre = Assert.IsType<AndPrecondition>(starve.Precondition);
        Assert.Contains(starvePre.Children,
            p => p is TargetAttributeThreshold t && t.AttributeName == "warmth" && t.Min == 5f);
    }
}
