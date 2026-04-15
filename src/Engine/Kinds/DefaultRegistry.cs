namespace Engine.Kinds;

using Engine.Graph;

/// <summary>
/// Creates a KindRegistry with the 5 MVP entity kinds and category-level relationship definitions.
/// Uses generic Phase 1 subtypes. Domain-specific subtypes replace these in Phase 2.
/// </summary>
public static class DefaultRegistry
{
    public static KindRegistry Create()
    {
        var registry = new KindRegistry();

        RegisterKinds(registry);
        RegisterRelationshipKinds(registry);

        return registry;
    }

    private static void RegisterKinds(KindRegistry registry)
    {
        registry.RegisterKind(new KindDefinition(
            EntityKind.Region,
            validSubtypes: new HashSet<string> { "generic_region", "tundra", "coastal", "mountain", "plains" },
            attributes:
            [
                new AttributeDefinition("fertility", 0f, 1f, 0.5f),
                new AttributeDefinition("water_balance", 0f, 100f, 50f),
                new AttributeDefinition("geologic_stability", 0f, 100f, 80f),
            ],
            healthAttribute: null)); // Regions are permanent

        registry.RegisterKind(new KindDefinition(
            EntityKind.Settlement,
            validSubtypes: new HashSet<string> { "generic_settlement", "village", "town", "city", "fortress", "ruin" },
            attributes:
            [
                new AttributeDefinition("residents", 0f, 100000f, 100f),
                new AttributeDefinition("security", 0f, 100f, 20f),
                new AttributeDefinition("prestige", 0f, 100f, 10f),
            ],
            healthAttribute: "residents"));

        registry.RegisterKind(new KindDefinition(
            EntityKind.Faction,
            validSubtypes: new HashSet<string> { "generic_faction", "order", "cult", "company", "league" },
            attributes:
            [
                new AttributeDefinition("cohesion", 0f, 100f, 70f),
                new AttributeDefinition("support", 0f, 100f, 40f),
                new AttributeDefinition("influence", 0f, 100f, 30f),
            ],
            healthAttribute: "cohesion"));

        registry.RegisterKind(new KindDefinition(
            EntityKind.Npc,
            validSubtypes: new HashSet<string> { "generic_npc", "hero", "merchant", "scholar", "warrior" },
            attributes:
            [
                new AttributeDefinition("vitality", 0f, 100f, 100f),
                new AttributeDefinition("hunger", 0f, 100f, 0f),
                new AttributeDefinition("ambition", 0f, 100f, 50f),
            ],
            healthAttribute: "vitality"));

        registry.RegisterKind(new KindDefinition(
            EntityKind.Artifact,
            validSubtypes: new HashSet<string> { "generic_artifact", "weapon", "armor", "instrument", "relic", "scroll" },
            attributes:
            [
                new AttributeDefinition("condition", 0f, 100f, 100f),
                new AttributeDefinition("compulsion", 0f, 100f, 0f),
                new AttributeDefinition("power", 0f, 100f, 10f),
            ],
            healthAttribute: "condition"));
    }

    private static void RegisterRelationshipKinds(KindRegistry registry)
    {
        var spatial = new HashSet<EntityCategory> { EntityCategory.Spatial };
        var agency = new HashSet<EntityCategory> { EntityCategory.Agency };
        var asset = new HashSet<EntityCategory> { EntityCategory.Asset };
        var spatialAgency = new HashSet<EntityCategory> { EntityCategory.Spatial, EntityCategory.Agency };
        var all = new HashSet<EntityCategory> { EntityCategory.Spatial, EntityCategory.Agency, EntityCategory.Asset, EntityCategory.Record };

        // Spatial x Spatial
        registry.RegisterRelationshipKind(new RelationshipKindDefinition(
            "borders", RelationshipPolarity.Neutral, spatial, spatial, isSymmetric: true));
        registry.RegisterRelationshipKind(new RelationshipKindDefinition(
            "contains", RelationshipPolarity.Neutral, spatial, spatial));

        // Spatial x Agency (Agency -> Spatial)
        registry.RegisterRelationshipKind(new RelationshipKindDefinition(
            "controlled_by", RelationshipPolarity.Neutral, spatial, agency, isExclusive: true));
        registry.RegisterRelationshipKind(new RelationshipKindDefinition(
            "located_at", RelationshipPolarity.Neutral, agency, spatial, isExclusive: true));
        registry.RegisterRelationshipKind(new RelationshipKindDefinition(
            "home_base", RelationshipPolarity.Neutral, agency, spatial, isExclusive: true));

        // Agency x Agency
        registry.RegisterRelationshipKind(new RelationshipKindDefinition(
            "allied_with", RelationshipPolarity.Positive, agency, agency, isSymmetric: true, cullThreshold: 0.1f));
        registry.RegisterRelationshipKind(new RelationshipKindDefinition(
            "rival_of", RelationshipPolarity.Negative, agency, agency, cullThreshold: 0.05f));
        registry.RegisterRelationshipKind(new RelationshipKindDefinition(
            "led_by", RelationshipPolarity.Neutral, agency, agency, isExclusive: true));
        registry.RegisterRelationshipKind(new RelationshipKindDefinition(
            "member_of", RelationshipPolarity.Neutral, agency, agency));
        registry.RegisterRelationshipKind(new RelationshipKindDefinition(
            "loyal_to", RelationshipPolarity.Positive, agency, agency));

        // Agency x Asset
        registry.RegisterRelationshipKind(new RelationshipKindDefinition(
            "holds", RelationshipPolarity.Neutral, agency, asset));
        registry.RegisterRelationshipKind(new RelationshipKindDefinition(
            "created_by", RelationshipPolarity.Neutral, asset, agency));

        // Spatial x Asset
        registry.RegisterRelationshipKind(new RelationshipKindDefinition(
            "stored_at", RelationshipPolarity.Neutral, asset, spatial, isExclusive: true));

        // Broad relationships
        registry.RegisterRelationshipKind(new RelationshipKindDefinition(
            "involved_in", RelationshipPolarity.Neutral, all, all));
    }
}
