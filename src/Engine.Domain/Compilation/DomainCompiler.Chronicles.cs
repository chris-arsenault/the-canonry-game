namespace Engine.Domain.Compilation;

using Engine.Constants;
using Engine.Domain.Compilation.KernelPrograms;
using Engine.Domain.Config;

public static partial class DomainCompiler
{
    private static ChronicleProgram BuildChronicleProgram(DomainConfig config, DomainIdRegistry ids)
    {
        var archetypes = new List<CompiledChronicleArchetypeRule>();
        foreach (var archetype in config.Semantics?.ChronicleArchetypes ?? [])
        {
            archetypes.Add(CompileChronicleArchetype(archetype, ids));
        }

        return new ChronicleProgram { Archetypes = archetypes };
    }

    private static CompiledChronicleArchetypeRule CompileChronicleArchetype(ChronicleArchetypeConfig archetype, DomainIdRegistry ids)
    {
        var roleIndices = BuildRoleIndices(archetype);
        var primaryRequirement = archetype.PrimaryRole.RequiredOutgoingRelationshipRequirements.Count > 0
            ? archetype.PrimaryRole.RequiredOutgoingRelationshipRequirements[0]
            : null;
        return new CompiledChronicleArchetypeRule(
            ids.GeneratorIdOf(archetype.Id).Value,
            archetype.MaxActive,
            archetype.CastingCadenceTicks,
            ids.KindIdOf(archetype.PrimaryRole.Kind.Value).Value,
            primaryRequirement is null ? 0 : ids.RelationshipKindIdOf(primaryRequirement.RelationshipKind).Value,
            primaryRequirement?.MinimumStrength ?? 0f,
            CompileSupportingRoles(archetype, ids),
            CompileChronicleActs(archetype, ids, roleIndices),
            CompileChronicleResolutions(archetype, ids, roleIndices));
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

    private static List<CompiledChronicleAct> CompileChronicleActs(
        ChronicleArchetypeConfig archetype,
        DomainIdRegistry ids,
        Dictionary<string, int> roleIndices)
    {
        var acts = new List<CompiledChronicleAct>();
        foreach (var act in archetype.Acts)
        {
            var condition = CompileChronicleCondition(act.AdvanceWhen, ids, roleIndices);
            acts.Add(new CompiledChronicleAct(
                act.DeadlineTicks,
                condition.Kind,
                condition.RoleA,
                condition.RoleB,
                condition.RelKindId,
                condition.MinStrength,
                act.Bias
                    .Select(bias => new CompiledChronicleBias(
                        roleIndices[bias.ActorRole],
                        roleIndices[bias.TargetRole],
                        bias.Gambit is null ? 0 : ids.GambitIdOf(bias.Gambit).Value,
                        bias.ScoreBonus))
                    .ToList(),
                act.Sustain
                    .Select(sustain => new CompiledChronicleSustain(
                        roleIndices[sustain.Role],
                        ids.AttributeIdOf(sustain.Attribute).Value,
                        sustain.DeltaPerPulse))
                    .ToList()));
        }

        return acts;
    }

    private static List<CompiledChronicleResolution> CompileChronicleResolutions(
        ChronicleArchetypeConfig archetype,
        DomainIdRegistry ids,
        Dictionary<string, int> roleIndices)
    {
        var resolutions = new List<CompiledChronicleResolution>();
        foreach (var resolution in archetype.Resolutions)
        {
            var when = CompileChronicleCondition(resolution.When, ids, roleIndices);
            var mask = 0;
            foreach (var role in resolution.MarkHistorical)
            {
                mask |= 1 << roleIndices[role];
            }

            resolutions.Add(new CompiledChronicleResolution(
                ids.ReasonCodeIdOf(resolution.OutcomeId).Value,
                resolution.Weight,
                when.Kind,
                when.RoleA,
                when.RoleB,
                when.RelKindId,
                when.MinStrength,
                mask,
                resolution.Relationships
                    .Select(rel => new CompiledChronicleResolutionRelationship(
                        roleIndices[rel.SourceRole],
                        roleIndices[rel.TargetRole],
                        ids.RelationshipKindIdOf(rel.RelationshipKind).Value,
                        (int)rel.Polarity,
                        rel.Strength,
                        rel.CullInstead))
                    .ToList()));
        }

        return resolutions;
    }

    private static (int Kind, int RoleA, int RoleB, int RelKindId, float MinStrength) CompileChronicleCondition(
        ChronicleConditionConfig? condition,
        DomainIdRegistry ids,
        Dictionary<string, int> roleIndices)
    {
        if (condition is null)
        {
            return (ChronicleConditionCodes.None, 0, 0, 0, 0f);
        }

        return condition.Kind switch
        {
            ChronicleConditionKind.RolePairRelationship => (
                ChronicleConditionCodes.RolePairRelationship,
                roleIndices[condition.RoleA],
                roleIndices[condition.RoleB ?? condition.RoleA],
                ids.RelationshipKindIdOf(condition.RelationshipKind ?? throw new InvalidOperationException($"Story condition on role '{condition.RoleA}' requires a relationship kind.")).Value,
                condition.MinStrength),
            ChronicleConditionKind.RoleHistorical => (
                ChronicleConditionCodes.RoleHistorical,
                roleIndices[condition.RoleA],
                0,
                0,
                0f),
            _ => throw new ArgumentOutOfRangeException(nameof(condition), condition.Kind, null),
        };
    }
}
