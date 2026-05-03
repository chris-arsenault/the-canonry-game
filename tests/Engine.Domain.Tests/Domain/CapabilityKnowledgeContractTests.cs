namespace Engine.Domain.Tests.Domain;

using Engine.Constants;
using Engine.Domain.Compilation;
using Engine.Domain.Compilation.KernelPrograms;
using Engine.Domain.Graph;
using Engine.Wiring;
using IceRemembers;

public sealed class CapabilityKnowledgeContractTests
{
    [Fact]
    public void CompiledDomain_ExposesGeneratorCapabilityStructureOutsideSymbols()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var generatorId = compiled.Ids.GeneratorIdOf("routes_draw_readers").Value;

        var graphNode = compiled.DefinitionGraph.Generators["routes_draw_readers"];
        var compiledRule = Assert.Single(
            compiled.Programs.Generators.Generators,
            generator => generator.GeneratorId == generatorId);

        Assert.Equal(4, graphNode.Produces.Count);
        Assert.Equal(6, graphNode.Relationships.Count);
        Assert.Contains(
            graphNode.Target.RequiredOutgoingRelationshipRequirements,
            requirement => requirement.RelationshipId.Value == IceRemembersRelationship.TradeRoute.ToDomainName());

        Assert.Equal(graphNode.Produces.Count, compiledRule.ProducedEntities.Count);
        Assert.Equal(graphNode.Relationships.Count, compiledRule.Relationships.Count);
        Assert.Contains(graphNode.Produces, produced =>
            produced.Ref == "$instrument" &&
            produced.CreateChance == 0.45f);
        Assert.Contains(compiledRule.ProducedEntities, produced =>
            produced.CreateChance == 0.45f &&
            produced.PhysicalPlacement == GeneratorProgramPlacement.On);
    }

    [Fact]
    public void CompiledDomain_ExposesGambitQueriesAndEffectsAsStructuredPrimitives()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var cleanseId = compiled.Ids.GambitIdOf(IceRemembersGambitId.ArtifactCleansesScar.ToGambitId()).Value;
        var anchoredId = compiled.Ids.RelationshipKindIdOf(IceRemembersRelationship.Anchored.ToDomainName()).Value;
        var cleansedById = compiled.Ids.RelationshipKindIdOf(IceRemembersRelationship.CleansedBy.ToDomainName()).Value;
        var conditionId = compiled.Ids.AttributeIdOf(IceRemembersAttributeId.Condition.ToDomainName()).Value;
        var hotSpringsId = compiled.Ids.TerrainIdOf(IceRemembersTerrain.HotSprings.ToDomainName()).Value;

        var graphNode = compiled.DefinitionGraph.Gambits[IceRemembersGambitId.ArtifactCleansesScar.ToGambitId()];
        var compiledRule = Assert.Single(
            compiled.Programs.Gambits.Gambits,
            gambit => gambit.GambitId == cleanseId);

        Assert.Contains(graphNode.Reads, read =>
            read.Scope == RefScope.Self &&
            read.ValueId is AttributeValueId attribute &&
            attribute.AttributeId.Value == IceRemembersAttributeId.Condition.ToDomainName());
        Assert.Contains(graphNode.Writes, write =>
            write.Scope == RefScope.Relational &&
            write.RelationshipId == new DefinitionRelationshipId(IceRemembersRelationship.CleansedBy.ToDomainName()));
        Assert.Contains(graphNode.ProviderSelectors, selector =>
            selector.Scope == RefScope.Structural &&
            selector.Kind == IceRemembersKinds.Region);

        Assert.Contains(compiledRule.Preconditions, precondition =>
            precondition.Code == BehaviorProgramPrecondition.SelfHasRelationshipToTarget &&
            precondition.A == anchoredId &&
            precondition.B == 1 &&
            precondition.X == 0.45f);
        Assert.Contains(compiledRule.Preconditions, precondition =>
            precondition.Code == BehaviorProgramPrecondition.SelfAttributeRange &&
            precondition.A == conditionId);
        Assert.Contains(compiledRule.Steps, step =>
            step.Operation == BehaviorProgramOperation.CreateEndpointRelationship &&
            step.A == cleansedById);
        Assert.Contains(compiledRule.Steps, step =>
            step.Operation == BehaviorProgramOperation.SetTargetCoverageTerrain &&
            step.A == hotSpringsId);
    }

    [Fact]
    public void CompiledDomain_ExposesTraitGambitModifiersAsDecisionPrimitives()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var cleanseId = compiled.Ids.GambitIdOf(IceRemembersGambitId.ArtifactCleansesScar.ToGambitId()).Value;
        var warId = compiled.Ids.GambitIdOf(IceRemembersGambitId.FactionDeclaresTerritorialWar.ToGambitId()).Value;
        var restorativeAuraId = compiled.Ids.TraitIdOf(IceRemembersTraitId.ArtifactRestorativeAura.ToDomainName()).Value;
        var fractiousCouncilId = compiled.Ids.TraitIdOf(IceRemembersTraitId.FactionFractiousCouncil.ToDomainName()).Value;

        Assert.Contains(compiled.Programs.Gambits.TraitModifiers, modifier =>
            modifier.GambitId == cleanseId &&
            modifier.TraitId == restorativeAuraId &&
            modifier.BearerCode == TraitEffectBearerCodes.Actor &&
            modifier.ScoreDelta == 0.10f &&
            modifier.SuccessChanceDelta == 0.10f);

        Assert.Contains(compiled.Programs.Gambits.TraitModifiers, modifier =>
            modifier.GambitId == warId &&
            modifier.TraitId == fractiousCouncilId &&
            modifier.BearerCode == TraitEffectBearerCodes.Actor &&
            modifier.ScoreDelta == 0.18f &&
            modifier.SuccessChanceDelta == -0.05f);
    }
}
