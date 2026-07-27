namespace Engine.Kernel;

internal sealed class KernelCommandRing
{
    private readonly KernelCommand[] _commands;
    private readonly object _gate = new();
    private int _head;
    private int _tail;
    private int _count;
    private int _nextCommandId;
    private long _rejectedWriteCount;

    public KernelCommandRing(int capacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(capacity);
        _commands = new KernelCommand[capacity];
    }

    public int Capacity => _commands.Length;

    public int Count
    {
        get => global::System.Threading.Volatile.Read(ref _count);
    }

    public long RejectedWriteCount => global::System.Threading.Volatile.Read(ref _rejectedWriteCount);

    public bool TryEnqueue(in KernelCommand command, out int commandId)
    {
        lock (_gate)
        {
            if (global::System.Threading.Volatile.Read(ref _count) == _commands.Length)
            {
                _rejectedWriteCount++;
                commandId = 0;
                return false;
            }

            commandId = ++_nextCommandId;
            _commands[_tail] = command.WithCommandId(commandId);
            _tail++;
            if (_tail == _commands.Length)
            {
                _tail = 0;
            }

            global::System.Threading.Interlocked.Increment(ref _count);
            return true;
        }
    }

    public bool TryRead(out KernelCommand command)
    {
        lock (_gate)
        {
            if (_count == 0)
            {
                command = default;
                return false;
            }

            command = _commands[_head];
            _commands[_head] = default;
            _head++;
            if (_head == _commands.Length)
            {
                _head = 0;
            }

            _count--;
            return true;
        }
    }

    internal void WriteSnapshot(global::System.IO.BinaryWriter writer)
    {
        lock (_gate)
        {
            writer.Write(_nextCommandId);
            writer.Write(_count);
            for (var offset = 0; offset < _count; offset++)
            {
                var command = _commands[
                    (_head + offset) % _commands.Length];
                writer.Write(command.CommandType);
                writer.Write(command.CommandId);
                writer.Write(command.IssueVersion);
                writer.Write(command.SubmitterId);
                writer.Write(command.SubmitterSequence);
                for (var slot = 0;
                     slot < KernelCommand.IntSlotCount;
                     slot++)
                {
                    writer.Write(command.GetIntSlot(slot));
                }

                for (var slot = 0;
                     slot < KernelCommand.ScalarSlotCount;
                     slot++)
                {
                    writer.Write(command.GetScalarSlot(slot).Units);
                }
            }
        }
    }

    internal void ReadSnapshot(global::System.IO.BinaryReader reader)
    {
        lock (_gate)
        {
            if (_count != 0)
            {
                throw new global::System.InvalidOperationException();
            }

            _nextCommandId = reader.ReadInt32();
            var count = reader.ReadInt32();
            if (count < 0 || count > _commands.Length)
            {
                throw new global::System.InvalidOperationException();
            }

            for (var index = 0; index < count; index++)
            {
                var commandType = reader.ReadInt32();
                var commandId = reader.ReadInt32();
                var issueVersion = reader.ReadInt32();
                var submitterId = reader.ReadInt32();
                var submitterSequence = reader.ReadInt32();
                var intSlots = new int[KernelCommand.IntSlotCount];
                for (var slot = 0; slot < intSlots.Length; slot++)
                {
                    intSlots[slot] = reader.ReadInt32();
                }

                var scalar0 = FixedScalar.FromUnits(reader.ReadInt64());
                var scalar1 = FixedScalar.FromUnits(reader.ReadInt64());
                _commands[index] = new KernelCommand(
                        commandType,
                        issueVersion,
                        submitterId,
                        submitterSequence,
                        intSlots[0],
                        intSlots[1],
                        intSlots[2],
                        intSlots[3],
                        intSlots[4],
                        intSlots[5],
                        intSlots[6],
                        intSlots[7],
                        intSlots[8],
                        intSlots[9],
                        scalar0,
                        scalar1)
                    .WithCommandId(commandId);
            }

            _head = 0;
            _tail = count == _commands.Length ? 0 : count;
            _count = count;
        }
    }
}
