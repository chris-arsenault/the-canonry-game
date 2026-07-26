namespace Engine.Host;

using Engine.Constants;

internal sealed partial class KernelConfigurationContext
{
    public int RegisterGambit(
        int kindId,
        int gambitId,
        int cooldownTicks,
        int entityCooldownTicks,
        int targetMode,
        int targetKindId,
        FixedScalar successChance) =>
        _kernel.Gambits.RegisterGambit(
            kindId,
            gambitId,
            cooldownTicks,
            entityCooldownTicks,
            targetMode,
            targetKindId,
            successChance);

    public void MarkGambitChronicleGranted(int gambitSlot) =>
        _kernel.Gambits.MarkChronicleGranted(gambitSlot);

    public void AddGambitCandidatePrecondition(
        int gambitSlot,
        int preconditionCode,
        int a,
        int b,
        int c,
        int d,
        FixedScalar x,
        FixedScalar y,
        int actorValueSlot,
        int relationshipTargetSource,
        int trueDestination,
        int falseDestination) =>
        _kernel.Gambits.AddCandidatePrecondition(
            gambitSlot,
            preconditionCode,
            a,
            b,
            c,
            d,
            x,
            y,
            actorValueSlot,
            relationshipTargetSource,
            trueDestination,
            falseDestination);

    public void AddGambitActorPrecondition(
        int gambitSlot,
        int actorValueSlot,
        int trueDestination,
        int falseDestination) =>
        _kernel.Gambits.AddActorPrecondition(
            gambitSlot,
            actorValueSlot,
            trueDestination,
            falseDestination);

    public void ConfigureGambitPreconditionPlan(
        int gambitSlot,
        int candidateEntry,
        int actorEntry,
        int actorValueCount,
        int relationshipTargetDirectionCode,
        int relationshipTargetKindId,
        int relationshipTargetHasMinimumStrength,
        FixedScalar relationshipTargetMinimumStrength) =>
        _kernel.Gambits.ConfigurePreconditionPlan(
            gambitSlot,
            candidateEntry,
            actorEntry,
            actorValueCount,
            relationshipTargetDirectionCode,
            relationshipTargetKindId,
            relationshipTargetHasMinimumStrength,
            relationshipTargetMinimumStrength);

    public void AddGambitScoreExpressionOp(
        int gambitSlot,
        int expressionCode,
        int a = 0,
        FixedScalar x = default) =>
        _kernel.Gambits.AddExpressionOp(gambitSlot, expressionCode, a, x);

    public void AddGambitProviderSelector(
        int gambitSlot,
        int selectorCode,
        int kindId = 0,
        int valueId = 0,
        int relationshipKindId = 0,
        FixedScalar minimumStrength = default) =>
        _kernel.Gambits.AddProviderSelector(
            gambitSlot,
            selectorCode,
            kindId,
            valueId,
            relationshipKindId,
            minimumStrength);

    public int AddGambitStep(
        int gambitSlot,
        int operationCode,
        int a = 0,
        int b = 0,
        int c = 0,
        int d = 0,
        FixedScalar x = default,
        FixedScalar y = default) =>
        _kernel.Gambits.AddStep(gambitSlot, operationCode, a, b, c, d, x, y);

    public void AddGambitStepExpressionOp(
        int stepSlot,
        int expressionCode,
        int a = 0,
        FixedScalar x = default) =>
        _kernel.Gambits.AddStepExpressionOp(stepSlot, expressionCode, a, x);

    public int RegisterTraitGambitModifier(
        int gambitId,
        int traitId,
        int bearerCode,
        FixedScalar scoreDelta,
        FixedScalar successChanceDelta) =>
        _kernel.Gambits.RegisterTraitModifier(
            gambitId,
            traitId,
            bearerCode,
            scoreDelta,
            successChanceDelta);
}
