namespace Engine.Tests.Processes;

using Engine.Attributes;
using Engine.Graph;
using Engine.Kinds;
using Engine.Processes;
using Engine.Tests;
using Engine.Tests.Behaviors;

public class PreconditionTests
{
    [Fact]
    public void PreconditionResult_PassContainsDiagnosticAndNoBlockers()
    {
        var result = PreconditionResult.Pass("ok");

        Assert.True(result.Passed);
        Assert.Equal("ok", result.Diagnostic);
        Assert.Empty(result.BlockingAttributes);
    }

    [Fact]
    public void PreconditionResult_FailContainsDiagnosticAndBlockers()
    {
        var result = PreconditionResult.Fail("blocked", "sustenance", "fame");

        Assert.False(result.Passed);
        Assert.Equal("blocked", result.Diagnostic);
        Assert.Equal(["sustenance", "fame"], result.BlockingAttributes);
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
        Assert.Equal(["sustenance"], result.BlockingAttributes);
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
        Assert.Equal(["sustenance"], result.BlockingAttributes);
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
        Assert.Equal(["sustenance"], result.BlockingAttributes);
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
        Assert.Equal(["ideology"], result.BlockingAttributes);
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
        Assert.Equal(["sustenance"], result.BlockingAttributes);
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
        Assert.Equal(["ideology", "sustenance"], result.BlockingAttributes.OrderBy(x => x).ToArray());
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
        var entity = world.Graph.CreateEntity(EntityKind.Settlement, "village", name, "c", 0, coordinates);
        entity.Attributes.Add(new ScalarValue("sustenance", 0f, 100f, sustenance));
        entity.Attributes.Add(new ScalarValue("fame", 0f, 100f, fame));
        var (x, y) = ideology ?? (0.5f, 0.5f);
        entity.Attributes.Add(new SemanticPlane("ideology", TestHelpers.CreateIdeologyPlane(), 0f, 1f, x, y));
        return entity;
    }
}
