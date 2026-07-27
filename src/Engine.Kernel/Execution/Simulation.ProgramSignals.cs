namespace Engine.Kernel;

internal sealed partial class Simulation
{
    internal void QueueChronicleGambitExecution(
        int actorOccurrenceHandle,
        int targetOccurrenceHandle,
        int gambitId,
        bool succeeded)
    {
        var signal = new ProgramSignal(
            KernelProgramCodes.ChronicleDirector,
            ProgramSignalCodes.ChronicleGambitExecution,
            actorOccurrenceHandle,
            targetOccurrenceHandle,
            gambitId,
            succeeded ? 1 : 0,
            0L);
        QueueProgramSignal(signal);
    }

    internal void QueueChronicleGeneratorFire(
        int targetOccurrenceHandle,
        int generatorId)
    {
        var signal = new ProgramSignal(
            KernelProgramCodes.ChronicleDirector,
            ProgramSignalCodes.ChronicleGeneratorFire,
            targetOccurrenceHandle,
            generatorId,
            0,
            0,
            0L);
        QueueProgramSignal(signal);
    }

    internal void QueueWorldBeatRelationshipCreated(
        int relationshipKindId)
    {
        var signal = new ProgramSignal(
            KernelProgramCodes.WorldBeats,
            ProgramSignalCodes.WorldBeatRelationshipCreated,
            relationshipKindId,
            0,
            0,
            0,
            0L);
        QueueProgramSignal(signal);
    }

    internal void QueueWorldBeatRelationshipEnded(
        int relationshipKindId)
    {
        var signal = new ProgramSignal(
            KernelProgramCodes.WorldBeats,
            ProgramSignalCodes.WorldBeatRelationshipEnded,
            relationshipKindId,
            0,
            0,
            0,
            0L);
        QueueProgramSignal(signal);
    }

    internal void ResetWorldBeatCharges()
    {
        QueueWorldBeatChargeReset(
            KernelProgramCodes.LowAltitudeVerbs,
            WorldBeatChargeDomainCodes.VerbWeight);
        QueueWorldBeatChargeReset(
            KernelProgramCodes.Generators,
            WorldBeatChargeDomainCodes.GeneratorFireWeight);
        QueueWorldBeatChargeReset(
            KernelProgramCodes.Gambits,
            WorldBeatChargeDomainCodes.GambitScore);
        QueueWorldBeatChargeReset(
            KernelProgramCodes.Gambits,
            WorldBeatChargeDomainCodes.GambitSuccess);
        QueueWorldBeatChargeReset(
            KernelProgramCodes.CultureFieldDecay,
            WorldBeatChargeDomainCodes.ContestDecayTide);
        QueueWorldBeatChargeReset(
            KernelProgramCodes.CultureFieldDecay,
            WorldBeatChargeDomainCodes.GlobalDecayTide);
        QueueWorldBeatChargeReset(
            KernelProgramCodes.Movement,
            WorldBeatChargeDomainCodes.MovementStepRate);
    }

    internal void ApplyWorldBeatCharge(
        int domainCode,
        int targetId,
        FixedScalar multiplier)
    {
        var programId = ProgramForWorldBeatCharge(domainCode);
        QueueProgramSignal(
            new ProgramSignal(
                programId,
                ProgramSignalCodes.WorldBeatChargeApply,
                domainCode,
                targetId,
                0,
                0,
                multiplier.Units));
    }

    internal void ApplyProgramSignal(in ProgramSignal signal)
    {
        switch (signal.SignalCode)
        {
            case ProgramSignalCodes.ChronicleGambitExecution
                when signal.ProgramId ==
                     KernelProgramCodes.ChronicleDirector:
                Chronicles.NoteExecution(
                    signal.A,
                    signal.B,
                    signal.C,
                    signal.D != 0);
                return;
            case ProgramSignalCodes.ChronicleGeneratorFire
                when signal.ProgramId ==
                     KernelProgramCodes.ChronicleDirector:
                Chronicles.NoteGeneratorFire(signal.A, signal.B);
                return;
            case ProgramSignalCodes.WorldBeatRelationshipCreated
                when signal.ProgramId == KernelProgramCodes.WorldBeats:
                WorldBeats.NoteRelationshipCreated(signal.A);
                return;
            case ProgramSignalCodes.WorldBeatRelationshipEnded
                when signal.ProgramId == KernelProgramCodes.WorldBeats:
                WorldBeats.NoteRelationshipEnded(signal.A);
                return;
            case ProgramSignalCodes.WorldBeatChargeReset:
                ApplyWorldBeatChargeReset(signal.ProgramId, signal.A);
                return;
            case ProgramSignalCodes.WorldBeatChargeApply:
                ApplyWorldBeatCharge(
                    signal.ProgramId,
                    signal.A,
                    signal.B,
                    FixedScalar.FromUnits(signal.ValueUnits));
                return;
            default:
                throw new global::System.InvalidOperationException();
        }
    }

    internal void ModifyEntityCultureInfluenceMultiplier(
        int entityId,
        FixedScalar delta,
        int tick)
    {
        if (_continuousOwners.IsEvaluatingCurrentThread)
        {
            _continuousOwners.Enqueue(
                new EntityInfluenceMultiplierIntent(
                    World.EntityOccurrenceHandleForIntent(entityId),
                    KernelFixedPoint.ModifierUnits(delta),
                    tick,
                    ExecutionContext));
            return;
        }

        World.ModifyEntityCultureInfluenceMultiplierDelta(
            entityId,
            delta,
            tick,
            ExecutionContext);
    }

    internal void ModifyCultureInfluenceMultiplier(
        int cultureId,
        FixedScalar delta,
        int tick)
    {
        if (_continuousOwners.IsEvaluatingCurrentThread)
        {
            _continuousOwners.Enqueue(
                new CultureInfluenceMultiplierIntent(
                    cultureId,
                    KernelFixedPoint.ModifierUnits(delta),
                    tick,
                    ExecutionContext));
            return;
        }

        World.ModifyCultureInfluenceMultiplierDelta(
            cultureId,
            delta,
            tick,
            ExecutionContext);
    }

    private void QueueProgramSignal(in ProgramSignal signal)
    {
        if (_continuousOwners.IsEvaluatingCurrentThread)
        {
            _continuousOwners.Enqueue(signal);
            return;
        }

        ApplyProgramSignal(signal);
    }

    private void QueueWorldBeatChargeReset(
        int programId,
        int domainCode)
    {
        QueueProgramSignal(
            new ProgramSignal(
                programId,
                ProgramSignalCodes.WorldBeatChargeReset,
                domainCode,
                0,
                0,
                0,
                0L));
    }

    private static int ProgramForWorldBeatCharge(int domainCode) =>
        domainCode switch
        {
            WorldBeatChargeDomainCodes.GeneratorFireWeight =>
                KernelProgramCodes.Generators,
            WorldBeatChargeDomainCodes.VerbWeight =>
                KernelProgramCodes.LowAltitudeVerbs,
            WorldBeatChargeDomainCodes.GambitScore or
            WorldBeatChargeDomainCodes.GambitSuccess =>
                KernelProgramCodes.Gambits,
            WorldBeatChargeDomainCodes.ContestDecayTide or
            WorldBeatChargeDomainCodes.GlobalDecayTide =>
                KernelProgramCodes.CultureFieldDecay,
            WorldBeatChargeDomainCodes.MovementStepRate =>
                KernelProgramCodes.Movement,
            _ => throw new global::System.ArgumentOutOfRangeException(
                nameof(domainCode))
        };

    private void ApplyWorldBeatChargeReset(
        int programId,
        int domainCode)
    {
        if (programId != ProgramForWorldBeatCharge(domainCode))
        {
            throw new global::System.InvalidOperationException();
        }

        switch (domainCode)
        {
            case WorldBeatChargeDomainCodes.GeneratorFireWeight:
                Generators.ResetAgeFireWeightMultipliers();
                return;
            case WorldBeatChargeDomainCodes.VerbWeight:
                LowAltitudeVerbs.ResetAgeWeightMultipliers();
                return;
            case WorldBeatChargeDomainCodes.GambitScore:
                Gambits.ResetAgeScoreMultipliers();
                return;
            case WorldBeatChargeDomainCodes.GambitSuccess:
                Gambits.ResetAgeSuccessMultipliers();
                return;
            case WorldBeatChargeDomainCodes.ContestDecayTide:
                CultureFieldDecay.SetContestTide(1);
                return;
            case WorldBeatChargeDomainCodes.GlobalDecayTide:
                CultureFieldDecay.SetGlobalTide(1);
                return;
            case WorldBeatChargeDomainCodes.MovementStepRate:
                Movement.SetAgeStepRateMultiplier(1);
                return;
            default:
                throw new global::System.ArgumentOutOfRangeException(
                    nameof(domainCode));
        }
    }

    private void ApplyWorldBeatCharge(
        int programId,
        int domainCode,
        int targetId,
        FixedScalar multiplier)
    {
        if (programId != ProgramForWorldBeatCharge(domainCode))
        {
            throw new global::System.InvalidOperationException();
        }

        switch (domainCode)
        {
            case WorldBeatChargeDomainCodes.GeneratorFireWeight:
                Generators.ApplyAgeFireWeightMultiplier(
                    targetId,
                    multiplier);
                return;
            case WorldBeatChargeDomainCodes.VerbWeight:
                LowAltitudeVerbs.ApplyAgeWeightMultiplier(
                    targetId,
                    multiplier);
                return;
            case WorldBeatChargeDomainCodes.GambitScore:
                Gambits.ApplyAgeScoreMultiplier(targetId, multiplier);
                return;
            case WorldBeatChargeDomainCodes.ContestDecayTide:
                CultureFieldDecay.ScaleContestTide(multiplier);
                return;
            case WorldBeatChargeDomainCodes.GlobalDecayTide:
                CultureFieldDecay.ScaleGlobalTide(multiplier);
                return;
            case WorldBeatChargeDomainCodes.GambitSuccess:
                Gambits.ApplyAgeSuccessMultiplier(targetId, multiplier);
                return;
            case WorldBeatChargeDomainCodes.MovementStepRate:
                Movement.ScaleAgeStepRateMultiplier(multiplier);
                return;
            default:
                throw new global::System.ArgumentOutOfRangeException(
                    nameof(domainCode));
        }
    }
}
