namespace Engine.Tests.Generators;

using Engine.Domain.Config;
using Engine.Domain.Loader;
using Engine.Domain.IceRemembers;
using Engine.Eventing;
using Engine.Generators;
using Engine.Graph;
using Engine.Kinds;
using Engine.Map;
using Engine.Simulation;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1030:Use events where appropriate",
    Justification = "Test names intentionally mirror the ticket acceptance criteria.")]
public class GeneratorSystemTests
{
    [Fact]
    public void Tick_BeforePulseInterval_DoesNothing()
    {
        var (sim, system) = CreateSystem(new Random(42), CreateGenerator("hero_emergence"));

        system.Tick(5, new HashSet<string>(StringComparer.Ordinal) { "hero_emergence" });

        var instance = sim.WorldState.Generators.ById["hero_emergence"];
        Assert.Equal(0, instance.TotalRuns);
        Assert.Null(instance.LastFiredTick);
    }

    [Fact]
    public void Tick_AtPulse_NoActiveGenerators_DoesNothing()
    {
        var (sim, system) = CreateSystem(new Random(42), CreateGenerator("hero_emergence"));

        system.Tick(25, new HashSet<string>(StringComparer.Ordinal));

        var instance = sim.WorldState.Generators.ById["hero_emergence"];
        Assert.Equal(0, instance.TotalRuns);
        Assert.Null(instance.LastFiredTick);
    }

    [Fact]
    public void Tick_AtPulse_TriggersGateApplicability()
    {
        var (sim, system) = CreateSystem(
            new Random(42),
            CreateGenerator(
                "crowding_event",
                triggers:
                [
                    new SemanticGeneratorTrigger
                    {
                        OnKind = TestKinds.Settlement,
                        AttributeName = "residents",
                        Direction = TriggerDirection.High,
                    },
                ]));

        foreach (var settlementId in sim.Graph.GetEntitiesByKind(TestKinds.Settlement))
        {
            sim.Graph.GetEntity(settlementId)!.Attributes.GetScalar("residents")!.SetBase(10f);
        }

        system.Tick(25, new HashSet<string>(StringComparer.Ordinal) { "crowding_event" });

        var instance = sim.WorldState.Generators.ById["crowding_event"];
        Assert.Equal(0, instance.TotalRuns);
        Assert.Null(instance.LastFiredTick);
    }

    [Fact]
    public void Tick_AtPulse_MaxRunsPerSim_Respected()
    {
        var (sim, system) = CreateSystem(
            new Random(42),
            CreateGenerator("one_shot", maxRunsPerSim: 1));

        system.Tick(25, new HashSet<string>(StringComparer.Ordinal) { "one_shot" });
        system.Tick(50, new HashSet<string>(StringComparer.Ordinal) { "one_shot" });

        var instance = sim.WorldState.Generators.ById["one_shot"];
        Assert.Equal(1, instance.TotalRuns);
        Assert.Equal(25, instance.LastFiredTick);
    }

    [Fact]
    public void Tick_AtPulse_WeightedRandom_DistributesCorrectly()
    {
        var (sim, system) = CreateSystem(
            new Random(42),
            CreateGenerator("rare_event", pulseWeight: 1),
            CreateGenerator("common_event", pulseWeight: 4));

        for (var pulse = 1; pulse <= 1000; pulse++)
        {
            system.Tick(
                pulse * system.PulseInterval,
                new HashSet<string>(StringComparer.Ordinal) { "rare_event", "common_event" });
        }

        var rareRuns = sim.WorldState.Generators.ById["rare_event"].TotalRuns;
        var commonRuns = sim.WorldState.Generators.ById["common_event"].TotalRuns;
        var totalRuns = rareRuns + commonRuns;
        var rareShare = rareRuns / (float)totalRuns;

        Assert.Equal(1000, totalRuns);
        Assert.InRange(rareShare, 0.15f, 0.25f);
        Assert.True(commonRuns > rareRuns);
    }

    [Fact]
    public void Tick_WhenChosenGeneratorFailsPlacement_RetriesOtherApplicableGenerators()
    {
        var impossibleCoverage = CreateGenerator(
            "impossible_coverage",
            pulseWeight: 100,
            produces:
            [
                new SemanticProducedEntity
                {
                    Ref = "$impossible_region",
                    Kind = TestKinds.Region,
                    Subtype = "scar",
                    Role = "impossible_region",
                    InheritCultureFrom = "$target",
                    Coverage = new SemanticRegionCoverage
                    {
                        Mode = CoveragePlacementMode.CreateNew,
                        DesiredCellCount = 500,
                        MinimumCellCount = 500,
                        AllowedTerrains = new HashSet<TerrainType> { TerrainType.HotSprings },
                        PreferredTerrains = new HashSet<TerrainType> { TerrainType.HotSprings },
                    },
                },
            ],
            relationships:
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$impossible_region",
                    DestinationRef = "$target",
                    Kind = "founded_from",
                },
            ]);
        var valid = CreateGenerator("still_valid", pulseWeight: 1);
        var (sim, system) = CreateSystem(new Random(42), impossibleCoverage, valid);
        var active = new HashSet<string>(StringComparer.Ordinal) { "impossible_coverage", "still_valid" };

        for (var pulse = 1; pulse <= 20; pulse++)
        {
            system.Tick(pulse * system.PulseInterval, active);
        }

        Assert.Equal(20, system.TotalFired);
        Assert.Equal(20, sim.WorldState.Generators.ById["still_valid"].TotalRuns);
        Assert.True(sim.WorldState.Generators.ById["impossible_coverage"].FailedRuns > 0);
    }

    [Fact]
    public void Fire_CreatesAllEntitiesInProducesList()
    {
        var generator = CreateGenerator(
            "hero_package",
            produces:
            [
                new SemanticProducedEntity
                {
                    Ref = "$hero",
                    Kind = TestKinds.Penguin,
                    Subtype = "warden",
                    Role = "hero",
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$weapon",
                    Kind = TestKinds.Artifact,
                    Subtype = "weapon",
                    Role = "weapon",
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
                    SourceRef = "$weapon",
                    DestinationRef = "$hero",
                    Kind = "anchored",
                },
            ]);

        var (_, _, _, message) = FireAndCaptureMessage(new Random(42), generator);

        Assert.Equal(2, message.CreatedEntityIds.Count);
        Assert.Contains(message.CreatedEntityIds, id => id.Value >= 0);
    }

    [Fact]
    public void Fire_AppliesCreateChance_SomeEntitiesOptionallyAbsent()
    {
        var generator = CreateGenerator(
            "optional_weapon",
            produces:
            [
                new SemanticProducedEntity
                {
                    Ref = "$weapon",
                    Kind = TestKinds.Artifact,
                    Subtype = "weapon",
                    Role = "weapon",
                    CreateChance = 0f,
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$hero",
                    Kind = TestKinds.Penguin,
                    Subtype = "warden",
                    Role = "hero",
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
                    SourceRef = "$weapon",
                    DestinationRef = "$hero",
                    Kind = "anchored",
                },
            ]);

        var (sim, _, _, message) = FireAndCaptureMessage(new Random(42), generator);

        Assert.Single(message.CreatedEntityIds);
        Assert.Equal(TestKinds.Penguin, sim.Graph.GetEntity(message.CreatedEntityIds[0])!.Kind);
    }

    [Fact]
    public void Fire_CultureInheritance_FromTarget_Works()
    {
        var generator = CreateGenerator("culture_inheritance");

        var (sim, _, target, message) = FireAndCaptureMessage(new Random(42), generator);

        var created = sim.Graph.GetEntity(message.CreatedEntityIds[0])!;
        Assert.Equal(target.Culture, created.Culture);
    }

    [Fact]
    public void Fire_Relationships_WiredCorrectly_WhenAllEntitiesPresent()
    {
        var generator = CreateGenerator(
            "relationship_package",
            produces:
            [
                new SemanticProducedEntity
                {
                    Ref = "$hero",
                    Kind = TestKinds.Penguin,
                    Subtype = "warden",
                    Role = "hero",
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$weapon",
                    Kind = TestKinds.Artifact,
                    Subtype = "weapon",
                    Role = "weapon",
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
                    SourceRef = "$weapon",
                    DestinationRef = "$hero",
                    Kind = "anchored",
                },
            ]);

        var (sim, _, target, message) = FireAndCaptureMessage(new Random(42), generator);
        var createdEntities = message.CreatedEntityIds
            .Select(id => sim.Graph.GetEntity(id)!)
            .ToDictionary(entity => entity.Kind);
        var hero = createdEntities[TestKinds.Penguin];
        var weapon = createdEntities[TestKinds.Artifact];

        Assert.Contains(
            sim.Graph.OutgoingRelationships(hero.Id, "home_base"),
            relationship => relationship.TargetId == target.Id);
        Assert.Contains(
            sim.Graph.OutgoingRelationships(weapon.Id, "anchored"),
            relationship => relationship.TargetId == hero.Id);
        Assert.Equal(2, message.CreatedRelationshipCount);
    }

    [Fact]
    public void Fire_Relationships_AutoSkipWhenOptionalRefMissing()
    {
        var generator = CreateGenerator(
            "optional_relationship_skip",
            produces:
            [
                new SemanticProducedEntity
                {
                    Ref = "$weapon",
                    Kind = TestKinds.Artifact,
                    Subtype = "weapon",
                    Role = "weapon",
                    CreateChance = 0f,
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$hero",
                    Kind = TestKinds.Penguin,
                    Subtype = "warden",
                    Role = "hero",
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
                    SourceRef = "$weapon",
                    DestinationRef = "$hero",
                    Kind = "anchored",
                },
            ]);

        var (sim, _, target, message) = FireAndCaptureMessage(new Random(42), generator);
        var heroId = Assert.Single(message.CreatedEntityIds);

        Assert.Contains(
            sim.Graph.OutgoingRelationships(heroId, "home_base"),
            relationship => relationship.TargetId == target.Id);
        Assert.DoesNotContain(
            sim.Graph.GetEntitiesByKind(TestKinds.Artifact),
            entityId => message.CreatedEntityIds.Contains(entityId));
        Assert.Equal(1, message.CreatedRelationshipCount);
    }

    [Fact]
    public void Fire_EntitiesPlacedNearTarget_OnBothPlanes()
    {
        var generator = CreateGenerator("near_placement");

        var (sim, _, target, message) = FireAndCaptureMessage(new Random(42), generator);
        var created = sim.Graph.GetEntity(message.CreatedEntityIds[0])!;
        var targetPhysical = new HexCoord(target.Coordinates.Q, target.Coordinates.R);

        Assert.Equal(1, HexCoord.Distance(targetPhysical, new HexCoord(created.Coordinates.Q, created.Coordinates.R)));
        Assert.Equal(1, SemanticHexCoord.Distance(target.SemanticPosition, created.SemanticPosition));
    }

    [Fact]
    public void Fire_EntitiesPlacedAcrossTarget_OnBothPlanes()
    {
        var generator = CreateGenerator(
            "across_placement",
            target: new SemanticGeneratorTarget
            {
                Kind = TestKinds.Settlement,
                Subtypes = new HashSet<string>(StringComparer.Ordinal) { "rookery" },
            },
            produces:
            [
                new SemanticProducedEntity
                {
                    Ref = "$hero",
                    Kind = TestKinds.Penguin,
                    Subtype = "warden",
                    Role = "hero",
                    PhysicalPlacement = SemanticPlacement.Across,
                    SemanticPlacement = SemanticPlacement.Across,
                    InheritCultureFrom = "$target",
                },
            ],
            triggers:
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = TestKinds.Settlement,
                    AttributeName = "shelter",
                    Direction = TriggerDirection.High,
                },
            ]);

        var (sim, system) = CreateSystem(new Random(42), generator);
        var target = sim.Graph.GetEntitiesByKind(TestKinds.Settlement)
            .Select(id => sim.Graph.GetEntity(id)!)
            .Single(entity => entity.Name == "Frosthaven");
        target.Attributes.GetScalar("shelter")!.SetBase(90f);

        foreach (var settlementId in sim.Graph.GetEntitiesByKind(TestKinds.Settlement).ToList())
        {
            var settlement = sim.Graph.GetEntity(settlementId)!;
            if (settlement.Id != target.Id && settlement.Subtype == "rookery")
            {
                sim.Graph.SetEntityHistorical(settlement.Id, 0);
            }
        }

        foreach (var penguinId in sim.Graph.GetEntitiesByKind(TestKinds.Penguin).ToList())
        {
            sim.Graph.SetEntityHistorical(penguinId, 0);
        }

        var cursor = sim.KernelEvents.CreateCursor(KernelEventCursorStart.Latest);

        system.Tick(25, new HashSet<string>(StringComparer.Ordinal) { "across_placement" });

        var fired = CaptureGeneratorFire(sim, cursor);
        var created = sim.Graph.GetEntity(Assert.Single(fired.CreatedEntityIds))!;
        var targetPhysical = new HexCoord(target.Coordinates.Q, target.Coordinates.R);
        var expectedPhysical = new HexCoord(-targetPhysical.Q, -targetPhysical.R);
        var expectedSemantic = new SemanticHexCoord(-target.SemanticPosition.Q, -target.SemanticPosition.R);
        var createdPhysical = new HexCoord(created.Coordinates.Q, created.Coordinates.R);
        var targetToMirrorDistance = HexCoord.Distance(targetPhysical, expectedPhysical);
        var targetToCreatedDistance = HexCoord.Distance(targetPhysical, createdPhysical);

        Assert.True(
            targetToCreatedDistance > targetToMirrorDistance / 2,
            "Across placement should land in the far half of the map, not near the target.");
        Assert.True(
            HexCoord.Distance(expectedPhysical, createdPhysical) < targetToCreatedDistance,
            "Across placement should land closer to the reflected map position than to the target.");
        Assert.InRange(SemanticHexCoord.Distance(expectedSemantic, created.SemanticPosition), 0, 1);
    }

    [Fact]
    public void Fire_WritesGeneratorFiredKernelEvent()
    {
        var generator = CreateGenerator("message_emission");

        var (_, _, target, message) = FireAndCaptureMessage(new Random(42), generator, currentTick: 25);

        Assert.Equal("message_emission", message.GeneratorId);
        Assert.Equal(target.Id, message.TargetEntityId);
        Assert.Single(message.CreatedEntityIds);
        Assert.Equal(1, message.CreatedRelationshipCount);
        Assert.Equal(25, message.Tick);
    }

    [Fact]
    public void Fire_WritesPrimitiveKernelEventsForGeneratorFireAndPulseEvaluation()
    {
        var generator = CreateGenerator("primitive_emission");
        var (sim, system) = CreateSystem(new Random(42), generator);
        var cursor = sim.KernelEvents.CreateCursor(KernelEventCursorStart.Latest);

        system.Tick(25, new HashSet<string>(StringComparer.Ordinal) { "primitive_emission" });

        Assert.True(sim.KernelEvents.TryRead(cursor, out var createdEvent));
        Assert.Equal(KernelEventType.EntityCreated, createdEvent.Event.EventType);
        var created = createdEvent.Event.AsEntityCreated();
        Assert.Equal(25, created.Tick);
        Assert.True(created.SubtypeId > 0);
        Assert.True(created.CultureId > 0);

        Assert.True(sim.KernelEvents.TryRead(cursor, out var relationshipEvent));
        Assert.Equal(KernelEventType.RelationshipChanged, relationshipEvent.Event.EventType);
        var relationshipChanged = relationshipEvent.Event.AsRelationshipChanged();
        Assert.Equal(25, relationshipChanged.Tick);
        Assert.Equal(sim.DomainIds.RelationshipKindIdOf("home_base").Value, relationshipChanged.RelationshipKindId);

        Assert.True(sim.KernelEvents.TryRead(cursor, out var firedEvent));
        Assert.Equal(KernelEventType.GeneratorFired, firedEvent.Event.EventType);
        var fired = firedEvent.Event.AsGeneratorFired();
        Assert.Equal(25, fired.Tick);
        Assert.True(fired.WorldVersion > 0);
        Assert.Equal(sim.DomainIds.GeneratorIdOf("primitive_emission").Value, fired.GeneratorId);
        Assert.True(fired.TargetEntityId >= 0);
        Assert.Equal(1, fired.CreatedEntityCount);
        Assert.Equal(1, fired.CreatedRelationshipCount);

        Assert.True(sim.KernelEvents.TryRead(cursor, out var evaluatedEvent));
        Assert.Equal(KernelEventType.GeneratorEvaluated, evaluatedEvent.Event.EventType);
        var evaluated = evaluatedEvent.Event.AsGeneratorEvaluated();
        Assert.Equal(25, evaluated.Tick);
        Assert.Equal(fired.TargetEntityId, evaluated.SelectedTargetEntityId);
        Assert.True(evaluated.CandidateCount > 0);
        Assert.Equal(2, evaluated.StatusCode);
        Assert.True(evaluated.WorldVersion >= fired.WorldVersion);
    }

    [Fact]
    public void Fire_QueryBoundAdjacentProducedEntity_BindsExistingInsteadOfCreatingDuplicate()
    {
        var generator = CreateGenerator(
            "sea_reefhold",
            target: new SemanticGeneratorTarget
            {
                Kind = TestKinds.Settlement,
                Subtypes = new HashSet<string>(StringComparer.Ordinal) { "harbor" },
            },
            produces:
            [
                new SemanticProducedEntity
                {
                    Ref = "$sea",
                    Kind = TestKinds.Region,
                    Subtype = "sea",
                    Role = "named_water",
                    Query = new SemanticEntityQuery
                    {
                        Subtypes = new HashSet<string>(StringComparer.Ordinal) { "sea" },
                        AdjacentToTarget = true,
                    },
                    InheritCultureFrom = "$target",
                    Coverage = new SemanticRegionCoverage
                    {
                        Mode = CoveragePlacementMode.CreateNew,
                        DesiredCellCount = 12,
                        MinimumCellCount = 6,
                    },
                },
                new SemanticProducedEntity
                {
                    Ref = "$reefhold",
                    Kind = TestKinds.Settlement,
                    Subtype = "reefhold",
                    Role = "offshore_hold",
                    InheritCultureFrom = "$target",
                },
            ],
            relationships:
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$reefhold",
                    DestinationRef = "$sea",
                    Kind = "founded_from",
                },
            ],
            triggers:
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = TestKinds.Settlement,
                    AttributeName = "shelter",
                    Direction = TriggerDirection.High,
                },
            ]);
        var (sim, system) = CreateSystem(new Random(42), generator);
        var harborId = sim.Graph.GetEntitiesByKind(TestKinds.Settlement)
            .Select(id => sim.Graph.GetEntity(id)!)
            .Single(entity => entity.Name == "Driftwatch")
            .Id;
        var harbor = sim.Graph.GetEntity(harborId)!;
        harbor.Attributes.GetScalar("shelter")!.SetBase(90f);

        var existingSea = CreateRegisteredEntity(
            sim,
            TestKinds.Region,
            "sea",
            "Near Sea",
            harbor.Culture,
            tick: 0,
            coordinates: (harbor.Coordinates.Q + 1, harbor.Coordinates.R));
        existingSea.SemanticPosition = harbor.SemanticPosition;
        var regionCountBefore = sim.Graph.GetEntitiesByKind(TestKinds.Region).Count;
        var cursor = sim.KernelEvents.CreateCursor(KernelEventCursorStart.Latest);

        system.Tick(25, new HashSet<string>(StringComparer.Ordinal) { "sea_reefhold" });

        var firedMessage = CaptureGeneratorFire(sim, cursor);
        Assert.Equal(regionCountBefore, sim.Graph.GetEntitiesByKind(TestKinds.Region).Count);
        Assert.DoesNotContain(existingSea.Id, firedMessage.CreatedEntityIds);

        var reefholdId = firedMessage.CreatedEntityIds
            .Single(id => sim.Graph.GetEntity(id)!.Kind == TestKinds.Settlement);
        Assert.Contains(
            sim.Graph.OutgoingRelationships(reefholdId, "founded_from"),
            relationship => relationship.TargetId == existingSea.Id);
    }

    [Fact]
    public void Fire_QueryBoundFollower_ReassignsMemberOfAndSkipsFactionLeader()
    {
        var generator = CreateGenerator(
            "unhuddled_with_third",
            target: new SemanticGeneratorTarget
            {
                Kind = TestKinds.Settlement,
                Subtypes = new HashSet<string>(StringComparer.Ordinal) { "rookery" },
            },
            produces:
            [
                new SemanticProducedEntity
                {
                    Ref = "$refuge",
                    Kind = TestKinds.Settlement,
                    Subtype = "sanctuary",
                    Role = "refuge",
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$compact",
                    Kind = TestKinds.Faction,
                    Subtype = "compact",
                    Role = "compact",
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$third_follower",
                    Kind = TestKinds.Penguin,
                    Subtype = "exile",
                    Role = "third_follower",
                    CreateIfMissing = false,
                    Query = new SemanticEntityQuery
                    {
                        AdjacentToTarget = true,
                        WithoutIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { "led_by" },
                    },
                    InheritCultureFrom = "$target",
                },
            ],
            relationships:
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$refuge",
                    DestinationRef = "$compact",
                    Kind = "controlled_by",
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$third_follower",
                    DestinationRef = "$compact",
                    Kind = "member_of",
                    ReplaceExistingOutgoingOfSameKind = true,
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$third_follower",
                    DestinationRef = "$refuge",
                    Kind = "home_base",
                },
            ]);
        var (sim, system) = CreateSystem(new Random(42), generator);
        var target = sim.Graph.GetEntitiesByKind(TestKinds.Settlement)
            .Select(id => sim.Graph.GetEntity(id)!)
            .Single(entity => entity.Name == "Frosthaven");
        target.Attributes.GetScalar("shelter")!.SetBase(90f);

        foreach (var settlementId in sim.Graph.GetEntitiesByKind(TestKinds.Settlement).ToList())
        {
            var settlement = sim.Graph.GetEntity(settlementId)!;
            if (settlement.Id != target.Id && settlement.Subtype == "rookery")
            {
                sim.Graph.SetEntityHistorical(settlement.Id, 0);
            }
        }

        foreach (var penguinId in sim.Graph.GetEntitiesByKind(TestKinds.Penguin).ToList())
        {
            sim.Graph.SetEntityHistorical(penguinId, 0);
        }

        var oldFaction = CreateRegisteredEntity(sim, TestKinds.Faction, "order", "Old Order", target.Culture, 0);
        var oldHome = CreateRegisteredEntity(
            sim,
            TestKinds.Settlement,
            "holdfast",
            "Old Home",
            target.Culture,
            0,
            (target.Coordinates.Q + 2, target.Coordinates.R));

        var candidate = CreateRegisteredEntity(
            sim,
            TestKinds.Penguin,
            "exile",
            "Nearby Exile",
            target.Culture,
            0,
            (target.Coordinates.Q + 1, target.Coordinates.R));
        CreateRegisteredRelationship(sim, "member_of", RelationshipPolarity.Neutral, candidate.Id, oldFaction.Id, 1f, 0);
        CreateRegisteredRelationship(sim, "home_base", RelationshipPolarity.Neutral, candidate.Id, oldHome.Id, 1f, 0);

        var rivalFaction = CreateRegisteredEntity(sim, TestKinds.Faction, "order", "Rival Order", target.Culture, 0);
        var leader = CreateRegisteredEntity(
            sim,
            TestKinds.Penguin,
            "warden",
            "Faction Leader",
            target.Culture,
            0,
            (target.Coordinates.Q, target.Coordinates.R + 1));
        CreateRegisteredRelationship(sim, "led_by", RelationshipPolarity.Neutral, rivalFaction.Id, leader.Id, 1f, 0);

        var cursor = sim.KernelEvents.CreateCursor(KernelEventCursorStart.Latest);

        system.Tick(25, new HashSet<string>(StringComparer.Ordinal) { "unhuddled_with_third" });

        var fired = CaptureGeneratorFire(sim, cursor);
        Assert.DoesNotContain(candidate.Id, fired.CreatedEntityIds);
        Assert.DoesNotContain(leader.Id, fired.CreatedEntityIds);

        var newCompactId = fired.CreatedEntityIds.Single(id => sim.Graph.GetEntity(id)!.Kind == TestKinds.Faction);
        var newRefugeId = fired.CreatedEntityIds.Single(id => sim.Graph.GetEntity(id)!.Kind == TestKinds.Settlement);

        Assert.Contains(
            sim.Graph.OutgoingRelationships(candidate.Id, "member_of"),
            relationship => relationship.TargetId == newCompactId);
        Assert.DoesNotContain(
            sim.Graph.OutgoingRelationships(candidate.Id, "member_of"),
            relationship => relationship.TargetId == oldFaction.Id);
        Assert.Contains(
            sim.Graph.OutgoingRelationships(candidate.Id, "home_base"),
            relationship => relationship.TargetId == newRefugeId);
        Assert.DoesNotContain(
            sim.Graph.OutgoingRelationships(candidate.Id, "home_base"),
            relationship => relationship.TargetId == oldHome.Id);
        Assert.Contains(
            sim.Graph.IncomingRelationships(leader.Id, "led_by"),
            relationship => relationship.SourceId == rivalFaction.Id);
    }

    [Fact]
    public void Fire_QueryBoundEntity_CanFilterByTargetCulture()
    {
        var generator = CreateGenerator(
            "culture_bound_follower",
            target: new SemanticGeneratorTarget
            {
                Kind = TestKinds.Settlement,
                Subtypes = new HashSet<string>(StringComparer.Ordinal) { "rookery" },
            },
            produces:
            [
                new SemanticProducedEntity
                {
                    Ref = "$same_culture",
                    Kind = TestKinds.Penguin,
                    Subtype = "exile",
                    Role = "same_culture",
                    CreateIfMissing = false,
                    InheritCultureFrom = "$target",
                    Query = new SemanticEntityQuery
                    {
                        AdjacentToTarget = true,
                        TargetCultureMatch = QueryTargetCultureMatch.SameAsTarget,
                    },
                },
                new SemanticProducedEntity
                {
                    Ref = "$different_culture",
                    Kind = TestKinds.Penguin,
                    Subtype = "exile",
                    Role = "different_culture",
                    CreateIfMissing = false,
                    InheritCultureFrom = "$target",
                    Query = new SemanticEntityQuery
                    {
                        AdjacentToTarget = true,
                        TargetCultureMatch = QueryTargetCultureMatch.DifferentFromTarget,
                    },
                },
            ],
            relationships:
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$same_culture",
                    DestinationRef = "$target",
                    Kind = "home_base",
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$different_culture",
                    DestinationRef = "$target",
                    Kind = "home_base",
                },
            ],
            triggers:
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = TestKinds.Settlement,
                    AttributeName = "shelter",
                    Direction = TriggerDirection.High,
                },
            ]);
        var (sim, system) = CreateSystem(new Random(42), generator);
        var target = sim.Graph.GetEntitiesByKind(TestKinds.Settlement)
            .Select(id => sim.Graph.GetEntity(id)!)
            .Single(entity => entity.Name == "Frosthaven");
        target.Attributes.GetScalar("shelter")!.SetBase(90f);

        foreach (var settlementId in sim.Graph.GetEntitiesByKind(TestKinds.Settlement).ToList())
        {
            var settlement = sim.Graph.GetEntity(settlementId)!;
            if (settlement.Id != target.Id && settlement.Subtype == "rookery")
            {
                sim.Graph.SetEntityHistorical(settlement.Id, 0);
            }
        }

        foreach (var penguinId in sim.Graph.GetEntitiesByKind(TestKinds.Penguin).ToList())
        {
            sim.Graph.SetEntityHistorical(penguinId, 0);
        }

        var sameCulture = CreateRegisteredEntity(
            sim,
            TestKinds.Penguin,
            "exile",
            "Same Culture",
            target.Culture,
            0,
            (target.Coordinates.Q + 1, target.Coordinates.R));
        var differentCulture = CreateRegisteredEntity(
            sim,
            TestKinds.Penguin,
            "exile",
            "Different Culture",
            IceRemembersCulture.WakeSingers.ToDomainName(),
            0,
            (target.Coordinates.Q, target.Coordinates.R + 1));

        var cursor = sim.KernelEvents.CreateCursor(KernelEventCursorStart.Latest);

        system.Tick(25, new HashSet<string>(StringComparer.Ordinal) { "culture_bound_follower" });

        var message = CaptureGeneratorFire(sim, cursor);
        Assert.Empty(message.CreatedEntityIds);
        Assert.Contains(
            sim.Graph.OutgoingRelationships(sameCulture.Id, "home_base"),
            relationship => relationship.TargetId == target.Id);
        Assert.Contains(
            sim.Graph.OutgoingRelationships(differentCulture.Id, "home_base"),
            relationship => relationship.TargetId == target.Id);
    }

    [Fact]
    public void Tick_AllowWilderness_KeepsRegionTargetForRelationshipsAndInheritsRegionCulture()
    {
        var generator = CreateGenerator(
            "region_opens_the_old_holding",
            target: new SemanticGeneratorTarget
            {
                Kind = TestKinds.Region,
                AllowWilderness = true,
            },
            produces:
            [
                new SemanticProducedEntity
                {
                    Ref = "$settlement",
                    Kind = TestKinds.Settlement,
                    Subtype = "rookery",
                    Role = "primary",
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$leader",
                    Kind = TestKinds.Penguin,
                    Subtype = "warden",
                    Role = "leader",
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$faction",
                    Kind = TestKinds.Faction,
                    Subtype = "order",
                    Role = "founding_faction",
                    InheritCultureFrom = "$target",
                },
            ],
            relationships:
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$leader",
                    DestinationRef = "$settlement",
                    Kind = "home_base",
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$settlement",
                    DestinationRef = "$faction",
                    Kind = "controlled_by",
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$leader",
                    DestinationRef = "$faction",
                    Kind = "member_of",
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$settlement",
                    DestinationRef = "$target",
                    Kind = "founded_from",
                },
            ],
            triggers:
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = TestKinds.Region,
                    AttributeName = "pristiness",
                    Direction = TriggerDirection.High,
                },
            ]);
        var (sim, system) = CreateSystem(new Random(42), generator);
        var regionIds = sim.Graph.GetEntitiesByKind(TestKinds.Region).ToHashSet();
        var settlementIds = sim.Graph.GetEntitiesByKind(TestKinds.Settlement).ToHashSet();

        foreach (var regionId in regionIds)
        {
            sim.Graph.GetEntity(regionId)!.Attributes.GetScalar("pristiness")!.SetBase(90f);
        }

        system.Tick(25, new HashSet<string>(StringComparer.Ordinal) { "region_opens_the_old_holding" });

        var newSettlementId = sim.Graph.GetEntitiesByKind(TestKinds.Settlement).Single(id => !settlementIds.Contains(id));
        var newSettlement = sim.Graph.GetEntity(newSettlementId)!;
        var foundedFrom = Assert.Single(sim.Graph.OutgoingRelationships(newSettlementId, "founded_from"));
        var containingRegionId = foundedFrom.TargetId;

        Assert.Equal("none", newSettlement.Culture);
        Assert.Contains(containingRegionId, regionIds);
    }

    [Fact]
    public void Tick_TargetSaturation_CountsRelationshipFactAndBlocksRepeatTargeting()
    {
        var generator = CreateGenerator(
            "saturating_generator",
            target: new SemanticGeneratorTarget
            {
                Kind = TestKinds.Settlement,
                Subtypes = new HashSet<string>(StringComparer.Ordinal) { "harbor" },
            },
            targetSaturation: new SemanticTargetSaturation
            {
                RelationshipKind = "home_base",
                RequiredFact = "called_unhuddled",
                Direction = TargetSaturationDirection.IncomingToTarget,
                MaxExisting = 0,
            },
            relationships:
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$hero",
                    DestinationRef = "$target",
                    Kind = "home_base",
                    Facts = new HashSet<string>(StringComparer.Ordinal) { "called_unhuddled" },
                },
            ]);
        var (sim, system) = CreateSystem(new Random(42), generator);
        var target = sim.Graph.GetEntitiesByKind(TestKinds.Settlement)
            .Select(id => sim.Graph.GetEntity(id)!)
            .Single(entity => entity.Name == "Driftwatch");
        foreach (var settlementId in sim.Graph.GetEntitiesByKind(TestKinds.Settlement).ToList())
        {
            var settlement = sim.Graph.GetEntity(settlementId)!;
            if (settlement.Id != target.Id && settlement.Subtype == "harbor")
            {
                sim.Graph.SetEntityHistorical(settlement.Id, 0);
            }
        }
        var initialIncomingCount = sim.Graph.IncomingRelationships(target.Id, "home_base").Count();

        system.Tick(25, new HashSet<string>(StringComparer.Ordinal) { "saturating_generator" });

        var createdRelationship = Assert.Single(
            sim.Graph.IncomingRelationships(target.Id, "home_base"),
            relationship => relationship.HasFact("called_unhuddled"));
        Assert.Equal(initialIncomingCount + 1, sim.Graph.IncomingRelationships(target.Id, "home_base").Count());
        Assert.Contains("called_unhuddled", createdRelationship.Facts);

        system.Tick(50, new HashSet<string>(StringComparer.Ordinal) { "saturating_generator" });

        var instance = sim.WorldState.Generators.ById["saturating_generator"];
        Assert.Equal(1, instance.TotalRuns);
    }

    [Fact]
    public void Fire_SecondaryRegionBinding_PlacesCreatedSettlementInThatRegion()
    {
        var generator = CreateGenerator(
            "seal_the_leak",
            target: new SemanticGeneratorTarget
            {
                Kind = TestKinds.Faction,
            },
            secondaryTargets:
            [
                new SemanticGeneratorBindingTarget
                {
                    Ref = "$leak_region",
                    Kind = TestKinds.Region,
                    Query = new SemanticEntityQuery
                    {
                        AttributeConditions =
                        [
                            new SemanticQueryAttributeCondition
                            {
                                AttributeName = "pristiness",
                                Direction = TriggerDirection.Low,
                            },
                        ],
                    },
                },
            ],
            produces:
            [
                new SemanticProducedEntity
                {
                    Ref = "$holdfast",
                    Kind = TestKinds.Settlement,
                    Subtype = "holdfast",
                    Role = "containment_hold",
                    PhysicalPlacement = SemanticPlacement.InRegionLocality,
                    SemanticPlacement = SemanticPlacement.Near,
                    PlacementRef = "$leak_region",
                    InheritCultureFrom = "$target",
                },
            ],
            relationships:
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$holdfast",
                    DestinationRef = "$target",
                    Kind = "controlled_by",
                },
            ],
            triggers:
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = TestKinds.Faction,
                    AttributeName = "cohesion",
                    Direction = TriggerDirection.High,
                },
            ]);
        var (sim, system) = CreateSystem(new Random(42), generator);
        foreach (var factionId in sim.Graph.GetEntitiesByKind(TestKinds.Faction))
        {
            sim.Graph.GetEntity(factionId)!.Attributes.GetScalar("cohesion")!.SetBase(10f);
        }

        foreach (var regionId in sim.Graph.GetEntitiesByKind(TestKinds.Region))
        {
            sim.Graph.GetEntity(regionId)!.Attributes.GetScalar("pristiness")!.SetBase(90f);
        }

        var targetFaction = sim.Graph.GetEntitiesByKind(TestKinds.Faction)
            .Select(id => sim.Graph.GetEntity(id)!)
            .First();
        targetFaction.Attributes.GetScalar("cohesion")!.SetBase(90f);

        var leakRegion = sim.Graph.GetEntitiesByKind(TestKinds.Region)
            .Select(id => sim.Graph.GetEntity(id)!)
            .First();
        leakRegion.Attributes.GetScalar("pristiness")!.SetBase(10f);

        var cursor = sim.KernelEvents.CreateCursor(KernelEventCursorStart.Latest);

        system.Tick(25, new HashSet<string>(StringComparer.Ordinal) { "seal_the_leak" });

        var fired = CaptureGeneratorFire(sim, cursor);
        var createdHoldfast = sim.Graph.GetEntity(Assert.Single(fired.CreatedEntityIds))!;
        var region = sim.CoverageMap.GetCoverage(leakRegion.Id);
        Assert.NotNull(region);
        var createdCoord = new HexCoord(createdHoldfast.Coordinates.Q, createdHoldfast.Coordinates.R);

        Assert.Contains(createdCoord, region!.Cells);
        Assert.Contains(
            sim.Graph.OutgoingRelationships(createdHoldfast.Id, "controlled_by"),
            relationship => relationship.TargetId == targetFaction.Id);
    }

    [Fact]
    public void Tick_SecondaryRegionBinding_WhenNoMatchExists_DoesNotFire()
    {
        var generator = CreateGenerator(
            "seal_the_leak",
            target: new SemanticGeneratorTarget
            {
                Kind = TestKinds.Faction,
            },
            secondaryTargets:
            [
                new SemanticGeneratorBindingTarget
                {
                    Ref = "$leak_region",
                    Kind = TestKinds.Region,
                    Query = new SemanticEntityQuery
                    {
                        AttributeConditions =
                        [
                            new SemanticQueryAttributeCondition
                            {
                                AttributeName = "pristiness",
                                Direction = TriggerDirection.Low,
                            },
                        ],
                    },
                },
            ],
            produces:
            [
                new SemanticProducedEntity
                {
                    Ref = "$holdfast",
                    Kind = TestKinds.Settlement,
                    Subtype = "holdfast",
                    Role = "containment_hold",
                    PhysicalPlacement = SemanticPlacement.InRegionLocality,
                    SemanticPlacement = SemanticPlacement.Near,
                    PlacementRef = "$leak_region",
                    InheritCultureFrom = "$target",
                },
            ],
            relationships:
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$holdfast",
                    DestinationRef = "$target",
                    Kind = "controlled_by",
                },
            ],
            triggers:
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = TestKinds.Faction,
                    AttributeName = "cohesion",
                    Direction = TriggerDirection.High,
                },
            ]);
        var (sim, system) = CreateSystem(new Random(42), generator);
        foreach (var factionId in sim.Graph.GetEntitiesByKind(TestKinds.Faction))
        {
            sim.Graph.GetEntity(factionId)!.Attributes.GetScalar("cohesion")!.SetBase(10f);
        }

        foreach (var regionId in sim.Graph.GetEntitiesByKind(TestKinds.Region))
        {
            sim.Graph.GetEntity(regionId)!.Attributes.GetScalar("pristiness")!.SetBase(90f);
        }

        var targetFaction = sim.Graph.GetEntitiesByKind(TestKinds.Faction)
            .Select(id => sim.Graph.GetEntity(id)!)
            .First();
        targetFaction.Attributes.GetScalar("cohesion")!.SetBase(90f);

        system.Tick(25, new HashSet<string>(StringComparer.Ordinal) { "seal_the_leak" });

        var instance = sim.WorldState.Generators.ById["seal_the_leak"];
        Assert.Equal(0, instance.TotalRuns);
        Assert.Null(instance.LastFiredTick);
    }

    [Fact]
    public void Fire_InRegionLocality_PicksRandomFreeHexWithinCoverage()
    {
        var generator = CreateGenerator(
            "random_region_locality",
            target: new SemanticGeneratorTarget
            {
                Kind = TestKinds.Faction,
            },
            secondaryTargets:
            [
                new SemanticGeneratorBindingTarget
                {
                    Ref = "$leak_region",
                    Kind = TestKinds.Region,
                    Query = new SemanticEntityQuery
                    {
                        AttributeConditions =
                        [
                            new SemanticQueryAttributeCondition
                            {
                                AttributeName = "pristiness",
                                Direction = TriggerDirection.Low,
                            },
                        ],
                    },
                },
            ],
            produces:
            [
                new SemanticProducedEntity
                {
                    Ref = "$holdfast",
                    Kind = TestKinds.Settlement,
                    Subtype = "holdfast",
                    Role = "containment_hold",
                    PhysicalPlacement = SemanticPlacement.InRegionLocality,
                    SemanticPlacement = SemanticPlacement.Near,
                    PlacementRef = "$leak_region",
                    InheritCultureFrom = "$target",
                },
            ],
            relationships:
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$holdfast",
                    DestinationRef = "$target",
                    Kind = "controlled_by",
                },
            ],
            triggers:
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = TestKinds.Faction,
                    AttributeName = "cohesion",
                    Direction = TriggerDirection.High,
                },
            ]);

        var positions = new HashSet<HexCoord>();
        for (var seed = 0; seed < 8; seed++)
        {
            var (sim, system) = CreateSystem(new Random(seed + 1), generator);
            foreach (var factionId in sim.Graph.GetEntitiesByKind(TestKinds.Faction))
            {
                sim.Graph.GetEntity(factionId)!.Attributes.GetScalar("cohesion")!.SetBase(10f);
            }

            foreach (var regionId in sim.Graph.GetEntitiesByKind(TestKinds.Region))
            {
                sim.Graph.GetEntity(regionId)!.Attributes.GetScalar("pristiness")!.SetBase(90f);
            }

            var targetFaction = sim.Graph.GetEntitiesByKind(TestKinds.Faction)
                .Select(id => sim.Graph.GetEntity(id)!)
                .First();
            targetFaction.Attributes.GetScalar("cohesion")!.SetBase(90f);

            var leakRegion = sim.Graph.GetEntitiesByKind(TestKinds.Region)
                .Select(id => sim.Graph.GetEntity(id)!)
                .First();
            leakRegion.Attributes.GetScalar("pristiness")!.SetBase(10f);

            var cursor = sim.KernelEvents.CreateCursor(KernelEventCursorStart.Latest);

            system.Tick(25, new HashSet<string>(StringComparer.Ordinal) { "random_region_locality" });

            var fired = CaptureGeneratorFire(sim, cursor);
            var createdHoldfast = sim.Graph.GetEntity(Assert.Single(fired.CreatedEntityIds))!;
            var region = sim.CoverageMap.GetCoverage(leakRegion.Id);
            Assert.NotNull(region);

            var createdCoord = new HexCoord(createdHoldfast.Coordinates.Q, createdHoldfast.Coordinates.R);
            Assert.Contains(createdCoord, region!.Cells);
            positions.Add(createdCoord);
        }

        Assert.True(positions.Count > 1, "Expected random in-coverage placement to vary across seeds.");
    }

    private static (SimulationRunner Sim, GeneratorSystem System) CreateSystem(
        Random random,
        params SemanticGeneratorConfig[] generators)
    {
        var domain = WithGenerators(generators);
        var definitionGraph = DomainLoader.BuildDefinitionGraph(domain);
        var sim = SimulationRunner.CreateFromDomain(domain);
        var system = new GeneratorSystem(
            sim.Graph,
            sim.WorldState.Generators,
            definitionGraph,
            sim.SpatialIndex,
            sim.WorldState.SemanticPlane,
            sim.CoverageMap,
            random,
            sim.KernelEvents,
            sim.WorldState,
            sim.DomainIds);

        return (sim, system);
    }

    private static Entity CreateRegisteredEntity(
        SimulationRunner sim,
        EntityKind kind,
        string subtype,
        string name,
        string culture,
        int tick,
        (int Q, int R)? coordinates = null) =>
        sim.Graph.CreateEntity(
            kind,
            subtype,
            name,
            culture,
            tick,
            coordinates,
            subtypeId: sim.DomainIds.SubtypeIdOf(subtype),
            cultureId: sim.DomainIds.CultureIdOf(culture));

    private static Relationship CreateRegisteredRelationship(
        SimulationRunner sim,
        string kind,
        RelationshipPolarity polarity,
        EntityId sourceId,
        EntityId targetId,
        float strength,
        int tick) =>
        sim.Graph.CreateRelationship(
            kind,
            polarity,
            sourceId,
            targetId,
            strength,
            tick,
            kindId: sim.DomainIds.RelationshipKindIdOf(kind));

    private static SemanticGeneratorConfig CreateGenerator(
        string id,
        int pulseWeight = 1,
        int? maxRunsPerSim = null,
        SemanticGeneratorTarget? target = null,
        SemanticTargetSaturation? targetSaturation = null,
        IReadOnlyList<SemanticGeneratorBindingTarget>? secondaryTargets = null,
        IReadOnlyList<SemanticProducedEntity>? produces = null,
        IReadOnlyList<SemanticGeneratorRelationship>? relationships = null,
        IReadOnlyList<SemanticGeneratorTrigger>? triggers = null) =>
        new()
        {
            Id = id,
            Name = id,
            Target = target ?? new SemanticGeneratorTarget
            {
                Kind = TestKinds.Settlement,
            },
            TargetSaturation = targetSaturation,
            SecondaryTargets = secondaryTargets ?? [],
            Produces = produces ??
            [
                new SemanticProducedEntity
                {
                    Ref = "$hero",
                    Kind = TestKinds.Penguin,
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
                    Direction = TriggerDirection.Any,
                },
            ],
            PulseWeight = pulseWeight,
            MaxRunsPerSim = maxRunsPerSim,
        };

    private static (SimulationRunner Sim, GeneratorSystem System, Entity Target, CapturedGeneratorFire Message)
        FireAndCaptureMessage(Random random, SemanticGeneratorConfig generator, int currentTick = 25)
    {
        var (sim, system) = CreateSystem(random, generator);
        var cursor = sim.KernelEvents.CreateCursor(KernelEventCursorStart.Latest);

        system.Tick(currentTick, new HashSet<string>(StringComparer.Ordinal) { generator.Id });

        var captured = CaptureGeneratorFire(sim, cursor);
        var target = sim.Graph.GetEntity(captured.TargetEntityId!.Value)!;
        return (sim, system, target, captured);
    }

    private static CapturedGeneratorFire CaptureGeneratorFire(
        SimulationRunner sim,
        KernelEventCursor cursor)
    {
        while (sim.KernelEvents.TryRead(cursor, out var sequenced))
        {
            if (sequenced.Event.EventType != KernelEventType.GeneratorFired)
            {
                continue;
            }

            var fired = sequenced.Event.AsGeneratorFired();
            var createdEntityIds = sim.Graph.AllEntities.Values
                .Where(entity => entity.CreatedAt == fired.Tick)
                .Select(entity => entity.Id)
                .ToList();
            var generatorId = sim.DomainIds.GeneratorNameOf(new global::Engine.Domain.GeneratorRuntimeId(fired.GeneratorId));
            return new CapturedGeneratorFire(generatorId, fired, createdEntityIds);
        }

        throw new Xunit.Sdk.XunitException("Expected a primitive GeneratorFired kernel event.");
    }

    private sealed record CapturedGeneratorFire(
        string GeneratorId,
        GeneratorFiredKernelEvent Event,
        IReadOnlyList<EntityId> CreatedEntityIds)
    {
        public int Tick => Event.Tick;

        public EntityId? TargetEntityId => new EntityId(Event.TargetEntityId);

        public int CreatedRelationshipCount => Event.CreatedRelationshipCount;
    }

    private static DomainConfig WithGenerators(params SemanticGeneratorConfig[] generators)
    {
        var config = IceRemembersDefinition.Create();
        var semantics = config.Semantics!;

        return new DomainConfig
        {
            Kinds = config.Kinds,
            Processes = config.Processes,
            Relationships = config.Relationships,
            RelationshipFacts = config.RelationshipFacts,
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
}
