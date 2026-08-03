namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Kinds;

using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;

// Actor succession as one kind-general generator (authorship parity,
// corrected 2026-08-03): it targets a homed parent of either actor kind
// and produces successors of the target's kind - neutral homeostasis,
// production follows presence. A homed parent without an incoming
// member-of launches a successor pair, a carried craft, and a newly named
// anchor at the culture's frontier: orca parents launch scout pairs at a
// shoal, penguin parents raise fledglings at a rookery ground. The
// successors' member-of edges lock the parent to one launch; each
// successor homes at the new anchor and can later qualify as a parent
// once. Culture is inherited from the parent.
public static partial class IceRemembersGeneratorDefinition
{
    private static SemanticGeneratorConfig TheLineContinues() => new()
    {
        Id = "the_line_continues",
        Name = "The Line Continues",
        Description = "A settled elder raises the next generation: orca parents launch a scout pair at a newly named shoal, penguin parents raise a fledgling pair at a new rookery ground. The people replace themselves on their own terms, at their own anchors.",
        CausalLimit = "Depends on an actor home-base edge and no incoming member-of; each parent launches once, the successors' member-of edges are the lock, and the desperation surge is the floor beneath collapse.",
        Target = new SemanticGeneratorTarget
        {
            Kind = K.Orca,
            SecondKind = K.Penguin,
            RequiredOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(Rel.HomeBase) },
            WithoutIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(Rel.MemberOf) },
        },
        Produces =
        [
            Required("$heir", K.Orca, "scout", "$target", "$target", onlyWhenTargetKind: K.Orca),
            Required("$heirmate", K.Orca, "scout", "$target", "$target", onlyWhenTargetKind: K.Orca),
            Required("$anchor", K.PointOfInterest, "shoal", "$target", "$target", frontier: true, onlyWhenTargetKind: K.Orca),
            Required("$fledgling", K.Penguin, "witness", "$target", "$target", onlyWhenTargetKind: K.Penguin),
            Required("$fledgemate", K.Penguin, "witness", "$target", "$target", onlyWhenTargetKind: K.Penguin),
            Required("$ground", K.PointOfInterest, "rookery", "$target", "$target", frontier: true, onlyWhenTargetKind: K.Penguin),
            Required("$craft", K.Transport, "scout", "$target", "$target"),
        ],
        Relationships =
        [
            // Both successors lock the parent: incoming member-of is the
            // once-per-parent gate. Relationships whose refs were not
            // created (the other kind's gated slots) are skipped.
            R("$heir", "$target", Rel.MemberOf, 0.78f),
            R("$heirmate", "$target", Rel.MemberOf, 0.74f),
            R("$target", "$heir", Rel.AlliedWith, 0.72f),
            R("$heir", "$craft", Rel.Holds, 1f),
            R("$heir", "$anchor", Rel.HomeBase, 0.8f),
            R("$heirmate", "$anchor", Rel.HomeBase, 0.8f),
            R("$heir", "$heirmate", Rel.AlliedWith, 0.76f),
            R("$craft", "$anchor", Rel.NativeTo, 0.84f),
            R("$craft", "$anchor", Rel.ConduitOf, 0.74f),
            R("$anchor", "$heir", Rel.ClaimedBy, 0.70f),
            R("$fledgling", "$target", Rel.MemberOf, 0.78f),
            R("$fledgemate", "$target", Rel.MemberOf, 0.74f),
            R("$target", "$fledgling", Rel.AlliedWith, 0.72f),
            R("$fledgling", "$craft", Rel.Holds, 1f),
            R("$fledgling", "$ground", Rel.HomeBase, 0.8f),
            R("$fledgemate", "$ground", Rel.HomeBase, 0.8f),
            R("$fledgling", "$fledgemate", Rel.AlliedWith, 0.76f),
            R("$craft", "$ground", Rel.NativeTo, 0.84f),
            R("$craft", "$ground", Rel.ConduitOf, 0.74f),
            R("$ground", "$fledgling", Rel.ClaimedBy, 0.70f),
        ],
        Triggers = TriggerOnActors(),
        // Succession is the actors' only reproduction channel and starved at
        // low weight even with eligible parents standing by; the weight
        // keeps launches flowing while homed parents and the fire windows
        // pace the tempo.
        FireWeight = 12,
    };

    // Carriers of either actor kind recognise a place to stop (kind-general
    // target; the settled home works for both kinds).
    private static SemanticGeneratorConfig WhatTheArtifactKnows() => new()
    {
        Id = "what_the_artifact_knows",
        Name = "What the Artifact Knows",
        Description = "A carrier who has held an artifact long enough recognises a place to stop. They put down a home, seal the artifact into the ground, and a small cell may form around them.",
        CausalLimit = "Depends on a held artifact and absence of a home base; creates a home-base relationship for the carrier.",
        Target = new SemanticGeneratorTarget
        {
            Kind = K.Penguin,
            SecondKind = K.Orca,
            RequiredOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(Rel.Holds) },
            WithoutOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(Rel.HomeBase) },
        },
        Produces =
        [
            Required("$rookery", K.Settlement, "rookery", "$target", "$target", frontier: true),
            Required("$seal", K.Artifact, "seal", "$rookery", "$target"),
            Optional("$cell", K.Faction, "cell", "$target", "$target", chance: 0.45f),
        ],
        Relationships =
        [
            R("$target", "$rookery", Rel.HomeBase, 1f),
            R("$seal", "$rookery", Rel.Anchored, 0.88f),
            R("$rookery", "$cell", Rel.ControlledBy, 0.82f),
            R("$cell", "$target", Rel.LedBy, 0.85f),
        ],
        Triggers = TriggerOnActors(),
    };
}
