namespace Engine.Gameplay;

public static class LowAltitudeOperations
{
    public static LowAltitudeVerbOperation Move => LowAltitudeVerbOperation.Move;
    public static LowAltitudeVerbOperation GrowInfluence => LowAltitudeVerbOperation.GrowInfluence;
    public static LowAltitudeVerbOperation ConsolidateInfluence => LowAltitudeVerbOperation.ConsolidateInfluence;
    public static LowAltitudeVerbOperation ReduceInfluence => LowAltitudeVerbOperation.ReduceInfluence;
    public static LowAltitudeVerbOperation ExpandCoverage => LowAltitudeVerbOperation.ExpandCoverage;
    public static LowAltitudeVerbOperation MoveHeadquarters => LowAltitudeVerbOperation.MoveHeadquarters;
}

public static class LowAltitudeTargets
{
    public static LowAltitudeVerbTargetMode Explore => LowAltitudeVerbTargetMode.Explore;
    public static LowAltitudeVerbTargetMode EntityKind => LowAltitudeVerbTargetMode.EntityKind;
    public static LowAltitudeVerbTargetMode RelationshipTarget => LowAltitudeVerbTargetMode.RelationshipTarget;
    public static LowAltitudeVerbTargetMode Terrain => LowAltitudeVerbTargetMode.Terrain;
    public static LowAltitudeVerbTargetMode OwnInfluence => LowAltitudeVerbTargetMode.OwnInfluence;
    public static LowAltitudeVerbTargetMode ForeignInfluence => LowAltitudeVerbTargetMode.ForeignInfluence;
    public static LowAltitudeVerbTargetMode ChroniclePursuit => LowAltitudeVerbTargetMode.ChroniclePursuit;
}

public static class AttributeEffectConditions
{
    public static EffectCondition RelatedInfluenceAtLocation => EffectCondition.RelatedInfluenceAtLocation;
    public static EffectCondition RelatedEntityDistance => EffectCondition.RelatedEntityDistance;
    public static EffectCondition MapSupportAtLocation => EffectCondition.MapSupportAtLocation;
    public static EffectCondition MapThreatAtLocation => EffectCondition.MapThreatAtLocation;
    public static EffectCondition MapCrowdingLoad => EffectCondition.MapCrowdingLoad;
    public static EffectCondition MapCrowdingComfort => EffectCondition.MapCrowdingComfort;
    public static EffectCondition OwnCultureInfluenceAtLocation => EffectCondition.OwnCultureInfluenceAtLocation;
    public static EffectCondition CultureSlotOccupancy => EffectCondition.CultureSlotOccupancy;
    public static EffectCondition CrowdingBeyondCultureSupport => EffectCondition.CrowdingBeyondCultureSupport;
    public static EffectCondition InsideOwnedCoverage => EffectCondition.InsideOwnedCoverage;
}

public static class GameplayEffectSet
{
    public static EffectsConfig From(IReadOnlyList<AttributeDeltaEffectConfig> effects) =>
        new() { AttributeDeltaEffects = effects };

    public static EffectsConfig From(
        IReadOnlyList<AttributeDeltaEffectConfig> effects,
        IReadOnlyList<RelationshipTagRuleConfig> relationshipTagRules) =>
        new() { AttributeDeltaEffects = effects, RelationshipTagRules = relationshipTagRules };
}
