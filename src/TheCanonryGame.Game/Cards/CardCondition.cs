namespace TheCanonryGame.Game.Cards;

internal readonly record struct CardEvaluationContext(int CurrentTick);

internal readonly record struct CardConditionResult(bool Passed, string Diagnostic)
{
    public static CardConditionResult Pass(string diagnostic = "") => new(true, diagnostic);

    public static CardConditionResult Fail(string diagnostic) => new(false, diagnostic);
}

internal interface ICardCondition
{
    CardConditionResult Evaluate(CardEvaluationContext context);

    string Describe();
}

internal sealed class AlwaysCardCondition : ICardCondition
{
    public static readonly AlwaysCardCondition Instance = new();

    private AlwaysCardCondition()
    {
    }

    public CardConditionResult Evaluate(CardEvaluationContext context) => CardConditionResult.Pass("always");

    public string Describe() => "always";
}
