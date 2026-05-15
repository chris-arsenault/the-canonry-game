namespace NameForge.Runtime.Tests;

using NameForge.Runtime;

public sealed class NameForgeGeneratorTests
{
    [Fact]
    public void Generate_WithGrammar_IsDeterministic()
    {
        var generator = new NameForgeGenerator(CreateTestProject());
        var request = new NameForgeGenerateRequest
        {
            CultureId = "test",
            Kind = "penguin",
            Subtype = "warden",
            Count = 4,
            Seed = "deterministic",
        };

        var first = generator.Generate(request);
        var second = generator.Generate(request);

        Assert.Equal(first.Names, second.Names);
        Assert.All(first.Names, name => Assert.False(string.IsNullOrWhiteSpace(name)));
        Assert.Equal(4, first.StrategyUsage["grammar"]);
    }

    [Fact]
    public void Generate_WithMarkovToken_UsesMarkovProvider()
    {
        var generator = new NameForgeGenerator(CreateMarkovProject(), new TestMarkovProvider());

        var result = generator.Generate(new NameForgeGenerateRequest
        {
            CultureId = "test",
            Kind = "penguin",
            Count = 1,
            Seed = "markov",
        });

        Assert.Equal("Bcd", result.Names[0]);
        Assert.Equal(1, result.StrategyUsage["markov"]);
    }

    [Fact]
    public void Generate_WithContextCondition_SelectsContextStrategy()
    {
        var generator = new NameForgeGenerator(CreateContextProject());

        var contextResult = generator.Generate(new NameForgeGenerateRequest
        {
            CultureId = "test",
            Kind = "artifact",
            Count = 1,
            Seed = "context",
            Context = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["producer"] = "monster_steals_artifact",
                ["producer_display_name"] = "Monster Theft",
            },
        });
        var fallbackResult = generator.Generate(new NameForgeGenerateRequest
        {
            CultureId = "test",
            Kind = "artifact",
            Count = 1,
            Seed = "context",
        });

        Assert.Equal("Monster Theft Ice", contextResult.Names[0]);
        Assert.Equal("Ice", fallbackResult.Names[0]);
        Assert.Equal("Context Artifact", contextResult.DebugInfo[0].GroupUsed);
        Assert.Equal("Fallback Artifact", fallbackResult.DebugInfo[0].GroupUsed);
    }

    [Fact]
    public void Generate_WithJoinFirstTwoRomanization_RewritesHyphenChains()
    {
        var project = CreateRomanizedProject();
        var generator = new NameForgeGenerator(project);

        var result = generator.Generate(new NameForgeGenerateRequest
        {
            CultureId = "test",
            Kind = "orca",
            Count = 1,
            Seed = "romanized",
        });

        Assert.Equal("Gorkmork Pork", result.Names[0]);
    }

    [Theory]
    [InlineData("aurora_stack", "penguin", "warden")]
    [InlineData("aurora_stack", "settlement", "rookery")]
    [InlineData("aurora_stack", "region", "shelf")]
    [InlineData("aurora_stack", "point_of_interest", "vent")]
    [InlineData("aurora_stack", "transport", "trade")]
    [InlineData("aurora_stack", "transport", "battleship")]
    [InlineData("aurora_stack", "transport", "scout")]
    [InlineData("aurora_stack", "transport", "citadel")]
    [InlineData("aurora_stack", "monster", "leviathan")]
    [InlineData("aurora_stack", "monster", "wraith")]
    [InlineData("aurora_stack", "monster", "beast")]
    [InlineData("aurora_stack", "monster", "revenant")]
    [InlineData("aurora_stack", "occurrence", "festival")]
    [InlineData("aurora_stack", "artifact", "weapon")]
    [InlineData("aurora_stack", "ability", "rite")]
    [InlineData("aurora_stack", "faction", "order")]
    [InlineData("wake_singers", "orca", "matriarch")]
    [InlineData("wake_singers", "orca", "bull")]
    [InlineData("wake_singers", "orca", "scout")]
    [InlineData("wake_singers", "orca", "elder")]
    [InlineData("wake_singers", "settlement", "rookery")]
    [InlineData("wake_singers", "settlement", "colony")]
    [InlineData("wake_singers", "settlement", "holdfast")]
    [InlineData("wake_singers", "settlement", "sanctuary")]
    [InlineData("wake_singers", "settlement", "harbor")]
    [InlineData("wake_singers", "settlement", "reefhold")]
    [InlineData("wake_singers", "region", "face")]
    [InlineData("wake_singers", "region", "shelf")]
    [InlineData("wake_singers", "region", "depths")]
    [InlineData("wake_singers", "region", "reach")]
    [InlineData("wake_singers", "region", "sea")]
    [InlineData("wake_singers", "region", "scar")]
    [InlineData("wake_singers", "point_of_interest", "vent")]
    [InlineData("wake_singers", "point_of_interest", "peak")]
    [InlineData("wake_singers", "point_of_interest", "bloom")]
    [InlineData("wake_singers", "point_of_interest", "shoal")]
    [InlineData("wake_singers", "transport", "trade")]
    [InlineData("wake_singers", "transport", "battleship")]
    [InlineData("wake_singers", "transport", "scout")]
    [InlineData("wake_singers", "transport", "citadel")]
    [InlineData("wake_singers", "monster", "leviathan")]
    [InlineData("wake_singers", "monster", "wraith")]
    [InlineData("wake_singers", "monster", "beast")]
    [InlineData("wake_singers", "monster", "revenant")]
    public void IceRemembersProject_CoversCurrentKinds(string culture, string kind, string subtype)
    {
        using var stream = File.OpenRead(FindIceRemembersProjectPath());
        var project = NameForgeProject.Load(stream);
        var generator = new NameForgeGenerator(project);

        var result = generator.TryGenerateOne(
            new NameForgeGenerateRequest
            {
                CultureId = culture,
                Kind = kind,
                Subtype = subtype,
                Seed = $"{culture}:{kind}:{subtype}",
            },
            out var name);

        Assert.True(result);
        Assert.False(string.IsNullOrWhiteSpace(name));
        Assert.InRange(name.Length, 1, 96);
        if (string.Equals(culture, "wake_singers", StringComparison.Ordinal))
        {
            Assert.DoesNotContain("-", name, StringComparison.Ordinal);
            if (kind is "settlement" or "region" or "point_of_interest" or "transport" or "monster")
            {
                Assert.False(name.StartsWith("The ", StringComparison.Ordinal), $"Wake-Singer {kind}/{subtype} name used an article: {name}");
            }
        }

        if (string.Equals(culture, "aurora_stack", StringComparison.Ordinal) &&
            kind is "settlement" or "point_of_interest" or "transport" or "monster")
        {
            Assert.Contains("(", name, StringComparison.Ordinal);
            Assert.Contains(")", name, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void IceRemembersProject_ContextArtifactNamesAreAdditive()
    {
        using var stream = File.OpenRead(FindIceRemembersProjectPath());
        var project = NameForgeProject.Load(stream);
        var generator = new NameForgeGenerator(project);

        var result = generator.Generate(new NameForgeGenerateRequest
        {
            CultureId = "aurora_stack",
            Kind = "artifact",
            Subtype = "weapon",
            Count = 40,
            Seed = "context-artifact-additive",
            Context = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["producer_kind"] = "generator",
                ["primary_entity_name"] = "Kaladin",
                ["secondary_entity_name"] = "Brightport",
            },
        });

        Assert.Contains(result.Names, name => name.Contains("Kaladin's", StringComparison.Ordinal));
        Assert.Contains(result.Names, name => !name.Contains("Kaladin's", StringComparison.Ordinal));

        var ungated = generator.Generate(new NameForgeGenerateRequest
        {
            CultureId = "aurora_stack",
            Kind = "artifact",
            Subtype = "weapon",
            Count = 40,
            Seed = "context-artifact-additive",
            Context = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["primary_entity_name"] = "Kaladin",
            },
        });
        Assert.DoesNotContain(ungated.Names, name => name.Contains("Kaladin's", StringComparison.Ordinal));
    }

    [Fact]
    public void IceRemembersProject_WakeSingerContextNamesKeepRomanizedSurface()
    {
        using var stream = File.OpenRead(FindIceRemembersProjectPath());
        var project = NameForgeProject.Load(stream);
        var generator = new NameForgeGenerator(project);

        var result = generator.Generate(new NameForgeGenerateRequest
        {
            CultureId = "wake_singers",
            Kind = "artifact",
            Subtype = "weapon",
            Count = 40,
            Seed = "context-wake-artifact",
            Context = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["producer_kind"] = "generator",
                ["primary_entity_name"] = "Kaladin",
            },
        });

        Assert.Contains(result.Names, name => name.Contains("Kaladin's", StringComparison.Ordinal));
        Assert.All(result.Names, name => Assert.DoesNotContain("-", name, StringComparison.Ordinal));
    }

    private static NameForgeProject CreateTestProject() =>
        new()
        {
            Id = "test",
            Cultures = new Dictionary<string, NameForgeCulture>(StringComparer.Ordinal)
            {
                ["test"] = new()
                {
                    Id = "test",
                    LexemeLists = new Dictionary<string, NameForgeLexemeList>(StringComparer.Ordinal)
                    {
                        ["verb"] = new() { Id = "verb", Entries = ["guard", "watch"] },
                        ["noun"] = new() { Id = "noun", Entries = ["ice", "route"] },
                    },
                    Grammars =
                    [
                        new()
                        {
                            Id = "actor_name",
                            Start = "name",
                            Capitalization = "titleWords",
                            Rules = new Dictionary<string, List<List<string>>>(StringComparer.Ordinal)
                            {
                                ["name"] = [["slot:verb~ing~cap", "slot:noun~poss"]],
                            },
                        },
                    ],
                    Profiles =
                    [
                        new()
                        {
                            Id = "actors",
                            EntityKinds = ["penguin"],
                            StrategyGroups =
                            [
                                new()
                                {
                                    Name = "Actors",
                                    Priority = 10,
                                    Conditions = new NameForgeGroupConditions { EntityKinds = ["penguin"] },
                                    Strategies = [new() { Type = "grammar", Weight = 1, GrammarId = "actor_name" }],
                                },
                            ],
                        },
                    ],
                },
            },
        };

    private static NameForgeProject CreateMarkovProject() =>
        new()
        {
            Id = "markov",
            Cultures = new Dictionary<string, NameForgeCulture>(StringComparer.Ordinal)
            {
                ["test"] = new()
                {
                    Id = "test",
                    Grammars =
                    [
                        new()
                        {
                            Id = "markov_name",
                            Start = "name",
                            Capitalization = "mixed",
                            Rules = new Dictionary<string, List<List<string>>>(StringComparer.Ordinal)
                            {
                                ["name"] = [["markov:test"]],
                            },
                        },
                    ],
                    Profiles =
                    [
                        new()
                        {
                            Id = "actors",
                            EntityKinds = ["penguin"],
                            StrategyGroups =
                            [
                                new()
                                {
                                    Name = "Actors",
                                    Priority = 10,
                                    Conditions = new NameForgeGroupConditions { EntityKinds = ["penguin"] },
                                    Strategies = [new() { Type = "grammar", Weight = 1, GrammarId = "markov_name" }],
                                },
                            ],
                        },
                    ],
                },
            },
        };

    private static NameForgeProject CreateRomanizedProject() =>
        new()
        {
            Id = "romanized",
            Cultures = new Dictionary<string, NameForgeCulture>(StringComparer.Ordinal)
            {
                ["test"] = new()
                {
                    Id = "test",
                    PostProcessing = new NameForgeNamePostProcessing { HyphenRomanization = "join_first_two" },
                    Grammars =
                    [
                        new()
                        {
                            Id = "orca_name",
                            Start = "name",
                            Capitalization = "mixed",
                            Rules = new Dictionary<string, List<List<string>>>(StringComparer.Ordinal)
                            {
                                ["name"] = [["gork-mork-pork"]],
                            },
                        },
                    ],
                    Profiles =
                    [
                        new()
                        {
                            Id = "orcas",
                            EntityKinds = ["orca"],
                            StrategyGroups =
                            [
                                new()
                                {
                                    Name = "Orcas",
                                    Priority = 10,
                                    Conditions = new NameForgeGroupConditions { EntityKinds = ["orca"] },
                                    Strategies = [new() { Type = "grammar", Weight = 1, GrammarId = "orca_name" }],
                                },
                            ],
                        },
                    ],
                },
            },
        };

    private static NameForgeProject CreateContextProject() =>
        new()
        {
            Id = "context",
            Cultures = new Dictionary<string, NameForgeCulture>(StringComparer.Ordinal)
            {
                ["test"] = new()
                {
                    Id = "test",
                    LexemeLists = new Dictionary<string, NameForgeLexemeList>(StringComparer.Ordinal)
                    {
                        ["noun"] = new() { Id = "noun", Entries = ["ice"] },
                    },
                    Grammars = CreateContextGrammars(),
                    Profiles = [CreateContextProfile()],
                },
            },
        };

    private static List<NameForgeGrammar> CreateContextGrammars() =>
        [
            new()
            {
                Id = "context_artifact",
                Start = "name",
                Capitalization = "titleWords",
                Rules = new Dictionary<string, List<List<string>>>(StringComparer.Ordinal)
                {
                    ["name"] = [["context:producer_display_name", "slot:noun"]],
                },
            },
            new()
            {
                Id = "fallback_artifact",
                Start = "name",
                Capitalization = "titleWords",
                Rules = new Dictionary<string, List<List<string>>>(StringComparer.Ordinal)
                {
                    ["name"] = [["slot:noun"]],
                },
            },
        ];

    private static NameForgeProfile CreateContextProfile() =>
        new()
        {
            Id = "artifacts",
            EntityKinds = ["artifact"],
            StrategyGroups =
            [
                new()
                {
                    Name = "Context Artifact",
                    Priority = 20,
                    Conditions = new NameForgeGroupConditions
                    {
                        EntityKinds = ["artifact"],
                        Context = new Dictionary<string, List<string>>(StringComparer.Ordinal)
                        {
                            ["producer"] = ["monster_steals_artifact"],
                        },
                    },
                    Strategies = [new() { Type = "grammar", Weight = 1, GrammarId = "context_artifact" }],
                },
                new()
                {
                    Name = "Fallback Artifact",
                    Priority = 1,
                    Conditions = new NameForgeGroupConditions { EntityKinds = ["artifact"] },
                    Strategies = [new() { Type = "grammar", Weight = 1, GrammarId = "fallback_artifact" }],
                },
            ],
        };

    private static string FindIceRemembersProjectPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "src",
                "IceRemembers",
                "Names",
                "ice-remembers.nameforge.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate Ice Remembers Name Forge project.");
    }

    private sealed class TestMarkovProvider : INameForgeMarkovModelProvider
    {
        public bool TryGetModel(string modelId, out NameForgeMarkovModel model)
        {
            model = new NameForgeMarkovModel
            {
                Order = 1,
                StartStates = new Dictionary<string, double>(StringComparer.Ordinal)
                {
                    ["a"] = 1d,
                },
                Transitions = new Dictionary<string, Dictionary<string, double>>(StringComparer.Ordinal)
                {
                    ["a"] = new(StringComparer.Ordinal) { ["b"] = 1d },
                    ["b"] = new(StringComparer.Ordinal) { ["c"] = 1d },
                    ["c"] = new(StringComparer.Ordinal) { ["d"] = 1d },
                    ["d"] = new(StringComparer.Ordinal) { ["$"] = 1d },
                },
            };

            return string.Equals(modelId, "test", StringComparison.Ordinal);
        }
    }
}
