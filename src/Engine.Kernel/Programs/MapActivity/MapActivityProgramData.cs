namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class MapActivityProgramData : IKernelProgramData
{
    public MapActivityProgramData(
        float presenceHeat = 1f,
        float heatDecayPerTick = 0.99f,
        float heatEmaAlpha = 0.01f)
    {
        PresenceHeat = presenceHeat;
        HeatDecayPerTick = heatDecayPerTick;
        HeatEmaAlpha = heatEmaAlpha;
    }

    public int ProgramId => KernelProgramCodes.MapActivity;

    public int SchemaVersion => 2;

    public float PresenceHeat { get; set; }

    public float HeatDecayPerTick { get; set; }

    public float HeatEmaAlpha { get; set; }
}
