namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Kinds;

using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;

// Desperation recoveries: when the leading culture outnumbers a culture
// past the desperation ratio, that culture's last holdouts answer with a
// single dramatic surge - a host of new entities and a few of the
// leader's own taken down. The ratio gate is the causal limit: firing
// restores the balance, which closes the gate. Fires through the normal
// fire windows like every other generator; no caps.
public static partial class IceRemembersGeneratorDefinition
{
    // 3:1 matches the owner's balance target of at most 60/20/20: the valve
    // opens whenever the leader exceeds three times a culture's count.
    private const float DesperationLeaderRatio = 3f;

    private static SemanticGeneratorConfig TheBeaconHostsTheDispossessed() => new()
    {
        Id = "the_beacon_hosts_the_dispossessed",
        Name = "The Beacon Hosts the Dispossessed",
        Description = "When the Stack has been all but extinguished, the beacon burns anyway, and everyone the leading power has wronged walks to it at once. A court reconvenes in exile, wardens take the wall, and two of the oppressor's own do not walk away.",
        CausalLimit = "Eligible only while the leading culture outnumbers Aurora past the desperation ratio; the surge itself restores the ratio and closes the gate.",
        DesperationRatio = DesperationLeaderRatio,
        Target = TargetAny(K.Settlement),
        SecondaryTargets =
        [
            LeaderVictim("$victim1"),
            LeaderVictim("$victim2"),
        ],
        Produces =
        [
            RequiredAurora("$court", K.Settlement, "holdfast", "$target", GeneratorCulturePlacement.CultureRoute, physicalFrontier: true),
            RequiredAurora("$warden", K.Penguin, "warden", "$court", GeneratorCulturePlacement.CultureRoute),
            RequiredAurora("$witness", K.Penguin, "witness", "$court", GeneratorCulturePlacement.CultureRoute),
            RequiredAurora("$hull", K.Transport, "battleship", "$court"),
        ],
        Relationships =
        [
            R("$warden", "$court", Rel.HomeBase, 1f),
            R("$witness", "$court", Rel.HomeBase, 1f),
            R("$warden", "$hull", Rel.Holds, 1f),
        ],
        Effects =
        [
            new SetBindingHistoricalEffect("$victim1"),
            new SetBindingHistoricalEffect("$victim2"),
        ],
        Triggers = TriggerOn(K.Settlement),
        FireWeight = 14,
    };

    private static SemanticGeneratorConfig EveryDebtComesDue() => new()
    {
        Id = "every_debt_comes_due",
        Name = "Every Debt Comes Due",
        Description = "When the Nightshelf has been driven to the last ledger, every hidden obligation is called in the same night. An undermarket opens where none was suspected, brokers step out of the dark, and two of the leading power's own are collected.",
        CausalLimit = "Eligible only while the leading culture outnumbers Nightshelf past the desperation ratio; the surge itself restores the ratio and closes the gate.",
        DesperationRatio = DesperationLeaderRatio,
        Target = TargetAny(K.Settlement),
        SecondaryTargets =
        [
            LeaderVictim("$victim1"),
            LeaderVictim("$victim2"),
        ],
        Produces =
        [
            RequiredNightshelf("$undermarket", K.Settlement, "holdfast", "$target", GeneratorCulturePlacement.Wilderness, physicalFrontier: true),
            RequiredNightshelf("$broker", K.Penguin, "broker", "$undermarket", GeneratorCulturePlacement.Wilderness),
            RequiredNightshelf("$fence", K.Penguin, "broker", "$undermarket", GeneratorCulturePlacement.Wilderness),
            RequiredNightshelf("$seal", K.Artifact, "seal", "$undermarket"),
        ],
        Relationships =
        [
            R("$broker", "$undermarket", Rel.HomeBase, 1f),
            R("$fence", "$undermarket", Rel.HomeBase, 1f),
            R("$broker", "$seal", Rel.Holds, 1f),
        ],
        Effects =
        [
            new SetBindingHistoricalEffect("$victim1"),
            new SetBindingHistoricalEffect("$victim2"),
        ],
        Triggers = TriggerOn(K.Settlement),
        FireWeight = 14,
    };

    private static SemanticGeneratorConfig TheDeepSingsTheDrownedSong() => new()
    {
        Id = "the_deep_sings_the_drowned_song",
        Name = "The Deep Sings the Drowned Song",
        Description = "When the Wake-Singers' song has nearly gone silent, the deep itself answers. A pod rises where the water remembers, a shoal is named for the drowned, and two of the leading power's own are pulled under.",
        CausalLimit = "Eligible only while the leading culture outnumbers the Wake-Singers past the desperation ratio; the surge itself restores the ratio and closes the gate.",
        DesperationRatio = DesperationLeaderRatio,
        Target = TargetAny(K.PointOfInterest),
        SecondaryTargets =
        [
            LeaderVictim("$victim1"),
            LeaderVictim("$victim2"),
        ],
        Produces =
        [
            RequiredWakeSingers("$shoal", K.PointOfInterest, "shoal", "$target", GeneratorCulturePlacement.CultureRoute, physicalFrontier: true),
            RequiredWakeSingers("$matriarch", K.Orca, "matriarch", "$shoal", GeneratorCulturePlacement.CultureRoute),
            RequiredWakeSingers("$scout", K.Orca, "scout", "$shoal", GeneratorCulturePlacement.CultureRoute),
        ],
        Relationships =
        [
            R("$matriarch", "$shoal", Rel.HomeBase, 1f),
            R("$scout", "$shoal", Rel.HomeBase, 1f),
            R("$scout", "$matriarch", Rel.MemberOf, 0.8f),
        ],
        Effects =
        [
            new SetBindingHistoricalEffect("$victim1"),
            new SetBindingHistoricalEffect("$victim2"),
        ],
        Triggers = TriggerOn(K.PointOfInterest),
        FireWeight = 14,
    };

    private static SemanticGeneratorTarget TargetAny(EntityKind kind) =>
        new() { Kind = kind };

    private static SemanticGeneratorBindingTarget LeaderVictim(string @ref) =>
        new()
        {
            Ref = @ref,
            Kind = K.Penguin,
            Query = new SemanticEntityQuery { LeaderCultureOnly = true },
        };
}
