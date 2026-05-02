namespace MonteCarlo.Tests.Observability;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class MonteCarloAcceptanceFactAttribute : FactAttribute
{
    public MonteCarloAcceptanceFactAttribute()
    {
        if (!MonteCarloAcceptanceTestSwitch.IsEnabled)
        {
            Skip = MonteCarloAcceptanceTestSwitch.SkipReason;
        }
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class MonteCarloAcceptanceTheoryAttribute : TheoryAttribute
{
    public MonteCarloAcceptanceTheoryAttribute()
    {
        if (!MonteCarloAcceptanceTestSwitch.IsEnabled)
        {
            Skip = MonteCarloAcceptanceTestSwitch.SkipReason;
        }
    }
}

internal static class MonteCarloAcceptanceTestSwitch
{
    internal const string EnvironmentVariable = "CANONRY_RUN_MONTE_CARLO_ACCEPTANCE";
    internal const string SkipReason =
        "Monte Carlo output-gap acceptance tests run the CLI for a long simulation; set " +
        EnvironmentVariable + "=1 to run them explicitly.";

    internal static bool IsEnabled =>
        string.Equals(
            Environment.GetEnvironmentVariable(EnvironmentVariable),
            "1",
            StringComparison.Ordinal);
}
