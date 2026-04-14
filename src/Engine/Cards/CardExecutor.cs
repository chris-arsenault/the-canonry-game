namespace Engine.Cards;

using Engine.Processes;

/// <summary>
/// Processes active cards in the tableau each tick. Evaluates preconditions,
/// fires mutations for cards whose conditions are met. Handles all three card types
/// (generator, system, action) through the shared CardDefinition structure.
/// </summary>
public sealed class CardExecutor
{
    private readonly Random _random;

    public CardExecutor(Random? random = null)
    {
        _random = random ?? Random.Shared;
    }

    /// <summary>
    /// Evaluate and potentially fire a single card instance.
    /// Returns true if the card fired.
    /// </summary>
    public bool TryFire(CardInstance instance, IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(context);

        if (!instance.IsActive)
        {
            return false;
        }

        if (!instance.IsOnCadence(context.CurrentTick))
        {
            return false;
        }

        if (!instance.IsReady(context.CurrentTick))
        {
            return false;
        }

        // Evaluate precondition tree
        var preconditionMet = instance.Definition.Precondition.Evaluate(context);
        instance.LastPreconditionResult = preconditionMet;

        if (!preconditionMet)
        {
            return false;
        }

        // For action cards: roll success/failure
        if (instance.Definition.Type == CardType.Action && instance.Definition.SuccessProbability < 1f)
        {
            var roll = (float)_random.NextDouble();
            if (roll > instance.Definition.SuccessProbability)
            {
                // Failure: execute failure mutations
                ExecuteMutations(instance.Definition.FailureMutations, context);
                instance.LastFiredTick = context.CurrentTick;
                instance.FireCount++;
                return true; // It "fired" (with failure outcome)
            }
        }

        // Success: execute success mutations
        ExecuteMutations(instance.Definition.Mutations, context);
        instance.LastFiredTick = context.CurrentTick;
        instance.FireCount++;

        return true;
    }

    private static void ExecuteMutations(IEnumerable<IMutation> mutations, IMutationContext context)
    {
        foreach (var mutation in mutations)
        {
            mutation.Execute(context);
        }
    }
}
