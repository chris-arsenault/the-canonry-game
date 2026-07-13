namespace Engine.Domain.Tests.Architecture;

public sealed class KernelProgramStateArchitectureTests
{
    [Fact]
    public void KernelProgramStates_UseBareArraysOnlyByExplicitException()
    {
        var root = FindRepoRoot();
        var allowed = BareArrayStateExceptions();
        var programDirectory = Path.Combine(root.FullName, "src", "Engine.Kernel", "Programs");
        var violations = new List<string>();
        foreach (var file in Directory.GetFiles(programDirectory, "*ProgramState.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            var relativePath = Path.GetRelativePath(root.FullName, file).Replace('\\', '/');
            foreach (var line in File.ReadLines(file))
            {
                if (!TryParseBareArrayFieldName(line, out var fieldName))
                {
                    continue;
                }

                var key = $"{relativePath}:{fieldName}";
                if (!allowed.Contains(key))
                {
                    violations.Add(key);
                }
            }
        }

        Assert.Empty(violations);
    }

    private static HashSet<string> BareArrayStateExceptions()
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "src/Engine.Kernel/Programs/Gambits/GambitProgramState.cs:GambitEnabled",
            "src/Engine.Kernel/Programs/Gambits/GambitProgramState.cs:ActionHistoryTicks",
            "src/Engine.Kernel/Programs/Gambits/GambitProgramState.cs:ActionHistoryGambitIds",
            "src/Engine.Kernel/Programs/Gambits/GambitProgramState.cs:ActionHistoryCounts",
            "src/Engine.Kernel/Programs/Gambits/GambitProgramState.cs:ActionHistoryNextSlots",
            "src/Engine.Kernel/Programs/Generators/GeneratorProgramState.cs:RunCounts",
            "src/Engine.Kernel/Programs/Generators/GeneratorProgramState.cs:Enabled",
            "src/Engine.Kernel/Programs/Generators/GeneratorProgramState.cs:NextCadenceFireTicks",
            "src/Engine.Kernel/Programs/Chronicles/ChronicleProgramState.cs:Active",
            "src/Engine.Kernel/Programs/Chronicles/ChronicleProgramState.cs:ArchetypeSlots",
            "src/Engine.Kernel/Programs/Chronicles/ChronicleProgramState.cs:ActIndices",
            "src/Engine.Kernel/Programs/Chronicles/ChronicleProgramState.cs:StartTicks",
            "src/Engine.Kernel/Programs/Chronicles/ChronicleProgramState.cs:ActStartTicks",
            "src/Engine.Kernel/Programs/Chronicles/ChronicleProgramState.cs:RoleEntityIds",
            "src/Engine.Kernel/Programs/Chronicles/ChronicleProgramState.cs:ChronicleSequences",
            "src/Engine.Kernel/Programs/Chronicles/ChronicleProgramState.cs:ArchetypeActiveCounts",
            "src/Engine.Kernel/Programs/Chronicles/ChronicleProgramState.cs:NextCastTicks",
            "src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramState.cs:_activeVerbSlots",
            "src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramState.cs:_intentIds",
            "src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramState.cs:_targetPlaneCodes",
            "src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramState.cs:_targetEntityIds",
            "src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramState.cs:_targetQ",
            "src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramState.cs:_targetR",
            "src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramState.cs:_targetS",
            "src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramState.cs:_startedTicks",
            "src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramState.cs:_expiresTicks",
            "src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramState.cs:_dwellUntilTicks",
            "src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramState.cs:_reachedTicks",
            "src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramState.cs:_intentDwellTicks",
            "src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramState.cs:_nextStepTicks",
            "src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramState.cs:_stepIntervalTicks",
            "src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramState.cs:_pathLoosenessPermille",
            "src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramState.cs:_initialInfluenceApplied",
            "src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramState.cs:_recentTargetKeys",
            "src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramState.cs:_recentTargetTicks",
            "src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramState.cs:_recentTargetCounts",
            "src/Engine.Kernel/Programs/Relationships/RelationshipProgramState.cs:_contactTicks",
            "src/Engine.Kernel/Programs/Relationships/RelationshipProgramState.cs:_lastContactTicks",
        };
        AddMovementRouteCacheStateExceptions(allowed);
        return allowed;
    }

    private static void AddMovementRouteCacheStateExceptions(HashSet<string> allowed)
    {
        allowed.Add("src/Engine.Kernel/Programs/Traversal/MovementProgramState.cs:_routeCachePlaneCodes");
        allowed.Add("src/Engine.Kernel/Programs/Traversal/MovementProgramState.cs:_routeCacheProfileIds");
        allowed.Add("src/Engine.Kernel/Programs/Traversal/MovementProgramState.cs:_routeCacheLevelSizes");
        allowed.Add("src/Engine.Kernel/Programs/Traversal/MovementProgramState.cs:_routeCacheSourceKeys");
        allowed.Add("src/Engine.Kernel/Programs/Traversal/MovementProgramState.cs:_routeCacheTargetKeys");
        allowed.Add("src/Engine.Kernel/Programs/Traversal/MovementProgramState.cs:_routeCacheTopologyEpochs");
        allowed.Add("src/Engine.Kernel/Programs/Traversal/MovementProgramState.cs:_routeCacheRoadEpochs");
        allowed.Add("src/Engine.Kernel/Programs/Traversal/MovementProgramState.cs:_routeCacheNextSegments");
        allowed.Add("src/Engine.Kernel/Programs/Traversal/MovementProgramState.cs:_routeCachePortalQ");
        allowed.Add("src/Engine.Kernel/Programs/Traversal/MovementProgramState.cs:_routeCachePortalR");
        allowed.Add("src/Engine.Kernel/Programs/Traversal/MovementProgramState.cs:_routeCacheOccupied");
    }

    private static bool TryParseBareArrayFieldName(string line, out string fieldName)
    {
        fieldName = string.Empty;
        var trimmed = line.Trim();
        if (!FieldDeclarationMayBeStateArray(trimmed))
        {
            return false;
        }

        var arrayMarker = trimmed.IndexOf("[]", StringComparison.Ordinal);
        if (arrayMarker < 0)
        {
            return false;
        }

        var fieldStart = arrayMarker + 2;
        while (fieldStart < trimmed.Length && trimmed[fieldStart] == ' ')
        {
            fieldStart++;
        }

        var fieldEnd = fieldStart;
        while (fieldEnd < trimmed.Length && IsFieldNameCharacter(trimmed[fieldEnd]))
        {
            fieldEnd++;
        }

        if (fieldEnd == fieldStart)
        {
            return false;
        }

        fieldName = trimmed[fieldStart..fieldEnd];
        return true;
    }

    private static bool FieldDeclarationMayBeStateArray(string trimmed)
    {
        return trimmed.EndsWith(';') &&
            trimmed.Contains("[]", StringComparison.Ordinal) &&
            (trimmed.StartsWith("private ", StringComparison.Ordinal) ||
                trimmed.StartsWith("internal ", StringComparison.Ordinal) ||
                trimmed.StartsWith("public ", StringComparison.Ordinal) ||
                trimmed.StartsWith("protected ", StringComparison.Ordinal));
    }

    private static bool IsFieldNameCharacter(char value) =>
        char.IsLetterOrDigit(value) || value == '_';

    private static DirectoryInfo FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TheCanonryGame.csproj")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new InvalidOperationException();
    }
}
