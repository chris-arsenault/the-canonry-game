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
    public void Generate_WithoutContext_DisablesContextProductionAlternative()
    {
        var generator = new NameForgeGenerator(CreateContextProductionProject());

        var fallbackResult = generator.Generate(new NameForgeGenerateRequest
        {
            CultureId = "test",
            Kind = "artifact",
            Count = 1,
            Seed = "missing-production-context",
        });
        var contextResult = generator.Generate(new NameForgeGenerateRequest
        {
            CultureId = "test",
            Kind = "artifact",
            Count = 1,
            Seed = "missing-production-context",
            Context = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["region"] = "trade terrace",
            },
        });

        Assert.Equal("Ice", fallbackResult.Names[0]);
        Assert.Equal("Trade Terrace Gate", contextResult.Names[0]);
    }

    [Fact]
    public void Generate_WithoutContext_DisablesContextOnlyStrategy()
    {
        var generator = new NameForgeGenerator(CreateContextStrategyProject());

        var fallbackResult = generator.Generate(new NameForgeGenerateRequest
        {
            CultureId = "test",
            Kind = "artifact",
            Count = 4,
            Seed = "missing-strategy-context",
        });
        var contextResult = generator.Generate(new NameForgeGenerateRequest
        {
            CultureId = "test",
            Kind = "artifact",
            Count = 1,
            Seed = "missing-strategy-context",
            Context = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["region"] = "trade terrace",
            },
        });

        Assert.All(fallbackResult.Names, name => Assert.Equal("Ice", name));
        Assert.Equal("Trade Terrace Gate", contextResult.Names[0]);
    }

    [Fact]
    public void Generate_ContextEntityNameTokenPrefersReferenceNameAutomatically()
    {
        var generator = new NameForgeGenerator(CreateReferenceContextProject());

        var result = generator.Generate(new NameForgeGenerateRequest
        {
            CultureId = "test",
            Kind = "artifact",
            Count = 1,
            Seed = "reference-context",
            Context = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["primary_entity_name"] = "Lightbringer Guldan The Spirit Keeper",
                ["primary_entity_reference_name"] = "Guldan",
            },
        });

        Assert.Equal("Guldan's Mace", result.Names[0]);
    }

    [Fact]
    public void Generate_WithGeneratedContext_ReturnsReferenceNameAndUsesItInDisplayName()
    {
        var generator = new NameForgeGenerator(CreateGeneratedContextProject());

        var result = generator.Generate(new NameForgeGenerateRequest
        {
            CultureId = "test",
            Kind = "penguin",
            Count = 1,
            Seed = "generated-context",
        });

        Assert.Equal("Lightbringer Guldan The Spirit Keeper", result.Names[0]);
        Assert.Equal("Guldan", result.ReferenceNames[0]);
    }

    [Fact]
    public void Generate_WithGeneratedHyphenatedContextKey_DoesNotSplitContextKey()
    {
        var generator = new NameForgeGenerator(CreateGeneratedHyphenContextProject());

        var result = generator.Generate(new NameForgeGenerateRequest
        {
            CultureId = "test",
            Kind = "artifact",
            Count = 1,
            Seed = "generated-hyphen-context",
        });

        Assert.Equal("Guldan's Mace", result.Names[0]);
        Assert.Equal("Guldan", result.ReferenceNames[0]);
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
    [InlineData("nightshelf", "settlement", "rookery")]
    [InlineData("nightshelf", "settlement", "colony")]
    [InlineData("nightshelf", "settlement", "holdfast")]
    [InlineData("nightshelf", "settlement", "sanctuary")]
    [InlineData("nightshelf", "settlement", "harbor")]
    [InlineData("nightshelf", "settlement", "reefhold")]
    [InlineData("nightshelf", "region", "face")]
    [InlineData("nightshelf", "region", "shelf")]
    [InlineData("nightshelf", "region", "depths")]
    [InlineData("nightshelf", "region", "reach")]
    [InlineData("nightshelf", "region", "sea")]
    [InlineData("nightshelf", "region", "scar")]
    [InlineData("nightshelf", "point_of_interest", "vent")]
    [InlineData("nightshelf", "point_of_interest", "peak")]
    [InlineData("nightshelf", "point_of_interest", "bloom")]
    [InlineData("nightshelf", "point_of_interest", "shoal")]
    [InlineData("nightshelf", "transport", "trade")]
    [InlineData("nightshelf", "transport", "battleship")]
    [InlineData("nightshelf", "transport", "scout")]
    [InlineData("nightshelf", "transport", "citadel")]
    [InlineData("nightshelf", "monster", "leviathan")]
    [InlineData("nightshelf", "monster", "wraith")]
    [InlineData("nightshelf", "monster", "beast")]
    [InlineData("nightshelf", "monster", "revenant")]
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
        var project = LoadIceRemembersProject();
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

        if (string.Equals(culture, "nightshelf", StringComparison.Ordinal) &&
            kind is "settlement" or "region" or "point_of_interest" or "transport" or "monster")
        {
            Assert.True(HasNightshelfIncompleteMarker(name), $"Nightshelf {kind}/{subtype} name was too complete: {name}");
        }
    }

    [Theory]
    [InlineData("settlement", "rookery", "nightshelf_settlement_rookery")]
    [InlineData("settlement", "colony", "nightshelf_settlement_colony")]
    [InlineData("settlement", "holdfast", "nightshelf_settlement_holdfast")]
    [InlineData("settlement", "sanctuary", "nightshelf_settlement_sanctuary")]
    [InlineData("settlement", "harbor", "nightshelf_settlement_harbor")]
    [InlineData("settlement", "reefhold", "nightshelf_settlement_reefhold")]
    [InlineData("region", "face", "nightshelf_region_face")]
    [InlineData("region", "shelf", "nightshelf_region_shelf")]
    [InlineData("region", "depths", "nightshelf_region_depths")]
    [InlineData("region", "reach", "nightshelf_region_reach")]
    [InlineData("region", "sea", "nightshelf_region_sea")]
    [InlineData("region", "scar", "nightshelf_region_scar")]
    [InlineData("point_of_interest", "vent", "nightshelf_point_vent")]
    [InlineData("point_of_interest", "peak", "nightshelf_point_peak")]
    [InlineData("point_of_interest", "bloom", "nightshelf_point_bloom")]
    [InlineData("point_of_interest", "shoal", "nightshelf_point_shoal")]
    [InlineData("transport", "trade", "nightshelf_transport_trade")]
    [InlineData("transport", "battleship", "nightshelf_transport_battleship")]
    [InlineData("transport", "scout", "nightshelf_transport_scout")]
    [InlineData("transport", "citadel", "nightshelf_transport_citadel")]
    [InlineData("monster", "leviathan", "nightshelf_monster_leviathan")]
    [InlineData("monster", "wraith", "nightshelf_monster_wraith")]
    [InlineData("monster", "beast", "nightshelf_monster_beast")]
    [InlineData("monster", "revenant", "nightshelf_monster_revenant")]
    public void IceRemembersProject_NightshelfUsesSubtypeSpecificIncompleteRoutes(
        string kind,
        string subtype,
        string grammarId)
    {
        var project = LoadIceRemembersProject();
        var generator = new NameForgeGenerator(project);

        var result = generator.Generate(new NameForgeGenerateRequest
        {
            CultureId = "nightshelf",
            Kind = kind,
            Subtype = subtype,
            Count = 1,
            Seed = $"nightshelf-route:{kind}:{subtype}",
        });

        Assert.Equal(grammarId, result.DebugInfo[0].GrammarId);
        Assert.True(HasNightshelfIncompleteMarker(result.Names[0]), $"Nightshelf {kind}/{subtype} name was too complete: {result.Names[0]}");
    }

    [Fact]
    public void IceRemembersProject_ContextArtifactNamesAreAdditive()
    {
        var project = LoadIceRemembersProject();
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
    public void IceRemembersProject_ActorNamesExposeCompactReferenceNames()
    {
        var project = LoadIceRemembersProject();
        var generator = new NameForgeGenerator(project);

        var result = generator.Generate(new NameForgeGenerateRequest
        {
            CultureId = "wake_singers",
            Kind = "orca",
            Subtype = "scout",
            Count = 1,
            Seed = "orca-reference-name",
        });

        Assert.False(string.IsNullOrWhiteSpace(result.ReferenceNames[0]));
        Assert.NotEqual(result.Names[0], result.ReferenceNames[0]);
        Assert.Contains(result.ReferenceNames[0], result.Names[0], StringComparison.Ordinal);
    }

    [Fact]
    public void IceRemembersProject_ArtifactOwnerNamesUseReferenceNameWhenAvailable()
    {
        var project = LoadIceRemembersProject();
        var generator = new NameForgeGenerator(project);
        const string fullOwnerName = "Lightbringer Guldan The Spirit Keeper";

        var result = generator.Generate(new NameForgeGenerateRequest
        {
            CultureId = "aurora_stack",
            Kind = "artifact",
            Subtype = "weapon",
            Count = 80,
            Seed = "reference-owner-artifact",
            Context = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["producer_kind"] = "generator",
                ["primary_entity_name"] = fullOwnerName,
                ["primary_entity_reference_name"] = "Guldan",
            },
        });

        Assert.Contains(result.Names, name => name.Contains("Guldan's", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Names, name => name.Contains(fullOwnerName, StringComparison.Ordinal));
    }

    [Fact]
    public void IceRemembersProject_WakeSingerContextNamesKeepRomanizedSurface()
    {
        var project = LoadIceRemembersProject();
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

    [Fact]
    public void IceRemembersProject_CausalLoopWarNamesUseRaidEndpoints()
    {
        var project = LoadIceRemembersProject();
        var generator = new NameForgeGenerator(project);

        var context = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["producer_kind"] = "generator",
            ["producer"] = "orca_route_wars_get_names",
            ["parent_producer"] = "orca_raids_trade_route",
            ["primary_entity_name"] = "Fallback Orca",
            ["trigger_relationship_source_entity_name"] = "Blackfin Sounder",
            ["trigger_relationship_target_entity_name"] = "High Rookery",
        };

        var result = generator.Generate(new NameForgeGenerateRequest
        {
            CultureId = "aurora_stack",
            Kind = "occurrence",
            Subtype = "war",
            Count = 120,
            Seed = "causal-loop-war",
            Context = context,
        });

        Assert.Contains(result.Names, name =>
            name.Contains("Blackfin Sounder", StringComparison.Ordinal) &&
            name.Contains("High Rookery", StringComparison.Ordinal));

        context.Remove("parent_producer");
        var ungated = generator.Generate(new NameForgeGenerateRequest
        {
            CultureId = "aurora_stack",
            Kind = "occurrence",
            Subtype = "war",
            Count = 80,
            Seed = "causal-loop-war",
            Context = context,
        });

        Assert.DoesNotContain(ungated.Names, name => name.Contains("High Rookery", StringComparison.Ordinal));
    }

    [Fact]
    public void IceRemembersProject_CausalLoopTheftNamesUseMonsterAndArtifact()
    {
        var project = LoadIceRemembersProject();
        var generator = new NameForgeGenerator(project);

        var result = generator.Generate(new NameForgeGenerateRequest
        {
            CultureId = "aurora_stack",
            Kind = "penguin",
            Subtype = "warden",
            Count = 120,
            Seed = "causal-loop-theft",
            Context = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["producer_kind"] = "generator",
                ["producer"] = "stolen_artifacts_draw_hunters",
                ["parent_producer"] = "monster_steals_artifact",
                ["primary_entity_name"] = "Glass Maw",
                ["trigger_relationship_source_entity_name"] = "Glass Maw",
                ["trigger_relationship_target_entity_name"] = "Storm Chalice",
            },
        });

        Assert.Contains(result.Names, name => name.Contains("Storm Chalice", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Names, name => name.Contains("Glass Maw", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void IceRemembersProject_CausalLoopFestivalNamesUseSlayerAndMonster()
    {
        var project = LoadIceRemembersProject();
        var generator = new NameForgeGenerator(project);

        var result = generator.Generate(new NameForgeGenerateRequest
        {
            CultureId = "aurora_stack",
            Kind = "occurrence",
            Subtype = "festival",
            Count = 120,
            Seed = "causal-loop-festival",
            Context = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["producer_kind"] = "generator",
                ["producer"] = "monster_slayings_become_festivals",
                ["parent_producer"] = "penguin_slays_monster",
                ["primary_entity_name"] = "Kaladin",
                ["trigger_relationship_source_entity_name"] = "Kaladin",
                ["trigger_relationship_target_entity_name"] = "Glass Maw",
            },
        });

        Assert.Contains(result.Names, name => name.Contains("Kaladin's", StringComparison.Ordinal));
        Assert.Contains(result.Names, name => name.Contains("Glass Maw", StringComparison.Ordinal));
    }

    [Fact]
    public void IceRemembersProject_CausalLoopTradeTransportNamesUseRouteEndpoints()
    {
        var project = LoadIceRemembersProject();
        var generator = new NameForgeGenerator(project);

        var result = generator.Generate(new NameForgeGenerateRequest
        {
            CultureId = "aurora_stack",
            Kind = "transport",
            Subtype = "trade",
            Count = 80,
            Seed = "causal-loop-trade-transport",
            Context = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["producer_kind"] = "generator",
                ["producer"] = "trade_routes_convene_negotiation",
                ["primary_entity_name"] = "Rimewake",
                ["trigger_relationship_source_entity_name"] = "Rimewake",
                ["trigger_relationship_target_entity_name"] = "Pearl Harbor",
            },
        });

        Assert.Contains(result.Names, name =>
            name.Contains("Rimewake", StringComparison.Ordinal) &&
            name.Contains("Pearl Harbor", StringComparison.Ordinal));

        var nightshelf = generator.Generate(new NameForgeGenerateRequest
        {
            CultureId = "nightshelf",
            Kind = "transport",
            Subtype = "trade",
            Count = 80,
            Seed = "causal-loop-nightshelf-trade-transport",
            Context = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["producer_kind"] = "generator",
                ["producer"] = "trade_routes_convene_negotiation",
                ["primary_entity_name"] = "Rimewake",
                ["trigger_relationship_source_entity_name"] = "Rimewake",
                ["trigger_relationship_target_entity_name"] = "Pearl Harbor",
            },
        });

        Assert.Contains(nightshelf.Names, name =>
            name.Contains("Rimewake", StringComparison.Ordinal) &&
            name.Contains("Pearl Harbor", StringComparison.Ordinal));
        Assert.All(nightshelf.Names, name => Assert.True(HasNightshelfIncompleteMarker(name), $"Nightshelf trade transport name was too complete: {name}"));
    }

    [Fact]
    public void IceRemembersProject_WakeSingerCausalLoopNamesKeepRomanizedSurface()
    {
        var project = LoadIceRemembersProject();
        var generator = new NameForgeGenerator(project);

        var result = generator.Generate(new NameForgeGenerateRequest
        {
            CultureId = "wake_singers",
            Kind = "transport",
            Subtype = "trade",
            Count = 80,
            Seed = "causal-loop-wake-trade",
            Context = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["producer_kind"] = "generator",
                ["producer"] = "trade_routes_convene_negotiation",
                ["primary_entity_name"] = "Tidehome",
                ["trigger_relationship_source_entity_name"] = "Tidehome",
                ["trigger_relationship_target_entity_name"] = "Moon Rookery",
            },
        });

        Assert.Contains(result.Names, name =>
            name.Contains("Tidehome", StringComparison.Ordinal) &&
            name.Contains("Moon Rookery", StringComparison.Ordinal));
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

    private static NameForgeProject CreateContextProductionProject() =>
        CreateSingleProfileProject(
            "context-production",
            [
                new()
                {
                    Id = "context_name",
                    Start = "name",
                    Capitalization = "titleWords",
                    Rules = new Dictionary<string, List<List<string>>>(StringComparer.Ordinal)
                    {
                        ["name"] = [["context:region", "gate"], ["ice"]],
                    },
                },
            ],
            [
                new() { Type = "grammar", Weight = 1, GrammarId = "context_name" },
            ]);

    private static NameForgeProject CreateContextStrategyProject() =>
        CreateSingleProfileProject(
            "context-strategy",
            [
                new()
                {
                    Id = "context_name",
                    Start = "name",
                    Capitalization = "titleWords",
                    Rules = new Dictionary<string, List<List<string>>>(StringComparer.Ordinal)
                    {
                        ["name"] = [["context:region", "gate"]],
                    },
                },
                new()
                {
                    Id = "fallback_name",
                    Start = "name",
                    Capitalization = "titleWords",
                    Rules = new Dictionary<string, List<List<string>>>(StringComparer.Ordinal)
                    {
                        ["name"] = [["ice"]],
                    },
                },
            ],
            [
                new() { Type = "grammar", Weight = 1, GrammarId = "context_name" },
                new() { Type = "grammar", Weight = 0, GrammarId = "fallback_name" },
            ]);

    private static NameForgeProject CreateReferenceContextProject() =>
        CreateSingleProfileProject(
            "reference-context",
            [
                new()
                {
                    Id = "owner_artifact",
                    Start = "name",
                    Capitalization = "titleWords",
                    Rules = new Dictionary<string, List<List<string>>>(StringComparer.Ordinal)
                    {
                        ["name"] = [["context:primary_entity_name~poss", "mace"]],
                    },
                },
            ],
            [
                new() { Type = "grammar", Weight = 1, GrammarId = "owner_artifact" },
            ]);

    private static NameForgeProject CreateSingleProfileProject(
        string id,
        List<NameForgeGrammar> grammars,
        List<NameForgeStrategy> strategies) =>
        new()
        {
            Id = id,
            Cultures = new Dictionary<string, NameForgeCulture>(StringComparer.Ordinal)
            {
                ["test"] = new()
                {
                    Id = "test",
                    Grammars = grammars,
                    Profiles =
                    [
                        new()
                        {
                            Id = "profile",
                            EntityKinds = ["artifact"],
                            StrategyGroups =
                            [
                                new()
                                {
                                    Name = "Names",
                                    Priority = 1,
                                    Conditions = new NameForgeGroupConditions { EntityKinds = ["artifact"] },
                                    Strategies = strategies,
                                },
                            ],
                        },
                    ],
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

    private static NameForgeProject CreateGeneratedContextProject() =>
        CreateGeneratedContextProject("penguin", "context_actor", new()
        {
            Id = "context_actor",
            Start = "name",
            Capitalization = "titleWords",
            GeneratedContext = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["reference_name"] = "reference_name",
            },
            Rules = new Dictionary<string, List<List<string>>>(StringComparer.Ordinal)
            {
                ["reference_name"] = [["guldan"]],
                ["name"] = [["lightbringer", "context:reference_name", "the", "spirit keeper"]],
            },
        });

    private static NameForgeProject CreateGeneratedHyphenContextProject() =>
        CreateGeneratedContextProject("artifact", "context_artifact", new()
        {
            Id = "context_artifact",
            Start = "name",
            Capitalization = "titleWords",
            GeneratedContext = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["reference_name"] = "reference_name",
                ["inherit-name"] = "reference_name",
            },
            Rules = new Dictionary<string, List<List<string>>>(StringComparer.Ordinal)
            {
                ["reference_name"] = [["guldan"]],
                ["name"] = [["context:inherit-name~poss", "mace"]],
            },
        });

    private static NameForgeProject CreateGeneratedContextProject(string kind, string grammarId, NameForgeGrammar grammar) =>
        new()
        {
            Id = "generated-context",
            Cultures = new Dictionary<string, NameForgeCulture>(StringComparer.Ordinal)
            {
                ["test"] = new()
                {
                    Id = "test",
                    Grammars = [grammar],
                    Profiles =
                    [
                        new()
                        {
                            Id = "profile",
                            EntityKinds = [kind],
                            StrategyGroups =
                            [
                                new()
                                {
                                    Name = "Generated Context",
                                    Priority = 10,
                                    Conditions = new NameForgeGroupConditions { EntityKinds = [kind] },
                                    Strategies = [new() { Type = "grammar", Weight = 1, GrammarId = grammarId }],
                                },
                            ],
                        },
                    ],
                },
            },
        };

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

    private static bool HasNightshelfIncompleteMarker(string name) =>
        name.Contains('~', StringComparison.Ordinal) ||
        name.Contains('\u263D', StringComparison.Ordinal) ||
        name.Contains('\u2234', StringComparison.Ordinal);

    private static NameForgeProject LoadIceRemembersProject() =>
        NameForgeProject.LoadFile(FindIceRemembersProjectPath());

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
