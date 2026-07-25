namespace Engine.Kernel.Tests;

using Engine.Kernel;

public sealed class DecisionModifierTests
{
    [Fact]
    public void DecisionModifierEvaluationSumsMatchingGambitTraitBearerValues()
    {
        const int kindId = 1;
        const int traitId = 7;
        const int gambitId = 50;
        var simulation = new Simulation(new SimulationOptions { EntityCapacity = 4 });
        var actor = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);
        var target = simulation.CreateEntity(kindId, 1, 1, 1, 0, 0, 0, 0, tick: 0);
        simulation.World.AddTrait(target, traitId, tick: 0);
        var modifiers = new[]
        {
            default,
            new DecisionModifier(gambitId, 0, traitId, TraitEffectBearerCodes.GambitTarget, value0: FixedScalar.FromSingle(2f), value1: FixedScalar.FromSingle(0.25f)),
            new DecisionModifier(gambitId + 1, 0, traitId, TraitEffectBearerCodes.GambitTarget, value0: FixedScalar.FromSingle(10f), value1: FixedScalar.FromSingle(1f)),
            new DecisionModifier(gambitId, 0, traitId + 1, TraitEffectBearerCodes.GambitTarget, value0: FixedScalar.FromSingle(10f), value1: FixedScalar.FromSingle(1f)),
        };

        var score = DecisionModifierEvaluation.SumGambitValue(
            simulation.World,
            modifierCount: 3,
            modifier => modifiers[modifier],
            gambitId,
            actor,
            target,
            useValue1: false);
        var success = DecisionModifierEvaluation.SumGambitValue(
            simulation.World,
            modifierCount: 3,
            modifier => modifiers[modifier],
            gambitId,
            actor,
            target,
            useValue1: true);

        Assert.Equal(FixedScalar.FromSingle(2f), score);
        Assert.Equal(FixedScalar.FromSingle(0.25f), success);
    }

    [Fact]
    public void DecisionModifierEvaluationSumsMatchingLowAltitudeVerbValues()
    {
        const int kindId = 1;
        const int traitId = 7;
        const int verbId = 20;
        var simulation = new Simulation(new SimulationOptions { EntityCapacity = 4 });
        var actor = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.World.AddTrait(actor, traitId, tick: 0);
        var modifiers = new[]
        {
            default,
            new DecisionModifier(verbId, kindId, traitId, TraitEffectBearerCodes.Actor, FixedScalar.FromSingle(1f), FixedScalar.FromSingle(2f), FixedScalar.FromSingle(3f)),
            new DecisionModifier(0, kindId + 1, traitId, TraitEffectBearerCodes.Actor, FixedScalar.FromSingle(10f)),
            new DecisionModifier(verbId + 1, kindId, traitId, TraitEffectBearerCodes.Actor, FixedScalar.FromSingle(10f)),
        };

        var totals = DecisionModifierEvaluation.SumLowAltitudeValues(
            simulation.World,
            modifierCount: 3,
            modifier => modifiers[modifier],
            kindId,
            verbId,
            actor);

        Assert.Equal(FixedScalar.FromSingle(1f), totals.Weight);
        Assert.Equal(FixedScalar.FromSingle(2f), totals.OpportunityBonus);
        Assert.Equal(FixedScalar.FromSingle(3f), totals.FitnessWeight);
    }
}
