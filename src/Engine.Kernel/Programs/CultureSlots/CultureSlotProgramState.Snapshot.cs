namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class CultureSlotProgramState : IActiveKernelProgramStateSnapshot
{
    public void WriteActiveSnapshot(global::System.IO.BinaryWriter writer)
    {
        var occupiedCount = 0;
        for (var slot = 1; slot < _occupantEntityIds.Length; slot++)
        {
            if (_occupantEntityIds[slot] > 0)
            {
                occupiedCount++;
            }
        }

        writer.Write(occupiedCount);
        for (var slot = 1; slot < _occupantEntityIds.Length; slot++)
        {
            if (_occupantEntityIds[slot] <= 0)
            {
                continue;
            }

            writer.Write(slot);
            writer.Write(_occupantEntityIds[slot]);
            writer.Write(_occupiedAtTicks[slot]);
            writer.Write(_occupantScores[slot].Units);
        }
    }

    public void ReadActiveSnapshot(global::System.IO.BinaryReader reader)
    {
        var occupiedCount = reader.ReadInt32();
        if ((uint)occupiedCount >= (uint)_occupantEntityIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var item = 0; item < occupiedCount; item++)
        {
            var slot = reader.ReadInt32();
            var entityId = reader.ReadInt32();
            var occupiedAtTick = reader.ReadInt32();
            var score = FixedScalar.FromUnits(reader.ReadInt64());
            if ((uint)slot == 0u ||
                slot >= _occupantEntityIds.Length ||
                entityId <= 0 ||
                occupiedAtTick < 0 ||
                score < 0 ||
                _occupantEntityIds[slot] != 0)
            {
                throw new global::System.InvalidOperationException();
            }

            _occupantEntityIds[slot] = entityId;
            _occupiedAtTicks[slot] = occupiedAtTick;
            _occupantScores[slot] = score;
        }
    }
}
