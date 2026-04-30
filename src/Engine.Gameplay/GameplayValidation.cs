namespace Engine.Gameplay;

public static class GameplayValidator
{
    public static GameplayValidationResult Validate(GameplayDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var errors = new List<GameplayValidationError>();
        ValidateLowAltitudeVerbDefinitions(definition.LowAltitudeVerbs, errors);
        ValidateRelationshipMaintenancePlanes(definition, errors);
        return new GameplayValidationResult(errors);
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
        ValidateTiming(verb, errors);
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
                RequireField(verb, hasTerrains, "Terrains", errors);
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

    private static void ValidateInfluenceFields(
        LowAltitudeVerbConfig verb,
        LowAltitudeVerbShape shape,
        List<GameplayValidationError> errors)
    {
        ValidateInfluenceDelta(verb, shape, errors);
        ValidateCoverageGrowth(verb, shape, errors);
        ValidateRelocation(verb, shape, errors);
        ValidateRequiredMapValue(verb, shape, errors);
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

        if (!shape.RequiresInfluenceDelta && (verb.InfluenceDelta != 0f || verb.InfluenceRadius != 0))
        {
            errors.Add(new GameplayValidationError(
                "InvalidInfluenceFields",
                $"Low-altitude verb '{verb.Id}' operation '{verb.Operation}' cannot carry influence delta/radius fields."));
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

    private static void ValidateRelationshipMaintenancePlanes(
        GameplayDefinition definition,
        List<GameplayValidationError> errors)
    {
        _ = definition;
        _ = errors;
        // RelationshipMaintenance SourceInfluenceAtTarget, TargetInfluenceAtSource, ContactMemory, and Plane validation lives here.
    }

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
