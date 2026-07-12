namespace Engine.Domain.Tests.Domain.Graph;

using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Domain.Loader;
using Engine.Domain.Startup;
using Engine.Kinds;
using Engine.WorldGen;

using IceRemembers;
using IceRemembers.Seed;

public class GeneratorCatalogInvariantTests
{
    [Fact]
    public void Validate_IceRemembersGeneratorCatalog_IsReachableFromSeedClosure()
    {
        var issues = FindSeedReachabilityIssues(IceRemembersDefinition.Create().Semantics!.Generators);

        Assert.Empty(issues);
    }

    [Fact]
    public void Validate_IceRemembersGeneratorCatalog_DeclaresCausalLimits()
    {
        var issues = new List<string>();

        foreach (var generator in IceRemembersDefinition.Create().Semantics!.Generators)
        {
            if (string.IsNullOrWhiteSpace(generator.CausalLimit))
            {
                issues.Add(
                    $"{generator.Id} must declare CausalLimit. " +
                    "See docs/systems/20-generator-causal-limits.md.");
            }
        }

        Assert.Empty(issues);
    }

    [Fact]
    public void Validate_GeneratorReachability_AllowsGeneratorUnlockedByAnotherGenerator()
    {
        var issues = FindSeedReachabilityIssues(
            [
                CreateFounderGenerator(),
                CreateReefholdKeeperGenerator(),
            ]);

        Assert.Empty(issues);
    }

    [Fact]
    public void Validate_GeneratorReachability_FailsWhenTargetSubtypeIsNeverSeededOrProduced()
    {
        var unreachable = new SemanticGeneratorConfig
        {
            Id = "sanctuary_appoints_keeper",
            Name = "Sanctuary Appoints Keeper",
            Target = new SemanticGeneratorTarget
            {
                Kind = TestKinds.Settlement,
                Subtypes = new HashSet<string>(StringComparer.Ordinal) { "never_seeded_sanctuary" },
            },
            Produces =
            [
                new SemanticProducedEntity
                {
                    Ref = "$keeper",
                    Kind = IceRemembersKinds.Penguin,
                    Subtype = "warden",
                    Role = "keeper",
                    InheritCultureFrom = "$target",
                },
            ],
            Relationships =
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$keeper",
                    DestinationRef = "$target",
                    Kind = "home_base",
                },
            ],
            Triggers =
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = TestKinds.Settlement,
                },
            ],
        };

        var issues = FindSeedReachabilityIssues([unreachable]);

        Assert.Single(issues);
        Assert.Contains("requires target subtype", issues[0], StringComparison.Ordinal);
        Assert.Contains("never_seeded_sanctuary", issues[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_GeneratorReachability_FailsOnClosedSubtypeCycle()
    {
        var issues = FindSeedReachabilityIssues(
            [
                CreateSanctuaryNamesKeeperGenerator(),
                CreateWitnessRaisesSanctuaryGenerator(),
            ]);

        Assert.Equal(2, issues.Count);
        Assert.All(issues, issue => Assert.Contains("seed or guaranteed generator output never provides it", issue, StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_GeneratorProducedRefWithoutRelationship_ThrowsActionableMessage()
    {
        var generator = CreateBaseGenerator(relationships: []);

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(WithGenerators(generator)));

        Assert.Contains("produces '$hero' but no relationship references it", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_GeneratorTriggerKindMismatch_ThrowsActionableMessage()
    {
        var generator = CreateBaseGenerator(
            triggers:
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = TestKinds.Faction,
                },
            ]);

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(WithGenerators(generator)));

        Assert.Contains("does not match trigger kind", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_GeneratorTargetSubtypeUnknown_ThrowsActionableMessage()
    {
        var generator = CreateBaseGenerator(
            target: new SemanticGeneratorTarget
            {
                Kind = TestKinds.Settlement,
                Subtypes = new HashSet<string>(StringComparer.Ordinal) { "missing_subtype" },
            });

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(WithGenerators(generator)));

        Assert.Contains("target references unknown subtype 'missing_subtype'", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_OptionalProduceWithTooManyRelationshipAnchors_ThrowsActionableMessage()
    {
        var generator = CreateBaseGenerator(
            produces:
            [
                new SemanticProducedEntity
                {
                    Ref = "$hero",
                    Kind = IceRemembersKinds.Penguin,
                    Subtype = "warden",
                    Role = "hero",
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$artifact",
                    Kind = TestKinds.Artifact,
                    Subtype = "weapon",
                    Role = "weapon",
                    CreateChance = 0.25f,
                    InheritCultureFrom = "$target",
                },
            ],
            relationships:
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$hero",
                    DestinationRef = "$target",
                    Kind = "home_base",
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$artifact",
                    DestinationRef = "$target",
                    Kind = "anchored",
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$artifact",
                    DestinationRef = "$hero",
                    Kind = "created_by",
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$artifact",
                    DestinationRef = "$hero",
                    Kind = "cleansed_by",
                },
            ]);

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(WithGenerators(generator)));

        Assert.Contains("optional produce '$artifact' is referenced by 3 relationships", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_ProbabilisticProduceWithRequiredRelationshipToMandatoryProduce_IsAllowed()
    {
        var generator = CreateBaseGenerator(
            produces:
            [
                new SemanticProducedEntity
                {
                    Ref = "$hero",
                    Kind = IceRemembersKinds.Penguin,
                    Subtype = "warden",
                    Role = "hero",
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$artifact",
                    Kind = TestKinds.Artifact,
                    Subtype = "weapon",
                    Role = "weapon",
                    CreateChance = 0.25f,
                    InheritCultureFrom = "$hero",
                },
            ],
            relationships:
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$hero",
                    DestinationRef = "$target",
                    Kind = "home_base",
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$artifact",
                    DestinationRef = "$hero",
                    Kind = "created_by",
                },
            ]);

        var graph = DomainLoader.BuildDefinitionGraph(WithGenerators(generator));

        Assert.Contains("test_penguin_package", graph.Generators.Keys);
    }

    [Fact]
    public void Validate_CyclicInheritCultureFrom_ThrowsActionableMessage()
    {
        var generator = CreateBaseGenerator(
            produces:
            [
                new SemanticProducedEntity
                {
                    Ref = "$hero",
                    Kind = IceRemembersKinds.Penguin,
                    Subtype = "warden",
                    Role = "hero",
                    InheritCultureFrom = "$rival",
                },
                new SemanticProducedEntity
                {
                    Ref = "$rival",
                    Kind = IceRemembersKinds.Penguin,
                    Subtype = "exile",
                    Role = "rival",
                    InheritCultureFrom = "$hero",
                },
            ],
            relationships:
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$hero",
                    DestinationRef = "$rival",
                    Kind = "rival_of",
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$rival",
                    DestinationRef = "$hero",
                    Kind = "rival_of",
                },
            ]);

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(WithGenerators(generator)));

        Assert.Contains("cyclic InheritCultureFrom chain", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_GeneratorWithoutTriggers_ThrowsActionableMessage()
    {
        var generator = CreateBaseGenerator(triggers: []);

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(WithGenerators(generator)));

        Assert.Contains("must declare at least one trigger", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_ArtifactDiscoveryWithoutRelationships_ThrowsActionableMessage()
    {
        var generator = new SemanticGeneratorConfig
        {
            Id = "test_artifact_discovery",
            Name = "Test Artifact Discovery",
            Target = new SemanticGeneratorTarget
            {
                Kind = TestKinds.Region,
            },
            Produces =
            [
                new SemanticProducedEntity
                {
                    Ref = "$artifact",
                    Kind = TestKinds.Artifact,
                    Subtype = "instrument",
                    Role = "discovered",
                    InheritCultureFrom = "$target",
                },
            ],
            Relationships = [],
            Triggers =
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = TestKinds.Region,
                },
            ],
        };

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(WithGenerators(generator)));

        Assert.Contains("produces '$artifact' but no relationship references it", ex.Message, StringComparison.Ordinal);
    }

    private static SemanticGeneratorConfig CreateFounderGenerator() =>
        new()
        {
            Id = "founds_reefhold",
            Name = "Founds Reefhold",
            Target = new SemanticGeneratorTarget { Kind = TestKinds.Settlement },
            Produces =
            [
                new SemanticProducedEntity
                {
                    Ref = "$reefhold",
                    Kind = TestKinds.Settlement,
                    Subtype = "reefhold",
                    Role = "new_site",
                    InheritCultureFrom = "$target",
                },
            ],
            Relationships =
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$reefhold",
                    DestinationRef = "$target",
                    Kind = "founded_from",
                },
            ],
            Triggers = [HighSettlementHealthTrigger()],
        };

    private static SemanticGeneratorConfig CreateReefholdKeeperGenerator() =>
        CreateSubtypeKeeperGenerator(
            "reefhold_appoints_keeper",
            "Reefhold Appoints Keeper",
            "reefhold",
            "$keeper",
            "warden",
            "keeper");

    private static SemanticGeneratorConfig CreateSanctuaryNamesKeeperGenerator() =>
        CreateSubtypeKeeperGenerator(
            "sanctuary_names_keeper",
            "Sanctuary Names Keeper",
            "cycle_sanctuary",
            "$scholar",
            "cycle_keeper",
            "reader");

    private static SemanticGeneratorConfig CreateSubtypeKeeperGenerator(
        string id,
        string name,
        string targetSubtype,
        string producedRef,
        string producedSubtype,
        string producedRole) =>
        new()
        {
            Id = id,
            Name = name,
            Target = new SemanticGeneratorTarget
            {
                Kind = TestKinds.Settlement,
                Subtypes = new HashSet<string>(StringComparer.Ordinal) { targetSubtype },
            },
            Produces =
            [
                new SemanticProducedEntity
                {
                    Ref = producedRef,
                    Kind = IceRemembersKinds.Penguin,
                    Subtype = producedSubtype,
                    Role = producedRole,
                    InheritCultureFrom = "$target",
                },
            ],
            Relationships =
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = producedRef,
                    DestinationRef = "$target",
                    Kind = "home_base",
                },
            ],
            Triggers = [HighSettlementHealthTrigger()],
        };

    private static SemanticGeneratorConfig CreateWitnessRaisesSanctuaryGenerator() =>
        new()
        {
            Id = "witness_raises_sanctuary",
            Name = "Witness Raises Sanctuary",
            Target = new SemanticGeneratorTarget
            {
                Kind = TestKinds.Penguin,
                Subtypes = new HashSet<string>(StringComparer.Ordinal) { "cycle_keeper" },
            },
            Produces =
            [
                new SemanticProducedEntity
                {
                    Ref = "$sanctuary",
                    Kind = TestKinds.Settlement,
                    Subtype = "cycle_sanctuary",
                    Role = "new_site",
                    InheritCultureFrom = "$target",
                },
            ],
            Relationships =
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$sanctuary",
                    DestinationRef = "$target",
                    Kind = "founded_from",
                },
            ],
            Triggers =
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = TestKinds.Penguin,
                },
            ],
        };

    private static SemanticGeneratorTrigger HighSettlementHealthTrigger() =>
        new()
        {
            OnKind = TestKinds.Settlement,
        };

    private static SemanticGeneratorConfig CreateBaseGenerator(
        SemanticGeneratorTarget? target = null,
        IReadOnlyList<SemanticProducedEntity>? produces = null,
        IReadOnlyList<SemanticGeneratorRelationship>? relationships = null,
        IReadOnlyList<SemanticGeneratorTrigger>? triggers = null) =>
        new()
        {
            Id = "test_penguin_package",
            Name = "Hero Emergence",
            Target = target ?? new SemanticGeneratorTarget
            {
                Kind = TestKinds.Settlement,
            },
            Produces = produces ??
            [
                new SemanticProducedEntity
                {
                    Ref = "$hero",
                    Kind = IceRemembersKinds.Penguin,
                    Subtype = "warden",
                    Role = "hero",
                    InheritCultureFrom = "$target",
                },
            ],
            Relationships = relationships ??
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$hero",
                    DestinationRef = "$target",
                    Kind = "home_base",
                },
            ],
            Triggers = triggers ??
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = TestKinds.Settlement,
                },
            ],
        };

    private static DomainConfig WithGenerators(params SemanticGeneratorConfig[] generators)
    {
        var config = IceRemembersDefinition.Create();
        var semantics = config.Semantics!;

        return new DomainConfig
        {
            Kinds = config.Kinds,
            Terrains = config.Terrains,
            TravelStructures = config.TravelStructures,
            Gambits = config.Gambits,
            Relationships = config.Relationships,
            Lifecycles = config.Lifecycles,
            Cultures = config.Cultures,
            CulturePower = config.CulturePower,
            Tags = config.Tags,
            MovementProfiles = config.MovementProfiles,
            Gameplay = config.Gameplay,
            Semantics = new SemanticDomainConfig
            {
                Stocks = semantics.Stocks,
                Verbs = semantics.Verbs,
                Flows = semantics.Flows,
                Modifiers = semantics.Modifiers,
                Generators = generators,
            },
        };
    }

    private static List<string> FindSeedReachabilityIssues(IReadOnlyList<SemanticGeneratorConfig> generators)
    {
        var seedFacts = ReachabilityFacts.FromSeed(IceRemembersSeedBuilder.BuildDefault(seed: 42));
        var available = seedFacts;
        var remaining = generators.ToDictionary(generator => generator.Id, StringComparer.Ordinal);

        while (true)
        {
            var unlocked = remaining.Values
                .Where(generator => IsReachable(generator, available))
                .ToList();

            if (unlocked.Count == 0)
            {
                break;
            }

            foreach (var generator in unlocked)
            {
                remaining.Remove(generator.Id);
                available = available.WithGuaranteedOutputs(generator);
            }
        }

        return remaining.Values
            .Select(generator => DescribeUnreachable(generator, available))
            .ToList();
    }

    private static bool IsReachable(SemanticGeneratorConfig generator, ReachabilityFacts facts)
    {
        if (!facts.Kinds.Contains(generator.Target.Kind))
        {
            return false;
        }

        if (generator.Target.Subtypes is { Count: > 0 } &&
            !facts.HasAnySubtype(generator.Target.Kind, generator.Target.Subtypes))
        {
            return false;
        }

        foreach (var secondaryTarget in generator.SecondaryTargets)
        {
            if (!facts.Kinds.Contains(secondaryTarget.Kind))
            {
                return false;
            }

            if (secondaryTarget.Query?.Subtypes is { Count: > 0 } &&
                !facts.HasAnySubtype(secondaryTarget.Kind, secondaryTarget.Query.Subtypes))
            {
                return false;
            }
        }

        return true;
    }

    private static string DescribeUnreachable(SemanticGeneratorConfig generator, ReachabilityFacts facts)
    {
        if (!facts.Kinds.Contains(generator.Target.Kind))
        {
            return
                $"Generator '{generator.Id}' is unreachable: requires target kind '{generator.Target.Kind}' but seed or guaranteed generator output never provides it.";
        }

        if (generator.Target.Subtypes is { Count: > 0 } && !facts.HasAnySubtype(generator.Target.Kind, generator.Target.Subtypes))
        {
            var required = string.Join(", ", generator.Target.Subtypes.Order(StringComparer.Ordinal));
            return
                $"Generator '{generator.Id}' is unreachable: requires target subtype(s) [{required}] on kind '{generator.Target.Kind}', but seed or guaranteed generator output never provides it.";
        }

        foreach (var secondaryTarget in generator.SecondaryTargets)
        {
            if (!facts.Kinds.Contains(secondaryTarget.Kind))
            {
                return
                    $"Generator '{generator.Id}' is unreachable: requires secondary target kind '{secondaryTarget.Kind}' but seed or guaranteed generator output never provides it.";
            }

            if (secondaryTarget.Query?.Subtypes is { Count: > 0 } &&
                !facts.HasAnySubtype(secondaryTarget.Kind, secondaryTarget.Query.Subtypes))
            {
                var required = string.Join(", ", secondaryTarget.Query.Subtypes.Order(StringComparer.Ordinal));
                return
                    $"Generator '{generator.Id}' is unreachable: requires secondary target subtype(s) [{required}] on kind '{secondaryTarget.Kind}', but seed or guaranteed generator output never provides it.";
            }
        }

        return $"Generator '{generator.Id}' is unreachable from seed closure.";
    }

    private sealed record ReachabilityFacts(
        HashSet<EntityKind> Kinds,
        Dictionary<EntityKind, HashSet<string>> Subtypes,
        HashSet<string> RelationshipKinds)
    {
        public static ReachabilityFacts FromSeed(DomainStartupSeed seed)
        {
            var kinds = new HashSet<EntityKind>();
            var subtypes = new Dictionary<EntityKind, HashSet<string>>();
            var relationshipKinds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var entity in seed.Entities)
            {
                var kind = EntityKind.From(entity.Kind);
                kinds.Add(kind);
                if (!subtypes.TryGetValue(kind, out var values))
                {
                    values = new HashSet<string>(StringComparer.Ordinal);
                    subtypes[kind] = values;
                }

                values.Add(entity.Subtype);
            }

            relationshipKinds.UnionWith(seed.Relationships.Select(relationship => relationship.RelationshipKind));

            return new ReachabilityFacts(kinds, subtypes, relationshipKinds);
        }

        public bool HasAnySubtype(EntityKind kind, IEnumerable<string> requiredSubtypes)
        {
            return Subtypes.TryGetValue(kind, out var values) &&
                   requiredSubtypes.Any(values.Contains);
        }

        public ReachabilityFacts WithGuaranteedOutputs(SemanticGeneratorConfig generator)
        {
            var kinds = new HashSet<EntityKind>(Kinds);
            var subtypes = Subtypes.ToDictionary(
                pair => pair.Key,
                pair => new HashSet<string>(pair.Value, StringComparer.Ordinal));
            var relationshipKinds = new HashSet<string>(RelationshipKinds, StringComparer.Ordinal);

            var guaranteedRefs = new HashSet<string>(StringComparer.Ordinal) { "$target" };
            foreach (var secondaryTarget in generator.SecondaryTargets)
            {
                guaranteedRefs.Add(secondaryTarget.Ref);
            }

            foreach (var produced in generator.Produces.Where(produced => produced.CreateChance >= 1f && produced.CreateIfMissing))
            {
                guaranteedRefs.Add(produced.Ref);
                kinds.Add(produced.Kind);
                if (!subtypes.TryGetValue(produced.Kind, out var values))
                {
                    values = new HashSet<string>(StringComparer.Ordinal);
                    subtypes[produced.Kind] = values;
                }

                if (!string.IsNullOrWhiteSpace(produced.Subtype))
                {
                    values.Add(produced.Subtype);
                }

                if (produced.Subtypes is { Count: > 0 })
                {
                    values.UnionWith(produced.Subtypes);
                }
            }

            foreach (var relationship in generator.Relationships)
            {
                if (guaranteedRefs.Contains(relationship.SourceRef) &&
                    guaranteedRefs.Contains(relationship.DestinationRef))
                {
                    relationshipKinds.Add(relationship.Kind);
                }
            }

            return new ReachabilityFacts(kinds, subtypes, relationshipKinds);
        }
    }
}
