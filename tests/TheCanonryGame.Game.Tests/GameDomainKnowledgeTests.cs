namespace TheCanonryGame.Game.Tests;

using Engine.Constants;
using Engine.Domain.Compilation;
using Engine.Eventing;
using Engine.Kernel.Driver;
using Engine.Projection;

using IceRemembers;

using TheCanonryGame.Game.Cards;
using TheCanonryGame.Game.Chronicle;
using TheCanonryGame.Game.Domain;
using TheCanonryGame.Game.Runtime;
using TheCanonryGame.Game.Tableau;

public sealed class GameDomainKnowledgeTests
{
    [Fact]
    public void FromCompiledDomain_ExposesGeneratorCardOutputsAndRelationships()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var generatorId = compiled.Ids.GeneratorIdOf("routes_draw_readers").Value;

        var knowledge = GameDomainKnowledge.FromCompiledDomain(compiled, new ProjectionSymbolCatalog());

        var generator = knowledge.GeneratorsById[generatorId];
        Assert.Equal(GameCapabilityKind.Generator, generator.Kind);
        Assert.Equal("routes_draw_readers", generator.StableName);
        Assert.Equal("Routes Draw Readers", generator.DisplayName);
        Assert.Equal("penguin", generator.PrimaryKindStableName);
        Assert.Collection(
            generator.Produces,
            produced =>
            {
                Assert.Equal("$broker", produced.Ref);
                Assert.Equal("penguin", produced.KindStableName);
                Assert.Equal("broker", produced.SubtypeStableName);
                Assert.Equal("Broker", produced.DisplayLabel);
                Assert.False(produced.IsOptional);
            },
            produced =>
            {
                Assert.Equal("$harbor", produced.Ref);
                Assert.Equal("settlement", produced.KindStableName);
                Assert.Equal("harbor", produced.SubtypeStableName);
                Assert.Equal("Harbor", produced.DisplayLabel);
                Assert.False(produced.IsOptional);
            },
            produced =>
            {
                Assert.Equal("$order", produced.Ref);
                Assert.Equal("faction", produced.KindStableName);
                Assert.Equal("order", produced.SubtypeStableName);
                Assert.Equal("Order", produced.DisplayLabel);
                Assert.True(produced.IsOptional);
            },
            produced =>
            {
                Assert.Equal("$instrument", produced.Ref);
                Assert.Equal("artifact", produced.KindStableName);
                Assert.Equal("instrument", produced.SubtypeStableName);
                Assert.Equal("Instrument", produced.DisplayLabel);
                Assert.True(produced.IsOptional);
            });
        Assert.Collection(
            generator.RelationshipChanges,
            relationship => Assert.Equal("home_base", relationship.RelationshipStableName),
            relationship => Assert.Equal("founded_from", relationship.RelationshipStableName),
            relationship => Assert.Equal("trade_route", relationship.RelationshipStableName),
            relationship => Assert.Equal("controlled_by", relationship.RelationshipStableName),
            relationship => Assert.Equal("member_of", relationship.RelationshipStableName),
            relationship => Assert.Equal("native_to", relationship.RelationshipStableName));
    }

    [Fact]
    public void FromCompiledDomain_ExposesGambitEffectsAndDecisionModifiers()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var gambitId = compiled.Ids.GambitIdOf("artifact_cleanses_scar").Value;

        var knowledge = GameDomainKnowledge.FromCompiledDomain(compiled, new ProjectionSymbolCatalog());

        var gambit = knowledge.GambitsById[gambitId];
        Assert.Equal(GameCapabilityKind.Gambit, gambit.Kind);
        Assert.Equal("artifact_cleanses_scar", gambit.StableName);
        Assert.Equal("Cleanse a Scar", gambit.DisplayName);
        Assert.Equal("artifact", gambit.PrimaryKindStableName);
        Assert.Empty(gambit.Produces);

        var relationship = Assert.Single(gambit.RelationshipChanges);
        Assert.Equal(CardRelationshipChangeKind.Create, relationship.ChangeKind);
        Assert.Equal("$self", relationship.SourceRef);
        Assert.Equal("$target", relationship.TargetRef);
        Assert.Equal("cleansed_by", relationship.RelationshipStableName);
        Assert.Equal(0.78f, relationship.Strength);

        var terrain = Assert.Single(gambit.TerrainTransforms);
        Assert.Equal("hot_springs", terrain.TerrainStableName);
        Assert.Equal("Hot Springs", terrain.TerrainDisplayName);

        Assert.Equal(2, gambit.DecisionModifiers.Count);
        var modifier = Assert.Single(gambit.DecisionModifiers, modifier =>
            modifier.TraitStableName == "artifact_restorative_aura");
        Assert.Equal("artifact_restorative_aura", modifier.TraitStableName);
        Assert.Equal(0.10f, modifier.ScoreDelta);
        Assert.Equal(0.10f, modifier.SuccessChanceDelta);
    }

    [Fact]
    public void FromCompiledDomain_ExposesChronicleQueries()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var knowledge = GameDomainKnowledge.FromCompiledDomain(compiled, new ProjectionSymbolCatalog());

        var query = Assert.Single(knowledge.ChronicleQueries);
        Assert.Equal("artifact_cleansing_awakes_spring", query.Id);
        Assert.Equal(ProvenanceProducerKindCodes.Gambit, query.ParentProducerKindCode);
        Assert.Equal(compiled.Ids.GambitIdOf("artifact_cleanses_scar").Value, query.ParentProducerId);
        Assert.Equal(
            compiled.Ids.RelationshipKindIdOf("cleansed_by").Value,
            query.TriggerRelationshipKindId);
        Assert.Equal(ProvenanceProducerKindCodes.Generator, query.ChildProducerKindCode);
        Assert.Equal(compiled.Ids.GeneratorIdOf("what_the_spring_remembers").Value, query.ChildProducerId);
    }

    [Fact]
    public void RuntimeChronicle_ExecutesCompiledSpringChainAgainstGenericProvenance()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var symbols = CreateSpringChainSymbols(compiled);
        var knowledge = GameDomainKnowledge.FromCompiledDomain(compiled, symbols);
        var provenance = new ProvenanceProjection();
        var baseChronicle = new UiChronicleProjection(symbols);
        var log = new KernelEventLog(capacity: 32);
        using var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            new IKernelEventProjection[] { provenance, baseChronicle });

        WriteSpringChainEvents(log, SpringChainIds.From(compiled));
        pump.DrainAvailable();

        var chronicle = new GameRuntimeChronicle(baseChronicle, provenance, symbols, knowledge);
        var chain = Assert.Single(chronicle.Lines, line => line.Contains("history chain", StringComparison.Ordinal));
        Assert.Contains("Cleanse a Scar", chain, StringComparison.Ordinal);
        Assert.Contains("What the Spring Remembers", chain, StringComparison.Ordinal);
        Assert.Contains("Cleansed By", chain, StringComparison.Ordinal);
    }

    [Fact]
    public void TableauCardsReceiveCompiledDomainKnowledge()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var generatorId = compiled.Ids.GeneratorIdOf("routes_draw_readers").Value;
        var gambitId = compiled.Ids.GambitIdOf("artifact_cleanses_scar").Value;
        var symbols = new ProjectionSymbolCatalog()
            .RegisterSymbol(ProjectionSymbolKind.Generator, generatorId, "routes_draw_readers", "Routes Draw Readers")
            .RegisterSymbol(ProjectionSymbolKind.Gambit, gambitId, "artifact_cleanses_scar", "Cleanse a Scar");
        var knowledge = GameDomainKnowledge.FromCompiledDomain(compiled, symbols);
        var projection = new UiProjectionState();

        var tableau = new GameRuntimeTableau(
            projection,
            symbols,
            new GameIntentPort(new NoopCommandSubmitter(), () => projection.WorldVersion),
            knowledge);

        var generatorCard = tableau.Cards.Single(card => card.Definition.GeneratorId == generatorId);
        Assert.Equal("penguin", generatorCard.Definition.Tags.Single(tag => tag == "penguin"));
        Assert.Equal(4, generatorCard.Definition.Produces.Count);
        Assert.Equal(6, generatorCard.Definition.RelationshipChanges.Count);
        Assert.Contains(generatorCard.Definition.Produces, produced =>
            produced.SubtypeStableName == "instrument" &&
            produced.IsOptional);

        var gambitCard = tableau.Cards.Single(card => card.Definition.GambitId == gambitId);
        Assert.Contains("artifact", gambitCard.Definition.Tags);
        Assert.Empty(gambitCard.Definition.Produces);
        Assert.Contains(gambitCard.Definition.RelationshipChanges, relationship =>
            relationship.RelationshipStableName == "cleansed_by");
        Assert.Contains(gambitCard.Definition.TerrainTransforms, transform =>
            transform.TerrainStableName == "hot_springs");
        Assert.Contains(gambitCard.Definition.DecisionModifiers, modifier =>
            modifier.TraitStableName == "artifact_restorative_aura");
    }

    private sealed class NoopCommandSubmitter : ICommandSubmitter
    {
        public bool TrySetGeneratorEnabled(in SetGeneratorEnabledCommand command, out CommandHandle handle)
        {
            _ = command;
            handle = default;
            return true;
        }
    }

    private static ProjectionSymbolCatalog CreateSpringChainSymbols(CompiledDomain compiled) =>
        new ProjectionSymbolCatalog()
            .RegisterSymbol(
                ProjectionSymbolKind.RelationshipKind,
                compiled.Ids.RelationshipKindIdOf("cleansed_by").Value,
                "cleansed_by",
                "Cleansed By")
            .RegisterSymbol(
                ProjectionSymbolKind.Gambit,
                compiled.Ids.GambitIdOf("artifact_cleanses_scar").Value,
                "artifact_cleanses_scar",
                "Cleanse a Scar")
            .RegisterSymbol(
                ProjectionSymbolKind.Generator,
                compiled.Ids.GeneratorIdOf("what_the_spring_remembers").Value,
                "what_the_spring_remembers",
                "What the Spring Remembers");

    private static void WriteSpringChainEvents(KernelEventLog log, SpringChainIds ids)
    {
        WriteEntity(log, entityId: 100, kindId: ids.ArtifactKindId, tick: 0, version: 1);
        WriteEntity(log, entityId: 200, kindId: ids.RegionKindId, tick: 0, version: 2);
        WriteCreatedRelationship(log, ids);
        WriteGambitExecuted(log, ids);
        WriteInvocation(
            log,
            invocationId: 1,
            producerKind: ProvenanceProducerKindCodes.Gambit,
            producerId: ids.GambitId,
            primary: 100,
            secondary: 200,
            parent: 0,
            triggerKind: ProvenanceTriggerKindCodes.Entity,
            triggerId: 200,
            outputCount: 1);
        WriteEntity(log, entityId: 400, kindId: ids.PenguinKindId, tick: 2, version: 6, invocationId: 2);
        WriteGeneratorFired(log, ids);
        WriteInvocation(
            log,
            invocationId: 2,
            producerKind: ProvenanceProducerKindCodes.Generator,
            producerId: ids.GeneratorId,
            primary: 200,
            secondary: 0,
            parent: 1,
            triggerKind: ProvenanceTriggerKindCodes.Relationship,
            triggerId: ids.RelationshipId,
            outputCount: 1);
    }

    private static void WriteEntity(
        KernelEventLog log,
        int entityId,
        int kindId,
        int tick,
        long version,
        int invocationId = 0)
    {
        var evt = KernelEvent.From(new EntityCreatedKernelEvent(
            Tick: tick,
            WorldVersion: version,
            EntityId: entityId,
            SourceCodeId: 1,
            KindId: kindId,
            SubtypeId: 0,
            CultureId: 0,
            PhysicalQ: 0,
            PhysicalR: 0,
            SemanticQ: 0,
            SemanticR: 0,
            SemanticS: 0));
        Assert.True(log.TryWrite(invocationId == 0 ? evt : WithProvenance(evt, invocationId)));
    }

    private static void WriteCreatedRelationship(KernelEventLog log, SpringChainIds ids)
    {
        Assert.True(log.TryWrite(WithProvenance(
            KernelEvent.From(new RelationshipChangedKernelEvent(
                Tick: 1,
                WorldVersion: 3,
                ChangeCodeId: 9,
                SourceCodeId: 1,
                RelationshipKindId: ids.CleansedById,
                PolarityCode: RelationshipPolarityCodes.Positive,
                SourceEntityId: 100,
                TargetEntityId: 200,
                RelationshipId: ids.RelationshipId,
                PreviousStrength: 0f,
                NewStrength: 0.78f)),
            invocationId: 1)));
    }

    private static void WriteGambitExecuted(KernelEventLog log, SpringChainIds ids)
    {
        Assert.True(log.TryWrite(WithProvenance(
            KernelEvent.From(new GambitExecutedKernelEvent(
                Tick: 1,
                WorldVersion: 4,
                ActorId: 100,
                ActorKindId: ids.ArtifactKindId,
                GambitId: ids.GambitId,
                AffectedEntityId0: 100,
                AffectedEntityId1: 200,
                AffectedEntityCount: 2,
                State: KernelEventState.Succeeded)),
            invocationId: 1)));
    }

    private static void WriteGeneratorFired(KernelEventLog log, SpringChainIds ids)
    {
        Assert.True(log.TryWrite(WithProvenance(
            KernelEvent.From(new GeneratorFiredKernelEvent(
                Tick: 2,
                WorldVersion: 7,
                GeneratorId: ids.GeneratorId,
                TargetEntityId: 200,
                CreatedEntityCount: 1,
                CreatedRelationshipCount: 0)),
            invocationId: 2)));
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
                Tick: invocationId,
                WorldVersion: invocationId + 10,
                InvocationId: invocationId,
                ProducerKindCode: producerKind,
                ProducerId: producerId,
                PrimaryEntityId: primary,
                SecondaryEntityId: secondary,
                ParentInvocationId: parent,
                TriggerKindCode: triggerKind,
                TriggerId: triggerId,
                OutputCount: outputCount)),
            invocationId)));
    }

    private static KernelEvent WithProvenance(KernelEvent kernelEvent, int invocationId) =>
        kernelEvent.WithExecutionContext(programId: 0, sourceCodeId: 1, provenanceInvocationId: invocationId);

    private readonly record struct SpringChainIds(
        int ArtifactKindId,
        int RegionKindId,
        int PenguinKindId,
        int GambitId,
        int GeneratorId,
        int CleansedById,
        int RelationshipId)
    {
        public static SpringChainIds From(CompiledDomain compiled) =>
            new(
                compiled.Ids.KindIdOf("artifact").Value,
                compiled.Ids.KindIdOf("region").Value,
                compiled.Ids.KindIdOf("penguin").Value,
                compiled.Ids.GambitIdOf("artifact_cleanses_scar").Value,
                compiled.Ids.GeneratorIdOf("what_the_spring_remembers").Value,
                compiled.Ids.RelationshipKindIdOf("cleansed_by").Value,
                300);
    }
}
