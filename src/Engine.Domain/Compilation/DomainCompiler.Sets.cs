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
        DefinitionGraph graph)
    {
        var gameplayPrograms = GameplayCompiler
            .CompileGameplay(config.Gameplay ?? new GameplayDefinition(), new DomainGameplayCompilationContext(config, ids))
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
        };
    }

    private static CompiledKernelDataSet BuildKernelData(DomainConfig config, DomainIdRegistry ids)
    {
        return new CompiledKernelDataSet
        {
            Entities = BuildEntityData(config, ids),
            Terrain = BuildTerrainData(config, ids),
        };
    }
}
