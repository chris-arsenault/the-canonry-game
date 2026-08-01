namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Kinds;

using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;

// Actor succession, authored once for every actor kind (authorship
// parity, actor-parity audit 2026-08-01). A homed parent without an
// incoming member-of launches a successor pair, a carried craft, and a
// newly named anchor at the culture's frontier. The successors' member-of
// edges lock the parent to one launch; each successor homes at the new
// anchor and can later qualify as a parent once. Culture is inherited
// from the parent, so the same generator serves every culture that
// fields the kind.
public static partial class IceRemembersGeneratorDefinition
{
    private const int ActorSuccessionFireWeight = 12;

    private static SemanticGeneratorConfig PodCurrentsLaunchScouts() =>
        ActorSuccession(
            id: "pod_currents_launch_scouts",
            name: "Pod Currents Launch Scouts",
            description: "An orca with a settled hunting ground launches a scout pair, a carried skiff, and a newly named shoal. The current creates a replacement path through claimed waters instead of free-floating orca bodies.",
            actorKind: K.Orca,
            successorSubtype: "scout",
            anchorSubtype: "shoal");

    private static SemanticGeneratorConfig RookeryTerracesRaiseFledglings() =>
        ActorSuccession(
            id: "rookery_terraces_raise_fledglings",
            name: "Rookery Terraces Raise Fledglings",
            description: "A penguin with a settled home raises a fledgling pair, a laden sled, and a newly named rookery ground. The colony creates a replacement path on its own shore instead of strangers appearing at events.",
            actorKind: K.Penguin,
            successorSubtype: "witness",
            anchorSubtype: "rookery");

    private static SemanticGeneratorConfig ActorSuccession(
        string id,
        string name,
        string description,
        EntityKind actorKind,
        string successorSubtype,
        string anchorSubtype) => new()
    {
        Id = id,
        Name = name,
        Description = description,
        CausalLimit = "Depends on an actor home-base edge and no incoming member-of; each parent launches once, the successors' member-of edges are the lock, and the desperation surge is the floor beneath collapse.",
        Target = TargetWithOutgoingAndWithoutIncoming(actorKind, Rel.HomeBase, Rel.MemberOf),
        Produces =
        [
            Required("$heir", actorKind, successorSubtype, "$target", "$target"),
            Required("$heirmate", actorKind, successorSubtype, "$target", "$target"),
            Required("$craft", K.Transport, "scout", "$target", "$target"),
            Required("$anchor", K.PointOfInterest, anchorSubtype, "$target", "$target", frontier: true),
        ],
        Relationships =
        [
            // Both successors lock the parent: incoming member-of is the
            // once-per-parent gate.
            R("$heir", "$target", Rel.MemberOf, 0.78f),
            R("$heirmate", "$target", Rel.MemberOf, 0.74f),
            R("$target", "$heir", Rel.AlliedWith, 0.72f),
            R("$heir", "$craft", Rel.Holds, 1f),
            // The launch seeds a colony, not a lone successor: both home at
            // the anchor their own launch names, so each generation can
            // later qualify as a parent - once. Unlimited launches per
            // parent grew the pods into the world's dominant war machine
            // (316 births per world, 90% of all orcas).
            R("$heir", "$anchor", Rel.HomeBase, 0.8f),
            R("$heirmate", "$anchor", Rel.HomeBase, 0.8f),
            R("$heir", "$heirmate", Rel.AlliedWith, 0.76f),
            R("$craft", "$anchor", Rel.NativeTo, 0.84f),
            R("$craft", "$anchor", Rel.ConduitOf, 0.74f),
            R("$anchor", "$heir", Rel.ClaimedBy, 0.70f),
        ],
        Triggers = TriggerOn(actorKind),
        // Succession is the kind's only reproduction channel and starved at
        // low weight even with eligible parents standing by; the weight
        // keeps launches flowing while homed parents and the fire windows
        // pace the tempo.
        FireWeight = ActorSuccessionFireWeight,
    };
}
