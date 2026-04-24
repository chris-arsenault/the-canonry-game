namespace Engine.Tests.Processes;

using Engine.Attributes;
using Engine.Graph;
using Engine.Kinds;
using Engine.Processes;
using Engine.Tests;
using Engine.Tests.Behaviors;
using Engine.Wiring;

public class PreconditionTests
{
    [Fact]
    public void PreconditionResult_PassContainsDiagnosticAndNoBlockers()
    {
        var result = PreconditionResult.Pass("ok");

        Assert.True(result.Passed);
        Assert.Equal("ok", result.Diagnostic);
        Assert.Empty(result.Blockers);
    }

    [Fact]
    public void PreconditionResult_FailContainsDiagnosticAndBlockers()
    {
        var result = PreconditionResult.Fail("blocked",
            AttributeRef.Self("sustenance", TestKinds.Settlement),
            AttributeRef.Self("fame", TestKinds.Settlement));

        Assert.False(result.Passed);
        Assert.Equal("blocked", result.Diagnostic);
        Assert.Equal(
            [AttributeRef.Self("sustenance", TestKinds.Settlement), AttributeRef.Self("fame", TestKinds.Settlement)],
            result.Blockers);
    }

    [Fact]
    public void TargetIsSelfAndTargetIsNotSelf_RespectResolvedTarget()
    {
        var world = CreateWorld();
        var self = CreateSettlement(world, "Self", (0, 0));
        var other = CreateSettlement(world, "Other", (1, 0));

        var selfContext = new TestMutationContext(self, self, worldState: world);
        Assert.True(new TargetIsSelf().Evaluate(selfContext).Passed);
        Assert.False(new TargetIsNotSelf().Evaluate(selfContext).Passed);

        var otherContext = new TestMutationContext(self, other, worldState: world);
        Assert.False(new TargetIsSelf().Evaluate(otherContext).Passed);
        Assert.True(new TargetIsNotSelf().Evaluate(otherContext).Passed);
    }

    [Fact]
    public void TargetIsSelfAndTargetIsNotSelf_BothFailWithoutATarget()
    {
        var world = CreateWorld();
        var self = CreateSettlement(world, "Self", (0, 0));
        var context = new TestMutationContext(self, worldState: world);

        Assert.False(new TargetIsSelf().Evaluate(context).Passed);
        Assert.False(new TargetIsNotSelf().Evaluate(context).Passed);
    }

    [Fact]
    public void SelfAttributeThreshold_ReadsSelfEntityAndReportsBlocker()
    {
        var world = CreateWorld();
        var self = CreateSettlement(world, "Self", (0, 0), sustenance: 2f);
        var other = CreateSettlement(world, "Other", (1, 0), sustenance: 10f);
        var context = new TestMutationContext(self, other, worldState: world);

        var result = new SelfAttributeThreshold("sustenance", min: 3f, max: null).Evaluate(context);

        Assert.False(result.Passed);
        Assert.Contains("self.sustenance=2.0", result.Diagnostic, StringComparison.Ordinal);
        Assert.Contains(">= 3", result.Diagnostic, StringComparison.Ordinal);
        Assert.Equal([AttributeRef.Self("sustenance", TestKinds.Settlement)], result.Blockers);
    }

    [Fact]
    public void TargetAttributeThreshold_ReadsResolvedTarget()
    {
        var world = CreateWorld();
        var self = CreateSettlement(world, "Self", (0, 0), sustenance: 10f);
        var target = CreateSettlement(world, "Target", (1, 0), sustenance: 2f);
        var context = new TestMutationContext(self, target, worldState: world);

        var result = new TargetAttributeThreshold("sustenance", min: 3f, max: null).Evaluate(context);

        Assert.False(result.Passed);
        Assert.Contains("target.sustenance=2.0", result.Diagnostic, StringComparison.Ordinal);
        Assert.Equal([AttributeRef.Target("sustenance", TestKinds.Settlement)], result.Blockers);
    }

    [Fact]
    public void TargetAttributeThreshold_FailsWithNoTargetDiagnostic()
    {
        var world = CreateWorld();
        var self = CreateSettlement(world, "Self", (0, 0));
        var context = new TestMutationContext(self, worldState: world);

        var result = new TargetAttributeThreshold("sustenance", min: 3f, max: null).Evaluate(context);

        Assert.False(result.Passed);
        Assert.Equal("no target", result.Diagnostic);
        Assert.Equal([AttributeRef.Target("sustenance")], result.Blockers);
    }

    [Fact]
    public void SemanticDistanceThreshold_PassesWithinThreshold()
    {
        var world = CreateWorld();
        var self = CreateSettlement(world, "Self", (0, 0), ideology: (0.1f, 0.1f));
        var target = CreateSettlement(world, "Target", (1, 0), ideology: (0.4f, 0.5f));
        var context = new TestMutationContext(self, target, worldState: world);

        var result = new SemanticDistanceThreshold("ideology", 0.5f).Evaluate(context);

        Assert.True(result.Passed);
    }

    [Fact]
    public void SemanticDistanceThreshold_FailsBeyondThreshold()
    {
        var world = CreateWorld();
        var self = CreateSettlement(world, "Self", (0, 0), ideology: (0.0f, 0.0f));
        var target = CreateSettlement(world, "Target", (1, 0), ideology: (1.0f, 1.0f));
        var context = new TestMutationContext(self, target, worldState: world);

        var result = new SemanticDistanceThreshold("ideology", 0.5f).Evaluate(context);

        Assert.False(result.Passed);
        Assert.Equal([AttributeRef.Target("ideology", TestKinds.Settlement)], result.Blockers);
    }

    [Fact]
    public void SemanticDistanceThreshold_FailsForNonPlaneAttribute()
    {
        var world = CreateWorld();
        var self = CreateSettlement(world, "Self", (0, 0));
        var target = CreateSettlement(world, "Target", (1, 0));
        var context = new TestMutationContext(self, target, worldState: world);

        var result = new SemanticDistanceThreshold("sustenance", 0.5f).Evaluate(context);

        Assert.False(result.Passed);
        Assert.Contains("not a semantic plane", result.Diagnostic, StringComparison.Ordinal);
        Assert.Equal([AttributeRef.Target("sustenance", TestKinds.Settlement)], result.Blockers);
    }

    [Fact]
    public void AndPrecondition_CollectsAllFailuresAndBlockers()
    {
        var world = CreateWorld();
        var self = CreateSettlement(world, "Self", (0, 0), sustenance: 0f, fame: 1f, ideology: (0f, 0f));
        var target = CreateSettlement(world, "Target", (1, 0), sustenance: 0f, fame: 1f, ideology: (1f, 1f));
        var context = new TestMutationContext(self, target, worldState: world);

        var precondition = new AndPrecondition(
        [
            new SelfAttributeThreshold("sustenance", min: 3f, max: null),
            new SemanticDistanceThreshold("ideology", 0.1f),
        ]);

        var result = precondition.Evaluate(context);

        Assert.False(result.Passed);
        Assert.Equal(
            [AttributeRef.Self("sustenance", TestKinds.Settlement), AttributeRef.Target("ideology", TestKinds.Settlement)],
            result.Blockers.OrderBy(x => x.Scope).ThenBy(x => x.Name).ToArray());
        Assert.Contains("; ", result.Diagnostic, StringComparison.Ordinal);
        Assert.Contains("self.sustenance", result.Diagnostic, StringComparison.Ordinal);
        Assert.Contains("ideology distance", result.Diagnostic, StringComparison.Ordinal);
    }

    [Fact]
    public void AndPrecondition_PassesWhenAllChildrenPass()
    {
        var world = CreateWorld();
        var self = CreateSettlement(world, "Self", (0, 0), sustenance: 5f, ideology: (0.2f, 0.2f));
        var target = CreateSettlement(world, "Target", (1, 0), ideology: (0.3f, 0.3f));
        var context = new TestMutationContext(self, target, worldState: world);

        var result = new AndPrecondition(
        [
            new AlwaysTrue(),
            new SelfAttributeThreshold("sustenance", min: 3f, max: null),
            new SemanticDistanceThreshold("ideology", 0.2f),
        ]).Evaluate(context);

        Assert.True(result.Passed);
        Assert.Equal("all conditions met", result.Diagnostic);
    }

    [Fact]
    public void RelatedEntityMatchesTarget_EmitsReusableRelationalBlocker()
    {
        var world = CreateWorld();
        var self = CreateSettlement(world, "Self", (0, 0));
        var target = CreateSettlement(world, "Target", (1, 0));
        var other = CreateSettlement(world, "Other", (2, 0));
        world.Graph.CreateRelationship("located_at", RelationshipPolarity.Neutral, self.Id, other.Id, 1f, 0);

        var result = new RelatedEntityMatchesTarget("located_at").Evaluate(
            new TestMutationContext(self, target, worldState: world));

        Assert.False(result.Passed);
        Assert.Contains("located_at", result.Diagnostic, StringComparison.Ordinal);
        Assert.Equal(
            [AttributeRef.TargetRelation("located_at", TestKinds.Settlement)],
            result.Blockers);
    }

    [Fact]
    public void RelatedEntityMatchesTarget_PassesWhenRelationPointsToResolvedTarget()
    {
        var world = CreateWorld();
        var self = CreateSettlement(world, "Self", (0, 0));
        var target = CreateSettlement(world, "Target", (1, 0));
        world.Graph.CreateRelationship("located_at", RelationshipPolarity.Neutral, self.Id, target.Id, 1f, 0);

        var result = new RelatedEntityMatchesTarget("located_at").Evaluate(
            new TestMutationContext(self, target, worldState: world));

        Assert.True(result.Passed);
        Assert.Equal("located_at matches target", result.Diagnostic);
    }

    [Fact]
    public void TargetHasRelationshipToSelf_PassesWhenTargetPointsBackToSelf()
    {
        var world = CreateWorld();
        var faction = world.Graph.CreateEntity(TestKinds.Faction, "order", "Faction", "c", 0, null);
        var settlement = CreateSettlement(world, "Target", (1, 0));
        world.Graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, settlement.Id, faction.Id, 1f, 0);

        var result = new TargetHasRelationshipToSelf("controlled_by").Evaluate(
            new TestMutationContext(faction, settlement, worldState: world));

        Assert.True(result.Passed);
        Assert.Contains("controlled_by", result.Diagnostic, StringComparison.Ordinal);
    }

    [Fact]
    public void SelfHasRelationshipToTarget_FailsWhenRelationshipMissing()
    {
        var world = CreateWorld();
        var npc = world.Graph.CreateEntity(TestKinds.Penguin, "warrior", "Npc", "c", 0, (0, 0));
        var faction = world.Graph.CreateEntity(TestKinds.Faction, "order", "Faction", "c", 0, null);

        var result = new SelfHasRelationshipToTarget("loyal_to").Evaluate(
            new TestMutationContext(npc, faction, worldState: world));

        Assert.False(result.Passed);
        Assert.Contains("self has no loyal_to relationship to target", result.Diagnostic, StringComparison.Ordinal);
    }

    [Fact]
    public void AlwaysTrue_AlwaysPasses()
    {
        var world = CreateWorld();
        var self = CreateSettlement(world, "Self", (0, 0));

        var result = new AlwaysTrue().Evaluate(new TestMutationContext(self, worldState: world));

        Assert.True(result.Passed);
        Assert.Equal("always", result.Diagnostic);
    }

    private static StubWorldState CreateWorld() => new(gridRadius: 5);

    private static Entity CreateSettlement(
        StubWorldState world,
        string name,
        (int Q, int R) coordinates,
        float sustenance = 0f,
        float fame = 0f,
        (float X, float Y)? ideology = null)
    {
        var entity = world.Graph.CreateEntity(TestKinds.Settlement, "village", name, "c", 0, coordinates);
        entity.Attributes.Add(new ScalarValue("sustenance", 0f, 100f, sustenance));
        entity.Attributes.Add(new ScalarValue("fame", 0f, 100f, fame));
        var (x, y) = ideology ?? (0.5f, 0.5f);
        entity.Attributes.Add(new SemanticPlane("ideology", TestHelpers.CreateIdeologyPlane(), 0f, 1f, x, y));
        return entity;
    }
}
