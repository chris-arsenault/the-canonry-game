namespace Engine.Domain.Tests.Architecture;

public sealed class SharedQueryEffectRefactorGuardTests
{
    [Fact]
    public void Generators_DoNotOwnEntityQueryMatchingInterpreter()
    {
        var root = FindRepoRoot();
        var generatorRuntime = ReadAllSourcesUnder(root, "src/Engine.Kernel/Programs/Generators");
        var forbiddenFragments = new[]
        {
            "private int CountMatchingEntitiesForQuery(",
            "private int SelectMatchingEntityForQuery(",
            "private bool EntityMatchesQuery(",
            "private bool EntityBasicsMatchQuery(",
            "private bool EntityFiltersMatchQuery(",
            "private bool RelationshipFiltersPass(",
        };

        Assert.Empty(ContainedFragments(generatorRuntime, forbiddenFragments));
    }

    [Fact]
    public void Generators_DoNotOwnTargetRelationshipQueryLoops()
    {
        var root = FindRepoRoot();
        var generatorRuntime = ReadAllSourcesUnder(root, "src/Engine.Kernel/Programs/Generators");
        var forbiddenFragments = new[]
        {
            "private bool RequiredDirectedRelationshipsPass(",
            "private bool RequiredAnyDirectionRelationshipsPass(",
        };

        Assert.Empty(ContainedFragments(generatorRuntime, forbiddenFragments));
    }

    [Fact]
    public void Gambits_DoNotOwnPreconditionQueryInterpreter()
    {
        var root = FindRepoRoot();
        var gambitRuntime = ReadAllSourcesUnder(root, "src/Engine.Kernel/Programs/Gambits");
        var forbiddenFragments = new[]
        {
            "private bool PreconditionPasses(",
            "private static bool SimplePreconditionPasses(",
            "private bool RangePreconditionPasses(",
            "private bool OtherRangePreconditionPasses(",
            "private bool RelationshipPreconditionPasses(",
            "private bool EndpointRelationshipCountBelowPasses(",
        };

        Assert.Empty(ContainedFragments(gambitRuntime, forbiddenFragments));
    }

    [Fact]
    public void TraitDecisionModifiers_UseSharedDecisionModifierPrimitive()
    {
        var root = FindRepoRoot();
        var kernelAndGameplaySource =
            ReadAllSourcesUnder(root, "src/Engine.Kernel") +
            ReadAllSourcesUnder(root, "src/Engine.Gameplay");
        var callerLocalTraitDecisionFragments = new[]
        {
            "TraitGambitDelta(",
            "TraitGambitScoreDelta(",
            "TraitGambitSuccessChanceDelta(",
            "TraitGambitModifierMatches(",
            "TraitModifierWeightDelta(",
            "TraitModifierExploreWeightDelta(",
            "TraitModifierStalenessWeightDelta(",
            "TraitModifierHotspotWeightDelta(",
            "TraitModifierCrowdingWeightDelta(",
            "TraitModifierAffordanceWeightDelta(",
            "TraitModifierRelationshipAffinityWeightDelta(",
        };

        Assert.Contains("DecisionModifier", kernelAndGameplaySource, StringComparison.Ordinal);
        Assert.Empty(ContainedFragments(kernelAndGameplaySource, callerLocalTraitDecisionFragments));
    }

    [Fact]
    public void EffectsGeneratorsAndGambits_CompileQueriesToSharedInstructionModel()
    {
        var root = FindRepoRoot();
        var source =
            ReadAllSourcesUnder(root, "src/Engine.Kernel") +
            ReadAllSourcesUnder(root, "src/Engine.Gameplay") +
            ReadAllSourcesUnder(root, "src/Engine.Domain");
        var requiredFragments = new[]
        {
            "SharedQuery",
            "QueryPredicate",
            "QueryBinding",
            "QueryEvaluation",
        };

        Assert.Empty(MissingFragments(source, requiredFragments));
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

    private static List<string> MissingFragments(string text, IReadOnlyList<string> fragments)
    {
        var violations = new List<string>();
        foreach (var fragment in fragments)
        {
            if (!text.Contains(fragment, StringComparison.Ordinal))
            {
                violations.Add(fragment);
            }
        }

        return violations;
    }

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
