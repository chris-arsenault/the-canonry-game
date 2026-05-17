namespace Engine.Domain.Compilation;

using Engine.Domain.Compilation.KernelData;
using Engine.Domain.Compilation.KernelPrograms;
using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Gameplay;

public static partial class DomainCompiler
{
    private static KernelProgramSet BuildPrograms(
        DomainConfig config,
        DomainIdRegistry ids,
        DefinitionGraph graph,
        List<DomainCompileDiagnostic> diagnostics)
    {
        var gameplayContext = new DomainGameplayCompilationContext(config, ids, diagnostics);
        var gameplayPrograms = GameplayCompiler
            .CompileGameplay(config.Gameplay ?? new GameplayDefinition(), gameplayContext)
            .Programs;

        return new KernelProgramSet
        {
            LowAltitudeVerbs = gameplayPrograms.LowAltitudeVerbs,
            MapActivity = gameplayPrograms.MapActivity,
            MapSignals = gameplayPrograms.MapSignals,
            Effects = gameplayPrograms.Effects,
            Upkeep = BuildUpkeepProgram(config, ids),
            Gambits = BuildGambitProgram(config, ids),
            Relationships = BuildRelationshipProgram(config, ids),
            Generators = BuildGeneratorProgram(config, ids, graph),
            CultureSlots = BuildCultureSlotProgram(graph),
        };
    }

    private static CultureSlotProgram BuildCultureSlotProgram(DefinitionGraph graph)
    {
        var slots = new List<CultureSlotRuntimeRule>();
        foreach (var culture in graph.Cultures.Values)
        {
            foreach (var slot in culture.Slots.Values)
            {
                slots.Add(new CultureSlotRuntimeRule(
                    culture.RuntimeId,
                    slot.SlotId,
                    slot.PolarityCode,
                    slot.AllowedKindIds.ToArray(),
                    slot.ScoreRules
                        .Select(rule => new CultureSlotScoreRuntimeRule(
                            rule.SignalCode,
                            rule.RelationshipKindId,
                            rule.Weight))
                        .ToArray(),
                    slot.MinimumScore,
                    slot.MinimumTenureTicks,
                    slot.DisplacementMargin,
                    slot.EntityInfluenceMultiplierDelta,
                    slot.CultureInfluenceMultiplierDelta));
            }
        }

        return new CultureSlotProgram
        {
            EnabledByDefault = slots.Count > 0,
            Slots = slots,
        };
    }

    private static CompiledKernelDataSet BuildKernelData(DomainConfig config, DomainIdRegistry ids)
    {
        var movementProfileIds = BuildMovementProfileIds(config);
        return new CompiledKernelDataSet
        {
            Entities = BuildEntityData(config, ids),
            Movement = BuildMovementData(config, ids, movementProfileIds),
            Terrain = BuildTerrainData(config, ids, movementProfileIds),
        };
    }

    private static Dictionary<string, int> BuildMovementProfileIds(DomainConfig config)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var profile in config.MovementProfiles)
        {
            if (string.IsNullOrWhiteSpace(profile.Name))
            {
                throw new InvalidOperationException("Movement profile names must be non-empty.");
            }

            if (!result.TryAdd(profile.Name, result.Count + 1))
            {
                throw new InvalidOperationException($"Duplicate movement profile '{profile.Name}'.");
            }
        }

        return result;
    }
}
