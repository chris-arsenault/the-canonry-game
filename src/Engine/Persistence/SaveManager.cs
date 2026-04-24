namespace Engine.Persistence;

using MessagePack;

using Engine.Graph;

/// <summary>
/// Serializes and deserializes the full simulation state using MessagePack.
/// Binary format: fast, compact, versioned.
/// </summary>
public static class SaveManager
{
    /// <summary>
    /// Capture the current simulation state into a serializable snapshot.
    /// </summary>
    public static SaveState Capture(WorldGraph graph, int currentTick,
        float currentMana = 0f, float manaCap = 10f)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var state = new SaveState
        {
            CurrentTick = currentTick,
            CurrentMana = currentMana,
            ManaCap = manaCap,
        };

        // Entities
        foreach (var (id, entity) in graph.AllEntities)
        {
            state.Entities.Add(new EntityData
            {
                Id = id.Value,
                Kind = entity.Kind,
                Subtype = entity.Subtype,
                Name = entity.Name,
                Status = entity.Status,
                Culture = entity.Culture,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                CoordQ = entity.Coordinates.Q,
                CoordR = entity.Coordinates.R,
                SemanticQ = entity.SemanticPosition.Q,
                SemanticR = entity.SemanticPosition.R,
            });
        }

        // Relationships (active)
        foreach (var (entityId, _) in graph.AllEntities)
        {
            foreach (var rel in graph.OutgoingRelationships(entityId))
            {
                state.Relationships.Add(new RelationshipData
                {
                    Kind = rel.Kind,
                    Polarity = rel.Polarity,
                    SourceId = rel.SourceId.Value,
                    TargetId = rel.TargetId.Value,
                    Strength = rel.Strength,
                    StartDate = rel.StartDate,
                    EndDate = rel.EndDate,
                    Facts = rel.Facts.ToList(),
                });
            }
        }

        // Attributes (from entity-owned AttributeSet)
        foreach (var (id, entity) in graph.AllEntities)
        {
            foreach (var attrName in entity.Attributes.Names)
            {
                state.Attributes.Add(new AttributeData
                {
                    EntityId = id.Value,
                    Name = attrName,
                    Value = entity.Attributes.GetValue(attrName),
                });
            }
        }

        return state;
    }

    /// <summary>Serialize a save state to bytes.</summary>
    public static byte[] Serialize(SaveState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return MessagePackSerializer.Serialize(state);
    }

    /// <summary>Deserialize bytes to a save state.</summary>
    public static SaveState Deserialize(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return MessagePackSerializer.Deserialize<SaveState>(data);
    }

    /// <summary>Serialize and write to a file.</summary>
    public static void SaveToFile(SaveState state, string path)
    {
        ArgumentNullException.ThrowIfNull(state);
        var bytes = Serialize(state);
        File.WriteAllBytes(path, bytes);
    }

    /// <summary>Read from file and deserialize.</summary>
    public static SaveState LoadFromFile(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return Deserialize(bytes);
    }
}
