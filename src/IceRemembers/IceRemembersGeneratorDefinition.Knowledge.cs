namespace IceRemembers;

using Engine.Domain.Config;

using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;

public static partial class IceRemembersGeneratorDefinition
{
    // The named face of frontier research. Idea-space itself climbs outward
    // through the semantic map-frontier (a culture echoes a head-start of its
    // mass into each cell it opens, so its edge carries outward instead of
    // stalling). This generator gives that climb its named figures: a faction
    // periodically sends a scholar to the near edge of the culture's idea-ground
    // - a static, physical-primary penguin who becomes an anchor for the tech
    // claims that will settle around them in later passes. Not the growth engine
    // itself; the growth is in the frontier's mass echo.
    private static SemanticGeneratorConfig FrontierScholarsSettleTheReaches() => new()
    {
        Id = "frontier_scholars_settle_the_reaches",
        Name = "Frontier Scholars Settle the Reaches",
        Description = "A faction sends one of its own to the far edge of what the culture understands. The scholar settles there and works the frontier, and idea-space opens a little further out around them.",
        CausalLimit = "Cadence-driven per eligible faction; plants a single frontier scholar at the culture's semantic border so new idea-ground keeps opening from fresh anchors rather than stalling at the heart's reach.",
        Target = new SemanticGeneratorTarget { Kind = K.Faction },
        Produces =
        [
            new SemanticProducedEntity
            {
                Ref = "$scholar",
                Kind = K.Penguin,
                Subtype = "scholar",
                Role = "scholar",
                // Physically the scholar stays near the launching faction; it is
                // idea-space they push into, not the ice.
                PhysicalPlacement = SemanticPlacement.Near,
                // Far out on the semantic plane (20+ rings from the faction, at
                // the edge of known idea-ground), preferring own-culture ground
                // but not requiring it. No HomeBase, so its idea-labor stays on
                // the frontier instead of remitting to the heart.
                SemanticPlacement = SemanticPlacement.Near,
                PlacementRef = "$target",
                InheritCultureFrom = "$target",
                CulturePlacement = GeneratorCulturePlacement.PreferInheritedCulture,
            },
        ],
        Relationships =
        [
            // The scholar belongs to the faction that sent it - an affiliation,
            // not a home base, so the tie does not drag the scholar's idea-labour
            // back to the heart. Its influence stays out on the frontier.
            R("$scholar", "$target", Rel.MemberOf, 0.7f),
        ],
        Triggers = TriggerOn(K.Faction),
        CadenceTicks = 150,
        CadenceJitterTicks = 50,
    };
}
