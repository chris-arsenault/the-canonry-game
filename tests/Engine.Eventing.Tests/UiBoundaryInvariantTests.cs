namespace Engine.Eventing.Tests;

public sealed class UiBoundaryInvariantTests
{
    private static readonly string[] ForbiddenUiFragments =
    [
        "_sim.Clock",
        "Clock.CurrentTick",
        "Clock.Speed",
        "SimulationSpeed",
        "_kernelTime?.CurrentTick",
        "_kernelTime.CurrentTick",
        "_kernelTime?.IsPaused",
        "_kernelTime.IsPaused",
        "_kernelTime?.TicksPerSecond",
        "_kernelTime.TicksPerSecond",
        ".Tableau.PlaceCard(",
        ".Tableau.RemoveCard(",
        ".Tableau.ActiveCards",
    ];

    [Fact]
    public void UiDoesNotReadLegacyClockOrDriverStateAndDoesNotMutateTableauDirectly()
    {
        var root = FindRepoRoot();
        var sourceDirectories = new[]
        {
            Path.Combine(root.FullName, "src", "TheCanonryGame.App"),
            Path.Combine(root.FullName, "src", "TheCanonryGame.UI"),
        };
        var missingDirectories = sourceDirectories
            .Where(directory => !Directory.Exists(directory))
            .Select(directory => Path.GetRelativePath(root.FullName, directory))
            .ToArray();
        Assert.Empty(missingDirectories);

        var sourceFiles = sourceDirectories.SelectMany(directory =>
            Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories));
        var violations = new List<string>();

        foreach (var sourceFile in sourceFiles)
        {
            var text = File.ReadAllText(sourceFile);
            foreach (var forbidden in ForbiddenUiFragments)
            {
                if (text.Contains(forbidden, StringComparison.Ordinal))
                {
                    violations.Add($"{Path.GetRelativePath(root.FullName, sourceFile)} contains {forbidden}");
                }
            }
        }

        Assert.Empty(violations);
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
