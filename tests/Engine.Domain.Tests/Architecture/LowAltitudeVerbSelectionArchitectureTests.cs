namespace Engine.Domain.Tests.Architecture;

public sealed class LowAltitudeVerbSelectionArchitectureTests
{
    [Fact]
    public void LowAltitudeCellTargetModes_DoNotUseUniformRandomCoordinateSampling()
    {
        var root = FindRepoRoot();
        var lowAltitudeRuntime = ReadAllSourcesUnder(root, "src/Engine.Kernel/Programs/LowAltitudeVerbs");
        var forbiddenFragments = new[]
        {
            "TryRandomCoordinate",
            "TryRandomSemanticCoordinate",
            "StalenessFactor",
            "ReadCellLastVisitedTick",
        };
        var violations = ContainedFragments(lowAltitudeRuntime, forbiddenFragments);

        Assert.Empty(violations);
    }

    [Fact]
    public void IceRemembersLowAltitudeVerbs_DoNotUseLegacySixWeightTuples()
    {
        var root = FindRepoRoot();
        var source = ReadSource(root, "src/IceRemembers/IceRemembersLowAltitudeVerbs.cs");
        var forbiddenFragments = new[]
        {
            "(float Base, float Explore, float Stale, float Hot, float Crowd, float Generator)",
            "weights: (",
            "weights.Base",
            "weights.Generator",
        };
        var violations = ContainedFragments(source, forbiddenFragments);

        Assert.Empty(violations);
    }

    [Fact]
    public void LowAltitudeTraceTelemetry_ExposesSelectionBucket()
    {
        var root = FindRepoRoot();
        var traceEvent = ReadSource(root, "src/Engine.Eventing/KernelEvent.cs");
        var telemetryRows = ReadSource(root, "src/Engine.Observability/TelemetryBatch.cs");

        Assert.Contains("selection_bucket_code", traceEvent, StringComparison.Ordinal);
        Assert.Contains("SelectionBucketCode", telemetryRows, StringComparison.Ordinal);
    }

    private static List<string> ContainedFragments(string text, IReadOnlyList<string> fragments)
    {
        var violations = new List<string>();
        foreach (var fragment in fragments)
        {
            if (text.Contains(fragment, StringComparison.Ordinal))
            {
                violations.Add(fragment);
            }
        }

        return violations;
    }

    private static string ReadSource(DirectoryInfo root, string relativePath) =>
        File.ReadAllText(Path.Combine(root.FullName, relativePath));

    private static string ReadAllSourcesUnder(DirectoryInfo root, string relativeDirectory)
    {
        var directory = Path.Combine(root.FullName, relativeDirectory);
        if (!Directory.Exists(directory))
        {
            return string.Empty;
        }

        return string.Join(
            Environment.NewLine,
            Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories)
                .Order(StringComparer.Ordinal)
                .Select(File.ReadAllText));
    }

    private static DirectoryInfo FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TheCanonryGame.csproj")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new InvalidOperationException("Could not find repository root.");
    }
}
