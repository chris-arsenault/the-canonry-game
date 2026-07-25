namespace Engine.Projection.Tests.Projection;

using Engine.Eventing;
using Engine.Projection;

public sealed class ProjectionSymbolCatalogTests
{
    [Fact]
    public void ResolveDisplayName_ReturnsRegisteredSymbolDisplayName()
    {
        var catalog = new ProjectionSymbolCatalog()
            .RegisterSymbol(
                ProjectionSymbolKind.Generator,
                7,
                "ice_memory",
                "Ice Memory");

        Assert.True(catalog.TryResolve(ProjectionSymbolKind.Generator, 7, out var symbol));
        Assert.Equal("ice_memory", symbol.StableName);
        Assert.Equal("Ice Memory", catalog.ResolveDisplayName(ProjectionSymbolKind.Generator, 7));
    }

    [Fact]
    public void ResolveEntityName_UsesExplicitEntityNameWhenRegistered()
    {
        var catalog = new ProjectionSymbolCatalog()
            .RegisterSymbol(ProjectionSymbolKind.Kind, 2, "settlement")
            .RegisterSymbol(ProjectionSymbolKind.Subtype, 3, "harbor")
            .RegisterEntityName(42, "Port Rime", "Rime");

        var name = catalog.ResolveEntityName(new ProjectionEntityNameKey(
            EntityId: 42,
            KindId: 2,
            SubtypeId: 3,
            CultureId: 4,
            NameSeed: 99));

        Assert.Equal("Port Rime", name);
        Assert.Equal("Rime", catalog.ResolveEntityReferenceName(new ProjectionEntityNameKey(
            EntityId: 42,
            KindId: 2,
            SubtypeId: 3,
            CultureId: 4,
            NameSeed: 99)));
    }

    [Fact]
    public void ResolveEntityName_GeneratesDeterministicNameFromPrimitiveSeed()
    {
        var catalog = new ProjectionSymbolCatalog()
            .RegisterSymbol(ProjectionSymbolKind.Kind, 2, "settlement")
            .RegisterSymbol(ProjectionSymbolKind.Subtype, 3, "harbor");
        var key = new ProjectionEntityNameKey(
            EntityId: 42,
            KindId: 2,
            SubtypeId: 3,
            CultureId: 4,
            NameSeed: 99);

        var first = catalog.ResolveEntityName(key);
        var second = catalog.ResolveEntityName(key);

        Assert.Equal(first, second);
        Assert.StartsWith("Harbor ", first, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolveEntityName_PassesCreationProvenanceToSynthesizer()
    {
        var provenance = CreateProjectedNameProvenance();
        var synthesizer = new CapturingSynthesizer();
        var catalog = CreateProvenanceNameCatalog(provenance, synthesizer);

        var name = catalog.ResolveEntityName(new ProjectionEntityNameKey(
            EntityId: 101,
            KindId: 2,
            SubtypeId: 0,
            CultureId: 4,
            NameSeed: 99));

        Assert.Equal("monster_steals_artifact", name);
        var nameProvenance = Assert.IsType<ProjectionEntityNameProvenance>(synthesizer.Captured.Provenance);
        Assert.Equal(2, nameProvenance.Invocation.InvocationId);
        Assert.Equal(50, nameProvenance.Invocation.ProducerId);
        Assert.Equal("monster_steals_artifact", nameProvenance.Producer?.StableName);
        Assert.Equal("Farwake Reach", nameProvenance.PrimaryEntityName);
        Assert.Equal("Farwake", nameProvenance.PrimaryEntityReferenceName);
        Assert.Equal("Aurora Stack", nameProvenance.SecondaryEntityName);
        Assert.Equal("Aurora", nameProvenance.SecondaryEntityReferenceName);
        Assert.Equal(1, nameProvenance.ParentInvocation?.InvocationId);
        Assert.Equal("artifact_curse_wakes_monster", nameProvenance.ParentProducer?.StableName);
        Assert.Equal(30, nameProvenance.RelationshipTrigger?.RelationshipId);
        Assert.Equal("stole_artifact", nameProvenance.RelationshipTriggerKind?.StableName);
        Assert.Equal("Aurora Stack", nameProvenance.RelationshipTriggerSourceEntityName);
        Assert.Equal("Aurora", nameProvenance.RelationshipTriggerSourceEntityReferenceName);
        Assert.Equal("Farwake Reach", nameProvenance.RelationshipTriggerTargetEntityName);
        Assert.Equal("Farwake", nameProvenance.RelationshipTriggerTargetEntityReferenceName);
        Assert.Single(nameProvenance.Outputs);
        Assert.Single(nameProvenance.ParentChain);
    }

    private static ProvenanceProjection CreateProjectedNameProvenance()
    {
        var log = new KernelEventLog(capacity: 16);
        var provenance = new ProvenanceProjection();
        using var pump = new KernelEventProjectionPump(log, KernelEventCursorStart.EarliestAvailable, provenance);

        WriteCreatedRelationship(log, invocationId: 1, relationshipKindId: 7);
        WriteInvocation(
            log,
            invocationId: 1,
            producerKind: ProvenanceProducerKindCodes.Gambit,
            producerId: 40,
            primary: 10,
            secondary: 20,
            parent: 0,
            triggerKind: ProvenanceTriggerKindCodes.Entity,
            triggerId: 10,
            outputCount: 1);
        WriteEntityCreated(log, invocationId: 2, entityId: 101, kindId: 2, cultureId: 4);
        WriteInvocation(
            log,
            invocationId: 2,
            producerKind: ProvenanceProducerKindCodes.Generator,
            producerId: 50,
            primary: 20,
            secondary: 10,
            parent: 1,
            triggerKind: ProvenanceTriggerKindCodes.Relationship,
            triggerId: 30,
            outputCount: 1);
        pump.DrainAvailable();
        return provenance;
    }

    private static ProjectionSymbolCatalog CreateProvenanceNameCatalog(
        ProvenanceProjection provenance,
        CapturingSynthesizer synthesizer) =>
        new ProjectionSymbolCatalog()
            .RegisterSymbol(ProjectionSymbolKind.Kind, 2, "artifact")
            .RegisterSymbol(ProjectionSymbolKind.Culture, 4, "wake_singers")
            .RegisterSymbol(ProjectionSymbolKind.Gambit, 40, "artifact_curse_wakes_monster")
            .RegisterSymbol(ProjectionSymbolKind.Generator, 50, "monster_steals_artifact")
            .RegisterSymbol(ProjectionSymbolKind.RelationshipKind, 7, "stole_artifact")
            .RegisterEntityName(10, "Aurora Stack", "Aurora")
            .RegisterEntityName(20, "Farwake Reach", "Farwake")
            .UseProvenanceProjection(provenance)
            .UseEntityNameSynthesizer(synthesizer);

    private static void WriteEntityCreated(
        KernelEventLog log,
        int invocationId,
        int entityId,
        int kindId,
        int cultureId)
    {
        Assert.True(log.TryWrite(WithProvenance(
            KernelEvent.From(new EntityCreatedKernelEvent(
                Tick: invocationId, WorldVersion: invocationId + 10, EntityId: entityId,
                SourceCodeId: 1, KindId: kindId, SubtypeId: 0, CultureId: cultureId,
                PhysicalQ: 0, PhysicalR: 0, SemanticQ: 0, SemanticR: 0, SemanticS: 0)),
            invocationId)));
    }

    private static void WriteCreatedRelationship(
        KernelEventLog log,
        int invocationId,
        int relationshipKindId)
    {
        Assert.True(log.TryWrite(WithProvenance(
            KernelEvent.From(new RelationshipChangedKernelEvent(
                Tick: 1, WorldVersion: 3, ChangeCodeId: 9, SourceCodeId: 1,
                RelationshipKindId: relationshipKindId, PolarityCode: RelationshipPolarityCodes.Positive,
                SourceEntityId: 10, TargetEntityId: 20, RelationshipId: 30,
                PreviousStrengthUnits: KernelFixedPoint.RelationshipStrengthUnits(0f),
                NewStrengthUnits: KernelFixedPoint.RelationshipStrengthUnits(0.78f))),
            invocationId)));
    }

    private static void WriteInvocation(
        KernelEventLog log,
        int invocationId,
        int producerKind,
        int producerId,
        int primary,
        int secondary,
        int parent,
        int triggerKind,
        int triggerId,
        int outputCount)
    {
        Assert.True(log.TryWrite(WithProvenance(
            KernelEvent.From(new ProvenanceInvocationRecordedKernelEvent(
                Tick: invocationId, WorldVersion: invocationId + 10, InvocationId: invocationId,
                ProducerKindCode: producerKind, ProducerId: producerId, PrimaryEntityId: primary,
                SecondaryEntityId: secondary, ParentInvocationId: parent, TriggerKindCode: triggerKind,
                TriggerId: triggerId, OutputCount: outputCount)),
            invocationId)));
    }

    private static KernelEvent WithProvenance(KernelEvent kernelEvent, int invocationId) =>
        kernelEvent.WithExecutionContext(programId: 0, sourceCodeId: 1, provenanceInvocationId: invocationId);

    private sealed class CapturingSynthesizer : IProjectionEntityNameSynthesizer
    {
        public ProjectionEntityNameContext Captured { get; private set; }

        public bool TrySynthesizeName(ProjectionEntityNameContext context, out string displayName)
        {
            Captured = context;
            displayName = context.Provenance?.Producer?.StableName ?? string.Empty;
            return true;
        }
    }
}
