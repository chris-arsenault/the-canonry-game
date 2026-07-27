namespace Engine.Kernel;

internal sealed class GambitProgramScratch
{
    internal readonly bool[] ActorPreconditionValues;
    internal readonly FixedScalar[] ExpressionStack;
    internal readonly int[] CandidateGambitSlots;
    internal readonly int[] CandidateTargetIds;
    internal readonly FixedScalar[] CandidateWeights;
    internal readonly int[] VariableIds;
    internal readonly FixedScalar[] VariableValues;
    internal int CandidateCount;

    public GambitProgramScratch(
        int entityCapacity,
        int gambitCapacity,
        int preconditionCapacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(entityCapacity);
        NumericGuard.ThrowIfNegativeOrZero(gambitCapacity);
        NumericGuard.ThrowIfNegativeOrZero(preconditionCapacity);
        var candidateCapacity = checked(entityCapacity * gambitCapacity);
        ActorPreconditionValues = new bool[preconditionCapacity + 1];
        ExpressionStack = new FixedScalar[32];
        CandidateGambitSlots = new int[candidateCapacity];
        CandidateTargetIds = new int[candidateCapacity];
        CandidateWeights = new FixedScalar[candidateCapacity];
        VariableIds = new int[32];
        VariableValues = new FixedScalar[32];
    }

    private GambitProgramScratch(
        int actorPreconditionCapacity,
        int candidateCapacity)
    {
        ActorPreconditionValues = new bool[actorPreconditionCapacity];
        ExpressionStack = new FixedScalar[32];
        CandidateGambitSlots = new int[candidateCapacity];
        CandidateTargetIds = new int[candidateCapacity];
        CandidateWeights = new FixedScalar[candidateCapacity];
        VariableIds = new int[32];
        VariableValues = new FixedScalar[32];
    }

    internal GambitProgramScratch CreatePeer() =>
        new(ActorPreconditionValues.Length, CandidateWeights.Length);

    internal void ResetCandidates()
    {
        CandidateCount = 0;
    }

    internal void AddCandidate(int gambitSlot, int targetId, FixedScalar weight)
    {
        if (CandidateCount >= CandidateWeights.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        CandidateGambitSlots[CandidateCount] = gambitSlot;
        CandidateTargetIds[CandidateCount] = targetId;
        CandidateWeights[CandidateCount] = weight;
        CandidateCount++;
    }
}
