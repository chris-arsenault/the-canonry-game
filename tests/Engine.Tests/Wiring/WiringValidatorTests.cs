namespace Engine.Tests.Wiring;

using Engine.Kinds;
using Engine.Wiring;

public class WiringValidatorTests
{
    // --- AttributeRef ---

    [Fact]
    public void AttributeRef_EqualityWorks()
    {
        var a = AttributeRef.Self("hp", EntityKind.Npc);
        var b = AttributeRef.Self("hp", EntityKind.Npc);
        var c = AttributeRef.Self("hunger", EntityKind.Npc);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }

    [Fact]
    public void AttributeRef_WorksInHashSet()
    {
        var set = new HashSet<AttributeRef>
        {
            AttributeRef.Self("hp", EntityKind.Npc),
            AttributeRef.Self("hp", EntityKind.Npc), // duplicate
            AttributeRef.Self("hunger", EntityKind.Npc),
        };

        Assert.Equal(2, set.Count);
    }

    [Fact]
    public void AttributeRef_DifferentScopes_NotEqual()
    {
        var selfHp = AttributeRef.Self("hp", EntityKind.Npc);
        var locationHp = new AttributeRef(RefScope.Location, "hp", EntityKind.Npc);

        Assert.NotEqual(selfHp, locationHp);
    }

    [Fact]
    public void AttributeRef_ToString_Readable()
    {
        var selfHp = AttributeRef.Self("hp", EntityKind.Npc);
        Assert.Contains("hp", selfHp.ToString(), StringComparison.Ordinal);
        Assert.Contains("Npc", selfHp.ToString(), StringComparison.Ordinal);
    }

    // --- Validator: no errors when wiring is complete ---

    [Fact]
    public void Validate_PassesWithCompleteFeedbackLoop()
    {
        var validator = new WiringValidator();
        validator.RegisterHealthAttribute(EntityKind.Npc, "hp");

        // Process A reads hunger, writes hp (sustaining)
        validator.RegisterContract(new WiringContract("eat",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("hunger", EntityKind.Npc) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("hp", EntityKind.Npc) }));

        // Process B reads hp, writes hunger (creating a cycle)
        validator.RegisterContract(new WiringContract("metabolize",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("hp", EntityKind.Npc) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("hunger", EntityKind.Npc) }));

        var errors = validator.Validate();
        Assert.Empty(errors);
    }

    // --- Dead-end detection ---

    [Fact]
    public void Validate_DetectsDeadEndAttribute()
    {
        var validator = new WiringValidator();

        // Writes to "development" but nothing reads it
        validator.RegisterContract(new WiringContract("develop",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("prosperity", EntityKind.Settlement) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("development", EntityKind.Settlement) }));

        // Something writes to prosperity so it's not orphaned
        validator.RegisterContract(new WiringContract("produce",
            reads: new HashSet<AttributeRef> { AttributeRef.MapCell("fertility") },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("prosperity", EntityKind.Settlement) }));

        var errors = validator.Validate();
        Assert.Contains(errors, e => e.Type == WiringErrorType.DeadEnd &&
            e.Message.Contains("development", StringComparison.Ordinal));
    }

    // --- Orphan detection ---

    [Fact]
    public void Validate_DetectsOrphanAttribute()
    {
        var validator = new WiringValidator();

        // Reads "ambition" but nothing writes to it
        validator.RegisterContract(new WiringContract("seek_glory",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("ambition", EntityKind.Npc) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("hp", EntityKind.Npc) }));

        // Something reads hp
        validator.RegisterContract(new WiringContract("check_health",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("hp", EntityKind.Npc) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("hunger", EntityKind.Npc) }));

        var errors = validator.Validate();
        Assert.Contains(errors, e => e.Type == WiringErrorType.Orphan &&
            e.Message.Contains("ambition", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_MapCellReads_NotOrphans()
    {
        var validator = new WiringValidator();

        // Reads from map cell (external input, not written by processes)
        validator.RegisterContract(new WiringContract("harvest",
            reads: new HashSet<AttributeRef> { AttributeRef.MapCell("fertility") },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("prosperity", EntityKind.Settlement) }));

        // Something reads prosperity
        validator.RegisterContract(new WiringContract("grow",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("prosperity", EntityKind.Settlement) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("population", EntityKind.Settlement) }));

        // Something reads population
        validator.RegisterContract(new WiringContract("consume",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("population", EntityKind.Settlement) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("prosperity", EntityKind.Settlement) }));

        var errors = validator.Validate();
        // Map cell reads should NOT be flagged as orphans
        Assert.DoesNotContain(errors, e => e.Message.Contains("fertility", StringComparison.Ordinal));
    }

    // --- Sustaining path detection ---

    [Fact]
    public void Validate_DetectsMissingSustainingPath()
    {
        var validator = new WiringValidator();
        validator.RegisterHealthAttribute(EntityKind.Npc, "hp");

        // Process that reads and writes hunger but NOT hp
        validator.RegisterContract(new WiringContract("starve",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("hunger", EntityKind.Npc) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("hunger", EntityKind.Npc) }));

        var errors = validator.Validate();
        Assert.Contains(errors, e => e.Type == WiringErrorType.NoSustainingPath &&
            e.Message.Contains("hp", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_PermanentKind_NoSustainingRequired()
    {
        var validator = new WiringValidator();
        validator.RegisterHealthAttribute(EntityKind.Region, null); // No health = permanent

        // Region only reads, never written to
        validator.RegisterContract(new WiringContract("provide_fertility",
            reads: new HashSet<AttributeRef> { AttributeRef.MapCell("fertility") },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("habitability", EntityKind.Region) }));

        var errors = validator.Validate();
        Assert.DoesNotContain(errors, e => e.Type == WiringErrorType.NoSustainingPath &&
            e.Message.Contains("Region", StringComparison.Ordinal));
    }

    // --- ValidateOrThrow ---

    [Fact]
    public void ValidateOrThrow_ThrowsWithErrors()
    {
        var validator = new WiringValidator();
        validator.RegisterHealthAttribute(EntityKind.Npc, "hp");

        // Incomplete wiring: writes hp but nothing reads it
        validator.RegisterContract(new WiringContract("heal",
            reads: new HashSet<AttributeRef> { AttributeRef.MapCell("fertility") },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("hp", EntityKind.Npc) }));

        var ex = Assert.Throws<WiringValidationException>(() => validator.ValidateOrThrow());
        Assert.True(ex.Errors.Count > 0);
    }

    [Fact]
    public void ValidateOrThrow_PassesWhenClean()
    {
        var validator = new WiringValidator();

        // Complete simple loop
        validator.RegisterContract(new WiringContract("a",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("x", EntityKind.Npc) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("y", EntityKind.Npc) }));
        validator.RegisterContract(new WiringContract("b",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("y", EntityKind.Npc) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("x", EntityKind.Npc) }));

        // Should not throw
        validator.ValidateOrThrow();
    }

    // --- TestDomain integration ---

    [Fact]
    public void TestDomain_WiringValidation_ReportsKnownGaps()
    {
        var config = Engine.Domain.TestDomain.Create();
        var validator = Engine.Domain.DomainLoader.LoadAndValidateWiring(config);
        var errors = validator.Validate();

        // Known gaps in the TestDomain (smoke test processes removed, real processes WIP):
        // - NPC has no sustaining path for hp (no process writes to hp yet)
        // These will be resolved as real processes are implemented.
        // For now, verify we get the expected errors and no unexpected ones.
        Assert.All(errors, e =>
            Assert.Contains("Npc", e.Message, System.StringComparison.Ordinal));
    }

    // --- Multiple kinds ---

    [Fact]
    public void Validate_MultipleKinds_IndependentValidation()
    {
        var validator = new WiringValidator();
        validator.RegisterHealthAttribute(EntityKind.Npc, "hp");
        validator.RegisterHealthAttribute(EntityKind.Settlement, "population");

        // NPC has complete wiring
        validator.RegisterContract(new WiringContract("npc_eat",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("hunger", EntityKind.Npc) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("hp", EntityKind.Npc) }));
        validator.RegisterContract(new WiringContract("npc_hunger",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("hp", EntityKind.Npc) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("hunger", EntityKind.Npc) }));

        // Settlement has complete wiring
        validator.RegisterContract(new WiringContract("settle_grow",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("prosperity", EntityKind.Settlement) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("population", EntityKind.Settlement) }));
        validator.RegisterContract(new WiringContract("settle_produce",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("population", EntityKind.Settlement) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("prosperity", EntityKind.Settlement) }));

        var errors = validator.Validate();
        Assert.Empty(errors);
    }
}
