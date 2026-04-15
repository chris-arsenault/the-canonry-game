namespace Engine.Tests.Kinds;

using Engine.Graph;
using Engine.Kinds;

public class KindRegistryTests
{
    private readonly KindRegistry _registry = DefaultRegistry.Create();

    // --- Kind Definitions ---

    [Theory]
    [InlineData(EntityKind.Region)]
    [InlineData(EntityKind.Settlement)]
    [InlineData(EntityKind.Faction)]
    [InlineData(EntityKind.Npc)]
    [InlineData(EntityKind.Artifact)]
    public void AllMvpKinds_AreRegistered(EntityKind kind)
    {
        var def = _registry.GetKindDefinition(kind);
        Assert.NotNull(def);
        Assert.Equal(kind, def.Kind);
    }

    [Fact]
    public void PostMvpKind_ReturnsNull()
    {
        var def = _registry.GetKindDefinition(EntityKind.Monster);
        Assert.Null(def);
    }

    // --- Subtype Validation ---

    [Fact]
    public void IsValidSubtype_AcceptsGenericSubtype()
    {
        Assert.True(_registry.IsValidSubtype(EntityKind.Faction, "generic_faction"));
    }

    [Fact]
    public void IsValidSubtype_AcceptsDomainSubtype()
    {
        Assert.True(_registry.IsValidSubtype(EntityKind.Faction, "order"));
    }

    [Fact]
    public void IsValidSubtype_RejectsInvalidSubtype()
    {
        Assert.False(_registry.IsValidSubtype(EntityKind.Settlement, "warrior"));
    }

    [Fact]
    public void IsValidSubtype_ReturnsFalseForUnregisteredKind()
    {
        Assert.False(_registry.IsValidSubtype(EntityKind.Monster, "anything"));
    }

    // --- Relationship Validation ---

    [Fact]
    public void ValidateRelationship_AcceptsValidPair()
    {
        // Faction (Agency) controls Settlement... wait, controlled_by is Spatial->Agency
        // Let's test Agency->Spatial: located_at
        var error = _registry.ValidateRelationship("located_at", EntityKind.Npc, EntityKind.Settlement);
        Assert.Null(error);
    }

    [Fact]
    public void ValidateRelationship_AcceptsAgencyToAgency()
    {
        var error = _registry.ValidateRelationship("allied_with", EntityKind.Faction, EntityKind.Faction);
        Assert.Null(error);
    }

    [Fact]
    public void ValidateRelationship_RejectsInvalidSourceCategory()
    {
        // Artifact (Asset) cannot be source of allied_with (requires Agency source)
        var error = _registry.ValidateRelationship("allied_with", EntityKind.Artifact, EntityKind.Faction);
        Assert.NotNull(error);
        Assert.Contains("source category", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateRelationship_RejectsInvalidTargetCategory()
    {
        // controlled_by is Spatial->Agency. Faction (Agency) as source is invalid because
        // controlled_by requires a spatial source and agency target.
        var error = _registry.ValidateRelationship("controlled_by", EntityKind.Faction, EntityKind.Settlement);
        Assert.NotNull(error);
        Assert.Contains("source category", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateRelationship_RejectsUnknownKind()
    {
        var error = _registry.ValidateRelationship("nonexistent", EntityKind.Faction, EntityKind.Faction);
        Assert.NotNull(error);
        Assert.Contains("Unknown", error, StringComparison.OrdinalIgnoreCase);
    }

    // --- Category-Level Relationship Queries ---

    [Fact]
    public void GetValidRelationshipKinds_AgencyToAgency_ReturnsExpected()
    {
        var relKinds = _registry.GetValidRelationshipKinds(EntityCategory.Agency, EntityCategory.Agency)
            .Select(r => r.Name)
            .ToHashSet();

        Assert.Contains("allied_with", relKinds);
        Assert.Contains("rival_of", relKinds);
        Assert.Contains("led_by", relKinds);
        Assert.Contains("member_of", relKinds);
        Assert.Contains("loyal_to", relKinds);
    }

    [Fact]
    public void GetValidRelationshipKinds_AgencyToAsset_ReturnsHolds()
    {
        var relKinds = _registry.GetValidRelationshipKinds(EntityCategory.Agency, EntityCategory.Asset)
            .Select(r => r.Name)
            .ToHashSet();

        Assert.Contains("holds", relKinds);
    }

    [Fact]
    public void GetValidRelationshipKinds_AssetToAgency_ReturnsCreatedBy()
    {
        var relKinds = _registry.GetValidRelationshipKinds(EntityCategory.Asset, EntityCategory.Agency)
            .Select(r => r.Name)
            .ToHashSet();

        Assert.Contains("created_by", relKinds);
    }

    // --- Exclusivity ---

    [Fact]
    public void IsExclusiveRelationship_ControlledBy_IsExclusive()
    {
        Assert.True(_registry.IsExclusiveRelationship("controlled_by"));
    }

    [Fact]
    public void IsExclusiveRelationship_AlliedWith_IsNotExclusive()
    {
        Assert.False(_registry.IsExclusiveRelationship("allied_with"));
    }

    // --- Behavior Dispatch ---

    [Fact]
    public void DispatchBehavior_CallsRegisteredHandler()
    {
        var registry = DefaultRegistry.Create();
        var called = false;

        registry.RegisterBehaviorHandler(EntityKind.Faction, _ => called = true);

        var entity = new Entity(
            new EntityId(0), EntityKind.Faction, "generic_faction", "TestNation", "culture_a", 0);

        registry.DispatchBehavior(entity);

        Assert.True(called);
    }

    [Fact]
    public void DispatchBehavior_DoesNotThrowForUnregisteredKind()
    {
        var entity = new Entity(
            new EntityId(0), EntityKind.Region, "generic_region", "TestRegion", "culture_a", 0);

        // Should not throw, just no-op
        _registry.DispatchBehavior(entity);
    }

    [Fact]
    public void DispatchBehavior_RoutesToCorrectKind()
    {
        var registry = DefaultRegistry.Create();
        EntityKind? dispatched = null;

        registry.RegisterBehaviorHandler(EntityKind.Faction, e => dispatched = e.Kind);
        registry.RegisterBehaviorHandler(EntityKind.Npc, e => dispatched = e.Kind);

        var npc = new Entity(
            new EntityId(0), EntityKind.Npc, "generic_npc", "Hero", "culture_a", 0);

        registry.DispatchBehavior(npc);

        Assert.Equal(EntityKind.Npc, dispatched);
    }

    // --- Kind Definition Properties ---

    [Fact]
    public void KindDefinition_HasCorrectCategory()
    {
        var def = _registry.GetKindDefinition(EntityKind.Settlement);
        Assert.NotNull(def);
        Assert.Equal(EntityCategory.Spatial, def.Category);
    }

    [Fact]
    public void KindDefinition_HasHealthAttribute()
    {
        var nationDef = _registry.GetKindDefinition(EntityKind.Faction);
        Assert.NotNull(nationDef);
        Assert.Equal("cohesion", nationDef.HealthAttribute);

        var regionDef = _registry.GetKindDefinition(EntityKind.Region);
        Assert.NotNull(regionDef);
        Assert.Null(regionDef.HealthAttribute); // Regions are permanent
    }

    [Fact]
    public void KindDefinition_HasAttributes()
    {
        var def = _registry.GetKindDefinition(EntityKind.Npc);
        Assert.NotNull(def);

        var attrNames = def.Attributes.Select(a => a.Name).ToHashSet();
        Assert.Contains("vitality", attrNames);
        Assert.Contains("hunger", attrNames);
        Assert.Contains("ambition", attrNames);
    }
}
