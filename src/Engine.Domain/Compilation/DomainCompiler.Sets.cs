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
            MinorActors = BuildMinorActorProgram(config, ids),
            Chronicles = BuildChronicleProgram(config, ids),
            WorldBeats = BuildWorldBeatProgram(config, ids),
            MapFrontier = BuildMapFrontierProgram(config, ids),
        };
    }

    private static MinorActorProgram BuildMinorActorProgram(DomainConfig config, DomainIdRegistry ids)
    {
        var rules = new List<MinorActorEmitterRule>();
        foreach (var rule in config.MinorActors)
        {
            var planeCode = rule.PlaneCode;
            rules.Add(new MinorActorEmitterRule(
                ids.KindIdOf(rule.SourceKind).Value,
                string.IsNullOrWhiteSpace(rule.SourceSubtype)
                    ? 0
                    : ids.SubtypeIdOf(rule.SourceSubtype).Value,
                rule.PurposeCode,
                rule.ActorKindCode,
                rule.ActionCode,
                planeCode,
                rule.TtlPulses,
                rule.EmitCadencePulses,
                rule.MaxActivePerSource,
                rule.SpawnRadius,
                rule.MoveChancePermille,
                rule.ActionCadencePulses,
                rule.WorkDelta,
                rule.Verbs
                    .Select(verb => new MinorActorVerbRule(
                        ids.VerbIdOf(verb.Id).Value,
                        verb.ActionCode,
                        verb.TargetingCode,
                        verb.PlaneCode == Engine.Constants.PlaneCodes.None ? planeCode : verb.PlaneCode,
                        verb.Radius,
                        verb.SampleCount,
                        verb.Weight,
                        verb.WorkDeltaMultiplier))
                    .ToArray()));
        }

        return new MinorActorProgram
        {
            EnabledByDefault = rules.Count > 0,
            EmitterRules = rules,
            CampaignRelationshipKindId = config.MinorActorCampaign is null
                ? 0
                : ids.RelationshipKindIdOf(config.MinorActorCampaign.RelationshipKind).Value,
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
            Cultures = BuildCultureData(config, ids),
        };
    }

    private static CompiledCultureData BuildCultureData(DomainConfig config, DomainIdRegistry ids)
    {
        var cultures = new List<CompiledCultureDefinition>(config.Cultures.Count);
        foreach (var culture in config.Cultures)
        {
            if (culture.PhysicalInfluenceProductionMultiplier < 0f ||
                culture.SemanticInfluenceProductionMultiplier < 0f)
            {
                throw new InvalidOperationException(
                    $"Culture '{culture.Id}' influence production multipliers must be non-negative.");
            }

            cultures.Add(new CompiledCultureDefinition(
                ids.CultureIdOf(culture.Id).Value,
                culture.PhysicalInfluenceProductionMultiplier,
                culture.SemanticInfluenceProductionMultiplier));
        }

        return new CompiledCultureData { Cultures = cultures };
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
