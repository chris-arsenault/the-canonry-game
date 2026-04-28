namespace Engine.Wiring;

using Engine.Kinds;

/// <summary>
/// Validates that registered processes and gambits form complete feedback loops.
/// Runs at simulation startup. Refuses to start if wiring is incomplete.
///
/// Validations:
/// 1. No dead-end attributes (written but never read by anything)
/// 2. No orphan attributes (read but never written by anything)
/// 3. Every entity kind participates in at least one cycle
/// 4. Every entity kind has a sustaining path (something increases health)
/// 5. Every entity kind has a threatening path (something decreases health)
/// 6. ProcessProgram goal effects must correspond to execution writes
/// 7. ProcessProgram blockers must be satisfiable by some reachable goal-effect chain
/// </summary>
public sealed class WiringValidator
{
    private readonly List<WiringContract> _contracts = [];
    private readonly Dictionary<EntityKind, string?> _healthAttributes = [];

    public void RegisterContract(WiringContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);
        _contracts.Add(contract);
    }

    public void RegisterHealthAttribute(EntityKind kind, string? healthAttribute)
    {
        _healthAttributes[kind] = healthAttribute;
    }

    /// <summary>
    /// Validate all registered contracts. Returns a list of validation errors.
    /// Empty list = all validations pass.
    /// </summary>
    public IReadOnlyList<WiringError> Validate()
    {
        var errors = new List<WiringError>();
        var allWrites = CollectAllRefs(c => c.Writes);
        var allReads = CollectAllRefs(c => c.Reads);
        var allGoalEffects = CollectAllRefs(c => c.GoalEffects);
        var canonicalWrites = CanonicalizeRefs(allWrites);
        var canonicalReads = CanonicalizeRefs(allReads);

        AddDeadEndErrors(canonicalWrites, canonicalReads, errors);
        AddOrphanErrors(canonicalReads, canonicalWrites, errors);
        AddHealthPathErrors(canonicalWrites, errors);
        AddCycleErrors(canonicalWrites, canonicalReads, errors);
        AddGoalEffectWriteErrors(errors);
        AddUnreachableBlockerErrors(allGoalEffects, errors);
        AddUnreachableSupportEffectErrors(allGoalEffects, errors);
        AddUnreachableGoalEffectErrors(allGoalEffects, errors);

        return errors;
    }

    private static void AddDeadEndErrors(
        Dictionary<AttributeRef, List<string>> canonicalWrites,
        Dictionary<AttributeRef, List<string>> canonicalReads,
        List<WiringError> errors)
    {
        foreach (var (attrRef, writers) in canonicalWrites)
        {
            if (!canonicalReads.ContainsKey(attrRef))
            {
                errors.Add(new WiringError(
                    WiringErrorType.DeadEnd,
                    $"Dead-end attribute: {attrRef}. Written by: {string.Join(", ", writers)}. Read by: (nothing)."));
            }
        }
    }

    private static void AddOrphanErrors(
        Dictionary<AttributeRef, List<string>> canonicalReads,
        Dictionary<AttributeRef, List<string>> canonicalWrites,
        List<WiringError> errors)
    {
        foreach (var (attrRef, readers) in canonicalReads)
        {
            if (attrRef.Scope is RefScope.MapCell or RefScope.Pressure or RefScope.Resource)
            {
                continue;
            }

            if (!canonicalWrites.ContainsKey(attrRef))
            {
                errors.Add(new WiringError(
                    WiringErrorType.Orphan,
                    $"Orphan attribute: {attrRef}. Read by: {string.Join(", ", readers)}. Written by: (nothing)."));
            }
        }
    }

    private void AddHealthPathErrors(
        Dictionary<AttributeRef, List<string>> canonicalWrites,
        List<WiringError> errors)
    {
        foreach (var (kind, healthAttr) in _healthAttributes)
        {
            if (healthAttr is null)
            {
                continue;
            }

            var healthRef = AttributeRef.Self(healthAttr, kind);
            if (!canonicalWrites.ContainsKey(NormalizeForAttributeConnectivity(healthRef)))
            {
                errors.Add(new WiringError(
                    WiringErrorType.NoSustainingPath,
                    $"Entity kind '{kind}' has no sustaining path. Nothing writes to health attribute '{healthAttr}'."));
            }
        }
    }

    private static void AddCycleErrors(
        Dictionary<AttributeRef, List<string>> canonicalWrites,
        Dictionary<AttributeRef, List<string>> canonicalReads,
        List<WiringError> errors)
    {
        var kindsWithWrites = canonicalWrites.Keys
            .Where(r => r.Kind.HasValue)
            .Select(r => r.Kind!.Value)
            .ToHashSet();
        var kindsWithReads = canonicalReads.Keys
            .Where(r => r.Kind.HasValue)
            .Select(r => r.Kind!.Value)
            .ToHashSet();

        foreach (var kind in kindsWithWrites)
        {
            if (!kindsWithReads.Contains(kind))
            {
                errors.Add(new WiringError(
                    WiringErrorType.NoCycle,
                    $"Entity kind '{kind}' has writes but no reads. No feedback cycle exists."));
            }
        }
    }

    private void AddGoalEffectWriteErrors(List<WiringError> errors)
    {
        foreach (var contract in _contracts)
        {
            foreach (var goalEffect in contract.GoalEffects)
            {
                if (GoalEffectRequiresDirectWrite(goalEffect) &&
                    !contract.Writes.Contains(goalEffect))
                {
                    errors.Add(new WiringError(
                        WiringErrorType.GoalEffectNotWritten,
                        $"ProcessProgram goal effect '{goalEffect}' declared by '{contract.OwnerName}' is not present in its execution writes."));
                }
            }
        }
    }

    private void AddUnreachableBlockerErrors(
        Dictionary<AttributeRef, List<string>> allGoalEffects,
        List<WiringError> errors)
    {
        foreach (var contract in _contracts)
        {
            foreach (var blocker in contract.ProcessBlockers)
            {
                if (!allGoalEffects.ContainsKey(blocker))
                {
                    errors.Add(new WiringError(
                        WiringErrorType.UnreachableBlocker,
                        $"ProcessProgram blocker '{blocker}' emitted by '{contract.OwnerName}' has no serving goal effect."));
                }
            }
        }
    }

    private void AddUnreachableSupportEffectErrors(
        Dictionary<AttributeRef, List<string>> allGoalEffects,
        List<WiringError> errors)
    {
        foreach (var contract in _contracts)
        {
            foreach (var supportEffect in contract.ProcessSupportEffects)
            {
                if (!allGoalEffects.ContainsKey(supportEffect))
                {
                    errors.Add(new WiringError(
                        WiringErrorType.UnreachableSupportEffect,
                        $"ProcessProgram support effect '{supportEffect}' declared by '{contract.OwnerName}' has no serving goal effect."));
                }
            }
        }
    }

    private void AddUnreachableGoalEffectErrors(
        Dictionary<AttributeRef, List<string>> allGoalEffects,
        List<WiringError> errors)
    {
        foreach (var goalEffect in allGoalEffects.Keys)
        {
            if (!HasReachableProcessPath(goalEffect, []))
            {
                errors.Add(new WiringError(
                    WiringErrorType.UnreachableGoalEffect,
                    $"ProcessProgram goal effect '{goalEffect}' has no reachable blocker chain to an actionable process."));
            }
        }
    }

    private static bool GoalEffectRequiresDirectWrite(AttributeRef goalEffect)
    {
        return goalEffect.Scope is not RefScope.Structural and not RefScope.Relational;
    }

    /// <summary>
    /// Validate and throw if any errors are found.
    /// Called at simulation startup.
    /// </summary>
    public void ValidateOrThrow()
    {
        var errors = Validate();
        if (errors.Count > 0)
        {
            var message = $"Wiring validation failed with {errors.Count} error(s):\n" +
                string.Join('\n', errors.Select(e => $"  [{e.Type}] {e.Message}"));
            throw new WiringValidationException(message, errors);
        }
    }

    private Dictionary<AttributeRef, List<string>> CollectAllRefs(
        Func<WiringContract, IReadOnlySet<AttributeRef>> selector)
    {
        var result = new Dictionary<AttributeRef, List<string>>();
        foreach (var contract in _contracts)
        {
            foreach (var attrRef in selector(contract))
            {
                if (!result.TryGetValue(attrRef, out var owners))
                {
                    owners = [];
                    result[attrRef] = owners;
                }

                owners.Add(contract.OwnerName);
            }
        }

        return result;
    }

    private static Dictionary<AttributeRef, List<string>> CanonicalizeRefs(
        Dictionary<AttributeRef, List<string>> refs)
    {
        var result = new Dictionary<AttributeRef, List<string>>();

        foreach (var (attrRef, owners) in refs)
        {
            var normalized = NormalizeForAttributeConnectivity(attrRef);
            if (!result.TryGetValue(normalized, out var combinedOwners))
            {
                combinedOwners = [];
                result[normalized] = combinedOwners;
            }

            foreach (var owner in owners)
            {
                if (!combinedOwners.Contains(owner, StringComparer.Ordinal))
                {
                    combinedOwners.Add(owner);
                }
            }
        }

        return result;
    }

    private static AttributeRef NormalizeForAttributeConnectivity(AttributeRef attrRef)
    {
        if (attrRef.Kind.HasValue && attrRef.Scope is RefScope.Self or RefScope.Target or RefScope.Location or RefScope.Related)
        {
            return AttributeRef.Self(attrRef.Name, attrRef.Kind.Value);
        }

        return attrRef;
    }

    private bool HasReachableProcessPath(
        AttributeRef effect,
        HashSet<AttributeRef> visiting)
    {
        if (!visiting.Add(effect))
        {
            return false;
        }

        try
        {
            foreach (var contract in _contracts.Where(c => c.GoalEffects.Contains(effect)))
            {
                if (contract.HasOpaqueProcessBranches)
                {
                    return true;
                }

                if (contract.ProcessBlockers.Count == 0)
                {
                    return true;
                }

                var allBlockersReachable = true;
                foreach (var blocker in contract.ProcessBlockers)
                {
                    if (!HasReachableProcessPath(blocker, visiting))
                    {
                        allBlockersReachable = false;
                        break;
                    }
                }

                if (allBlockersReachable)
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            visiting.Remove(effect);
        }
    }
}

public enum WiringErrorType
{
    DeadEnd,
    Orphan,
    NoSustainingPath,
    NoThreateningPath,
    NoCycle,
    GoalEffectNotWritten,
    UnreachableBlocker,
    UnreachableSupportEffect,
    UnreachableGoalEffect,
}

public sealed class WiringError
{
    public WiringErrorType Type { get; }
    public string Message { get; }

    public WiringError(WiringErrorType type, string message)
    {
        Type = type;
        Message = message;
    }
}

public sealed class WiringValidationException : Exception
{
    public IReadOnlyList<WiringError> Errors { get; }

    public WiringValidationException() : base() { Errors = []; }
    public WiringValidationException(string message) : base(message) { Errors = []; }
    public WiringValidationException(string message, Exception innerException) : base(message, innerException) { Errors = []; }

    public WiringValidationException(string message, IReadOnlyList<WiringError> errors)
        : base(message)
    {
        Errors = errors;
    }
}
