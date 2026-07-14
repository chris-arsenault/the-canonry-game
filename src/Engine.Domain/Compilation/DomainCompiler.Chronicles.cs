namespace Engine.Domain.Compilation;

using Engine.Constants;
using Engine.Domain.Compilation.KernelPrograms;
using Engine.Domain.Config;

public static partial class DomainCompiler
{
    private static ChronicleProgram BuildChronicleProgram(DomainConfig config, DomainIdRegistry ids)
    {
        var shared = config.Semantics?.ChronicleSharedBeats ?? [];
        var archetypes = new List<CompiledChronicleArchetypeRule>();
        foreach (var archetype in config.Semantics?.ChronicleArchetypes ?? [])
        {
            archetypes.Add(CompileChronicleArchetype(archetype, shared, ids));
        }

        return new ChronicleProgram { Archetypes = archetypes };
    }

    private static CompiledChronicleArchetypeRule CompileChronicleArchetype(
        ChronicleArchetypeConfig archetype,
        IReadOnlyList<ChronicleBeatConfig> shared,
        DomainIdRegistry ids)
    {
        var roleIndices = BuildRoleIndices(archetype);
        var primaryRequirement = archetype.PrimaryRole.RequiredOutgoingRelationshipRequirements.Count > 0
            ? archetype.PrimaryRole.RequiredOutgoingRelationshipRequirements[0]
            : null;
        var beats = ChronicleBeatSet.EffectiveBeats(archetype, shared)
            .Select(beat => CompileChronicleBeat(archetype, beat, ids, roleIndices))
            .ToList();
        return new CompiledChronicleArchetypeRule(
            ids.GeneratorIdOf(archetype.Id).Value,
            archetype.MaxActive,
            archetype.CastingCadenceTicks,
            ids.KindIdOf(archetype.PrimaryRole.Kind.Value).Value,
            primaryRequirement is null ? 0 : ids.RelationshipKindIdOf(primaryRequirement.RelationshipKind).Value,
            primaryRequirement?.MinimumStrength ?? 0f,
            ids.ReasonCodeIdOf(archetype.FadeOutcomeId).Value,
            CompileSupportingRoles(archetype, ids),
            beats);
    }

    private static Dictionary<string, int> BuildRoleIndices(ChronicleArchetypeConfig archetype)
    {
        var indices = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [archetype.PrimaryRoleRef] = 0,
        };
        for (var role = 0; role < archetype.SupportingRoles.Count; role++)
        {
            indices[archetype.SupportingRoles[role].Ref] = role + 1;
        }

        foreach (var (stance, roleRef) in archetype.Stances)
        {
            if (!indices.ContainsKey(roleRef))
            {
                throw new InvalidOperationException(
                    $"Chronicle archetype '{archetype.Id}' maps stance '{stance}' to unknown role '{roleRef}'.");
            }
        }

        return indices;
    }

    private static List<CompiledChronicleRole> CompileSupportingRoles(ChronicleArchetypeConfig archetype, DomainIdRegistry ids)
    {
        var roles = new List<CompiledChronicleRole>();
        foreach (var role in archetype.SupportingRoles)
        {
            var query = role.Query;
            roles.Add(new CompiledChronicleRole(
                ids.KindIdOf(role.Kind.Value).Value,
                query?.RelatedToTargetRelationship is null ? 0 : ids.RelationshipKindIdOf(query.RelatedToTargetRelationship).Value,
                query?.RelatedToTargetIncoming ?? false,
                query?.RelatedToTargetMinStrength ?? 0f));
        }

        return roles;
    }

    private static CompiledChronicleBeat CompileChronicleBeat(
        ChronicleArchetypeConfig archetype,
        ChronicleBeatConfig beat,
        DomainIdRegistry ids,
        Dictionary<string, int> roleIndices)
    {
        var strike = beat.Strike;
        ValidateStrike(archetype, beat);
        return new CompiledChronicleBeat(
            ids.ReasonCodeIdOf(beat.Id).Value,
            beat.HeatGain,
            beat.TerminalOutcomeId is null ? 0 : ids.ReasonCodeIdOf(beat.TerminalOutcomeId).Value,
            CompileChronicleCondition(archetype, beat.Open, ids, roleIndices),
            (int)strike.Kind,
            RoleIndex(archetype, strike.ActorRole, roleIndices),
            RoleIndex(archetype, strike.TargetRole, roleIndices),
            CompileStrikeProducerId(strike, ids),
            strike.AcceptFailure,
            strike.Fact is null
                ? new CompiledChronicleCondition(ChronicleConditionCodes.None, 0, 0, 0, 0f)
                : CompileChronicleCondition(archetype, strike.Fact, ids, roleIndices),
            CompileCharges(archetype, beat, ids, roleIndices),
            CompileGrants(archetype, beat, ids, roleIndices));
    }

    private static void ValidateStrike(ChronicleArchetypeConfig archetype, ChronicleBeatConfig beat)
    {
        if (!StrikeShapeIsValid(beat.Strike))
        {
            throw new InvalidOperationException(
                $"Chronicle beat '{beat.Id}' of archetype '{archetype.Id}' has an invalid {beat.Strike.Kind} strike.");
        }

        if (beat.TerminalOutcomeId is not null && beat.Strike.Kind == ChronicleStrikeKind.None)
        {
            throw new InvalidOperationException(
                $"Chronicle beat '{beat.Id}' of archetype '{archetype.Id}' is terminal but never strikes.");
        }
    }

    private static bool StrikeShapeIsValid(ChronicleStrikeConfig strike) => strike.Kind switch
    {
        ChronicleStrikeKind.None => strike.Fact is null,
        ChronicleStrikeKind.RolePairGambit => strike.ActorRole is not null && strike.TargetRole is not null,
        ChronicleStrikeKind.OutsiderGambitOnRole => strike.TargetRole is not null,
        ChronicleStrikeKind.StateFact => strike.Fact is not null,
        ChronicleStrikeKind.RoleGambit => strike.ActorRole is not null && strike.Gambit is not null,
        ChronicleStrikeKind.GeneratorOnRole => strike.TargetRole is not null,
        _ => false,
    };

    /// <summary>
    /// The strike's producer filter shares one kernel column: gambit ids for
    /// execution strikes, generator ids for GeneratorOnRole strikes.
    /// </summary>
    private static int CompileStrikeProducerId(ChronicleStrikeConfig strike, DomainIdRegistry ids)
    {
        if (strike.Kind == ChronicleStrikeKind.GeneratorOnRole)
        {
            return strike.Generator is null ? 0 : ids.GeneratorIdOf(strike.Generator).Value;
        }

        return strike.Gambit is null ? 0 : ids.GambitIdOf(strike.Gambit).Value;
    }

    private static List<CompiledChronicleCharge> CompileCharges(
        ChronicleArchetypeConfig archetype,
        ChronicleBeatConfig beat,
        DomainIdRegistry ids,
        Dictionary<string, int> roleIndices) =>
        beat.Charges
            .Select(charge => new CompiledChronicleCharge(
                RoleIndex(archetype, charge.ActorRole, roleIndices),
                RoleIndex(archetype, charge.TargetRole, roleIndices),
                charge.Gambit is null ? 0 : ids.GambitIdOf(charge.Gambit).Value,
                charge.ScoreBonus,
                charge.HeatScaled))
            .ToList();

    private static List<CompiledChronicleGrant> CompileGrants(
        ChronicleArchetypeConfig archetype,
        ChronicleBeatConfig beat,
        DomainIdRegistry ids,
        Dictionary<string, int> roleIndices) =>
        beat.Grants
            .Select(grant => new CompiledChronicleGrant(
                RoleIndex(archetype, grant.Role, roleIndices),
                ids.GambitIdOf(grant.Gambit).Value))
            .ToList();

    private static CompiledChronicleCondition CompileChronicleCondition(
        ChronicleArchetypeConfig archetype,
        ChronicleConditionConfig condition,
        DomainIdRegistry ids,
        Dictionary<string, int> roleIndices) =>
        new(
            ChronicleConditionCode(condition.Kind),
            RoleIndex(archetype, condition.RoleA, roleIndices),
            RoleIndex(archetype, condition.RoleB, roleIndices),
            ChronicleConditionRelOrAttrId(archetype, condition, ids),
            condition.Value);

    private static readonly Dictionary<ChronicleConditionKind, int> ChronicleConditionCodesByKind =
        new()
        {
            [ChronicleConditionKind.Always] = ChronicleConditionCodes.Always,
            [ChronicleConditionKind.RolePairRelationship] = ChronicleConditionCodes.RolePairRelationship,
            [ChronicleConditionKind.RolePairRelationshipAbsent] = ChronicleConditionCodes.RolePairRelationshipAbsent,
            [ChronicleConditionKind.RoleHistorical] = ChronicleConditionCodes.RoleHistorical,
            [ChronicleConditionKind.RoleAttributeBelow] = ChronicleConditionCodes.RoleAttributeBelow,
            [ChronicleConditionKind.HeatAtLeast] = ChronicleConditionCodes.HeatAtLeast,
            [ChronicleConditionKind.RolePairWithinDistance] = ChronicleConditionCodes.RolePairWithinDistance,
            [ChronicleConditionKind.RolePairBeyondDistance] = ChronicleConditionCodes.RolePairBeyondDistance,
            [ChronicleConditionKind.RoleAttributeAtLeast] = ChronicleConditionCodes.RoleAttributeAtLeast,
            [ChronicleConditionKind.HeatBelow] = ChronicleConditionCodes.HeatBelow,
        };

    private static int ChronicleConditionCode(ChronicleConditionKind kind) =>
        ChronicleConditionCodesByKind.TryGetValue(kind, out var code)
            ? code
            : throw new ArgumentOutOfRangeException(nameof(kind), kind, null);

    private static int ChronicleConditionRelOrAttrId(
        ChronicleArchetypeConfig archetype,
        ChronicleConditionConfig condition,
        DomainIdRegistry ids)
    {
        switch (condition.Kind)
        {
            case ChronicleConditionKind.RolePairRelationship:
            case ChronicleConditionKind.RolePairRelationshipAbsent:
                var relationshipKind = condition.RelationshipKind ??
                    throw new InvalidOperationException(
                        $"Chronicle condition in archetype '{archetype.Id}' requires a relationship kind.");
                return ids.RelationshipKindIdOf(relationshipKind).Value;
            case ChronicleConditionKind.RoleAttributeBelow:
            case ChronicleConditionKind.RoleAttributeAtLeast:
                var attribute = condition.Attribute ??
                    throw new InvalidOperationException(
                        $"Chronicle condition in archetype '{archetype.Id}' requires an attribute.");
                return ids.AttributeIdOf(attribute).Value;
            default:
                return 0;
        }
    }

    private static int RoleIndex(
        ChronicleArchetypeConfig archetype,
        string? role,
        Dictionary<string, int> roleIndices)
    {
        if (role is null)
        {
            return 0;
        }

        return roleIndices.TryGetValue(role, out var index)
            ? index
            : throw new InvalidOperationException(
                $"Chronicle archetype '{archetype.Id}' references unknown role '{role}'.");
    }
}
