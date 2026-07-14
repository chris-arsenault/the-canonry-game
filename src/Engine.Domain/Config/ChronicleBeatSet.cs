namespace Engine.Domain.Config;

/// <summary>
//// Resolves an archetype's effective beat pool: shared (cross-story) beats
/// whose stance names all map onto the archetype's roles - rewritten to role
/// refs, narratives included - followed by the archetype's own beats. Used by
/// both the domain compiler and game-layer presentation so the pool is
/// assembled exactly once, the same way, in both places.
/// </summary>
public static class ChronicleBeatSet
{
    public static IReadOnlyList<ChronicleBeatConfig> EffectiveBeats(
        ChronicleArchetypeConfig archetype,
        IReadOnlyList<ChronicleBeatConfig> sharedBeats)
    {
        ArgumentNullException.ThrowIfNull(archetype);
        ArgumentNullException.ThrowIfNull(sharedBeats);
        var beats = new List<ChronicleBeatConfig>();
        if (archetype.Stances.Count > 0)
        {
            foreach (var beat in sharedBeats)
            {
                var rewritten = TryRewriteForArchetype(beat, archetype);
                if (rewritten is not null)
                {
                    beats.Add(rewritten);
                }
            }
        }

        beats.AddRange(archetype.Beats);
        return beats;
    }

    private static ChronicleBeatConfig? TryRewriteForArchetype(
        ChronicleBeatConfig beat,
        ChronicleArchetypeConfig archetype)
    {
        var open = RewriteCondition(beat.Open, archetype);
        var strike = TryRewriteStrike(beat.Strike, archetype);
        var charges = RewriteCharges(beat.Charges, archetype);
        var grants = RewriteGrants(beat.Grants, archetype);
        if (open is null || strike is null || charges is null || grants is null)
        {
            return null;
        }

        return new ChronicleBeatConfig
        {
            Id = beat.Id,
            Narrative = RewriteNarrative(beat.Narrative, archetype),
            Open = open,
            Strike = strike,
            HeatGain = beat.HeatGain,
            TerminalOutcomeId = beat.TerminalOutcomeId,
            Charges = charges,
            Grants = grants,
        };
    }

    private static ChronicleStrikeConfig? TryRewriteStrike(
        ChronicleStrikeConfig strike,
        ChronicleArchetypeConfig archetype)
    {
        var fact = strike.Fact is null ? null : RewriteCondition(strike.Fact, archetype);
        var actor = Rewrite(strike.ActorRole, archetype);
        var target = Rewrite(strike.TargetRole, archetype);
        if ((strike.Fact is not null && fact is null) ||
            (strike.ActorRole is not null && actor is null) ||
            (strike.TargetRole is not null && target is null))
        {
            return null;
        }

        return new ChronicleStrikeConfig
        {
            Kind = strike.Kind,
            ActorRole = actor,
            TargetRole = target,
            Gambit = strike.Gambit,
            Generator = strike.Generator,
            AcceptFailure = strike.AcceptFailure,
            JoinsRole = strike.JoinsRole,
            Fact = fact,
        };
    }

    private static ChronicleConditionConfig? RewriteCondition(
        ChronicleConditionConfig condition,
        ChronicleArchetypeConfig archetype)
    {
        var roleA = Rewrite(condition.RoleA, archetype);
        var roleB = Rewrite(condition.RoleB, archetype);
        if ((condition.RoleA is not null && roleA is null) ||
            (condition.RoleB is not null && roleB is null))
        {
            return null;
        }

        return new ChronicleConditionConfig
        {
            Kind = condition.Kind,
            RoleA = roleA,
            RoleB = roleB,
            RelationshipKind = condition.RelationshipKind,
            Attribute = condition.Attribute,
            Value = condition.Value,
        };
    }

    private static List<ChronicleChargeConfig>? RewriteCharges(
        IReadOnlyList<ChronicleChargeConfig> charges,
        ChronicleArchetypeConfig archetype)
    {
        var rewritten = new List<ChronicleChargeConfig>(charges.Count);
        foreach (var charge in charges)
        {
            var actor = Rewrite(charge.ActorRole, archetype);
            var target = Rewrite(charge.TargetRole, archetype);
            if (actor is null || target is null)
            {
                return null;
            }

            rewritten.Add(new ChronicleChargeConfig
            {
                ActorRole = actor,
                TargetRole = target,
                Gambit = charge.Gambit,
                ScoreBonus = charge.ScoreBonus,
                HeatScaled = charge.HeatScaled,
            });
        }

        return rewritten;
    }

    private static List<ChronicleGrantConfig>? RewriteGrants(
        IReadOnlyList<ChronicleGrantConfig> grants,
        ChronicleArchetypeConfig archetype)
    {
        var rewritten = new List<ChronicleGrantConfig>(grants.Count);
        foreach (var grant in grants)
        {
            var role = Rewrite(grant.Role, archetype);
            if (role is null)
            {
                return null;
            }

            rewritten.Add(new ChronicleGrantConfig { Role = role, Gambit = grant.Gambit });
        }

        return rewritten;
    }

    private static string? RewriteNarrative(string? narrative, ChronicleArchetypeConfig archetype)
    {
        if (narrative is null)
        {
            return null;
        }

        var result = narrative;
        foreach (var (stance, roleRef) in archetype.Stances)
        {
            result = result.Replace($"{{{stance}}}", $"{{{roleRef}}}", StringComparison.Ordinal);
        }

        return result;
    }

    /// <summary>
    /// A shared beat's role name is a stance; an archetype beat's is a role
    /// ref. Role refs pass through, stances translate, unknown names fail.
    /// </summary>
    private static string? Rewrite(string? name, ChronicleArchetypeConfig archetype)
    {
        if (name is null)
        {
            return null;
        }

        if (name.StartsWith('$'))
        {
            return name;
        }

        return archetype.Stances.TryGetValue(name, out var roleRef) ? roleRef : null;
    }
}
