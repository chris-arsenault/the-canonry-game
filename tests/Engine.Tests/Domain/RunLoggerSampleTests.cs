namespace Engine.Tests.Domain;

using System.IO;
using Engine.Observability;
using Engine.Simulation;
using Xunit.Abstractions;

/// <summary>
/// Generates a sample run log under <c>.artifacts/logs/sample-*.log</c> so the
/// format can be inspected manually. Runs long enough to include generator
/// firings (150 ticks) and is safe to keep in CI: the file just accumulates
/// artifacts we can delete anytime.
/// </summary>
public class RunLoggerSampleTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Sample_Run_WritesLogForInspection()
    {
        var logPath = RunLogger.DefaultLogPath();
        var sim = SimulationRunner.CreateFromTestMap();

        using (var logger = new RunLogger(logPath, sim))
        {
            for (var i = 0; i < 150; i++)
            {
                sim.Tick();
                logger.OnTickEnded();
            }
        }

        var lines = File.ReadAllLines(logPath);
        _output.WriteLine($"sample log: {Path.GetFullPath(logPath)}");
        Assert.True(File.Exists(logPath));
        Assert.Contains(lines, line => line.Contains("==  gen:", System.StringComparison.Ordinal));
    }
}
