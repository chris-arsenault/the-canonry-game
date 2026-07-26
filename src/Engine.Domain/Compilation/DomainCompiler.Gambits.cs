namespace Engine.Domain.Compilation;

using Engine.Constants;
using Engine.Domain.Compilation.KernelPrograms;
using Engine.Domain.Config;
using Engine.Gameplay;

public static partial class DomainCompiler
{
    private static GambitProgram BuildGambitProgram(
        DomainConfig config,
        DomainIdRegistry ids)
    {
        var gambits = new List<CompiledGambitRule>();
        foreach (var gambit in config.Gambits)
        {
            var targetKindId = ResolveTargetKindId(gambit, ids);
            gambits.Add(new CompiledGambitRule(
                ids.KindIdOf(gambit.TargetKind).Value,
                ids.GambitIdOf(gambit.Id).Value,
                gambit.Cooldown,
                gambit.EntityCooldown,
                ResolveTargetMode(gambit, targetKindId),
                targetKindId,
                FixedScalar.FromSingle(gambit.SuccessChance),
                gambit.ChronicleGranted,
                CompileProviderSelectors(gambit, ids),
                CompilePreconditions(gambit.Precondition, ids),
                CompileExpression(gambit.ScoreExpression, ids),
                CompileSteps(gambit, ids)));
        }

        return new GambitProgram
        {
            EnabledByDefault = gambits.Count > 0,
            Gambits = gambits,
            TraitModifiers = CompileTraitGambitModifiers(config, ids),
        };
    }

    private static List<CompiledTraitGambitModifierRule> CompileTraitGambitModifiers(
        DomainConfig config,
        DomainIdRegistry ids)
    {
        var modifiers = new List<CompiledTraitGambitModifierRule>();
        foreach (var (_, kindConfig) in config.Kinds)
        {
            foreach (var trait in kindConfig.Traits)
            {
                foreach (var operation in trait.Operations)
                {
                    if (operation is not TraitGambitModifierOperationConfig modifier)
                    {
                        continue;
                    }

                    modifiers.Add(new CompiledTraitGambitModifierRule(
                        ids.GambitIdOf(modifier.Gambit).Value,
                        ids.TraitIdOf(trait.Name).Value,
                        TraitEffectBearerCode(modifier.TraitBearer),
                        FixedScalar.FromSingle(modifier.ScoreDelta),
                        FixedScalar.FromSingle(modifier.SuccessChanceDelta)));
                }
            }
        }

        return modifiers;
    }

    private static int TraitEffectBearerCode(TraitEffectBearer bearer) => bearer switch
    {
        TraitEffectBearer.Actor => TraitEffectBearerCodes.Actor,
        TraitEffectBearer.GambitTarget => TraitEffectBearerCodes.GambitTarget,
        _ => throw new ArgumentOutOfRangeException(
            nameof(bearer),
            bearer,
            "Unsupported gambit trait bearer."),
    };
}
