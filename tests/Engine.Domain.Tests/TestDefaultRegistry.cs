namespace Engine.Domain.Tests;

using Engine.Graph;
using Engine.Kinds;

public static class TestDefaultRegistry
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
            TestKinds.Region,
            EntityCategory.Spatial,
            validSubtypes: new HashSet<string>(StringComparer.Ordinal) { "generic_region", "tundra", "coastal", "mountain", "plains" },
            attributes:
            [
                new AttributeDefinition("fertility", 0f, 1f, 0.5f),
                new AttributeDefinition("holding", 0f, 100f, 50f),
                new AttributeDefinition("stability", 0f, 100f, 80f),
            ],
            healthAttribute: null));

        registry.RegisterKind(new KindDefinition(
            TestKinds.Settlement,
            EntityCategory.Spatial,
            validSubtypes: new HashSet<string>(StringComparer.Ordinal) { "generic_settlement", "village", "town", "city", "fortress", "ruin" },
            attributes:
            [
                new AttributeDefinition("temp_hp", 0f, 100f, 90f),
                new AttributeDefinition("security", 0f, 100f, 20f),
                new AttributeDefinition("prestige", 0f, 100f, 10f),
            ],
            healthAttribute: "temp_hp"));

        registry.RegisterKind(new KindDefinition(
            TestKinds.Faction,
            EntityCategory.Agency,
            validSubtypes: new HashSet<string>(StringComparer.Ordinal) { "order", "compact", "cell", "choir" },
            attributes:
            [
                new AttributeDefinition("cohesion", 0f, 100f, 70f),
                new AttributeDefinition("reach", 0f, 100f, 30f),
                new AttributeDefinition("provisions", 0f, 100f, 50f),
            ],
            healthAttribute: "cohesion"));

        registry.RegisterKind(new KindDefinition(
            TestKinds.Penguin,
            EntityCategory.Agency,
            validSubtypes: new HashSet<string>(StringComparer.Ordinal) { "merchant", "scholar", "warrior", "warden" },
            attributes:
            [
                new AttributeDefinition("vitality", 0f, 100f, 100f),
                new AttributeDefinition("warmth", 0f, 100f, 100f),
                new AttributeDefinition("echo", 0f, 100f, 50f),
            ],
            healthAttribute: "vitality"));

        registry.RegisterKind(new KindDefinition(
            TestKinds.Artifact,
            EntityCategory.Asset,
            validSubtypes: new HashSet<string>(StringComparer.Ordinal) { "generic_artifact", "weapon", "armor", "instrument", "relic", "scroll" },
            attributes:
            [
                new AttributeDefinition("condition", 0f, 100f, 100f),
                new AttributeDefinition("containment", 0f, 100f, 90f),
                new AttributeDefinition("power", 0f, 100f, 10f),
            ],
            healthAttribute: "condition"));
    }

    private static void RegisterRelationshipKinds(KindRegistry registry)
    {
        var spatial = new HashSet<EntityCategory> { EntityCategory.Spatial };
        var agency = new HashSet<EntityCategory> { EntityCategory.Agency };
        var asset = new HashSet<EntityCategory> { EntityCategory.Asset };
        var all = new HashSet<EntityCategory> { EntityCategory.Spatial, EntityCategory.Agency, EntityCategory.Asset, EntityCategory.Record };

        registry.RegisterRelationshipKind(new RelationshipKindDefinition(
            "borders", RelationshipPolarity.Neutral, spatial, spatial, isSymmetric: true));
        registry.RegisterRelationshipKind(new RelationshipKindDefinition(
            "contains", RelationshipPolarity.Neutral, spatial, spatial));
        registry.RegisterRelationshipKind(new RelationshipKindDefinition(
            "controlled_by", RelationshipPolarity.Neutral, spatial, agency, isExclusive: true));
        registry.RegisterRelationshipKind(new RelationshipKindDefinition(
            "located_at", RelationshipPolarity.Neutral, agency, spatial, isExclusive: true));
        registry.RegisterRelationshipKind(new RelationshipKindDefinition(
            "home_base", RelationshipPolarity.Neutral, agency, spatial, isExclusive: true));
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
        registry.RegisterRelationshipKind(new RelationshipKindDefinition(
            "holds", RelationshipPolarity.Neutral, agency, asset));
        registry.RegisterRelationshipKind(new RelationshipKindDefinition(
            "created_by", RelationshipPolarity.Neutral, asset, agency));
        registry.RegisterRelationshipKind(new RelationshipKindDefinition(
            "stored_at", RelationshipPolarity.Neutral, asset, spatial, isExclusive: true));
        registry.RegisterRelationshipKind(new RelationshipKindDefinition(
            "involved_in", RelationshipPolarity.Neutral, all, all));
    }
}
