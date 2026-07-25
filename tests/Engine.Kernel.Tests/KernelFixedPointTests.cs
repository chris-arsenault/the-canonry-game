namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Eventing;

public sealed class KernelFixedPointTests
{
    [Fact]
    public void FieldFamiliesUseIndependentDocumentedScales()
    {
        Assert.Equal(12_345, KernelFixedPoint.CultureMassUnits(12.345f));
        Assert.Equal(625_000, KernelFixedPoint.RelationshipStrengthUnits(0.625f));
        Assert.Equal(12_345_000, KernelFixedPoint.AttributeUnits(123.45f));
        Assert.Equal(87_500, KernelFixedPoint.RoadQualityUnits(87.5f));
        Assert.Equal(375, KernelFixedPoint.ModifierUnits(0.00375f));
    }

    [Fact]
    public void ArithmeticClampsExplicitlyAndRejectsUnrepresentableInputs()
    {
        Assert.Equal(
            KernelFixedPoint.RelationshipStrengthMaximumUnits,
            KernelFixedPoint.SaturatingAdd(
                KernelFixedPoint.RelationshipStrengthMaximumUnits - 2,
                10,
                0,
                KernelFixedPoint.RelationshipStrengthMaximumUnits));
        Assert.Equal(
            333,
            KernelFixedPoint.MultiplyByRatio(
                1_000,
                333_333,
                0,
                1_000));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => KernelFixedPoint.CultureMassUnits(1_000_001f));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => KernelFixedPoint.AttributeUnits(float.PositiveInfinity));
    }

    [Fact]
    public void AttributeAndRelationshipEventsCarryIntegerUnits()
    {
        var attribute = new AttributeChangedKernelEvent(
            Tick: 1,
            WorldVersion: 2,
            EntityId: 3,
            AttributeId: 4,
            SourceCodeId: 5,
            OldValueUnits: KernelFixedPoint.AttributeUnits(50.125f),
            NewValueUnits: KernelFixedPoint.AttributeUnits(49.875f));
        var relationship = new RelationshipChangedKernelEvent(
            Tick: 1,
            WorldVersion: 2,
            ChangeCodeId: 3,
            SourceCodeId: 4,
            RelationshipKindId: 5,
            PolarityCode: RelationshipPolarityCodes.Positive,
            SourceEntityId: 7,
            TargetEntityId: 8,
            RelationshipId: 9,
            PreviousStrengthUnits: KernelFixedPoint.RelationshipStrengthUnits(0.75f),
            NewStrengthUnits: KernelFixedPoint.RelationshipStrengthUnits(0.625f),
            ActionId: 0,
            IsActive: 1);
        Assert.Equal(KernelFixedPoint.AttributeUnits(50.125f), attribute.OldValueUnits);
        Assert.Equal(KernelFixedPoint.AttributeUnits(49.875f), attribute.NewValueUnits);
        Assert.Equal(
            KernelFixedPoint.RelationshipStrengthUnits(0.625f),
            relationship.NewStrengthUnits);
        Assert.Equal(
            KernelFixedPoint.RelationshipStrengthUnits(0.75f),
            relationship.PreviousStrengthUnits);
    }

    [Fact]
    public void CultureAndRoadEventsCarryIntegerUnits()
    {
        var culture = new CellCultureInfluenceChangedKernelEvent(
            Tick: 1,
            WorldVersion: 2,
            Q: 3,
            R: 4,
            SourceCodeId: 5,
            PlaneCode: PlaneCodes.Physical,
            CultureId: 6,
            Z: 0,
            PreviousShareUnits: FixedScalar.FromSingle(0.25f).Units,
            CurrentShareUnits: FixedScalar.FromSingle(0.5f).Units,
            PreviousRawInfluenceUnits: KernelFixedPoint.CultureMassUnits(12.5f),
            CurrentRawInfluenceUnits: KernelFixedPoint.CultureMassUnits(25.75f),
            CurrentTotalRawInfluenceUnits: KernelFixedPoint.CultureMassUnits(51.5f));
        var road = new CellRoadChangedKernelEvent(
            Tick: 1,
            WorldVersion: 2,
            PlaneCode: PlaneCodes.Physical,
            Q: 3,
            R: 4,
            SourceCodeId: 5,
            TravelCount: 6,
            PreviousQualityUnits: KernelFixedPoint.RoadQualityUnits(25.5f),
            CurrentQualityUnits: KernelFixedPoint.RoadQualityUnits(26.75f));

        Assert.Equal(
            KernelFixedPoint.CultureMassUnits(25.75f),
            culture.CurrentRawInfluenceUnits);
        Assert.Equal(
            KernelFixedPoint.CultureMassUnits(51.5f),
            culture.CurrentTotalRawInfluenceUnits);
        Assert.Equal(KernelFixedPoint.RoadQualityUnits(26.75f), road.CurrentQualityUnits);
        Assert.Equal(KernelFixedPoint.RoadQualityUnits(25.5f), road.PreviousQualityUnits);
    }
}
