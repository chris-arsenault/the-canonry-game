namespace Engine.Gameplay.Tests;

using Engine.Gameplay;

public sealed class GameplayValidationTests
{
    [Fact]
    public void Validate_RejectsMissingVerbIdentityAndUnsupportedPlane()
    {
        var result = Validate(new MovementLowAltitudeVerbConfig
        {
            Id = "",
            Kind = " ",
            Plane = "astral",
            TargetMode = LowAltitudeVerbTargetMode.Explore,
        });

        Assert.Contains(result.Errors, error => error.Code == "UnsupportedVerb");
        Assert.Contains(result.Errors, error => error.Code == "MissingKind");
        Assert.Contains(result.Errors, error => error.Code == "InvalidPlane");
    }

    [Fact]
    public void Validate_EnforcesTargetFieldMatrix()
    {
        var result = Validate(
            new MovementLowAltitudeVerbConfig
            {
                Id = "explore_with_kind",
                Kind = "actor",
                TargetMode = LowAltitudeVerbTargetMode.Explore,
                TargetKind = "settlement",
            },
            new MovementLowAltitudeVerbConfig
            {
                Id = "entity_missing_kind",
                Kind = "actor",
                TargetMode = LowAltitudeVerbTargetMode.EntityKind,
            },
            new MovementLowAltitudeVerbConfig
            {
                Id = "relationship_with_kind",
                Kind = "actor",
                TargetMode = LowAltitudeVerbTargetMode.RelationshipTarget,
                RelationshipKind = "allies",
                TargetKind = "settlement",
            },
            new MovementLowAltitudeVerbConfig
            {
                Id = "terrain_missing_set",
                Kind = "actor",
                TargetMode = LowAltitudeVerbTargetMode.Terrain,
            });

        Assert.Contains(result.Errors, error =>
            error.Code == "InvalidField" &&
            error.Message.Contains("explore_with_kind", StringComparison.Ordinal) &&
            error.Message.Contains("TargetKind", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error =>
            error.Code == "MissingField" &&
            error.Message.Contains("entity_missing_kind", StringComparison.Ordinal) &&
            error.Message.Contains("TargetKind", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error =>
            error.Code == "InvalidField" &&
            error.Message.Contains("relationship_with_kind", StringComparison.Ordinal) &&
            error.Message.Contains("TargetKind", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error =>
            error.Code == "MissingField" &&
            error.Message.Contains("terrain_missing_set", StringComparison.Ordinal) &&
            error.Message.Contains("Terrains", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RejectsOperationTargetModeMismatchAndUnknownTargetMode()
    {
        var result = Validate(
            new InfluenceLowAltitudeVerbConfig
            {
                Id = "grow_on_terrain",
                Kind = "actor",
                InfluenceOperation = LowAltitudeVerbOperation.GrowInfluence,
                TargetMode = LowAltitudeVerbTargetMode.Terrain,
                Terrains = new HashSet<string>(StringComparer.Ordinal) { "tundra" },
                Delta = 1f,
            },
            new MovementLowAltitudeVerbConfig
            {
                Id = "unknown_target_mode",
                Kind = "actor",
                TargetMode = (LowAltitudeVerbTargetMode)999,
            });

        Assert.Contains(result.Errors, error =>
            error.Code == "InvalidVerbTargetMode" &&
            error.Message.Contains("grow_on_terrain", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error =>
            error.Code == "UnsupportedTargetMode" &&
            error.Message.Contains("unknown_target_mode", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RejectsMissingShapeSpecificRuntimeInputs()
    {
        var result = Validate(
            new CoverageExpansionLowAltitudeVerbConfig
            {
                Id = "bad_claim",
                Kind = "actor",
                TargetMode = LowAltitudeVerbTargetMode.OwnInfluence,
                Delta = 0f,
                GrowthChance = 0f,
                RequiredMapValue = 0f,
            },
            new AnchorRelocationLowAltitudeVerbConfig
            {
                Id = "bad_relocate",
                Kind = "actor",
                TargetMode = LowAltitudeVerbTargetMode.OwnInfluence,
                Delta = 0f,
                RelocationChance = 0f,
                RequiredMapValue = 0f,
            });

        Assert.Contains(result.Errors, error =>
            error.Code == "MissingInfluenceDelta" &&
            error.Message.Contains("bad_claim", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error =>
            error.Code == "MissingCoverageGrowth" &&
            error.Message.Contains("bad_claim", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error =>
            error.Code == "MissingRequiredMapValue" &&
            error.Message.Contains("bad_claim", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error =>
            error.Code == "MissingRelocationChance" &&
            error.Message.Contains("bad_relocate", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error =>
            error.Code == "MissingRequiredMapValue" &&
            error.Message.Contains("bad_relocate", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RejectsInvalidVerbTiming()
    {
        var result = Validate(new MovementLowAltitudeVerbConfig
        {
            Id = "bad_timing",
            Kind = "actor",
            TargetMode = LowAltitudeVerbTargetMode.Explore,
            MinCommitmentTicks = 0,
            MaxCommitmentTicks = -1,
            DwellTicks = -1,
            CandidateSampleCount = 0,
        });

        Assert.Contains(result.Errors, error => error.Code == "InvalidTiming");
    }

    [Fact]
    public void Validate_RejectsInvalidForeignInfluenceTargetingBounds()
    {
        var result = Validate(new InfluenceLowAltitudeVerbConfig
        {
            Id = "bad_foreign_filter",
            Kind = "actor",
            InfluenceOperation = LowAltitudeVerbOperation.ReduceInfluence,
            TargetMode = LowAltitudeVerbTargetMode.ForeignInfluence,
            Delta = 1f,
            ForeignInfluenceTargeting = new ForeignInfluenceTargetingConfig
            {
                MinimumRelationshipStrength = -0.1f,
                AllowUnrelatedInfluenceAtOrAbove = 101f,
            },
        });

        Assert.Equal(2, result.Errors.Count(error => error.Code == "InvalidForeignInfluenceTargeting"));
    }

    [Fact]
    public void ValidateRelationshipMaintenance_RejectsUnsupportedPlaneOnPlaneBoundCondition()
    {
        var errors = GameplayValidator.ValidateRelationshipMaintenance(
            "nearby",
            [
                new RelationshipMaintenanceRuleConfig
                {
                    Condition = RelationshipMaintenanceCondition.DistanceWithin,
                    Plane = "astral",
                    Delta = 0.1f,
                },
            ]);

        Assert.Contains(errors, error => error.Code == "InvalidRelationshipMaintenancePlane");
    }

    [Fact]
    public void Validate_AcceptsCoverageAndRelocationWhenRequiredInputsArePresent()
    {
        var result = Validate(
            new CoverageExpansionLowAltitudeVerbConfig
            {
                Id = "claim",
                Kind = "actor",
                TargetMode = LowAltitudeVerbTargetMode.OwnInfluence,
                Delta = 1f,
                GrowthChance = 0.2f,
                RequiredMapValue = 50f,
            },
            new AnchorRelocationLowAltitudeVerbConfig
            {
                Id = "relocate",
                Kind = "actor",
                TargetMode = LowAltitudeVerbTargetMode.OwnInfluence,
                Delta = 1f,
                RelocationChance = 0.15f,
                RequiredMapValue = 60f,
            });

        Assert.True(result.IsValid);
    }

    private static GameplayValidationResult Validate(params LowAltitudeVerbConfig[] verbs) =>
        GameplayValidator.Validate(new GameplayDefinition { LowAltitudeVerbs = verbs });
}
