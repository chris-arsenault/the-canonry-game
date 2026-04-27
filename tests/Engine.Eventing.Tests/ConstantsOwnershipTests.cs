namespace Engine.Eventing.Tests;

public sealed class ConstantsOwnershipTests
{
    private static readonly HashSet<string> PublicConstIntAllowList =
    [
        Normalize("src/Engine.Kernel/Boundary.cs"),
        Normalize("src/Engine.Kernel/KernelCommand.cs"),
        Normalize("src/Engine.Eventing/KernelEvent.cs"),
        Normalize("src/TheCanonryGame.UI/Theme/UiTokens.cs"),
    ];

    private static readonly string[] CodeArgumentPrefixes =
    [
        "PlaneCode:",
        "CurrentStatusCode:",
        "PreviousStatusCode:",
        "StatusCode:",
        "PolarityCode:",
        "polarityCode:",
    ];

    [Fact]
    public void PrimitiveCodebooksAreDeclaredOnlyInEngineConstants()
    {
        var root = FindRepoRoot();
        var violations = new List<string>();

        foreach (var sourceFile in EnumerateSourceFiles(root))
        {
            var relativePath = Normalize(Path.GetRelativePath(root.FullName, sourceFile));
            if (relativePath.StartsWith("src/Engine.Constants/", StringComparison.Ordinal))
            {
                continue;
            }

            var lines = File.ReadAllLines(sourceFile);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Contains("public static class ", StringComparison.Ordinal) &&
                    (line.Contains("Codes", StringComparison.Ordinal) ||
                     line.Contains("Constants", StringComparison.Ordinal)))
                {
                    violations.Add($"{relativePath}:{i + 1}: move primitive codebooks to Engine.Constants");
                }

                if (line.Contains("public const int ", StringComparison.Ordinal) &&
                    !PublicConstIntAllowList.Contains(relativePath))
                {
                    violations.Add($"{relativePath}:{i + 1}: public integer constants belong in Engine.Constants");
                }
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void ProjectionDoesNotRedeclareKernelEventCodebooks()
    {
        var root = FindRepoRoot();
        var projectionFiles = EnumerateSourceFiles(root)
            .Where(file => Normalize(Path.GetRelativePath(root.FullName, file))
                .StartsWith("src/Engine.Projection/", StringComparison.Ordinal));
        var forbiddenFragments = new[]
        {
            "UiProjectionPlane",
            "UiProjectionStatusCodes",
        };
        var violations = new List<string>();

        foreach (var sourceFile in projectionFiles)
        {
            var text = File.ReadAllText(sourceFile);
            foreach (var fragment in forbiddenFragments)
            {
                if (text.Contains(fragment, StringComparison.Ordinal))
                {
                    violations.Add($"{Normalize(Path.GetRelativePath(root.FullName, sourceFile))} contains {fragment}");
                }
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void EventCodeArgumentsDoNotUseRawNumericLiterals()
    {
        var root = FindRepoRoot();
        var sourceDirectories = new[]
        {
            Path.Combine(root.FullName, "src"),
            Path.Combine(root.FullName, "tests"),
        };
        var violations = new List<string>();

        foreach (var sourceFile in sourceDirectories.SelectMany(EnumerateSourceFiles))
        {
            var relativePath = Normalize(Path.GetRelativePath(root.FullName, sourceFile));
            var lines = File.ReadAllLines(sourceFile);
            for (var i = 0; i < lines.Length; i++)
            {
                if (ContainsRawCodeArgument(lines[i]) || IsRawCodeNameSwitchLine(lines[i]))
                {
                    violations.Add($"{relativePath}:{i + 1}: use Engine.Constants instead of raw primitive code values");
                }
            }
        }

        Assert.Empty(violations);
    }

    private static IEnumerable<string> EnumerateSourceFiles(DirectoryInfo root) =>
        EnumerateSourceFiles(Path.Combine(root.FullName, "src"));

    private static IEnumerable<string> EnumerateSourceFiles(string directory) =>
        Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(path =>
                !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                !path.Contains($"{Path.DirectorySeparatorChar}.godot{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    private static bool ContainsRawCodeArgument(string line)
    {
        foreach (var prefix in CodeArgumentPrefixes)
        {
            var index = line.IndexOf(prefix, StringComparison.Ordinal);
            if (index < 0)
            {
                continue;
            }

            var valueStart = index + prefix.Length;
            while (valueStart < line.Length && char.IsWhiteSpace(line[valueStart]))
            {
                valueStart++;
            }

            if (valueStart < line.Length && IsNumericLiteralStart(line, valueStart))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsRawCodeNameSwitchLine(string line)
    {
        var index = 0;
        while (index < line.Length && char.IsWhiteSpace(line[index]))
        {
            index++;
        }

        if (index >= line.Length || !IsNumericLiteralStart(line, index))
        {
            return false;
        }

        if (line[index] == '-')
        {
            index++;
        }

        while (index < line.Length && char.IsDigit(line[index]))
        {
            index++;
        }

        while (index < line.Length && char.IsWhiteSpace(line[index]))
        {
            index++;
        }

        if (index + 1 >= line.Length || line[index] != '=' || line[index + 1] != '>')
        {
            return false;
        }

        index += 2;
        while (index < line.Length && char.IsWhiteSpace(line[index]))
        {
            index++;
        }

        return index < line.Length && line[index] == '"';
    }

    private static bool IsNumericLiteralStart(string line, int index) =>
        char.IsDigit(line[index]) ||
        (line[index] == '-' && index + 1 < line.Length && char.IsDigit(line[index + 1]));

    private static DirectoryInfo FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TheCanonryGame.csproj")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new InvalidOperationException("Could not find repository root.");
    }

    private static string Normalize(string path) => path.Replace('\\', '/');
}
