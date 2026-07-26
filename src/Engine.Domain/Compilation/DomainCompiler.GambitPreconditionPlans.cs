namespace Engine.Domain.Compilation;

using Engine.Constants;
using Engine.Domain.Compilation.KernelPrograms;

public static partial class DomainCompiler
{
    private enum GambitPreconditionComposition
    {
        And,
        Or,
    }

    private interface IGambitPreconditionNode
    {
    }

    private sealed class GambitPreconditionLeafNode(
        BehaviorProgramPreconditionOp op,
        bool negated,
        int sourceOrder) : IGambitPreconditionNode
    {
        public BehaviorProgramPreconditionOp Op { get; } = op;

        public bool Negated { get; } = negated;

        public int SourceOrder { get; } = sourceOrder;

        public int ActorValueSlot { get; set; }
    }

    private sealed class GambitPreconditionCompositeNode(
        GambitPreconditionComposition composition,
        IReadOnlyList<IGambitPreconditionNode> children) : IGambitPreconditionNode
    {
        public GambitPreconditionComposition Composition { get; } = composition;

        public IReadOnlyList<IGambitPreconditionNode> Children { get; } = children;
    }

    private sealed class GambitPreconditionConstantNode(bool value) : IGambitPreconditionNode
    {
        public bool Value { get; } = value;
    }

    private sealed record RequiredRelationshipTargetSource(
        int DirectionCode,
        int RelationshipKindId,
        bool HasMinimumStrength,
        FixedScalar MinimumStrength);

    private static BehaviorProgramPreconditionPlan CompileGambitPreconditionPlan(
        IGambitPreconditionNode root)
    {
        var actorValueCount = 0;
        AssignActorValueSlots(root, ref actorValueCount);

        var relationshipTargetSource = SelectRequiredRelationshipTargetSource(root);
        var candidateInstructions = new List<BehaviorProgramPreconditionInstruction>();
        var candidateEntry = CompileCandidateBranch(
            root,
            GambitPreconditionDestinationCodes.Passed,
            GambitPreconditionDestinationCodes.Failed,
            candidateInstructions,
            relationshipTargetSource);

        var actorRoot = SimplifyActorProjection(ProjectActorPreconditions(root));
        var actorInstructions = new List<BehaviorProgramActorPreconditionInstruction>();
        var actorEntry = CompileActorBranch(
            actorRoot,
            GambitPreconditionDestinationCodes.Passed,
            GambitPreconditionDestinationCodes.Failed,
            actorInstructions);

        return new BehaviorProgramPreconditionPlan(
            candidateInstructions,
            candidateEntry,
            actorInstructions,
            actorEntry,
            actorValueCount,
            ToBehaviorRelationshipTargetSource(relationshipTargetSource));
    }

    private static void AssignActorValueSlots(
        IGambitPreconditionNode node,
        ref int actorValueCount)
    {
        switch (node)
        {
            case GambitPreconditionLeafNode leaf when PreconditionIsActorScoped(leaf.Op):
                leaf.ActorValueSlot = ++actorValueCount;
                return;
            case GambitPreconditionCompositeNode composite:
                foreach (var child in composite.Children)
                {
                    AssignActorValueSlots(child, ref actorValueCount);
                }

                return;
        }
    }

    private static IGambitPreconditionNode ProjectActorPreconditions(
        IGambitPreconditionNode node)
    {
        return node switch
        {
            GambitPreconditionConstantNode constant => constant,
            GambitPreconditionLeafNode leaf when leaf.ActorValueSlot > 0 => leaf,
            GambitPreconditionLeafNode => new GambitPreconditionConstantNode(true),
            GambitPreconditionCompositeNode composite => new GambitPreconditionCompositeNode(
                composite.Composition,
                composite.Children.Select(ProjectActorPreconditions).ToArray()),
            _ => throw new InvalidOperationException(
                $"Unsupported gambit precondition node '{node.GetType().Name}'."),
        };
    }

    private static IGambitPreconditionNode SimplifyActorProjection(
        IGambitPreconditionNode node)
    {
        if (node is not GambitPreconditionCompositeNode composite)
        {
            return node;
        }

        var children = composite.Children
            .Select(SimplifyActorProjection)
            .ToList();
        if (composite.Composition == GambitPreconditionComposition.And)
        {
            if (children.OfType<GambitPreconditionConstantNode>().Any(static child => !child.Value))
            {
                return new GambitPreconditionConstantNode(false);
            }

            children.RemoveAll(static child => child is GambitPreconditionConstantNode { Value: true });
            return CollapseComposite(composite.Composition, children, emptyValue: true);
        }

        if (children.OfType<GambitPreconditionConstantNode>().Any(static child => child.Value))
        {
            return new GambitPreconditionConstantNode(true);
        }

        children.RemoveAll(static child => child is GambitPreconditionConstantNode { Value: false });
        return CollapseComposite(composite.Composition, children, emptyValue: false);
    }

    private static IGambitPreconditionNode CollapseComposite(
        GambitPreconditionComposition composition,
        IReadOnlyList<IGambitPreconditionNode> children,
        bool emptyValue)
    {
        return children.Count switch
        {
            0 => new GambitPreconditionConstantNode(emptyValue),
            1 => children[0],
            _ => new GambitPreconditionCompositeNode(composition, children),
        };
    }

    private static int CompileCandidateBranch(
        IGambitPreconditionNode node,
        int trueDestination,
        int falseDestination,
        List<BehaviorProgramPreconditionInstruction> instructions,
        RequiredRelationshipTargetSource? relationshipTargetSource)
    {
        switch (node)
        {
            case GambitPreconditionConstantNode constant:
                return constant.Value ? trueDestination : falseDestination;
            case GambitPreconditionLeafNode leaf:
                var leafTrue = leaf.Negated ? falseDestination : trueDestination;
                var leafFalse = leaf.Negated ? trueDestination : falseDestination;
                var op = leaf.Op;
                instructions.Add(new BehaviorProgramPreconditionInstruction(
                    op.Code,
                    op.A,
                    op.B,
                    op.C,
                    op.D,
                    op.X,
                    op.Y,
                    leaf.ActorValueSlot,
                    relationshipTargetSource is not null &&
                        RelationshipTargetSourceForLeaf(leaf) == relationshipTargetSource,
                    leafTrue,
                    leafFalse));
                return instructions.Count;
            case GambitPreconditionCompositeNode composite:
                return CompileCandidateComposite(
                    composite,
                    trueDestination,
                    falseDestination,
                    instructions,
                    relationshipTargetSource);
            default:
                throw new InvalidOperationException(
                    $"Unsupported gambit precondition node '{node.GetType().Name}'.");
        }
    }

    private static int CompileCandidateComposite(
        GambitPreconditionCompositeNode composite,
        int trueDestination,
        int falseDestination,
        List<BehaviorProgramPreconditionInstruction> instructions,
        RequiredRelationshipTargetSource? relationshipTargetSource)
    {
        var children = OrderedChildren(composite);
        var next = composite.Composition == GambitPreconditionComposition.And
            ? trueDestination
            : falseDestination;
        for (var i = children.Length - 1; i >= 0; i--)
        {
            next = composite.Composition == GambitPreconditionComposition.And
                ? CompileCandidateBranch(
                    children[i],
                    next,
                    falseDestination,
                    instructions,
                    relationshipTargetSource)
                : CompileCandidateBranch(
                    children[i],
                    trueDestination,
                    next,
                    instructions,
                    relationshipTargetSource);
        }

        return next;
    }

    private static int CompileActorBranch(
        IGambitPreconditionNode node,
        int trueDestination,
        int falseDestination,
        List<BehaviorProgramActorPreconditionInstruction> instructions)
    {
        switch (node)
        {
            case GambitPreconditionConstantNode constant:
                return constant.Value ? trueDestination : falseDestination;
            case GambitPreconditionLeafNode leaf:
                if (leaf.ActorValueSlot <= 0)
                {
                    throw new InvalidOperationException(
                        "Actor feasibility plan contains a target-dependent predicate.");
                }

                var leafTrue = leaf.Negated ? falseDestination : trueDestination;
                var leafFalse = leaf.Negated ? trueDestination : falseDestination;
                instructions.Add(new BehaviorProgramActorPreconditionInstruction(
                    leaf.ActorValueSlot,
                    leafTrue,
                    leafFalse));
                return instructions.Count;
            case GambitPreconditionCompositeNode composite:
                return CompileActorComposite(
                    composite,
                    trueDestination,
                    falseDestination,
                    instructions);
            default:
                throw new InvalidOperationException(
                    $"Unsupported gambit actor precondition node '{node.GetType().Name}'.");
        }
    }

    private static int CompileActorComposite(
        GambitPreconditionCompositeNode composite,
        int trueDestination,
        int falseDestination,
        List<BehaviorProgramActorPreconditionInstruction> instructions)
    {
        var children = OrderedChildren(composite);
        var next = composite.Composition == GambitPreconditionComposition.And
            ? trueDestination
            : falseDestination;
        for (var i = children.Length - 1; i >= 0; i--)
        {
            next = composite.Composition == GambitPreconditionComposition.And
                ? CompileActorBranch(children[i], next, falseDestination, instructions)
                : CompileActorBranch(children[i], trueDestination, next, instructions);
        }

        return next;
    }

    private static IGambitPreconditionNode[] OrderedChildren(
        GambitPreconditionCompositeNode composite)
    {
        return composite.Children
            .OrderBy(PreconditionCost)
            .ThenBy(FirstSourceOrder)
            .ToArray();
    }

    private static int PreconditionCost(IGambitPreconditionNode node)
    {
        return node switch
        {
            GambitPreconditionConstantNode => 0,
            GambitPreconditionLeafNode leaf => PreconditionLeafCost(leaf.Op),
            GambitPreconditionCompositeNode composite => composite.Children.Count == 0
                ? 0
                : composite.Children.Min(PreconditionCost),
            _ => int.MaxValue,
        };
    }

    private static int FirstSourceOrder(IGambitPreconditionNode node)
    {
        return node switch
        {
            GambitPreconditionLeafNode leaf => leaf.SourceOrder,
            GambitPreconditionCompositeNode composite => composite.Children.Count == 0
                ? int.MaxValue
                : composite.Children.Min(FirstSourceOrder),
            _ => int.MaxValue,
        };
    }

    private static int PreconditionLeafCost(BehaviorProgramPreconditionOp op)
    {
        if (PreconditionIsActorScoped(op))
        {
            return 0;
        }

        if (PreconditionIsTargetScoped(op))
        {
            return 1;
        }

        if (op.Code is BehaviorProgramPrecondition.SelfHasRelationshipToTarget or
            BehaviorProgramPrecondition.TargetHasRelationshipToSelf or
            BehaviorProgramPrecondition.RelatedEntityMatchesTarget)
        {
            return 2;
        }

        return op.Code == BehaviorProgramPrecondition.WithinTravelReach ? 4 : 3;
    }

    private static bool PreconditionIsActorScoped(BehaviorProgramPreconditionOp op)
    {
        return PreconditionIsActorStateScoped(op.Code) ||
            PreconditionIsActorContextScoped(op.Code) ||
            PreconditionUsesEndpoint(op, BehaviorProgramEndpoint.Self);
    }

    private static bool PreconditionIsActorStateScoped(BehaviorProgramPrecondition code)
    {
        return code is BehaviorProgramPrecondition.Always or
            BehaviorProgramPrecondition.SelfAttributeRange or
            BehaviorProgramPrecondition.EntityCountAtLeast or
            BehaviorProgramPrecondition.SelfCultureMass or
            BehaviorProgramPrecondition.SelfWorkTierBelow;
    }

    private static bool PreconditionIsActorContextScoped(BehaviorProgramPrecondition code)
    {
        return code is BehaviorProgramPrecondition.HasColocatedEntity or
            BehaviorProgramPrecondition.SelfHasOutgoingRelationship or
            BehaviorProgramPrecondition.SelfHasOutgoingRelationshipToKind or
            BehaviorProgramPrecondition.SelfHexFactRange or
            BehaviorProgramPrecondition.SelfOnTerrainGroup or
            BehaviorProgramPrecondition.RelationshipExists;
    }

    private static bool PreconditionIsTargetScoped(BehaviorProgramPreconditionOp op)
    {
        if (op.Code is BehaviorProgramPrecondition.TargetIsSelf or
            BehaviorProgramPrecondition.TargetIsNotSelf or
            BehaviorProgramPrecondition.TargetAttributeRange or
            BehaviorProgramPrecondition.TargetHexFactRange)
        {
            return true;
        }

        return PreconditionUsesEndpoint(op, BehaviorProgramEndpoint.Target);
    }

    private static bool PreconditionUsesEndpoint(
        BehaviorProgramPreconditionOp op,
        BehaviorProgramEndpoint endpoint)
    {
        if (op.Code is BehaviorProgramPrecondition.EndpointCultureIs or
            BehaviorProgramPrecondition.EndpointSubtypeIs)
        {
            return op.A == (int)endpoint;
        }

        return op.Code == BehaviorProgramPrecondition.EndpointRelationshipCountBelow &&
            op.B == (int)endpoint;
    }

    private static RequiredRelationshipTargetSource? SelectRequiredRelationshipTargetSource(
        IGambitPreconditionNode root)
    {
        var required = RequiredRelationshipTargetSources(root);
        var source = required
            .OrderByDescending(static candidate => candidate.HasMinimumStrength)
            .ThenByDescending(static candidate => candidate.MinimumStrength)
            .ThenBy(static candidate => candidate.RelationshipKindId)
            .ThenBy(static candidate => candidate.DirectionCode)
            .FirstOrDefault();
        return source;
    }

    private static BehaviorProgramRelationshipTargetSource? ToBehaviorRelationshipTargetSource(
        RequiredRelationshipTargetSource? source)
    {
        return source is null
            ? null
            : new BehaviorProgramRelationshipTargetSource(
                source.DirectionCode,
                source.RelationshipKindId,
                source.HasMinimumStrength,
                source.MinimumStrength);
    }

    private static HashSet<RequiredRelationshipTargetSource> RequiredRelationshipTargetSources(
        IGambitPreconditionNode node)
    {
        switch (node)
        {
            case GambitPreconditionLeafNode leaf:
                return RelationshipTargetSourceForLeaf(leaf) is { } source
                    ? [source]
                    : [];
            case GambitPreconditionCompositeNode composite when
                composite.Composition == GambitPreconditionComposition.And:
                var union = new HashSet<RequiredRelationshipTargetSource>();
                foreach (var child in composite.Children)
                {
                    union.UnionWith(RequiredRelationshipTargetSources(child));
                }

                return union;
            case GambitPreconditionCompositeNode composite:
                if (composite.Children.Count == 0)
                {
                    return [];
                }

                var intersection = RequiredRelationshipTargetSources(composite.Children[0]);
                for (var i = 1; i < composite.Children.Count; i++)
                {
                    intersection.IntersectWith(
                        RequiredRelationshipTargetSources(composite.Children[i]));
                }

                return intersection;
            default:
                return [];
        }
    }

    private static RequiredRelationshipTargetSource? RelationshipTargetSourceForLeaf(
        GambitPreconditionLeafNode leaf)
    {
        if (leaf.Negated)
        {
            return null;
        }

        var direction = leaf.Op.Code switch
        {
            BehaviorProgramPrecondition.SelfHasRelationshipToTarget or
                BehaviorProgramPrecondition.RelatedEntityMatchesTarget =>
                GambitRelationshipTargetDirectionCodes.Outgoing,
            BehaviorProgramPrecondition.TargetHasRelationshipToSelf =>
                GambitRelationshipTargetDirectionCodes.Incoming,
            _ => 0,
        };
        if (direction == 0 || leaf.Op.A <= 0)
        {
            return null;
        }

        return new RequiredRelationshipTargetSource(
            direction,
            leaf.Op.A,
            leaf.Op.B != 0,
            leaf.Op.X);
    }
}
