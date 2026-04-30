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

        // ExpandCoverage, MoveAnchor, Terrain, Semantic, and Physical are validated here once definitions move in.
    }

    private static void ValidateRelationshipMaintenancePlanes(
        GameplayDefinition definition,
        List<GameplayValidationError> errors)
    {
        _ = definition;
        _ = errors;
        // RelationshipMaintenance SourceInfluenceAtTarget, TargetInfluenceAtSource, ContactMemory, and Plane validation lives here.
    }
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
