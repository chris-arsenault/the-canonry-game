namespace Engine.Tests.Domain.Graph;

using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Domain.Loader;
using Engine.Domain.IceRemembers;
using Engine.Graph;
using Engine.Kinds;
using Engine.Map;

public class GeneratorCatalogInvariantTests
{
    [Fact]
    public void Validate_IceRemembersGeneratorCatalog_IsReachableFromSeedClosure()
    {
        var issues = FindSeedReachabilityIssues(IceRemembersDefinition.Create().Semantics!.Generators);

        Assert.Empty(issues);
    }

    [Fact]
    public void Validate_GeneratorReachability_AllowsGeneratorUnlockedByAnotherGenerator()
    {
        var founder = new SemanticGeneratorConfig
        {
            Id = "founds_reefhold",
            Name = "Founds Reefhold",
            Target = new SemanticGeneratorTarget
            {
                Kind = TestKinds.Settlement,
            },
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
            Triggers =
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = TestKinds.Settlement,
                    AttributeName = "shelter",
                    Direction = TriggerDirection.High,
                },
            ],
        };
        var dependent = new SemanticGeneratorConfig
        {
            Id = "reefhold_appoints_keeper",
            Name = "Reefhold Appoints Keeper",
            Target = new SemanticGeneratorTarget
            {
                Kind = TestKinds.Settlement,
                Subtypes = new HashSet<string>(StringComparer.Ordinal) { "reefhold" },
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
                    AttributeName = "shelter",
                    Direction = TriggerDirection.High,
                },
            ],
        };

        var issues = FindSeedReachabilityIssues([founder, dependent]);

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
                Subtypes = new HashSet<string>(StringComparer.Ordinal) { "sanctuary" },
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
                    AttributeName = "shelter",
                    Direction = TriggerDirection.High,
                },
            ],
        };

        var issues = FindSeedReachabilityIssues([unreachable]);

        Assert.Single(issues);
        Assert.Contains("requires target subtype", issues[0], StringComparison.Ordinal);
        Assert.Contains("sanctuary", issues[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_GeneratorReachability_FailsOnClosedSubtypeCycle()
    {
        var a = new SemanticGeneratorConfig
        {
            Id = "sanctuary_names_keeper",
            Name = "Sanctuary Names Keeper",
            Target = new SemanticGeneratorTarget
            {
                Kind = TestKinds.Settlement,
                Subtypes = new HashSet<string>(StringComparer.Ordinal) { "sanctuary" },
            },
            Produces =
            [
                new SemanticProducedEntity
                {
                    Ref = "$scholar",
                    Kind = IceRemembersKinds.Penguin,
                    Subtype = "ruler",
                    Role = "reader",
                    InheritCultureFrom = "$target",
                },
            ],
            Relationships =
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$scholar",
                    DestinationRef = "$target",
                    Kind = "home_base",
                },
            ],
            Triggers =
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = TestKinds.Settlement,
                    AttributeName = "shelter",
                    Direction = TriggerDirection.High,
                },
            ],
        };
        var b = new SemanticGeneratorConfig
        {
            Id = "witness_raises_sanctuary",
            Name = "Witness Raises Sanctuary",
            Target = new SemanticGeneratorTarget
            {
                Kind = TestKinds.Penguin,
                Subtypes = new HashSet<string>(StringComparer.Ordinal) { "ruler" },
            },
            Produces =
            [
                new SemanticProducedEntity
                {
                    Ref = "$sanctuary",
                    Kind = TestKinds.Settlement,
                    Subtype = "sanctuary",
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
                    AttributeName = "echo",
                    Direction = TriggerDirection.High,
                },
            ],
        };

        var issues = FindSeedReachabilityIssues([a, b]);

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
                    AttributeName = "cohesion",
                    Direction = TriggerDirection.Low,
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
    public void Validate_GeneratorTriggerAttributeMissingOnKind_ThrowsActionableMessage()
    {
        var generator = CreateBaseGenerator(
            triggers:
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = TestKinds.Settlement,
                    AttributeName = "missing_attribute",
                    Direction = TriggerDirection.Low,
                },
            ]);

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(WithGenerators(generator)));

        Assert.Contains("unknown attribute 'missing_attribute'", ex.Message, StringComparison.Ordinal);
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
                AllowWilderness = true,
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
                    AttributeName = "pristiness",
                    Direction = TriggerDirection.High,
                },
            ],
        };

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(WithGenerators(generator)));

        Assert.Contains("produces '$artifact' but no relationship references it", ex.Message, StringComparison.Ordinal);
    }

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
                    AttributeName = "shelter",
                    Direction = TriggerDirection.Low,
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
            Processes = config.Processes,
            Relationships = config.Relationships,
            Pressures = config.Pressures,
            Lifecycles = config.Lifecycles,
            Resources = config.Resources,
            Cultures = config.Cultures,
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
        var seedFacts = ReachabilityFacts.FromSeed(TestMapBuilder.BuildDefault(seed: 42).Graph);
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
            var required = string.Join(", ", generator.Target.Subtypes.OrderBy(value => value, StringComparer.Ordinal));
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
                var required = string.Join(", ", secondaryTarget.Query.Subtypes.OrderBy(value => value, StringComparer.Ordinal));
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
        public static ReachabilityFacts FromSeed(WorldGraph graph)
        {
            var kinds = new HashSet<EntityKind>();
            var subtypes = new Dictionary<EntityKind, HashSet<string>>();
            var relationshipKinds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var entity in graph.AllEntities.Values.Where(entity => entity.Status == EntityStatus.Active))
            {
                kinds.Add(entity.Kind);
                if (!subtypes.TryGetValue(entity.Kind, out var values))
                {
                    values = new HashSet<string>(StringComparer.Ordinal);
                    subtypes[entity.Kind] = values;
                }

                values.Add(entity.Subtype);
            }

            foreach (var entity in graph.AllEntities.Values.Where(entity => entity.Status == EntityStatus.Active))
            {
                foreach (var relationship in graph.OutgoingRelationships(entity.Id))
                {
                    relationshipKinds.Add(relationship.Kind);
                }
            }

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
