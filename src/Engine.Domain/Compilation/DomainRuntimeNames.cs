namespace Engine.Domain.Compilation;

public static class DomainRuntimeNames
{
    public const string NoneCultureId = "none";
    public const string RelationshipCreatedCode = "created";
    public const string RelationshipCulledCode = "culled";
    public const string RelationshipStrengthChangedCode = "strength_changed";
    public const string GambitSourceCode = "gambit";
    public const string UpkeepSourceCode = "upkeep";
    public const string StartupSourceCode = "startup";
    public const string RelationshipSourceCode = "relationship";
    public const string GeneratorSourceCode = "generator";
    public const string DegeneracySourceCode = "degeneracy";
    public const string PlanningSourceCode = "planning";
    public const string GameplaySourceCode = "gameplay";
    public const string MapControlSourceCode = "map_control";
    public const string GameplayEffectsSourceCode = "gameplay_effects";
    public const string MapActivitySourceCode = MapControlSourceCode;
    public const string MapSignalsSourceCode = MapControlSourceCode;
    public const string LowAltitudeVerbSourceCode = GameplaySourceCode;
    public const string EffectsSourceCode = GameplayEffectsSourceCode;
}
