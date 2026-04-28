namespace Engine.Domain.Compilation;

using Engine.Domain.Compilation.KernelData;
using Engine.Domain.Compilation.KernelPrograms;
using Engine.Domain.Config;
using Engine.Domain.Graph;

public static partial class DomainCompiler
{
    private static KernelProgramSet BuildPrograms(
        DomainConfig config,
        DomainIdRegistry ids,
        DefinitionGraph graph)
    {
        return new KernelProgramSet
        {
            Upkeep = BuildUpkeepProgram(config, ids),
            Processes = BuildProcessProgram(config, ids),
            Gambits = BuildGambitProgram(config, ids),
            Relationships = BuildRelationshipProgram(config, ids),
            Pressures = BuildPressureProgram(config, ids),
            Generators = BuildGeneratorProgram(config, ids, graph),
            Degeneracy = BuildDegeneracyProgram(config, ids),
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
