namespace IceRemembers;

using Engine.Domain.Config;

using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;

// Each culture's native devotion consequence. Everyone leaves offerings
// now, but until these two only Wake-Singer rites consumed them - the
// other cultures worshipped into their rival's economy (track fidelity:
// devotion births 91% Wake-Singer). The Stack's sky answers devotion with
// judgment; the Nightshelf's dead answer with kept secrets.
public static partial class IceRemembersGeneratorDefinition
{
    private const float RiteMinimumOfferingStrength = 0.50f;
    private const float RiteOfferingSpend = -0.30f;

    private static SemanticGeneratorConfig TheSkyRendersJudgment() => new()
    {
        Id = "the_sky_renders_judgment",
        Name = "The Sky Renders Judgment",
        Description = "A site heavy with offerings draws the aurora's attention, and the light passes sentence. A warden of the Stack takes up the vigil there, and a reading of the judgment enters the open record.",
        CausalLimit = "Depends on strong incoming offering strength; spends the site's offerings when the judgment falls.",
        Target = TargetWithStrongIncomingAndWithoutIncoming(
            K.PointOfInterest,
            Rel.OfferedTo,
            RiteMinimumOfferingStrength,
            Rel.CleansedBy),
        Produces =
        [
            RequiredAurora("$warden", K.Penguin, "warden", "$target"),
            RequiredAurora("$reading", K.Artifact, "tome", "$target"),
        ],
        Relationships =
        [
            R("$warden", "$target", Rel.HomeBase, 0.85f),
            R("$warden", "$reading", Rel.Holds, 1f),
        ],
        Effects =
        [
            new AdjustTargetRelationshipStrengthEffect(
                L(Rel.OfferedTo),
                RiteOfferingSpend,
                GeneratorRelationshipInstanceSelection.AllTargetRelationships),
        ],
        Triggers = TriggerOn(K.PointOfInterest),
        FireWeight = 8,
    };

    private static SemanticGeneratorConfig TheDeadKeepTheirSecrets() => new()
    {
        Id = "the_dead_keep_their_secrets",
        Name = "The Dead Keep Their Secrets",
        Description = "Where offerings pool in the dark, the Nightshelf's dead answer in their own coin. An archivist of the shelf takes custody of the site, and one sealed secret - something the offerings paid to keep quiet - enters the ledger.",
        CausalLimit = "Depends on strong incoming offering strength; spends the site's offerings when the secret is sealed.",
        Target = TargetWithStrongIncomingAndWithoutIncoming(
            K.PointOfInterest,
            Rel.OfferedTo,
            RiteMinimumOfferingStrength,
            Rel.CleansedBy),
        Produces =
        [
            RequiredNightshelf("$archivist", K.Penguin, "witness", "$target"),
            RequiredNightshelf("$secret", K.Artifact, "seal", "$target"),
        ],
        Relationships =
        [
            R("$archivist", "$target", Rel.HomeBase, 0.85f),
            R("$archivist", "$secret", Rel.Holds, 1f),
            R("$target", "$archivist", Rel.ClaimedBy, 0.70f),
        ],
        Effects =
        [
            new AdjustTargetRelationshipStrengthEffect(
                L(Rel.OfferedTo),
                RiteOfferingSpend,
                GeneratorRelationshipInstanceSelection.AllTargetRelationships),
        ],
        Triggers = TriggerOn(K.PointOfInterest),
        FireWeight = 8,
    };
}
