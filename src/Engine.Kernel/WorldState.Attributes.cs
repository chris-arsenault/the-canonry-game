namespace Engine.Kernel;

using Engine.Constants;
using Engine.Eventing;

internal sealed partial class WorldState
{
    public FixedScalar GetAttribute(int entityId, int attributeId)
    {
        EnsureEntity(entityId);
        return KernelFixedPoint.AttributeScalar(_attributes[AttributeIndex(entityId, attributeId)]);
    }

    public bool HasAttribute(int entityId, int attributeId)
    {
        EnsureEntity(entityId);
        return _attributeAssigned[AttributeIndex(entityId, attributeId)];
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
        var index = AttributeIndex(entityId, attributeId);
        var previous = _attributes[index];
        if (_attributeAssigned[index] && clamped == previous)
        {
            return;
        }

        _attributes[index] = clamped;
        _attributeAssigned[index] = true;
        _entityUpdatedAt[entityId] = tick;
        MarkEntityCultureWorkDirty(entityId);
        AdvanceWorldVersion();
        Write(KernelEvent.From(new AttributeChangedKernelEvent(
            tick,
            WorldVersion,
            EntityOccurrenceHandle(entityId),
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
        var previous = _attributes[AttributeIndex(entityId, attributeId)];
        var next = KernelFixedPoint.SaturatingAdd(
            previous,
            deltaUnits,
            KernelFixedPoint.AttributeMinimumUnits,
            KernelFixedPoint.AttributeMaximumUnits);
        SetAttributeUnits(entityId, attributeId, next, tick, context);
    }

    private int AttributeIndex(int entityId, int attributeId)
    {
        if ((uint)attributeId >= (uint)_attributeStride || attributeId == 0)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(attributeId));
        }

        return checked((entityId * _attributeStride) + attributeId);
    }
}
