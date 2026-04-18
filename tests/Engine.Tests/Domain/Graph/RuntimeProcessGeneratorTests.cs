namespace Engine.Tests.Domain.Graph;

using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Domain.TestDomain;
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
        var config = TestDomainDefinition.Create();
        var ungated = new ProcessDefinition(
            "ungated_transfer",
            "Ungated Transfer",
            DefinitionRole.Process,
            EntityKind.Npc,
            new TargetIsSelf(),
            Expr.Score(Expr.Lit(1f)));
        ungated.Mutations.Add(new TransferAttributeMutation(
            TransferEndpoint.Self, "vitality",
            TransferEndpoint.Self, "motivation",
            5f));

        var broken = new DomainConfig
        {
            Kinds = config.Kinds,
            Processes = [.. config.Processes, ungated],
            Relationships = config.Relationships,
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
    public void Generate_FromTestDomain_ProducesOneProcessPerEntityVerb()
    {
        var config = TestDomainDefinition.Create();
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
        var config = TestDomainDefinition.Create();
        var processesById = config.Processes.ToDictionary(p => p.Id);

        var physicalMobility = new[] { "npc_travel", "artifact_compel" };
        foreach (var id in physicalMobility)
        {
            var proc = processesById[id];
            Assert.Contains(proc.Mutations, m => m is MoveTowardTargetMutation);
        }

        var semanticMobility = new[] { "settlement_debate", "faction_evangelize", "region_commune" };
        foreach (var id in semanticMobility)
        {
            var proc = processesById[id];
            Assert.Contains(proc.Mutations, m => m is MoveTowardTargetOnSemanticPlaneMutation);
        }
    }

    [Fact]
    public void Generate_DriftVerbs_OneProcessPerKind_OnOwnerPlane()
    {
        var config = TestDomainDefinition.Create();
        var processesById = config.Processes.ToDictionary(p => p.Id);

        foreach (var id in new[] { "npc_wander", "artifact_circulate" })
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
        var config = TestDomainDefinition.Create();
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
        var config = TestDomainDefinition.Create();
        // npc_eat: hex.food → Npc.vitality.
        var eat = config.Processes.Single(p => p.Id == "npc_eat");

        var mutation = Assert.Single(eat.Mutations.OfType<HexToHealthMutation>());
        Assert.Equal("vitality", mutation.AttributeName);
        Assert.Equal(ResourceType.Food, mutation.Resource);
        Assert.Equal(2f, mutation.RatePerHexValue);
        // No location-resource mutation (hex is possibility space, not stock).
        Assert.DoesNotContain(eat.Mutations, m => m is ModifyLocationResourceMutation);
    }

    [Fact]
    public void Generate_HexToHealth_RequiresColocatedNamedEntity()
    {
        var config = TestDomainDefinition.Create();
        var eat = config.Processes.Single(p => p.Id == "npc_eat");
        var precondition = Assert.IsType<AndPrecondition>(eat.Precondition);
        Assert.Contains(precondition.Children, p => p is HasColocatedNamedEntityPrecondition);
    }

    [Fact]
    public void Generate_UntouchedToHex_DebitsFiveFromAttr_BumpsHexByOne()
    {
        var config = TestDomainDefinition.Create();
        // npc_inspire: Npc.motivation → hex.population.
        var inspire = config.Processes.Single(p => p.Id == "npc_inspire");

        var attrMut = Assert.Single(inspire.Mutations.OfType<ModifySelfAttributeMutation>());
        Assert.Contains("motivation", attrMut.Describe(), StringComparison.Ordinal);
        Assert.Contains("delta=-5", attrMut.Describe(), StringComparison.Ordinal);

        var resMut = Assert.Single(inspire.Mutations.OfType<ModifyLocationResourceMutation>());
        Assert.Contains("population", resMut.Describe(), StringComparison.Ordinal);
        Assert.Contains("delta=1", resMut.Describe(), StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_ConversionVerb_UsesConservationTransfer_TargetToSelf()
    {
        var config = TestDomainDefinition.Create();
        // npc_wield: Artifact.power → Npc.motivation. Owner = Npc (destination).
        var wield = config.Processes.Single(p => p.Id == "npc_wield");

        Assert.Equal(EntityKind.Npc, wield.TargetKind);
        var transfer = Assert.Single(wield.Mutations.OfType<TransferAttributeMutation>());
        Assert.Equal(TransferEndpoint.Target, transfer.Source);
        Assert.Equal("power", transfer.SourceAttribute);
        Assert.Equal(TransferEndpoint.Self, transfer.Destination);
        Assert.Equal("motivation", transfer.DestinationAttribute);
        Assert.Equal(5f, transfer.Amount);
    }

    [Fact]
    public void Generate_ConversionVerb_RequiresOwnerPlaneProximity()
    {
        var config = TestDomainDefinition.Create();

        // Owner on physical plane → physical proximity check.
        var wield = config.Processes.Single(p => p.Id == "npc_wield");
        var wieldPre = Assert.IsType<AndPrecondition>(wield.Precondition);
        Assert.Contains(wieldPre.Children,
            p => p is WithinPlaneDistancePrecondition w && w.Plane == PrimaryPlane.Physical && w.MaxDistance == 1);

        // Owner on semantic plane → semantic proximity check.
        var employ = config.Processes.Single(p => p.Id == "settlement_employ");
        var employPre = Assert.IsType<AndPrecondition>(employ.Precondition);
        Assert.Contains(employPre.Children,
            p => p is WithinPlaneDistancePrecondition w && w.Plane == PrimaryPlane.Semantic && w.MaxDistance == 1);
    }

    [Fact]
    public void Generate_ConversionVerb_SoftGateOfFiveOnOutflowSide()
    {
        var config = TestDomainDefinition.Create();
        // settlement_employ: Npc.motivation → Settlement.dominion. Owner = Settlement (dest).
        // Source stock is on the target (Npc.motivation); precondition checks target has ≥5.
        var employ = config.Processes.Single(p => p.Id == "settlement_employ");
        var pre = Assert.IsType<AndPrecondition>(employ.Precondition);
        Assert.Contains(pre.Children,
            p => p is TargetAttributeThreshold t && t.AttributeName == "motivation" && t.Min == 5f);

        // faction_starve: Npc.satiety → Faction.provisions. Owner = Faction (dest).
        var starve = config.Processes.Single(p => p.Id == "faction_starve");
        var starvePre = Assert.IsType<AndPrecondition>(starve.Precondition);
        Assert.Contains(starvePre.Children,
            p => p is TargetAttributeThreshold t && t.AttributeName == "satiety" && t.Min == 5f);
    }
}
