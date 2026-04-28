namespace Engine.Domain.Graph;

using Engine.Domain.Config;
using Engine.Kinds;
using Engine.Processes;
using Engine.Resources;
using Engine.Wiring;

/// <summary>
/// Materializes the semantic verb catalog into runtime ProcessDefinitions. One
/// process per entity-owned verb. Dispatch is by verb role and flow shape; the
/// demo rules (transfer 5 on attribute↔attribute, drain-5-for-hex+1 on attr→hex,
/// hex-as-rate-multiplier×2 on hex→health, no hex drain on hex→health, within-1-hex
/// proximity on cross-entity verbs using the owner's primary plane) are baked in.
///
/// This replaces the hand-authored ProcessDefinitions that used to live in
/// DomainConfig.Processes. The semantic verb catalog is the single source of truth
/// for verb identifiers in C#.
/// </summary>
public static class RuntimeProcessGenerator
{
    private const float TransferUnit = 5f;
    private const float SoftGateMin = TransferUnit;
    private const float HexIncrement = 1f;
    private const float HexToHealthRate = 2f;
    private const int ProximityRadius = 1;

    public static IReadOnlyList<ProcessDefinition> Generate(
        SemanticDomainConfig semantics,
        IReadOnlyDictionary<EntityKind, KindConfig> kinds)
    {
        ArgumentNullException.ThrowIfNull(semantics);
        ArgumentNullException.ThrowIfNull(kinds);

        var flowsByVerb = semantics.Flows
            .GroupBy(flow => flow.VerbId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

        var processes = new List<ProcessDefinition>();

        foreach (var verb in semantics.Verbs)
        {
            if (verb.OwnerType != SemanticOwnerType.Entity) continue;
            if (verb.OwnerKind is not { } ownerKind) continue;
            if (!kinds.TryGetValue(ownerKind, out var kindConfig)) continue;

            var process = verb.Role switch
            {
                SemanticVerbRole.Mobility => BuildMobility(verb, ownerKind, kindConfig),
                SemanticVerbRole.Drift => BuildDrift(verb, ownerKind, kindConfig),
                SemanticVerbRole.Action => BuildAction(verb, ownerKind, kindConfig, flowsByVerb),
                _ => null,
            };
            if (process is not null)
            {
                ApplyProcessMetadata(process, verb);
                processes.Add(process);
            }
        }

        return processes;
    }

    private static void ApplyProcessMetadata(ProcessDefinition process, SemanticVerbConfig verb)
    {
        process.WorkCommitmentTicks = verb.Id switch
        {
            "settlement_grow" or "faction_organize" or "region_fortify" => 3,
            _ => 0,
        };
    }

    private static ProcessDefinition BuildMobility(
        SemanticVerbConfig verb,
        EntityKind ownerKind,
        KindConfig kindConfig)
    {
        // Mobility is the sub-goal for any other process that fails the owner's
        // proximity check. Its GoalEffects carries the "in_range" structural fact so
        // the process program's blocker→serving-process lookup finds it. The mutation is
        // a no-op when already adjacent, so we don't need a distance-gated
        // precondition on the mobility verb itself.
        var inRangeFact = kindConfig.PrimaryPlane == PrimaryPlane.Physical
            ? WithinPlaneDistancePrecondition.PhysicalInRangeFact
            : WithinPlaneDistancePrecondition.SemanticInRangeFact;

        var process = new ProcessDefinition(
            verb.Id,
            Titleize(verb.Id),
            ownerKind,
            new TargetIsNotSelf(),
            Expr.Score(Expr.Lit(1f)))
        {
            Cooldown = 0,
            EntityCooldown = 0,
            RewardExpression = Expr.Lit(1f),
        };
        process.Mutations.Add(kindConfig.PrimaryPlane == PrimaryPlane.Physical
            ? new MoveTowardTargetMutation()
            : new MoveTowardTargetOnSemanticPlaneMutation());
        process.GoalEffects.Add(AttributeRef.StructuralFact(inRangeFact, ownerKind));
        return process;
    }

    private static ProcessDefinition BuildDrift(
        SemanticVerbConfig verb,
        EntityKind ownerKind,
        KindConfig kindConfig)
    {
        // Drift is the low-score fallback. Always evaluable. No target required.
        var process = new ProcessDefinition(
            verb.Id,
            Titleize(verb.Id),
            ownerKind,
            new AlwaysTrue(),
            Expr.Score(Expr.Lit(0.05f)))
        {
            Cooldown = 0,
            EntityCooldown = 0,
            RewardExpression = Expr.Lit(1f),
        };
        process.Mutations.Add(kindConfig.PrimaryPlane == PrimaryPlane.Physical
            ? new WanderMutation()
            : new WanderOnSemanticPlaneMutation());
        return process;
    }

    private static ProcessDefinition? BuildAction(
        SemanticVerbConfig verb,
        EntityKind ownerKind,
        KindConfig kindConfig,
        Dictionary<string, List<SemanticFlowConfig>> flowsByVerb)
    {
        if (!flowsByVerb.TryGetValue(verb.Id, out var flows) || flows.Count == 0) return null;

        if (flows.TrueForAll(f => f.Mechanism == SemanticMechanism.Decay)) return null;

        if (flows.Exists(f => f.Mechanism == SemanticMechanism.Sacrifice))
        {
            return BuildSelfSacrifice(verb, ownerKind, flows);
        }

        return BuildSingleResourceAction(verb, ownerKind, kindConfig, flows) ??
            BuildConversionAction(verb, ownerKind, kindConfig, flows);
    }

    private static ProcessDefinition? BuildSingleResourceAction(
        SemanticVerbConfig verb,
        EntityKind ownerKind,
        KindConfig kindConfig,
        List<SemanticFlowConfig> flows)
    {
        if (flows.Count != 1)
        {
            return null;
        }

        var flow = flows[0];
        if (!IsResourceFlow(flow, ownerKind))
        {
            return null;
        }

        return flow.Role == SemanticFlowRole.Inflow
            ? BuildHexToHealth(verb, ownerKind, flow, kindConfig)
            : BuildUntouchedToHex(verb, ownerKind, flow);
    }

    private static bool IsResourceFlow(SemanticFlowConfig flow, EntityKind ownerKind) =>
        flow.TargetKind == ownerKind &&
        flow.SourceRefs.Count == 1 &&
        flow.SourceRefs[0].Scope == RefScope.Resource &&
        flow.Role is SemanticFlowRole.Inflow or SemanticFlowRole.Outflow;

    private static ProcessDefinition? BuildConversionAction(
        SemanticVerbConfig verb,
        EntityKind ownerKind,
        KindConfig kindConfig,
        List<SemanticFlowConfig> flows)
    {
        var conversionOut = flows.SingleOrDefault(f =>
            f.Role == SemanticFlowRole.Outflow && f.Mechanism == SemanticMechanism.Conversion);
        var conversionIn = flows.SingleOrDefault(f =>
            f.Role == SemanticFlowRole.Inflow && f.Mechanism == SemanticMechanism.Conversion);

        if (conversionOut is null || conversionIn is null || conversionOut.TargetKind == conversionIn.TargetKind)
        {
            return null;
        }

        return BuildConversion(verb, ownerKind, kindConfig, conversionOut, conversionIn);
    }

    private static ProcessDefinition BuildSelfSacrifice(
        SemanticVerbConfig verb,
        EntityKind ownerKind,
        List<SemanticFlowConfig> flows)
    {
        var outflow = flows.Single(f => f.Role == SemanticFlowRole.Outflow);
        var inflow = flows.Single(f => f.Role == SemanticFlowRole.Inflow);
        var sourceAttr = outflow.TargetAttribute; // the one draining (source of the sacrifice)
        var targetAttr = inflow.TargetAttribute;  // the one growing

        // Self-sacrifice is the last-resort action. It only fires when the target
        // attribute is in the Critical band (<15); above that, the agent must find
        // an external solution (conversion, hex consumption, etc.). This prevents
        // agents from endlessly cannibalising themselves to top up mild deficits.
        var process = new ProcessDefinition(
            verb.Id,
            Titleize(verb.Id),
            ownerKind,
            new AndPrecondition(
            [
                new TargetIsSelf(),
                new SelfAttributeThreshold(sourceAttr, min: SoftGateMin, max: null, processRelevant: false),
                new SelfAttributeThreshold(targetAttr, min: null, max: StandardNeedScale.Critical, processRelevant: false),
            ]),
            Expr.Score(
                Expr.Div(
                    Expr.Sub(Expr.Lit(StandardNeedScale.Critical), Expr.Self(targetAttr)),
                    Expr.Lit(5f))))
        {
            Cooldown = 3,
            EntityCooldown = 1,
            RewardExpression = Expr.Sub(Expr.Lit(StandardNeedScale.Critical), Expr.Self(targetAttr)),
        };
        // Conservation-safe debit+credit pair; caps at source available.
        process.Mutations.Add(new TransferAttributeMutation(
            TransferEndpoint.Self, sourceAttr,
            TransferEndpoint.Self, targetAttr,
            TransferUnit));
        process.GoalEffects.Add(AttributeRef.Self(targetAttr, ownerKind));
        process.Reads.Add(AttributeRef.Self(sourceAttr, ownerKind));
        process.Writes.Add(AttributeRef.Self(sourceAttr, ownerKind));
        process.Writes.Add(AttributeRef.Self(targetAttr, ownerKind));
        return process;
    }

    private static ProcessDefinition BuildHexToHealth(
        SemanticVerbConfig verb,
        EntityKind ownerKind,
        SemanticFlowConfig flow,
        KindConfig kindConfig)
    {
        var healthAttr = flow.TargetAttribute;
        var resource = ResolveResource(flow.SourceRefs[0]);
        var recoveryHosts = kindConfig.HexToHealthRecoveryHosts;

        return recoveryHosts.Count == 0
            ? BuildSelfDirectedHexToHealth(verb, ownerKind, kindConfig, healthAttr, resource)
            : BuildHostedHexToHealth(verb, ownerKind, kindConfig, healthAttr, resource, recoveryHosts);
    }

    private static ProcessDefinition BuildSelfDirectedHexToHealth(
        SemanticVerbConfig verb,
        EntityKind ownerKind,
        KindConfig kindConfig,
        string healthAttr,
        ResourceType resource)
    {
        var process = new ProcessDefinition(
            verb.Id,
            Titleize(verb.Id),
            ownerKind,
            new AndPrecondition(
            [
                new TargetIsSelf(),
                new LocationResourceThreshold(resource, min: 1f, max: null, targetLocation: false, processRelevant: false),
                new SelfAttributeThreshold(healthAttr, min: null, max: 95f, processRelevant: false),
            ]),
            Expr.Score(
                Expr.Div(
                    Expr.Sub(Expr.Lit(100f), Expr.Self(healthAttr)),
                    Expr.Lit(20f))))
        {
            Cooldown = 3,
            EntityCooldown = 1,
            RewardExpression = Expr.Mul(
                Expr.SelfLocationResource(resource),
                Expr.Lit(HexToHealthRate)),
        };
        process.Mutations.Add(new HexToHealthMutation(healthAttr, resource, HexToHealthRate));
        process.GoalEffects.Add(AttributeRef.Self(healthAttr, ownerKind));
        process.Reads.Add(AttributeRef.Self(healthAttr, ownerKind));
        process.Reads.Add(AttributeRef.Resource(resource.ToDomainName()));
        process.Writes.Add(AttributeRef.Self(healthAttr, ownerKind));
        process.ResourceSupplyRoleCode = ProcessResourceSupplyRoleCodes.HexHealthRecovery;
        ApplyNamedProviderRecovery(process, kindConfig);
        return process;
    }

    private static ProcessDefinition BuildHostedHexToHealth(
        SemanticVerbConfig verb,
        EntityKind ownerKind,
        KindConfig kindConfig,
        string healthAttr,
        ResourceType resource,
        IReadOnlyList<RecoveryHostQueryConfig> recoveryHosts)
    {
        var process = new ProcessDefinition(
            verb.Id,
            Titleize(verb.Id),
            ownerKind,
            new AndPrecondition(
            [
                new TargetIsNotSelf(),
                new WithinPlaneDistancePrecondition(0, kindConfig.PrimaryPlane),
                new LocationResourceThreshold(resource, min: 1f, max: null, targetLocation: true, processRelevant: false),
                new SelfAttributeThreshold(healthAttr, min: null, max: 95f, processRelevant: false),
            ]),
            Expr.Score(
                Expr.Min(
                    Expr.Lit(1f),
                    Expr.Div(
                        Expr.Mul(
                            Expr.TargetLocationResource(resource),
                            Expr.Sub(Expr.Lit(100f), Expr.Self(healthAttr))),
                        Expr.Lit(100f)))))
        {
            Cooldown = 3,
            EntityCooldown = 1,
            RewardExpression = Expr.Mul(
                Expr.TargetLocationResource(resource),
                Expr.Lit(HexToHealthRate)),
        };
        process.Mutations.Add(new HexToHealthMutation(healthAttr, resource, HexToHealthRate, targetLocation: true));
        process.GoalEffects.Add(AttributeRef.Self(healthAttr, ownerKind));
        process.Reads.Add(AttributeRef.Self(healthAttr, ownerKind));
        process.Reads.Add(AttributeRef.Resource(resource.ToDomainName()));
        process.Writes.Add(AttributeRef.Self(healthAttr, ownerKind));
        process.ResourceSupplyRoleCode = ProcessResourceSupplyRoleCodes.HexHealthRecovery;
        ApplyNamedProviderRecovery(process, kindConfig);
        foreach (var hostQuery in recoveryHosts)
        {
            foreach (var hostKind in hostQuery.AnyOfKinds)
            {
                process.ProviderSelectors.Add(AttributeRef.Coordinates(hostKind));
            }
        }
        return process;
    }

    private static ProcessDefinition BuildUntouchedToHex(
        SemanticVerbConfig verb,
        EntityKind ownerKind,
        SemanticFlowConfig flow)
    {
        var sourceAttr = flow.TargetAttribute;
        var resource = ResolveResource(flow.SourceRefs[0]);

        var process = new ProcessDefinition(
            verb.Id,
            Titleize(verb.Id),
            ownerKind,
            new AndPrecondition(
            [
                new TargetIsSelf(),
                new SelfAttributeThreshold(sourceAttr, min: SoftGateMin, max: null, processRelevant: false),
            ]),
            Expr.Score(
                Expr.Div(
                    Expr.Self(sourceAttr),
                    Expr.Lit(20f))))
        {
            Cooldown = 4,
            EntityCooldown = 1,
            RewardExpression = Expr.Self(sourceAttr),
        };
        process.Mutations.Add(new ModifySelfAttributeMutation(sourceAttr, -TransferUnit));
        process.Mutations.Add(new ModifyLocationResourceMutation(resource, +HexIncrement));
        process.GoalEffects.Add(AttributeRef.Resource(resource.ToDomainName()));
        process.Reads.Add(AttributeRef.Self(sourceAttr, ownerKind));
        process.Reads.Add(AttributeRef.Resource(resource.ToDomainName()));
        process.Writes.Add(AttributeRef.Self(sourceAttr, ownerKind));
        process.Writes.Add(AttributeRef.Resource(resource.ToDomainName()));
        process.ResourceSupplyRoleCode = ProcessResourceSupplyRoleCodes.HexResourceProducer;
        return process;
    }

    private static void ApplyNamedProviderRecovery(ProcessDefinition process, KindConfig kindConfig)
    {
        if (kindConfig.NamedProviderRecoveryHosts.Count == 0)
        {
            return;
        }

        var host = kindConfig.NamedProviderRecoveryHosts[0];
        process.NamedProviderKind = host.Kind;
        process.NamedProviderAttribute = host.AttributeName;
        process.Reads.Add(AttributeRef.Target(host.AttributeName, host.Kind));
        process.Writes.Add(AttributeRef.Target(host.AttributeName, host.Kind));
    }

    private static ProcessDefinition BuildConversion(
        SemanticVerbConfig verb,
        EntityKind ownerKind,
        KindConfig kindConfig,
        SemanticFlowConfig outflow,
        SemanticFlowConfig inflow)
    {
        var sourceKind = outflow.TargetKind;
        var sourceAttr = outflow.TargetAttribute;
        var destKind = inflow.TargetKind;
        var destAttr = inflow.TargetAttribute;

        bool ownerIsDestination = ownerKind == destKind;

        var preconditionChildren = new List<IPrecondition>
        {
            new TargetIsNotSelf(),
            new WithinPlaneDistancePrecondition(ProximityRadius, kindConfig.PrimaryPlane),
        };
        if (ownerIsDestination)
        {
            preconditionChildren.Add(new TargetAttributeThreshold(sourceAttr, min: SoftGateMin, max: null, processRelevant: false));
            preconditionChildren.Add(new SelfAttributeThreshold(destAttr, min: null, max: 95f, processRelevant: false));
        }
        else
        {
            preconditionChildren.Add(new SelfAttributeThreshold(sourceAttr, min: SoftGateMin, max: null, processRelevant: false));
        }

        var scoreExpression = ownerIsDestination
            ? Expr.Score(Expr.Div(Expr.Sub(Expr.Lit(100f), Expr.Self(destAttr)), Expr.Lit(20f)))
            : Expr.Score(Expr.Div(Expr.Self(sourceAttr), Expr.Lit(20f)));

        var process = new ProcessDefinition(
            verb.Id,
            Titleize(verb.Id),
            ownerKind,
            new AndPrecondition(preconditionChildren),
            scoreExpression)
        {
            Cooldown = 3,
            EntityCooldown = 1,
            RewardExpression = ownerIsDestination
                ? Expr.Sub(Expr.Lit(100f), Expr.Self(destAttr))
                : Expr.Self(sourceAttr),
        };

        // Conservation-safe transfer: target debit and self credit happen atomically,
        // both capped at whatever the source actually holds. No mass manufactured.
        if (ownerIsDestination)
        {
            AddDestinationOwnedConversion(process, ownerKind, sourceKind, sourceAttr, destAttr);
        }
        else
        {
            AddSourceOwnedConversion(process, ownerKind, destKind, sourceAttr, destAttr);
        }
        return process;
    }

    private static void AddDestinationOwnedConversion(
        ProcessDefinition process,
        EntityKind ownerKind,
        EntityKind sourceKind,
        string sourceAttr,
        string destAttr)
    {
        process.Mutations.Add(new TransferAttributeMutation(
            TransferEndpoint.Target, sourceAttr,
            TransferEndpoint.Self, destAttr,
            TransferUnit));
        process.Reads.Add(AttributeRef.Target(sourceAttr, sourceKind));
        process.Writes.Add(AttributeRef.Target(sourceAttr, sourceKind));
        process.Writes.Add(AttributeRef.Self(destAttr, ownerKind));
        process.GoalEffects.Add(AttributeRef.Self(destAttr, ownerKind));
        process.ProviderSelectors.Add(AttributeRef.Target(sourceAttr, sourceKind));
    }

    private static void AddSourceOwnedConversion(
        ProcessDefinition process,
        EntityKind ownerKind,
        EntityKind destKind,
        string sourceAttr,
        string destAttr)
    {
        process.Mutations.Add(new TransferAttributeMutation(
            TransferEndpoint.Self, sourceAttr,
            TransferEndpoint.Target, destAttr,
            TransferUnit));
        process.Reads.Add(AttributeRef.Self(sourceAttr, ownerKind));
        process.Writes.Add(AttributeRef.Self(sourceAttr, ownerKind));
        process.Writes.Add(AttributeRef.Target(destAttr, destKind));
        process.GoalEffects.Add(AttributeRef.Target(destAttr, destKind));
        process.ProviderSelectors.Add(AttributeRef.Target(destAttr, destKind));
    }

    private static ResourceType ResolveResource(AttributeRef resourceRef)
    {
        foreach (var rt in Enum.GetValues<ResourceType>())
        {
            if (string.Equals(rt.ToDomainName(), resourceRef.Name, StringComparison.Ordinal))
            {
                return rt;
            }
        }
        throw new DefinitionGraphException(
            $"RuntimeProcessGenerator: unknown hex resource '{resourceRef.Name}' on flow source ref.");
    }

    private static string Titleize(string verbId)
    {
        var parts = verbId.Split('_', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts.Select(p =>
            p.Length == 0 ? p : char.ToUpperInvariant(p[0]) + p[1..]));
    }
}
