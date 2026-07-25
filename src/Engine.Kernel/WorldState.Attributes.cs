namespace Engine.Kernel;

using Engine.Constants;
using Engine.Eventing;

internal sealed partial class WorldState
{
    public FixedScalar GetAttribute(int entityId, int attributeId)
    {
        EnsureEntity(entityId);
        ValidatePositive(attributeId);
        return KernelFixedPoint.AttributeScalar(_attributes.GetOrDefault(entityId, attributeId));
    }

    public bool HasAttribute(int entityId, int attributeId)
    {
        EnsureEntity(entityId);
        ValidatePositive(attributeId);
        return _attributes.TryGet(entityId, attributeId, out _);
    }

    public void SetAttribute(
        int entityId,
        int attributeId,
        FixedScalar value,
        int tick,
        in KernelExecutionContext context)
    {
        EnsureEntity(entityId);
        ValidatePositive(attributeId);
        ValidatePositive(context.SourceCodeId);
        SetAttributeUnits(
            entityId,
            attributeId,
            KernelFixedPoint.AttributeUnits(ClampAttribute(attributeId, value)),
            tick,
            context);
    }

    private void SetAttributeUnits(
        int entityId,
        int attributeId,
        int valueUnits,
        int tick,
        in KernelExecutionContext context)
    {
        var clamped = KernelFixedPoint.AttributeUnits(
            ClampAttribute(attributeId, KernelFixedPoint.AttributeScalar(valueUnits)));
        var previous = _attributes.GetOrDefault(entityId, attributeId);
        if (clamped == previous)
        {
            return;
        }

        _attributes.Set(entityId, attributeId, clamped);
        _entityUpdatedAt[entityId] = tick;
        AdvanceWorldVersion();
        RecordProvenanceOutput(
            context,
            ProvenanceOutputKindCodes.AttributeChanged,
            entityId,
            entityId,
            0,
            attributeId,
            ProvenanceOutputKindCodes.AttributeChanged);
        Write(KernelEvent.From(new AttributeChangedKernelEvent(
            tick,
            WorldVersion,
            entityId,
            attributeId,
            context.SourceCodeId,
            previous,
            clamped)),
            context);
    }

    public void ModifyAttribute(
        int entityId,
        int attributeId,
        FixedScalar delta,
        int tick,
        in KernelExecutionContext context)
    {
        ModifyAttributeUnits(
            entityId,
            attributeId,
            KernelFixedPoint.AttributeUnits(delta),
            tick,
            context);
    }

    internal void ModifyAttributeUnits(
        int entityId,
        int attributeId,
        int deltaUnits,
        int tick,
        in KernelExecutionContext context)
    {
        EnsureEntity(entityId);
        ValidatePositive(attributeId);
        ValidatePositive(context.SourceCodeId);
        var previous = _attributes.GetOrDefault(entityId, attributeId);
        var next = KernelFixedPoint.SaturatingAdd(
            previous,
            deltaUnits,
            KernelFixedPoint.AttributeMinimumUnits,
            KernelFixedPoint.AttributeMaximumUnits);
        SetAttributeUnits(entityId, attributeId, next, tick, context);
    }
}
