namespace Engine.Gameplay;

public static class GameplayValidator
{
    public static GameplayValidationResult Validate(GameplayDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var errors = new List<GameplayValidationError>();
        ValidateLowAltitudeVerbDefinitions(definition.LowAltitudeVerbs, errors);
        return new GameplayValidationResult(errors);
    }

    public static IReadOnlyList<GameplayValidationError> ValidateRelationshipMaintenance(
        string relationshipId,
        IReadOnlyList<RelationshipMaintenanceRuleConfig> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var errors = new List<GameplayValidationError>();
        for (var index = 0; index < rules.Count; index++)
        {
            ValidateRelationshipMaintenancePlane(relationshipId, index, rules[index], errors);
        }

        return errors;
    }

    private static void ValidateLowAltitudeVerbDefinitions(
        IReadOnlyList<LowAltitudeVerbConfig> verbs,
        List<GameplayValidationError> errors)
    {
        foreach (var verb in verbs)
        {
            ValidateLowAltitudeVerb(verb, errors);
        }
    }

    private static void ValidateLowAltitudeVerb(
        LowAltitudeVerbConfig verb,
        List<GameplayValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(verb.Id))
        {
            errors.Add(new GameplayValidationError("UnsupportedVerb", "NoRuntimeFallthrough: low-altitude verbs require ids."));
        }

        if (string.IsNullOrWhiteSpace(verb.Kind))
        {
            errors.Add(new GameplayValidationError("MissingKind", $"Low-altitude verb '{verb.Id}' requires a kind."));
        }

        var plane = verb.Plane ?? "physical";
        if (!IsPhysical(plane) && !IsSemantic(plane))
        {
            errors.Add(new GameplayValidationError("InvalidPlane", $"Low-altitude verb '{verb.Id}' uses unsupported plane '{plane}'."));
            return;
        }

        var shape = LowAltitudeVerbShape.For(verb.Operation);
        ValidateAuthoredVerbShape(verb, errors);
        if (!shape.AllowsPlane(plane))
        {
            errors.Add(new GameplayValidationError(
                "InvalidVerbPlane",
                $"Low-altitude verb '{verb.Id}' operation '{verb.Operation}' cannot run on plane '{plane}'."));
        }

        if (!shape.TargetModes.Contains(verb.TargetMode))
        {
            errors.Add(new GameplayValidationError(
                "InvalidVerbTargetMode",
                $"Low-altitude verb '{verb.Id}' operation '{verb.Operation}' cannot target '{verb.TargetMode}'."));
        }

        ValidateTargetFields(verb, plane, errors);
        ValidateInfluenceFields(verb, shape, errors);
        ValidateForeignInfluenceTargeting(verb, errors);
        ValidateTiming(verb, errors);
    }

    private static void ValidateAuthoredVerbShape(
        LowAltitudeVerbConfig verb,
        List<GameplayValidationError> errors)
    {
        switch (verb)
        {
            case MovementLowAltitudeVerbConfig:
                break;
            case InfluenceLowAltitudeVerbConfig:
                ValidateInfluenceVerbShape(verb, errors);
                break;
            case CoverageExpansionLowAltitudeVerbConfig:
            case AnchorRelocationLowAltitudeVerbConfig:
            case RelationshipStrengthLowAltitudeVerbConfig:
                break;
            default:
                errors.Add(new GameplayValidationError(
                    "UnsupportedVerbShape",
                    $"Low-altitude verb '{verb.Id}' uses unsupported authored verb shape."));
                break;
        }
    }

    private static void ValidateInfluenceVerbShape(
        LowAltitudeVerbConfig verb,
        List<GameplayValidationError> errors)
    {
        if (verb.Operation is not LowAltitudeVerbOperation.GrowInfluence and
            not LowAltitudeVerbOperation.ConsolidateInfluence and
            not LowAltitudeVerbOperation.ReduceInfluence)
        {
            errors.Add(new GameplayValidationError(
                "InvalidInfluenceShapeOperation",
                $"Low-altitude verb '{verb.Id}' influence shape cannot use operation '{verb.Operation}'."));
        }
    }

    private static void ValidateTargetFields(
        LowAltitudeVerbConfig verb,
        string plane,
        List<GameplayValidationError> errors)
    {
        var hasTargetKind = !string.IsNullOrWhiteSpace(verb.TargetKind);
        var hasRelationship = !string.IsNullOrWhiteSpace(verb.RelationshipKind);
        var hasTerrains = verb.Terrains is { Count: > 0 };
        switch (verb.TargetMode)
        {
            case LowAltitudeVerbTargetMode.Explore:
            case LowAltitudeVerbTargetMode.OwnInfluence:
            case LowAltitudeVerbTargetMode.ForeignInfluence:
            case LowAltitudeVerbTargetMode.ChroniclePursuit:
            case LowAltitudeVerbTargetMode.AttractorTrek:
                RejectField(verb, hasTargetKind, "TargetKind", errors);
                RejectField(verb, hasRelationship, "RelationshipKind", errors);
                RejectField(verb, hasTerrains, "Terrains", errors);
                break;
            case LowAltitudeVerbTargetMode.EntityKind:
                RequireField(verb, hasTargetKind, "TargetKind", errors);
                RejectField(verb, hasRelationship, "RelationshipKind", errors);
                RejectField(verb, hasTerrains, "Terrains", errors);
                break;
            case LowAltitudeVerbTargetMode.RelationshipTarget:
                RejectField(verb, hasTargetKind, "TargetKind", errors);
                RequireField(verb, hasRelationship, "RelationshipKind", errors);
                RejectField(verb, hasTerrains, "Terrains", errors);
                break;
            case LowAltitudeVerbTargetMode.Terrain:
                RejectField(verb, hasTargetKind, "TargetKind", errors);
                RejectField(verb, hasRelationship, "RelationshipKind", errors);
                RequireField(verb, HasCellPredicate(verb, hasTerrains), "Terrains", errors);
                if (IsSemantic(plane))
                {
                    errors.Add(new GameplayValidationError(
                        "InvalidTerrainPlane",
                        $"Low-altitude verb '{verb.Id}' cannot use terrain targeting on the semantic plane."));
                }

                break;
            default:
                errors.Add(new GameplayValidationError("UnsupportedTargetMode", $"Low-altitude verb '{verb.Id}' uses unsupported target mode."));
                break;
        }
    }

    // A terrain-mode verb needs some cell predicate: an exact terrain list,
    // a minimum warmth band, or a site-flag mask.
    private static bool HasCellPredicate(LowAltitudeVerbConfig verb, bool hasTerrains) =>
        hasTerrains || verb.MinWarmthBand > 0 || verb.Sites is { Count: > 0 };

    private static void ValidateInfluenceFields(
        LowAltitudeVerbConfig verb,
        LowAltitudeVerbShape shape,
        List<GameplayValidationError> errors)
    {
        ValidateInfluenceDelta(verb, shape, errors);
        ValidateCoverageGrowth(verb, shape, errors);
        ValidateRelocation(verb, shape, errors);
        ValidateRequiredMapValue(verb, shape, errors);
        ValidateRelationshipStrengthDelta(verb, shape, errors);
    }

    private static void ValidateInfluenceDelta(
        LowAltitudeVerbConfig verb,
        LowAltitudeVerbShape shape,
        List<GameplayValidationError> errors)
    {
        if (shape.RequiresInfluenceDelta && verb.InfluenceDelta <= 0f)
        {
            errors.Add(new GameplayValidationError(
                "MissingInfluenceDelta",
                $"Low-altitude verb '{verb.Id}' operation '{verb.Operation}' requires positive influence delta."));
        }

        if (!shape.RequiresInfluenceDelta &&
            !shape.AllowsOptionalInfluenceDelta &&
            (verb.InfluenceDelta != 0f || verb.InfluenceRadius != 0))
        {
            errors.Add(new GameplayValidationError(
                "InvalidInfluenceFields",
                $"Low-altitude verb '{verb.Id}' operation '{verb.Operation}' cannot carry influence delta/radius fields."));
        }

        if (shape.AllowsOptionalInfluenceDelta)
        {
            ValidateOptionalInfluenceDelta(verb, errors);
        }
    }

    private static void ValidateOptionalInfluenceDelta(
        LowAltitudeVerbConfig verb,
        List<GameplayValidationError> errors)
    {
        if (verb.InfluenceDelta < 0f)
        {
            errors.Add(new GameplayValidationError(
                "InvalidInfluenceFields",
                $"Low-altitude verb '{verb.Id}' operation '{verb.Operation}' cannot carry negative influence delta."));
        }

        if (verb.InfluenceRadius < 0)
        {
            errors.Add(new GameplayValidationError(
                "InvalidInfluenceFields",
                $"Low-altitude verb '{verb.Id}' operation '{verb.Operation}' cannot carry negative influence radius."));
        }

        if (verb.InfluenceDelta == 0f &&
            verb.InfluenceRadius != 0)
        {
            errors.Add(new GameplayValidationError(
                "InvalidInfluenceFields",
                $"Low-altitude verb '{verb.Id}' operation '{verb.Operation}' cannot carry influence radius without influence delta."));
        }
    }

    private static void ValidateCoverageGrowth(
        LowAltitudeVerbConfig verb,
        LowAltitudeVerbShape shape,
        List<GameplayValidationError> errors)
    {
        if (verb.CoverageGrowthChance != 0f && !shape.AllowsCoverageGrowth)
        {
            errors.Add(new GameplayValidationError(
                "InvalidCoverageGrowth",
                $"Low-altitude verb '{verb.Id}' operation '{verb.Operation}' cannot carry coverage growth chance."));
        }

        if (shape.AllowsCoverageGrowth && verb.CoverageGrowthChance <= 0f)
        {
            errors.Add(new GameplayValidationError(
                "MissingCoverageGrowth",
                $"Low-altitude verb '{verb.Id}' operation '{verb.Operation}' requires positive coverage growth chance."));
        }
    }

    private static void ValidateRelocation(
        LowAltitudeVerbConfig verb,
        LowAltitudeVerbShape shape,
        List<GameplayValidationError> errors)
    {
        if (verb.HeadquartersMoveChance != 0f && !shape.AllowsRelocation)
        {
            errors.Add(new GameplayValidationError(
                "InvalidRelocationChance",
                $"Low-altitude verb '{verb.Id}' operation '{verb.Operation}' cannot carry relocation chance."));
        }

        if (shape.AllowsRelocation && verb.HeadquartersMoveChance <= 0f)
        {
            errors.Add(new GameplayValidationError(
                "MissingRelocationChance",
                $"Low-altitude verb '{verb.Id}' operation '{verb.Operation}' requires positive relocation chance."));
        }
    }

    private static void ValidateRequiredMapValue(
        LowAltitudeVerbConfig verb,
        LowAltitudeVerbShape shape,
        List<GameplayValidationError> errors)
    {
        if (shape.RequiresMapValue && verb.RequiredInfluence <= 0f)
        {
            errors.Add(new GameplayValidationError(
                "MissingRequiredMapValue",
                $"Low-altitude verb '{verb.Id}' operation '{verb.Operation}' requires a positive required map value."));
        }

        if (!shape.RequiresMapValue && verb.RequiredInfluence != 0f)
        {
            errors.Add(new GameplayValidationError(
                "InvalidRequiredMapValue",
                $"Low-altitude verb '{verb.Id}' operation '{verb.Operation}' cannot carry required map value."));
        }
    }

    private static void ValidateRelationshipStrengthDelta(
        LowAltitudeVerbConfig verb,
        LowAltitudeVerbShape shape,
        List<GameplayValidationError> errors)
    {
        if (shape.RequiresRelationshipStrengthDelta && verb.RelationshipStrengthDelta == 0f)
        {
            errors.Add(new GameplayValidationError(
                "MissingRelationshipStrengthDelta",
                $"Low-altitude verb '{verb.Id}' operation '{verb.Operation}' requires non-zero relationship strength delta."));
        }

        if (!shape.RequiresRelationshipStrengthDelta && verb.RelationshipStrengthDelta != 0f)
        {
            errors.Add(new GameplayValidationError(
                "InvalidRelationshipStrengthDelta",
                $"Low-altitude verb '{verb.Id}' operation '{verb.Operation}' cannot carry relationship strength delta."));
        }
    }

    private static void ValidateTiming(LowAltitudeVerbConfig verb, List<GameplayValidationError> errors)
    {
        if (verb.MinCommitmentTicks <= 0 ||
            verb.MaxCommitmentTicks < verb.MinCommitmentTicks ||
            verb.DwellTicks < 0 ||
            verb.CandidateSampleCount <= 0)
        {
            errors.Add(new GameplayValidationError(
                "InvalidTiming",
                $"Low-altitude verb '{verb.Id}' has invalid commitment, dwell, or sample timing."));
        }
    }

    private static void ValidateForeignInfluenceTargeting(
        LowAltitudeVerbConfig verb,
        List<GameplayValidationError> errors)
    {
        var targeting = verb.ForeignInfluenceTargeting;
        if (targeting is null)
        {
            return;
        }

        if (verb.TargetMode != LowAltitudeVerbTargetMode.ForeignInfluence ||
            verb.Operation != LowAltitudeVerbOperation.ReduceInfluence)
        {
            errors.Add(new GameplayValidationError(
                "InvalidForeignInfluenceTargeting",
                $"Low-altitude verb '{verb.Id}' can only configure foreign influence targeting on ReduceInfluence/ForeignInfluence."));
        }

        if (targeting.MinimumRelationshipStrength < 0f)
        {
            errors.Add(new GameplayValidationError(
                "InvalidForeignInfluenceTargeting",
                $"Low-altitude verb '{verb.Id}' foreign influence targeting cannot use negative relationship strength."));
        }

        if (targeting.AllowUnrelatedInfluenceAtOrAbove > 100f)
        {
            errors.Add(new GameplayValidationError(
                "InvalidForeignInfluenceTargeting",
                $"Low-altitude verb '{verb.Id}' foreign influence targeting threshold must be at most 100."));
        }
    }

    private static void RequireField(
        LowAltitudeVerbConfig verb,
        bool present,
        string field,
        List<GameplayValidationError> errors)
    {
        if (!present)
        {
            errors.Add(new GameplayValidationError("MissingField", $"Low-altitude verb '{verb.Id}' requires {field}."));
        }
    }

    private static void RejectField(
        LowAltitudeVerbConfig verb,
        bool present,
        string field,
        List<GameplayValidationError> errors)
    {
        if (present)
        {
            errors.Add(new GameplayValidationError("InvalidField", $"Low-altitude verb '{verb.Id}' cannot carry {field}."));
        }
    }

    private static void ValidateRelationshipMaintenancePlane(
        string relationshipId,
        int index,
        RelationshipMaintenanceRuleConfig rule,
        List<GameplayValidationError> errors)
    {
        var usesPlane = RelationshipMaintenanceUsesPlane(rule.Condition);
        var hasPlane = !string.IsNullOrWhiteSpace(rule.Plane);
        if (usesPlane && !hasPlane)
        {
            errors.Add(new GameplayValidationError(
                "MissingRelationshipMaintenancePlane",
                $"RelationshipMaintenance '{relationshipId}' rule {index} condition '{rule.Condition}' requires Plane."));
            return;
        }

        if (!usesPlane && hasPlane)
        {
            errors.Add(new GameplayValidationError(
                "InvalidRelationshipMaintenancePlane",
                $"RelationshipMaintenance '{relationshipId}' rule {index} condition '{rule.Condition}' cannot carry Plane."));
            return;
        }

        if (hasPlane && !IsPhysical(rule.Plane!) && !IsSemantic(rule.Plane!))
        {
            errors.Add(new GameplayValidationError(
                "InvalidRelationshipMaintenancePlane",
                $"RelationshipMaintenance '{relationshipId}' rule {index} uses unsupported Plane '{rule.Plane}'."));
        }
    }

    private static bool RelationshipMaintenanceUsesPlane(RelationshipMaintenanceCondition condition) =>
        condition switch
        {
            RelationshipMaintenanceCondition.DistanceWithin => true,
            RelationshipMaintenanceCondition.CoPresence => true,
            RelationshipMaintenanceCondition.ContactMemory => true,
            RelationshipMaintenanceCondition.SourceInfluenceAtTarget => true,
            RelationshipMaintenanceCondition.TargetInfluenceAtSource => true,
            _ => false,
        };

    private static bool IsPhysical(string plane) =>
        string.Equals(plane, "physical", StringComparison.OrdinalIgnoreCase);

    private static bool IsSemantic(string plane) =>
        string.Equals(plane, "semantic", StringComparison.OrdinalIgnoreCase);
}

public sealed class GameplayValidationResult
{
    public GameplayValidationResult(IReadOnlyList<GameplayValidationError> errors)
    {
        Errors = errors;
    }

    public IReadOnlyList<GameplayValidationError> Errors { get; }

    public bool IsValid => Errors.Count == 0;
}

public sealed record GameplayValidationError(string Code, string Message);
