namespace Engine.Domain.Compilation;

using Engine.Constants;
using Engine.Domain.Compilation.KernelPrograms;
using Engine.Domain.Config;

public static partial class DomainCompiler
{
    private static WorldBeatProgram BuildWorldBeatProgram(DomainConfig config, DomainIdRegistry ids)
    {
        var ages = new List<CompiledWorldBeatAge>();
        foreach (var age in config.Semantics?.WorldBeatAges ?? [])
        {
            ages.Add(CompileWorldBeatAge(age, ids));
        }

        return new WorldBeatProgram { Ages = ages };
    }

    private static CompiledWorldBeatAge CompileWorldBeatAge(WorldBeatAgeConfig age, DomainIdRegistry ids)
    {
        return new CompiledWorldBeatAge(
            ids.GeneratorIdOf(age.Id).Value,
            (int)age.Track,
            age.Priority,
            age.MinOpenTicks,
            age.CooldownTicks,
            [.. age.OpenWhen.Select(condition => CompileWorldBeatCondition(age, condition, ids))],
            [.. age.CloseWhen.Select(condition => CompileWorldBeatCondition(age, condition, ids))],
            [.. age.Charges.Select(charge => CompileWorldBeatCharge(age, charge, ids))]);
    }

    private static CompiledWorldBeatCondition CompileWorldBeatCondition(
        WorldBeatAgeConfig age,
        WorldBeatConditionConfig condition,
        DomainIdRegistry ids)
    {
        var paramId = condition.Fact switch
        {
            WorldBeatFact.ActiveEntitiesOfKind => ids.KindIdOf(
                condition.Kind ?? throw MissingParam(age, condition, "Kind")).Value,
            WorldBeatFact.LivingRelationshipsOfKind or WorldBeatFact.CulledRelationshipsOfKind =>
                ids.RelationshipKindIdOf(
                    condition.RelationshipKind ?? throw MissingParam(age, condition, "RelationshipKind")).Value,
            _ => 0,
        };
        return new CompiledWorldBeatCondition(
            WorldBeatFactCode(condition.Fact),
            condition.Comparator == WorldBeatComparator.AtLeast
                ? WorldBeatComparatorCodes.AtLeast
                : WorldBeatComparatorCodes.Below,
            FixedScalar.FromSingle(condition.Value),
            paramId);
    }

    private static CompiledWorldBeatCharge CompileWorldBeatCharge(
        WorldBeatAgeConfig age,
        WorldBeatChargeConfig charge,
        DomainIdRegistry ids)
    {
        if (charge.Multiplier <= 0f)
        {
            throw new InvalidOperationException(
                $"World beat age '{age.Id}' has a non-positive charge multiplier.");
        }

        var targetId = charge.Target is null ? 0 : ResolveChargeTarget(age, charge, ids);
        return new CompiledWorldBeatCharge(
            WorldBeatChargeDomainCode(charge.Domain),
            targetId,
            FixedScalar.FromSingle(charge.Multiplier));
    }

    private static int ResolveChargeTarget(WorldBeatAgeConfig age, WorldBeatChargeConfig charge, DomainIdRegistry ids) =>
        charge.Domain switch
        {
            WorldBeatChargeDomain.GeneratorPulseWeight => ids.GeneratorIdOf(charge.Target!).Value,
            WorldBeatChargeDomain.VerbWeight => ids.VerbIdOf(charge.Target!).Value,
            WorldBeatChargeDomain.GambitScore => ids.GambitIdOf(charge.Target!).Value,
            WorldBeatChargeDomain.GambitSuccess => ids.GambitIdOf(charge.Target!).Value,
            _ => throw new InvalidOperationException(
                $"World beat age '{age.Id}' charge domain '{charge.Domain}' does not take a target."),
        };

    private static int WorldBeatFactCode(WorldBeatFact fact) => fact switch
    {
        WorldBeatFact.TicksSinceStart => WorldBeatFactCodes.TicksSinceStart,
        WorldBeatFact.ActiveEntitiesOfKind => WorldBeatFactCodes.ActiveEntitiesOfKind,
        WorldBeatFact.LivingRelationshipsOfKind => WorldBeatFactCodes.LivingRelationshipsOfKind,
        WorldBeatFact.CulledRelationshipsOfKind => WorldBeatFactCodes.CulledRelationshipsOfKind,
        WorldBeatFact.TotalCultureRawMass => WorldBeatFactCodes.TotalCultureRawMass,
        WorldBeatFact.CultureMassGrowthPermille => WorldBeatFactCodes.CultureMassGrowthPermille,
        WorldBeatFact.LegendaryResolutions => WorldBeatFactCodes.LegendaryResolutions,
        _ => throw new InvalidOperationException($"Unknown world beat fact '{fact}'."),
    };

    private static int WorldBeatChargeDomainCode(WorldBeatChargeDomain domain) => domain switch
    {
        WorldBeatChargeDomain.GeneratorPulseWeight => WorldBeatChargeDomainCodes.GeneratorPulseWeight,
        WorldBeatChargeDomain.VerbWeight => WorldBeatChargeDomainCodes.VerbWeight,
        WorldBeatChargeDomain.GambitScore => WorldBeatChargeDomainCodes.GambitScore,
        WorldBeatChargeDomain.ContestDecayTide => WorldBeatChargeDomainCodes.ContestDecayTide,
        WorldBeatChargeDomain.GlobalDecayTide => WorldBeatChargeDomainCodes.GlobalDecayTide,
        WorldBeatChargeDomain.GambitSuccess => WorldBeatChargeDomainCodes.GambitSuccess,
        WorldBeatChargeDomain.MovementStepRate => WorldBeatChargeDomainCodes.MovementStepRate,
        _ => throw new InvalidOperationException($"Unknown world beat charge domain '{domain}'."),
    };

    private static InvalidOperationException MissingParam(
        WorldBeatAgeConfig age,
        WorldBeatConditionConfig condition,
        string param) =>
        new($"World beat age '{age.Id}' condition on '{condition.Fact}' requires {param}.");
}
