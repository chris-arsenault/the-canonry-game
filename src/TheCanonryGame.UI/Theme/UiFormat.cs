namespace TheCanonryGame.UI.Theme;

using Godot;

public static class UiFormat
{
    public static string ColorHex(Color color)
    {
        var r = Mathf.RoundToInt(color.R * 255);
        var g = Mathf.RoundToInt(color.G * 255);
        var b = Mathf.RoundToInt(color.B * 255);
        return $"#{r:x2}{g:x2}{b:x2}";
    }
}
