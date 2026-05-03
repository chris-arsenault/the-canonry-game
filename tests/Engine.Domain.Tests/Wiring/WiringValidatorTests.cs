namespace Engine.Domain.Tests.Wiring;

using Engine.Kinds;
using Engine.Wiring;

public class WiringValidatorTests
{
    // --- AttributeRef ---

    [Fact]
    public void AttributeRef_EqualityWorks()
    {
        var a = AttributeRef.Self("hp", TestKinds.Penguin);
        var b = AttributeRef.Self("hp", TestKinds.Penguin);
        var c = AttributeRef.Self("warmth", TestKinds.Penguin);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }

    [Fact]
    public void AttributeRef_WorksInHashSet()
    {
        var set = new HashSet<AttributeRef>
        {
            AttributeRef.Self("hp", TestKinds.Penguin),
            AttributeRef.Self("hp", TestKinds.Penguin), // duplicate
            AttributeRef.Self("warmth", TestKinds.Penguin),
        };

        Assert.Equal(2, set.Count);
    }

    [Fact]
    public void AttributeRef_DifferentScopes_NotEqual()
    {
        var selfHp = AttributeRef.Self("hp", TestKinds.Penguin);
        var locationHp = new AttributeRef(RefScope.Location, "hp", TestKinds.Penguin);

        Assert.NotEqual(selfHp, locationHp);
    }

    [Fact]
    public void AttributeRef_ToString_Readable()
    {
        var selfHp = AttributeRef.Self("hp", TestKinds.Penguin);
        Assert.Contains("hp", selfHp.ToString(), StringComparison.Ordinal);
        Assert.Contains("penguin", selfHp.ToString(), StringComparison.Ordinal);
    }

    // --- Validator: no errors when wiring is complete ---

    [Fact]
    public void Validate_PassesWithCompleteFeedbackLoop()
    {
        var validator = new WiringValidator();
        validator.RegisterHealthAttribute(TestKinds.Penguin, "hp");

        // Behavior A reads warmth, writes hp (sustaining)
        validator.RegisterContract(new WiringContract("eat",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("warmth", TestKinds.Penguin) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("hp", TestKinds.Penguin) }));

        // Behavior B reads hp, writes warmth (creating a cycle)
        validator.RegisterContract(new WiringContract("metabolize",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("hp", TestKinds.Penguin) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("warmth", TestKinds.Penguin) }));

        var errors = validator.Validate();
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_DetectsGoalEffectThatIsNotWritten()
    {
        var validator = new WiringValidator();

        validator.RegisterContract(new WiringContract(
            "phantom_growth",
            reads: new HashSet<AttributeRef>(),
            writes: new HashSet<AttributeRef>(),
            goalEffects: new HashSet<AttributeRef> { AttributeRef.Self("population", TestKinds.Settlement) }));

        var errors = validator.Validate();
        Assert.Contains(errors, e => e.Type == WiringErrorType.GoalEffectNotWritten &&
            e.Message.Contains("population", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RelationalPlannerGoalEffect_DoesNotRequireDirectWrite()
    {
        var validator = new WiringValidator();

        validator.RegisterContract(new WiringContract(
            "move_to_target",
            reads: new HashSet<AttributeRef> { AttributeRef.Coordinates(TestKinds.Penguin) },
            writes: new HashSet<AttributeRef> { AttributeRef.Coordinates(TestKinds.Penguin) },
            goalEffects: new HashSet<AttributeRef> { AttributeRef.TargetRelation("located_at", TestKinds.Settlement) }));

        var errors = validator.Validate();

        Assert.DoesNotContain(errors, e => e.Type == WiringErrorType.GoalEffectNotWritten);
    }

    [Fact]
    public void Validate_DetectsUnreachablePlannerBlocker()
    {
        var validator = new WiringValidator();

        validator.RegisterContract(new WiringContract(
            "grow",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("sustenance", TestKinds.Settlement) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("population", TestKinds.Settlement) },
            goalEffects: new HashSet<AttributeRef> { AttributeRef.Self("population", TestKinds.Settlement) },
            behaviorBlockers: new HashSet<AttributeRef> { AttributeRef.Self("sustenance", TestKinds.Settlement) }));

        var errors = validator.Validate();
        Assert.Contains(errors, e => e.Type == WiringErrorType.UnreachableBlocker &&
            e.Message.Contains("sustenance", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Type == WiringErrorType.UnreachableGoalEffect &&
            e.Message.Contains("population", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_DetectsCircularPlannerChainWithoutActionableLeaf()
    {
        var validator = new WiringValidator();

        validator.RegisterContract(new WiringContract(
            "grow",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("sustenance", TestKinds.Settlement) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("population", TestKinds.Settlement) },
            goalEffects: new HashSet<AttributeRef> { AttributeRef.Self("population", TestKinds.Settlement) },
            behaviorBlockers: new HashSet<AttributeRef> { AttributeRef.Self("sustenance", TestKinds.Settlement) }));

        validator.RegisterContract(new WiringContract(
            "stockpile",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("population", TestKinds.Settlement) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("sustenance", TestKinds.Settlement) },
            goalEffects: new HashSet<AttributeRef> { AttributeRef.Self("sustenance", TestKinds.Settlement) },
            behaviorBlockers: new HashSet<AttributeRef> { AttributeRef.Self("population", TestKinds.Settlement) }));

        var errors = validator.Validate();
        Assert.Contains(errors, e => e.Type == WiringErrorType.UnreachableGoalEffect &&
            e.Message.Contains("population", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Type == WiringErrorType.UnreachableGoalEffect &&
            e.Message.Contains("sustenance", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_PassesWhenPlannerBlockerChainHasActionableBaseCase()
    {
        var validator = new WiringValidator();

        validator.RegisterContract(new WiringContract(
            "grow",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("sustenance", TestKinds.Settlement) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("population", TestKinds.Settlement) },
            goalEffects: new HashSet<AttributeRef> { AttributeRef.Self("population", TestKinds.Settlement) },
            behaviorBlockers: new HashSet<AttributeRef> { AttributeRef.Self("sustenance", TestKinds.Settlement) }));

        validator.RegisterContract(new WiringContract(
            "stockpile",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("population", TestKinds.Settlement) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("sustenance", TestKinds.Settlement) },
            goalEffects: new HashSet<AttributeRef> { AttributeRef.Self("sustenance", TestKinds.Settlement) }));

        var errors = validator.Validate();
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_DetectsUnreachablePlannerSupportEffect()
    {
        var validator = new WiringValidator();

        validator.RegisterContract(new WiringContract(
            "grow",
            reads: new HashSet<AttributeRef> { AttributeRef.StructuralFact("harbor_access") },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("population", TestKinds.Settlement) },
            goalEffects: new HashSet<AttributeRef> { AttributeRef.Self("population", TestKinds.Settlement) },
            behaviorSupportEffects: new HashSet<AttributeRef> { AttributeRef.StructuralFact("harbor_access") }));

        var errors = validator.Validate();

        Assert.Contains(errors, e => e.Type == WiringErrorType.UnreachableSupportEffect &&
            e.Message.Contains("harbor_access", StringComparison.Ordinal));
    }

    // --- Dead-end detection ---

    [Fact]
    public void Validate_DetectsDeadEndAttribute()
    {
        var validator = new WiringValidator();

        // Writes to "development" but nothing reads it
        validator.RegisterContract(new WiringContract("develop",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("prosperity", TestKinds.Settlement) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("development", TestKinds.Settlement) }));

        // Something writes to prosperity so it's not orphaned
        validator.RegisterContract(new WiringContract("produce",
            reads: new HashSet<AttributeRef> { AttributeRef.MapCell("fertility") },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("prosperity", TestKinds.Settlement) }));

        var errors = validator.Validate();
        Assert.Contains(errors, e => e.Type == WiringErrorType.DeadEnd &&
            e.Message.Contains("development", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_TreatsSelfAndTargetRefsAsSameEntityStockForConnectivity()
    {
        var validator = new WiringValidator();

        validator.RegisterContract(new WiringContract("produce_power",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("condition", TestKinds.Artifact) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("power", TestKinds.Artifact) }));

        validator.RegisterContract(new WiringContract("consume_power",
            reads: new HashSet<AttributeRef> { AttributeRef.Target("power", TestKinds.Artifact) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("echo", TestKinds.Penguin) }));

        var errors = validator.Validate();

        Assert.DoesNotContain(errors, e =>
            e.Type == WiringErrorType.DeadEnd &&
            e.Message.Contains("artifact.power", StringComparison.Ordinal));
        Assert.DoesNotContain(errors, e =>
            e.Type == WiringErrorType.Orphan &&
            e.Message.Contains("artifact.power", StringComparison.Ordinal));
    }

    // --- Orphan detection ---

    [Fact]
    public void Validate_DetectsOrphanAttribute()
    {
        var validator = new WiringValidator();

        // Reads "echo" but nothing writes to it
        validator.RegisterContract(new WiringContract("seek_glory",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("echo", TestKinds.Penguin) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("hp", TestKinds.Penguin) }));

        // Something reads hp
        validator.RegisterContract(new WiringContract("check_health",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("hp", TestKinds.Penguin) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("warmth", TestKinds.Penguin) }));

        var errors = validator.Validate();
        Assert.Contains(errors, e => e.Type == WiringErrorType.Orphan &&
            e.Message.Contains("echo", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_MapCellReads_NotOrphans()
    {
        var validator = new WiringValidator();

        // Reads from map cell (external input, not written by behaviors)
        validator.RegisterContract(new WiringContract("harvest",
            reads: new HashSet<AttributeRef> { AttributeRef.MapCell("fertility") },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("prosperity", TestKinds.Settlement) }));

        // Something reads prosperity
        validator.RegisterContract(new WiringContract("grow",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("prosperity", TestKinds.Settlement) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("population", TestKinds.Settlement) }));

        // Something reads population
        validator.RegisterContract(new WiringContract("consume",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("population", TestKinds.Settlement) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("prosperity", TestKinds.Settlement) }));

        var errors = validator.Validate();
        // Map cell reads should NOT be flagged as orphans
        Assert.DoesNotContain(errors, e => e.Message.Contains("fertility", StringComparison.Ordinal));
    }

    // --- Sustaining path detection ---

    [Fact]
    public void Validate_DetectsMissingSustainingPath()
    {
        var validator = new WiringValidator();
        validator.RegisterHealthAttribute(TestKinds.Penguin, "hp");

        // Behavior that reads and writes warmth but NOT hp
        validator.RegisterContract(new WiringContract("starve",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("warmth", TestKinds.Penguin) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("warmth", TestKinds.Penguin) }));

        var errors = validator.Validate();
        Assert.Contains(errors, e => e.Type == WiringErrorType.NoSustainingPath &&
            e.Message.Contains("hp", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_PermanentKind_NoSustainingRequired()
    {
        var validator = new WiringValidator();
        validator.RegisterHealthAttribute(TestKinds.Region, null); // No health = permanent

        // Region only reads, never written to
        validator.RegisterContract(new WiringContract("provide_fertility",
            reads: new HashSet<AttributeRef> { AttributeRef.MapCell("fertility") },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("habitability", TestKinds.Region) }));

        var errors = validator.Validate();
        Assert.DoesNotContain(errors, e => e.Type == WiringErrorType.NoSustainingPath &&
            e.Message.Contains("Region", StringComparison.Ordinal));
    }

    // --- ValidateOrThrow ---

    [Fact]
    public void ValidateOrThrow_ThrowsWithErrors()
    {
        var validator = new WiringValidator();
        validator.RegisterHealthAttribute(TestKinds.Penguin, "hp");

        // Incomplete wiring: writes hp but nothing reads it
        validator.RegisterContract(new WiringContract("heal",
            reads: new HashSet<AttributeRef> { AttributeRef.MapCell("fertility") },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("hp", TestKinds.Penguin) }));

        var ex = Assert.Throws<WiringValidationException>(() => validator.ValidateOrThrow());
        Assert.True(ex.Errors.Count > 0);
    }

    [Fact]
    public void ValidateOrThrow_PassesWhenClean()
    {
        var validator = new WiringValidator();

        // Complete simple loop
        validator.RegisterContract(new WiringContract("a",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("x", TestKinds.Penguin) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("y", TestKinds.Penguin) }));
        validator.RegisterContract(new WiringContract("b",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("y", TestKinds.Penguin) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("x", TestKinds.Penguin) }));

        // Should not throw
        validator.ValidateOrThrow();
    }

    // --- IceRemembers integration ---

    [Fact]
    public void IceRemembers_WiringValidation_ReportsKnownGaps()
    {
        var config = IceRemembers.IceRemembersDefinition.Create();
        var validator = Engine.Domain.Loader.DomainLoader.LoadAndValidateWiring(config);
        var errors = validator.Validate();

        // Known gaps — real behaviors are WIP. Just verify no unexpected error types.
        Assert.All(errors, e =>
            Assert.True(
                e.Type == Engine.Wiring.WiringErrorType.DeadEnd
                || e.Type == Engine.Wiring.WiringErrorType.NoSustainingPath
                || e.Type == Engine.Wiring.WiringErrorType.Orphan,
                $"Unexpected error type: {e.Type} — {e.Message}"));
    }

    // --- Multiple kinds ---

    [Fact]
    public void Validate_MultipleKinds_IndependentValidation()
    {
        var validator = new WiringValidator();
        validator.RegisterHealthAttribute(TestKinds.Penguin, "hp");
        validator.RegisterHealthAttribute(TestKinds.Settlement, "population");

        // NPC has complete wiring
        validator.RegisterContract(new WiringContract("npc_sustain",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("warmth", TestKinds.Penguin) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("hp", TestKinds.Penguin) }));
        validator.RegisterContract(new WiringContract("npc_warmth",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("hp", TestKinds.Penguin) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("warmth", TestKinds.Penguin) }));

        // Settlement has complete wiring
        validator.RegisterContract(new WiringContract("settle_grow",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("prosperity", TestKinds.Settlement) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("population", TestKinds.Settlement) }));
        validator.RegisterContract(new WiringContract("settle_produce",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("population", TestKinds.Settlement) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("prosperity", TestKinds.Settlement) }));

        var errors = validator.Validate();
        Assert.Empty(errors);
    }
}
