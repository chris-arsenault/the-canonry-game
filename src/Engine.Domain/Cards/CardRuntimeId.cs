namespace Engine.Cards;

/// <summary>
/// Stable primitive card identity used by runtime messages and decision links.
/// </summary>
public readonly record struct CardRuntimeId(int Value)
{
    public static readonly CardRuntimeId None = new(0);

    public bool IsNone => Value == 0;

    public static CardRuntimeId FromStableName(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        unchecked
        {
            const uint offset = 2166136261u;
            const uint prime = 16777619u;
            var hash = offset;
            foreach (var ch in value)
            {
                hash ^= ch;
                hash *= prime;
            }

            var id = (int)(hash & 0x7fffffffu);
            return new CardRuntimeId(id == 0 ? 1 : id);
        }
    }
}
