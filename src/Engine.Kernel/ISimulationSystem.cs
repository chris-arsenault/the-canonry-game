namespace Engine.Kernel;

public interface ISimulationSystem
{
    void Tick(Simulation simulation, int tick);
}
