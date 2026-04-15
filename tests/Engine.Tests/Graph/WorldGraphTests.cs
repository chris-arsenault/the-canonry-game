namespace Engine.Tests.Graph;

using Engine.Graph;
using Engine.Kinds;

public class WorldGraphTests
{
    private readonly WorldGraph _graph = new();

    // --- Entity CRUD ---

    [Fact]
    public void CreateEntity_ReturnsEntityWithCorrectProperties()
    {
        var entity = _graph.CreateEntity(EntityKind.Faction, "generic", "TestNation", "culture_a", tick: 0);

        Assert.Equal(EntityKind.Faction, entity.Kind);
        Assert.Equal("generic", entity.Subtype);
        Assert.Equal("TestNation", entity.Name);
        Assert.Equal("culture_a", entity.Culture);
        Assert.Equal(EntityStatus.Active, entity.Status);
        Assert.Equal(0, entity.CreatedAt);
    }

    [Fact]
    public void CreateEntity_AssignsUniqueIds()
    {
        var a = _graph.CreateEntity(EntityKind.Faction, "generic", "A", "c", tick: 0);
        var b = _graph.CreateEntity(EntityKind.Faction, "generic", "B", "c", tick: 0);

        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void GetEntity_ReturnsNullForNonexistent()
    {
        var result = _graph.GetEntity(new EntityId(999));

        Assert.Null(result);
    }

    [Fact]
    public void GetEntitiesByKind_ReturnsCorrectSet()
    {
        _graph.CreateEntity(EntityKind.Faction, "generic", "N1", "c", tick: 0);
        _graph.CreateEntity(EntityKind.Faction, "generic", "N2", "c", tick: 0);
        _graph.CreateEntity(EntityKind.Settlement, "generic", "S1", "c", tick: 0);

        Assert.Equal(2, _graph.GetEntitiesByKind(EntityKind.Faction).Count);
        Assert.Single(_graph.GetEntitiesByKind(EntityKind.Settlement));
        Assert.Empty(_graph.GetEntitiesByKind(EntityKind.Npc));
    }

    [Fact]
    public void SetEntityHistorical_ChangesStatus()
    {
        var entity = _graph.CreateEntity(EntityKind.Npc, "generic", "Hero", "c", tick: 0);

        _graph.SetEntityHistorical(entity.Id, tick: 50);

        Assert.Equal(EntityStatus.Historical, entity.Status);
        Assert.Equal(50, entity.UpdatedAt);
    }

    [Fact]
    public void SetEntityHistorical_ThrowsForNonexistent()
    {
        Assert.Throws<ArgumentException>(() =>
            _graph.SetEntityHistorical(new EntityId(999), tick: 0));
    }

    // --- Relationships ---

    [Fact]
    public void CreateRelationship_ReturnsRelationshipWithCorrectProperties()
    {
        var nation = _graph.CreateEntity(EntityKind.Faction, "generic", "N", "c", tick: 0);
        var settlement = _graph.CreateEntity(EntityKind.Settlement, "generic", "S", "c", tick: 0);

        var rel = _graph.CreateRelationship(
            "controls", RelationshipPolarity.Neutral, nation.Id, settlement.Id, 0.8f, tick: 0);

        Assert.Equal("controls", rel.Kind);
        Assert.Equal(RelationshipPolarity.Neutral, rel.Polarity);
        Assert.Equal(nation.Id, rel.SourceId);
        Assert.Equal(settlement.Id, rel.TargetId);
        Assert.Equal(0.8f, rel.Strength);
        Assert.True(rel.IsActive);
        Assert.Null(rel.EndDate);
    }

    [Fact]
    public void CreateRelationship_ThrowsForNonexistentEntity()
    {
        var nation = _graph.CreateEntity(EntityKind.Faction, "generic", "N", "c", tick: 0);

        Assert.Throws<ArgumentException>(() =>
            _graph.CreateRelationship(
                "controls", RelationshipPolarity.Neutral,
                nation.Id, new EntityId(999), 0.5f, tick: 0));
    }

    [Fact]
    public void CreateRelationship_ClampsStrengthToRange()
    {
        var a = _graph.CreateEntity(EntityKind.Faction, "generic", "A", "c", tick: 0);
        var b = _graph.CreateEntity(EntityKind.Faction, "generic", "B", "c", tick: 0);

        var rel = _graph.CreateRelationship(
            "allied_with", RelationshipPolarity.Positive, a.Id, b.Id, 1.5f, tick: 0);

        Assert.Equal(1.0f, rel.Strength);
    }

    // --- Adjacency ---

    [Fact]
    public void OutgoingRelationships_ReturnsCorrectEdges()
    {
        var nation = _graph.CreateEntity(EntityKind.Faction, "generic", "N", "c", tick: 0);
        var s1 = _graph.CreateEntity(EntityKind.Settlement, "generic", "S1", "c", tick: 0);
        var s2 = _graph.CreateEntity(EntityKind.Settlement, "generic", "S2", "c", tick: 0);

        _graph.CreateRelationship("controls", RelationshipPolarity.Neutral, nation.Id, s1.Id, 1f, 0);
        _graph.CreateRelationship("controls", RelationshipPolarity.Neutral, nation.Id, s2.Id, 1f, 0);

        var outgoing = _graph.OutgoingRelationships(nation.Id).ToList();
        Assert.Equal(2, outgoing.Count);
        Assert.All(outgoing, r => Assert.Equal("controls", r.Kind));
    }

    [Fact]
    public void OutgoingRelationships_FilterByKind()
    {
        var nation = _graph.CreateEntity(EntityKind.Faction, "generic", "N", "c", tick: 0);
        var s = _graph.CreateEntity(EntityKind.Settlement, "generic", "S", "c", tick: 0);
        var rival = _graph.CreateEntity(EntityKind.Faction, "generic", "R", "c", tick: 0);

        _graph.CreateRelationship("controls", RelationshipPolarity.Neutral, nation.Id, s.Id, 1f, 0);
        _graph.CreateRelationship("rival_of", RelationshipPolarity.Negative, nation.Id, rival.Id, 0.5f, 0);

        var controls = _graph.OutgoingRelationships(nation.Id, kindFilter: "controls").ToList();
        Assert.Single(controls);
        Assert.Equal(s.Id, controls[0].TargetId);
    }

    [Fact]
    public void IncomingRelationships_ReturnsCorrectEdges()
    {
        var s = _graph.CreateEntity(EntityKind.Settlement, "generic", "S", "c", tick: 0);
        var n1 = _graph.CreateEntity(EntityKind.Faction, "generic", "N1", "c", tick: 0);
        var n2 = _graph.CreateEntity(EntityKind.Faction, "generic", "N2", "c", tick: 0);

        _graph.CreateRelationship("controls", RelationshipPolarity.Neutral, n1.Id, s.Id, 1f, 0);
        _graph.CreateRelationship("influences", RelationshipPolarity.Positive, n2.Id, s.Id, 0.3f, 0);

        var incoming = _graph.IncomingRelationships(s.Id).ToList();
        Assert.Equal(2, incoming.Count);
    }

    // --- Culling and History ---

    [Fact]
    public void CullRelationship_SetsEndDateAndRemovesFromActive()
    {
        var a = _graph.CreateEntity(EntityKind.Faction, "generic", "A", "c", tick: 0);
        var b = _graph.CreateEntity(EntityKind.Faction, "generic", "B", "c", tick: 0);

        var rel = _graph.CreateRelationship(
            "allied_with", RelationshipPolarity.Positive, a.Id, b.Id, 0.5f, tick: 0);

        _graph.CullRelationship(rel, tick: 50);

        Assert.False(rel.IsActive);
        Assert.Equal(50, rel.EndDate);
        Assert.Empty(_graph.OutgoingRelationships(a.Id));
        Assert.Equal(0, _graph.ActiveRelationshipCount);
        Assert.Equal(1, _graph.HistoricalRelationshipCount);
    }

    [Fact]
    public void AllRelationshipsBetween_ReturnsActiveAndHistorical()
    {
        var a = _graph.CreateEntity(EntityKind.Faction, "generic", "A", "c", tick: 0);
        var b = _graph.CreateEntity(EntityKind.Faction, "generic", "B", "c", tick: 0);

        var alliance = _graph.CreateRelationship(
            "allied_with", RelationshipPolarity.Positive, a.Id, b.Id, 0.8f, tick: 0);
        _graph.CullRelationship(alliance, tick: 30);

        _graph.CreateRelationship(
            "rival_of", RelationshipPolarity.Negative, a.Id, b.Id, 0.6f, tick: 31);

        var all = _graph.AllRelationshipsBetween(a.Id, b.Id).ToList();
        Assert.Equal(2, all.Count);
        Assert.Contains(all, r => r.Kind == "allied_with" && !r.IsActive);
        Assert.Contains(all, r => r.Kind == "rival_of" && r.IsActive);
    }

    [Fact]
    public void AllRelationshipsBetween_FilterByKind()
    {
        var a = _graph.CreateEntity(EntityKind.Faction, "generic", "A", "c", tick: 0);
        var b = _graph.CreateEntity(EntityKind.Faction, "generic", "B", "c", tick: 0);

        _graph.CreateRelationship("allied_with", RelationshipPolarity.Positive, a.Id, b.Id, 0.8f, 0);
        _graph.CreateRelationship("rival_of", RelationshipPolarity.Negative, a.Id, b.Id, 0.3f, 0);

        var alliances = _graph.AllRelationshipsBetween(a.Id, b.Id, kindFilter: "allied_with").ToList();
        Assert.Single(alliances);
    }

    // --- Traversal ---

    [Fact]
    public void EntitiesWithinHops_Returns2HopNeighbors()
    {
        // NPC -> loyal_to -> Nation -> controls -> Settlement
        var npc = _graph.CreateEntity(EntityKind.Npc, "generic", "Hero", "c", tick: 0);
        var nation = _graph.CreateEntity(EntityKind.Faction, "generic", "N", "c", tick: 0);
        var s1 = _graph.CreateEntity(EntityKind.Settlement, "generic", "S1", "c", tick: 0);
        var s2 = _graph.CreateEntity(EntityKind.Settlement, "generic", "S2", "c", tick: 0);

        _graph.CreateRelationship("loyal_to", RelationshipPolarity.Positive, npc.Id, nation.Id, 0.9f, 0);
        _graph.CreateRelationship("controls", RelationshipPolarity.Neutral, nation.Id, s1.Id, 1f, 0);
        _graph.CreateRelationship("controls", RelationshipPolarity.Neutral, nation.Id, s2.Id, 1f, 0);

        var reachable = _graph.EntitiesWithinHops(npc.Id, maxHops: 2).ToList();

        Assert.Equal(3, reachable.Count); // nation + s1 + s2
        Assert.Contains(reachable, e => e.Id == nation.Id);
        Assert.Contains(reachable, e => e.Id == s1.Id);
        Assert.Contains(reachable, e => e.Id == s2.Id);
    }

    [Fact]
    public void EntitiesWithinHops_RespectsEdgeFilter()
    {
        var npc = _graph.CreateEntity(EntityKind.Npc, "generic", "Hero", "c", tick: 0);
        var nation = _graph.CreateEntity(EntityKind.Faction, "generic", "N", "c", tick: 0);
        var rival = _graph.CreateEntity(EntityKind.Faction, "generic", "R", "c", tick: 0);

        _graph.CreateRelationship("loyal_to", RelationshipPolarity.Positive, npc.Id, nation.Id, 0.9f, 0);
        _graph.CreateRelationship("rival_of", RelationshipPolarity.Negative, npc.Id, rival.Id, 0.5f, 0);

        // Only traverse loyal_to edges
        var reachable = _graph.EntitiesWithinHops(
            npc.Id, maxHops: 1, edgeFilter: r => r.Kind == "loyal_to").ToList();

        Assert.Single(reachable);
        Assert.Equal(nation.Id, reachable[0].Id);
    }

    [Fact]
    public void EntitiesWithinHops_ReturnsEmptyForNonexistent()
    {
        var result = _graph.EntitiesWithinHops(new EntityId(999), maxHops: 1).ToList();
        Assert.Empty(result);
    }

    // --- Strength Update ---

    [Fact]
    public void UpdateRelationshipStrength_ClampsAndFiresEvent()
    {
        var a = _graph.CreateEntity(EntityKind.Faction, "generic", "A", "c", tick: 0);
        var b = _graph.CreateEntity(EntityKind.Faction, "generic", "B", "c", tick: 0);

        var rel = _graph.CreateRelationship(
            "allied_with", RelationshipPolarity.Positive, a.Id, b.Id, 0.5f, tick: 0);

        float? eventPrevious = null;
        _graph.RelationshipStrengthChanged += (_, e) => eventPrevious = e.PreviousStrength;

        _graph.UpdateRelationshipStrength(rel, 1.5f);

        Assert.Equal(1.0f, rel.Strength); // clamped
        Assert.Equal(0.5f, eventPrevious); // event fired with correct previous value
    }

    // --- Events ---

    [Fact]
    public void Events_FireOnEntityCreation()
    {
        Entity? created = null;
        _graph.EntityCreated += (_, e) => created = e.Entity;

        var entity = _graph.CreateEntity(EntityKind.Faction, "generic", "N", "c", tick: 0);

        Assert.NotNull(created);
        Assert.Equal(entity.Id, created.Id);
    }

    [Fact]
    public void Events_FireOnRelationshipCreation()
    {
        Relationship? created = null;
        _graph.RelationshipCreated += (_, e) => created = e.Relationship;

        var a = _graph.CreateEntity(EntityKind.Faction, "generic", "A", "c", tick: 0);
        var b = _graph.CreateEntity(EntityKind.Settlement, "generic", "S", "c", tick: 0);

        _graph.CreateRelationship("controls", RelationshipPolarity.Neutral, a.Id, b.Id, 1f, 0);

        Assert.NotNull(created);
        Assert.Equal("controls", created.Kind);
    }

    [Fact]
    public void Events_FireOnCulling()
    {
        int? endTick = null;
        _graph.RelationshipCulled += (_, e) => endTick = e.EndTick;

        var a = _graph.CreateEntity(EntityKind.Faction, "generic", "A", "c", tick: 0);
        var b = _graph.CreateEntity(EntityKind.Faction, "generic", "B", "c", tick: 0);
        var rel = _graph.CreateRelationship(
            "allied_with", RelationshipPolarity.Positive, a.Id, b.Id, 0.5f, tick: 0);

        _graph.CullRelationship(rel, tick: 42);

        Assert.Equal(42, endTick);
    }

    // --- Integration: Nations controlling settlements ---

    [Fact]
    public void Integration_SettlementsControlledByNation()
    {
        var nationA = _graph.CreateEntity(EntityKind.Faction, "generic", "NationA", "c", tick: 0);
        var nationB = _graph.CreateEntity(EntityKind.Faction, "generic", "NationB", "c", tick: 0);
        var s1 = _graph.CreateEntity(EntityKind.Settlement, "generic", "S1", "c", tick: 0);
        var s2 = _graph.CreateEntity(EntityKind.Settlement, "generic", "S2", "c", tick: 0);
        var s3 = _graph.CreateEntity(EntityKind.Settlement, "generic", "S3", "c", tick: 0);

        _graph.CreateRelationship("controls", RelationshipPolarity.Neutral, nationA.Id, s1.Id, 1f, 0);
        _graph.CreateRelationship("controls", RelationshipPolarity.Neutral, nationA.Id, s2.Id, 1f, 0);
        _graph.CreateRelationship("controls", RelationshipPolarity.Neutral, nationB.Id, s3.Id, 1f, 0);

        var nationASettlements = _graph.OutgoingRelationships(nationA.Id, kindFilter: "controls")
            .Select(r => r.TargetId)
            .ToHashSet();

        Assert.Equal(2, nationASettlements.Count);
        Assert.Contains(s1.Id, nationASettlements);
        Assert.Contains(s2.Id, nationASettlements);
        Assert.DoesNotContain(s3.Id, nationASettlements);
    }
}
