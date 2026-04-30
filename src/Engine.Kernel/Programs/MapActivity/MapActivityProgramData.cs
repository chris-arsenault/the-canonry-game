namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class MapActivityProgramData : IKernelProgramData
{
    public MapActivityProgramData(float presenceHeat = 1f)
    {
        PresenceHeat = presenceHeat;
    }

    public int ProgramId => KernelProgramCodes.MapActivity;

    public int SchemaVersion => 1;

    public float PresenceHeat { get; set; }
}

