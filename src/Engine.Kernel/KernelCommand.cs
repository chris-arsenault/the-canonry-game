namespace Engine.Kernel;

internal readonly record struct NoOpKernelCommand(
    int IssueVersion,
    int SubmitterId = 0,
    int SubmitterSequence = 0);

internal readonly record struct SetGeneratorEnabledKernelCommand(
    int IssueVersion,
    int GeneratorId,
    int Enabled,
    int SubmitterId = 0,
    int SubmitterSequence = 0);

internal readonly record struct SetGambitEnabledKernelCommand(
    int IssueVersion,
    int GambitId,
    int Enabled,
    int SubmitterId = 0,
    int SubmitterSequence = 0);

internal readonly record struct BlessCellCultureKernelCommand(
    int IssueVersion,
    int CultureId,
    int PlaneCode,
    int Q,
    int R,
    int Radius,
    FixedScalar Amount,
    int SubmitterId = 0,
    int SubmitterSequence = 0);

internal readonly record struct FavorEntityKernelCommand(
    int IssueVersion,
    int EntityId,
    int DurationTicks,
    FixedScalar MultiplierDelta,
    int SubmitterId = 0,
    int SubmitterSequence = 0);

internal readonly partial record struct KernelCommand
{
    private readonly int _a;
    private readonly int _b;
    private readonly int _c;
    private readonly int _d;
    private readonly int _e;
    private readonly int _f;
    private readonly int _g;
    private readonly int _h;
    private readonly int _i;
    private readonly int _j;
    private readonly FixedScalar _x;
    private readonly FixedScalar _y;

    public KernelCommand(
        int commandType,
        int issueVersion,
        int submitterId = 0,
        int submitterSequence = 0,
        int a = 0,
        int b = 0,
        int c = 0,
        int d = 0,
        int e = 0,
        int f = 0,
        int g = 0,
        int h = 0,
        int i = 0,
        int j = 0,
        FixedScalar x = default,
        FixedScalar y = default)
    {
        CommandType = commandType;
        CommandId = 0;
        IssueVersion = issueVersion;
        SubmitterId = submitterId;
        SubmitterSequence = submitterSequence;
        _a = a;
        _b = b;
        _c = c;
        _d = d;
        _e = e;
        _f = f;
        _g = g;
        _h = h;
        _i = i;
        _j = j;
        _x = x;
        _y = y;
    }

    private KernelCommand(
        int commandType,
        int commandId,
        int issueVersion,
        int submitterId,
        int submitterSequence,
        int a,
        int b,
        int c,
        int d,
        int e,
        int f,
        int g,
        int h,
        int i,
        int j,
        FixedScalar x,
        FixedScalar y)
    {
        CommandType = commandType;
        CommandId = commandId;
        IssueVersion = issueVersion;
        SubmitterId = submitterId;
        SubmitterSequence = submitterSequence;
        _a = a;
        _b = b;
        _c = c;
        _d = d;
        _e = e;
        _f = f;
        _g = g;
        _h = h;
        _i = i;
        _j = j;
        _x = x;
        _y = y;
    }

    public const int IntSlotCount = 10;
    public const int ScalarSlotCount = 2;

    public int CommandType { get; }
    public int CommandId { get; }
    public int IssueVersion { get; }
    public int SubmitterId { get; }
    public int SubmitterSequence { get; }

    public KernelCommand WithCommandId(int commandId) =>
        new(
            CommandType,
            commandId,
            IssueVersion,
            SubmitterId,
            SubmitterSequence,
            _a,
            _b,
            _c,
            _d,
            _e,
            _f,
            _g,
            _h,
            _i,
            _j,
            _x,
            _y);

    public int GetIntSlot(int slot)
    {
        NumericGuard.ThrowIfNegative(slot);
        NumericGuard.ThrowIfGreaterThanOrEqual(slot, IntSlotCount);
        return slot < 5 ? GetLowIntSlot(slot) : GetHighIntSlot(slot);
    }

    private int GetLowIntSlot(int slot) =>
        slot switch
        {
            0 => _a,
            1 => _b,
            2 => _c,
            3 => _d,
            4 => _e,
            _ => 0,
        };

    private int GetHighIntSlot(int slot) =>
        slot switch
        {
            5 => _f,
            6 => _g,
            7 => _h,
            8 => _i,
            9 => _j,
            _ => 0,
        };

    public FixedScalar GetScalarSlot(int slot)
    {
        NumericGuard.ThrowIfNegative(slot);
        NumericGuard.ThrowIfGreaterThanOrEqual(slot, ScalarSlotCount);
        return slot switch
        {
            0 => _x,
            1 => _y,
            _ => 0,
        };
    }
}
