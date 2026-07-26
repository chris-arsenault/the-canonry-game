namespace IceRemembers;

using Engine.Domain.Config;

using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;
using Terrain = IceRemembers.IceRemembersTerrain;

// The thin-palette kinds get their loops: wars mark the ground where they
// were fought, pods name the water that never closes, and canons move
// because someone carries them. The place-kinds stay static - their agency
// is what happens around them, not walking.
public static partial class IceRemembersGeneratorDefinition
{
    private const int WarMarkWasteRadius = 5;
    private const int WarMarkMaximumLocalWastes = 8;

    private static IReadOnlyList<SemanticGeneratorConfig> CreatePaletteGenerators() =>
    [
        TheWarLeavesItsMark(),
        ThePolynyaNeverCloses(),
        TheCanonIsCarried(),
    ];

    // A named war is a place as much as an event. Where it grinds on, the
    // ground under it goes to wastes - the occurrence itself becomes
    // terrain-consequential, not just an edge in the graph.
    private static SemanticGeneratorConfig TheWarLeavesItsMark() => new()
    {
        Id = "the_war_leaves_its_mark",
        Name = "The War Leaves Its Mark",
        Description = "The fighting has a name now, and the ground beneath it is learning what that costs. Around the war's heart the land goes to wastes; the ice records the dead with more clarity than the living.",
        CausalLimit = "Depends on a named war that has not yet scarred its ground and fewer than eight local waste cells; the scar's corrupted_by edge on the war is the repeat lock.",
        Target = new SemanticGeneratorTarget
        {
            Kind = K.Occurrence,
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { "war" },
            RequiredOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(Rel.AtWarWith) },
            WithoutIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(Rel.CorruptedBy) },
            TerrainCountRequirements =
            [
                new SemanticGeneratorTerrainCountRequirement
                {
                    TerrainName = Terrain.Wastes.ToDomainName(),
                    Radius = WarMarkWasteRadius,
                    MaximumExclusive = WarMarkMaximumLocalWastes,
                },
            ],
        },
        Produces =
        [
            ScarRegion("$scar", "$target"),
        ],
        Relationships =
        [
            R("$scar", "$target", Rel.CorruptedBy, 0.80f),
        ],
        Triggers = TriggerOn(K.Occurrence),
        FireWeight = 8,
    };

    // The Frozen Sea's one mercy: water that stays open through the dark.
    // A pod that knows its ground adds the polynya to the song-map, and the
    // shoal it names draws the fishing and the hunting after it.
    private static SemanticGeneratorConfig ThePolynyaNeverCloses() => new()
    {
        Id = "the_polynya_never_closes",
        Name = "The Polynya Never Closes",
        Description = "Even in the Long Dark some water refuses the freeze. A pod that holds its hunting ground carries the open water's shape back through the deep, and the shoal enters the song-map: krill under the dark, a breathing place, a name.",
        CausalLimit = "Depends on a pod with a hunting ground that has not yet named a shoal; the pod's conduit_of edge is the repeat lock.",
        Target = new SemanticGeneratorTarget
        {
            Kind = K.Orca,
            RequiredOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(Rel.HomeBase) },
            WithoutOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(Rel.ConduitOf) },
        },
        Produces =
        [
            Required("$polynya", K.PointOfInterest, "shoal", "$target", "$target"),
        ],
        Relationships =
        [
            R("$target", "$polynya", Rel.ConduitOf, 0.65f),
            R("$polynya", "$target", Rel.ClaimedBy, 0.55f),
        ],
        Triggers = TriggerOn(K.Orca),
        FireWeight = 4,
    };

    // Canons do not walk; they are carried. A held saga or accord takes a
    // carrier - courier-fashion, belonging to the route more than to either
    // end - and the idea starts moving along the living roads.
    private static SemanticGeneratorConfig TheCanonIsCarried() => new()
    {
        Id = "the_canon_is_carried",
        Name = "The Canon Is Carried",
        Description = "A canon strong enough to be claimed is strong enough to travel. Someone takes it up courier-fashion - posts the route, keeps the schedule - and what was one place's law or one hero's saga starts arriving where the roads go.",
        CausalLimit = "Depends on a claimed canon with no carrier yet; the carrier's conduit_of edge is the repeat lock.",
        Target = new SemanticGeneratorTarget
        {
            Kind = K.Canon,
            RequiredOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(Rel.ClaimedBy) },
            WithoutIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(Rel.ConduitOf) },
        },
        Produces =
        [
            Required("$carrier", K.Penguin, "broker", "$target", "$target"),
        ],
        Relationships =
        [
            R("$carrier", "$target", Rel.ConduitOf, 0.70f),
            R("$carrier", "$target", Rel.Holds, 0.60f),
        ],
        Triggers = TriggerOn(K.Canon),
        FireWeight = 4,
    };
}
