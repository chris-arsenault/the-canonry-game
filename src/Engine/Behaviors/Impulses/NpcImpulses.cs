namespace Engine.Behaviors.Impulses;

#pragma warning disable CA1062 // Impulse parameters validated by UtilityEvaluator framework

using Engine.Graph;
using Engine.Kinds;
using Engine.Map;

/// <summary>
/// Hunger: NPCs with low local food availability seek settlements with surplus.
/// Scored higher when hunger is high.
/// </summary>
public sealed class HungerImpulse : IImpulse
{
    public string Name => "seek_food";

    public float HungerIncrement { get; set; } = 0.5f;

    public float Score(Entity entity, IWorldState worldState)
    {
        var hunger = worldState.Attributes.Get(entity.Id, "hunger");
        return Math.Clamp(hunger / 100f, 0.05f, 0.95f);
    }

    public void Execute(Entity entity, IWorldState worldState)
    {
        // Increase hunger each tick
        worldState.Attributes.Modify(entity.Id, "hunger", HungerIncrement);

        // If at a settlement, consume food (reduce hunger)
        if (entity.Coordinates is not null)
        {
            var coord = new HexCoord(entity.Coordinates.Value.Q, entity.Coordinates.Value.R);
            var entitiesHere = worldState.SpatialIndex.EntitiesAtCell(coord);

            foreach (var eid in entitiesHere)
            {
                var other = worldState.Graph.GetEntity(eid);
                if (other?.Kind == EntityKind.Settlement)
                {
                    // At a settlement: reduce hunger based on settlement prosperity
                    var prosperity = worldState.Attributes.Get(eid, "prosperity");
                    var relief = Math.Min(prosperity * 0.02f, 5f);
                    worldState.Attributes.Modify(entity.Id, "hunger", -relief);
                    break;
                }
            }
        }
    }
}

/// <summary>
/// Ambition: NPCs seek high-value locations based on their subtype.
/// Scored based on the NPC's ambition attribute.
/// </summary>
public sealed class AmbitionImpulse : IImpulse
{
    public string Name => "pursue_ambition";

    public float Score(Entity entity, IWorldState worldState)
    {
        var ambition = worldState.Attributes.Get(entity.Id, "ambition");
        var hunger = worldState.Attributes.Get(entity.Id, "hunger");

        // Ambition is deprioritized when hungry
        return Math.Clamp((ambition / 100f) * (1f - (hunger / 200f)), 0.05f, 0.8f);
    }

    public void Execute(Entity entity, IWorldState worldState)
    {
        // For now: ambition slightly increases NPC's effective influence on their settlement
        if (entity.Coordinates is null)
        {
            return;
        }

        var coord = new HexCoord(entity.Coordinates.Value.Q, entity.Coordinates.Value.R);
        foreach (var eid in worldState.SpatialIndex.EntitiesAtCell(coord))
        {
            var other = worldState.Graph.GetEntity(eid);
            if (other?.Kind == EntityKind.Settlement)
            {
                // NPC presence boosts settlement prosperity slightly
                worldState.Attributes.Modify(eid, "prosperity", 0.1f);
                break;
            }
        }
    }
}
