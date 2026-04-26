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
        var uiDirectory = Path.Combine(root.FullName, "src", "UI");
        var sourceFiles = Directory.GetFiles(uiDirectory, "*.cs", SearchOption.AllDirectories);
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
