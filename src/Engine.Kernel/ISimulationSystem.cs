namespace Engine.Kernel;

internal interface ISimulationSystem
{
    void Tick(Simulation simulation, int tick);
}
