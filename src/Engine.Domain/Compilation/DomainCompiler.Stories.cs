namespace Engine.Domain.Compilation;

using Engine.Constants;
using Engine.Domain.Compilation.KernelPrograms;
using Engine.Domain.Config;

public static partial class DomainCompiler
{
    private static StoryProgram BuildStoryProgram(DomainConfig config, DomainIdRegistry ids)
    {
        var archetypes = new List<CompiledStoryArchetypeRule>();
        foreach (var archetype in config.Semantics?.StoryArchetypes ?? [])
        {
            archetypes.Add(CompileStoryArchetype(archetype, ids));
        }

        return new StoryProgram { Archetypes = archetypes };
    }

    private static CompiledStoryArchetypeRule CompileStoryArchetype(StoryArchetypeConfig archetype, DomainIdRegistry ids)
    {
        var roleIndices = BuildRoleIndices(archetype);
        var primaryRequirement = archetype.PrimaryRole.RequiredOutgoingRelationshipRequirements.Count > 0
            ? archetype.PrimaryRole.RequiredOutgoingRelationshipRequirements[0]
            : null;
        return new CompiledStoryArchetypeRule(
            ids.GeneratorIdOf(archetype.Id).Value,
            archetype.MaxActive,
            archetype.CastingCadenceTicks,
            ids.KindIdOf(archetype.PrimaryRole.Kind.Value).Value,
            primaryRequirement is null ? 0 : ids.RelationshipKindIdOf(primaryRequirement.RelationshipKind).Value,
            primaryRequirement?.MinimumStrength ?? 0f,
            CompileSupportingRoles(archetype, ids),
            CompileStoryActs(archetype, ids, roleIndices),
            CompileStoryResolutions(archetype, ids, roleIndices));
    }

    private static Dictionary<string, int> BuildRoleIndices(StoryArchetypeConfig archetype)
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

    private static List<CompiledStoryRole> CompileSupportingRoles(StoryArchetypeConfig archetype, DomainIdRegistry ids)
    {
        var roles = new List<CompiledStoryRole>();
        foreach (var role in archetype.SupportingRoles)
        {
            var query = role.Query;
            roles.Add(new CompiledStoryRole(
                ids.KindIdOf(role.Kind.Value).Value,
                query?.RelatedToTargetRelationship is null ? 0 : ids.RelationshipKindIdOf(query.RelatedToTargetRelationship).Value,
                query?.RelatedToTargetIncoming ?? false,
                query?.RelatedToTargetMinStrength ?? 0f));
        }

        return roles;
    }

    private static List<CompiledStoryAct> CompileStoryActs(
        StoryArchetypeConfig archetype,
        DomainIdRegistry ids,
        Dictionary<string, int> roleIndices)
    {
        var acts = new List<CompiledStoryAct>();
        foreach (var act in archetype.Acts)
        {
            var condition = CompileStoryCondition(act.AdvanceWhen, ids, roleIndices);
            acts.Add(new CompiledStoryAct(
                act.DeadlineTicks,
                condition.Kind,
                condition.RoleA,
                condition.RoleB,
                condition.RelKindId,
                condition.MinStrength,
                act.Bias
                    .Select(bias => new CompiledStoryBias(
                        roleIndices[bias.ActorRole],
                        roleIndices[bias.TargetRole],
                        bias.Gambit is null ? 0 : ids.GambitIdOf(bias.Gambit).Value,
                        bias.ScoreBonus))
                    .ToList(),
                act.Sustain
                    .Select(sustain => new CompiledStorySustain(
                        roleIndices[sustain.Role],
                        ids.AttributeIdOf(sustain.Attribute).Value,
                        sustain.DeltaPerPulse))
                    .ToList()));
        }

        return acts;
    }

    private static List<CompiledStoryResolution> CompileStoryResolutions(
        StoryArchetypeConfig archetype,
        DomainIdRegistry ids,
        Dictionary<string, int> roleIndices)
    {
        var resolutions = new List<CompiledStoryResolution>();
        foreach (var resolution in archetype.Resolutions)
        {
            var when = CompileStoryCondition(resolution.When, ids, roleIndices);
            var mask = 0;
            foreach (var role in resolution.MarkHistorical)
            {
                mask |= 1 << roleIndices[role];
            }

            resolutions.Add(new CompiledStoryResolution(
                ids.ReasonCodeIdOf(resolution.OutcomeId).Value,
                resolution.Weight,
                when.Kind,
                when.RoleA,
                when.RoleB,
                when.RelKindId,
                when.MinStrength,
                mask,
                resolution.Relationships
                    .Select(rel => new CompiledStoryResolutionRelationship(
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

    private static (int Kind, int RoleA, int RoleB, int RelKindId, float MinStrength) CompileStoryCondition(
        StoryConditionConfig? condition,
        DomainIdRegistry ids,
        Dictionary<string, int> roleIndices)
    {
        if (condition is null)
        {
            return (StoryConditionCodes.None, 0, 0, 0, 0f);
        }

        return condition.Kind switch
        {
            StoryConditionKind.RolePairRelationship => (
                StoryConditionCodes.RolePairRelationship,
                roleIndices[condition.RoleA],
                roleIndices[condition.RoleB ?? condition.RoleA],
                ids.RelationshipKindIdOf(condition.RelationshipKind ?? throw new InvalidOperationException($"Story condition on role '{condition.RoleA}' requires a relationship kind.")).Value,
                condition.MinStrength),
            StoryConditionKind.RoleHistorical => (
                StoryConditionCodes.RoleHistorical,
                roleIndices[condition.RoleA],
                0,
                0,
                0f),
            _ => throw new ArgumentOutOfRangeException(nameof(condition), condition.Kind, null),
        };
    }
}
