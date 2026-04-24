namespace Engine.Tests.Processes;

using Engine.Attributes;
using Engine.Graph;
using Engine.Kinds;
using Engine.Processes;
using Engine.Resources;
using Engine.Tests;
using Engine.Tests.Behaviors;

public class ExpressionTests
{
    [Fact]
    public void AttributePaths_ResolveSelfTargetAndPlaneComponents()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateEntity(world, "Self", (0, 0), fame: 7f, ideology: (0.2f, 0.8f));
        var target = CreateEntity(world, "Target", (2, 0), fame: 11f, ideology: (0.7f, 0.4f));
        var context = new ExpressionContext(self, target, world.Grid);

        Assert.Equal(7f, Expr.Self("fame").Evaluate(context));
        Assert.Equal(11f, Expr.Target("fame").Evaluate(context));
        Assert.Equal(0.2f, Expr.SelfX("ideology").Evaluate(context), precision: 3);
        Assert.Equal(0.8f, Expr.SelfY("ideology").Evaluate(context), precision: 3);
        Assert.Equal(0f, Expr.Self("missing").Evaluate(context));
        Assert.Equal(0f, Expr.Target("missing").Evaluate(context));
    }

    [Fact]
    public void TargetPaths_ReturnZeroWhenTargetIsNull()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateEntity(world, "Self", (0, 0), fame: 7f, ideology: (0.2f, 0.8f));
        var context = new ExpressionContext(self, target: null, world.Grid);

        Assert.Equal(0f, Expr.Target("fame").Evaluate(context));
        Assert.Equal(0f, Expr.TargetX("ideology").Evaluate(context));
        Assert.Equal(0f, Expr.TargetY("ideology").Evaluate(context));
    }

    [Fact]
    public void ArithmeticExpressions_HandleKnownValuesAndDivisionByZero()
    {
        var context = CreateContext();

        Assert.Equal(5f, Expr.Add(Expr.Lit(2f), Expr.Lit(3f)).Evaluate(context));
        Assert.Equal(-1f, Expr.Sub(Expr.Lit(2f), Expr.Lit(3f)).Evaluate(context));
        Assert.Equal(6f, Expr.Mul(Expr.Lit(2f), Expr.Lit(3f)).Evaluate(context));
        Assert.Equal(2f, Expr.Div(Expr.Lit(6f), Expr.Lit(3f)).Evaluate(context));
        Assert.Equal(0f, Expr.Div(Expr.Lit(6f), Expr.Lit(0f)).Evaluate(context));
    }

    [Fact]
    public void BuiltInFunctions_ResolveDistanceSemanticDistanceAndMathHelpers()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateEntity(world, "Self", (0, 0), fame: 7f, ideology: (0f, 0f));
        var target = CreateEntity(world, "Target", (2, 0), fame: 11f, ideology: (0.3f, 0.4f));
        var context = new ExpressionContext(self, target, world.Grid);

        Assert.Equal(2f, Expr.Distance().Evaluate(context));
        Assert.Equal(0.5f, Expr.SemanticDistance("ideology").Evaluate(context), precision: 3);
        Assert.Equal(float.MaxValue, Expr.SemanticDistance("fame").Evaluate(context));
        Assert.Equal(2f, Expr.Min(Expr.Lit(2f), Expr.Lit(7f)).Evaluate(context));
        Assert.Equal(7f, Expr.Max(Expr.Lit(2f), Expr.Lit(7f)).Evaluate(context));
        Assert.Equal(5f, Expr.Clamp(Expr.Lit(9f), Expr.Lit(1f), Expr.Lit(5f)).Evaluate(context));
        Assert.Equal(4f, Expr.Abs(Expr.Lit(-4f)).Evaluate(context));
    }

    [Fact]
    public void Jitter_ProducesVarianceAcrossEvaluations()
    {
        var context = CreateContext();
        var values = new HashSet<float>();

        for (var i = 0; i < 20; i++)
        {
            values.Add(Expr.Jitter(Expr.Lit(10f), 2f).Evaluate(context));
        }

        Assert.True(values.Count > 1);
    }

    [Fact]
    public void VariableRef_ReadsMutationContextVariable()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateEntity(world, "Self", (0, 0), fame: 7f, ideology: (0f, 0f));
        var context = new TestMutationContext(self, worldState: world);
        context.SetVariable("delta", 12f);

        var exprContext = new ExpressionContext(self, mutationContext: context);

        Assert.Equal(12f, new VariableRef("delta").Evaluate(exprContext));
    }

    [Fact]
    public void VariableRef_ReturnsZeroWithoutMutationContext()
    {
        var context = CreateContext();

        Assert.Equal(0f, new VariableRef("delta").Evaluate(context));
    }

    [Fact]
    public void LocationResourcePaths_ReadAmbientResourceValuesFromMutationContext()
    {
        var world = new StubWorldState(gridRadius: 5, withResources: true);
        var self = CreateEntity(world, "Self", (0, 0), fame: 7f, ideology: (0f, 0f));
        var target = CreateEntity(world, "Target", (2, 0), fame: 11f, ideology: (0.3f, 0.4f));
        var selfCoord = new Engine.Map.HexCoord(0, 0);
        var targetCoord = new Engine.Map.HexCoord(2, 0);
        world.Resources!.Food.SetStockpile(selfCoord, 6f);
        world.Resources.Food.SetStockpile(targetCoord, 9f);

        var mutationContext = new TestMutationContext(self, target, worldState: world);
        var context = new ExpressionContext(self, target, world.Grid, mutationContext);

        Assert.Equal(6f, Expr.SelfLocationResource(ResourceType.Food).Evaluate(context));
        Assert.Equal(9f, Expr.TargetLocationResource(ResourceType.Food).Evaluate(context));
    }

    private static ExpressionContext CreateContext()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateEntity(world, "Self", (0, 0), fame: 7f, ideology: (0f, 0f));
        var target = CreateEntity(world, "Target", (2, 0), fame: 11f, ideology: (0.3f, 0.4f));
        return new ExpressionContext(self, target, world.Grid);
    }

    private static Entity CreateEntity(
        StubWorldState world,
        string name,
        (int Q, int R) coordinates,
        float fame,
        (float X, float Y) ideology)
    {
        var entity = world.Graph.CreateEntity(TestKinds.Settlement, "village", name, "c", 0, coordinates);
        entity.Attributes.Add(new ScalarValue("fame", 0f, 100f, fame));
        entity.Attributes.Add(new SemanticPlane("ideology", TestHelpers.CreateIdeologyPlane(), 0f, 1f, ideology.X, ideology.Y));
        return entity;
    }
}
