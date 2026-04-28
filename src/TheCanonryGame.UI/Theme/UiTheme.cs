#pragma warning disable CA2000 // StyleBoxes are owned by the Godot Theme / Control they are handed to
using Godot;

namespace TheCanonryGame.UI.Theme;

/// <summary>
/// Builds the shared Godot Theme resource that every UI Control inherits.
/// Applied once at the root of the UI layer in Main — cascades through the tree.
///
/// Also exposes helpers for building consistent StyleBoxes (panels, cards,
/// buttons, badges, status chips) so per-panel code reads from one place.
/// </summary>
public static class UiTheme
{
    public static readonly SystemFont DisplayFont = BuildFont(UiTokens.DisplayFontNames);
    public static readonly SystemFont BodyFont = BuildFont(UiTokens.BodyFontNames);
    public static readonly SystemFont MonoFont = BuildFont(UiTokens.MonoFontNames);

    /// <summary>
    /// Build the root Godot Theme. Assign once via Control.Theme on the UI root;
    /// all descendants inherit.
    /// </summary>
    public static Godot.Theme Build()
    {
        var theme = new Godot.Theme
        {
            DefaultFont = BodyFont,
            DefaultFontSize = UiTokens.SizeBody,
        };

        ConfigureLabelTheme(theme);
        ConfigureButtonTheme(theme);
        ConfigurePanelTheme(theme);
        ConfigureScrollAndSeparatorTheme(theme);
        ConfigureTextTheme(theme);
        ConfigureOptionAndPopupTheme(theme);
        ConfigureTooltipTheme(theme);
        return theme;
    }

    private static void ConfigureLabelTheme(Godot.Theme theme)
    {
        theme.SetColor("font_color", "Label", UiTokens.Vellum);
        theme.SetFont("font", "Label", BodyFont);
        theme.SetFontSize("font_size", "Label", UiTokens.SizeBody);
    }

    private static void ConfigureButtonTheme(Godot.Theme theme)
    {
        theme.SetColor("font_color", "Button", UiTokens.Vellum);
        theme.SetColor("font_hover_color", "Button", UiTokens.Vellum);
        theme.SetColor("font_pressed_color", "Button", UiTokens.Vellum);
        theme.SetColor("font_disabled_color", "Button", UiTokens.Charcoal);
        theme.SetFont("font", "Button", BodyFont);
        theme.SetFontSize("font_size", "Button", UiTokens.SizeBody);
        theme.SetStylebox("normal", "Button", ButtonStyle(ButtonState.Normal));
        theme.SetStylebox("hover", "Button", ButtonStyle(ButtonState.Hover));
        theme.SetStylebox("pressed", "Button", ButtonStyle(ButtonState.Pressed));
        theme.SetStylebox("focus", "Button", ButtonStyle(ButtonState.Focus));
        theme.SetStylebox("disabled", "Button", ButtonStyle(ButtonState.Disabled));

        theme.SetColor("font_color", "CheckBox", UiTokens.Vellum);
        theme.SetColor("font_hover_color", "CheckBox", UiTokens.Vellum);
        theme.SetColor("font_pressed_color", "CheckBox", UiTokens.Vellum);
        theme.SetFont("font", "CheckBox", BodyFont);
        theme.SetFontSize("font_size", "CheckBox", UiTokens.SizeBodySmall);
    }

    private static void ConfigurePanelTheme(Godot.Theme theme)
    {
        theme.SetStylebox("panel", "PanelContainer", PanelRootStyle());
        theme.SetStylebox("panel", "Panel", PanelRootStyle());
    }

    private static void ConfigureScrollAndSeparatorTheme(Godot.Theme theme)
    {
        theme.SetStylebox("scroll", "HScrollBar", SliderGrooveStyle());
        theme.SetStylebox("scroll_focus", "HScrollBar", SliderGrooveStyle());
        theme.SetStylebox("grabber", "HScrollBar", SliderGrabberStyle());
        theme.SetStylebox("grabber_highlight", "HScrollBar", SliderGrabberStyle(hover: true));
        theme.SetStylebox("grabber_pressed", "HScrollBar", SliderGrabberStyle(hover: true));

        theme.SetStylebox("scroll", "VScrollBar", SliderGrooveStyle());
        theme.SetStylebox("scroll_focus", "VScrollBar", SliderGrooveStyle());
        theme.SetStylebox("grabber", "VScrollBar", SliderGrabberStyle());
        theme.SetStylebox("grabber_highlight", "VScrollBar", SliderGrabberStyle(hover: true));
        theme.SetStylebox("grabber_pressed", "VScrollBar", SliderGrabberStyle(hover: true));

        // HSeparator / VSeparator — a single hairline.
        theme.SetStylebox("separator", "HSeparator", HairlineSeparator());
        theme.SetStylebox("separator", "VSeparator", HairlineSeparator());
        theme.SetConstant("separation", "HSeparator", 1);
        theme.SetConstant("separation", "VSeparator", 1);
    }

    private static void ConfigureTextTheme(Godot.Theme theme)
    {
        theme.SetColor("default_color", "RichTextLabel", UiTokens.Vellum);
        theme.SetFont("normal_font", "RichTextLabel", BodyFont);
        theme.SetFont("bold_font", "RichTextLabel", DisplayFont);
        theme.SetFont("italics_font", "RichTextLabel", BodyFont);
        theme.SetFont("mono_font", "RichTextLabel", MonoFont);
        theme.SetFontSize("normal_font_size", "RichTextLabel", UiTokens.SizeBody);
        theme.SetFontSize("bold_font_size", "RichTextLabel", UiTokens.SizeBody);
        theme.SetFontSize("italics_font_size", "RichTextLabel", UiTokens.SizeBody);
        theme.SetFontSize("mono_font_size", "RichTextLabel", UiTokens.SizeBody);

        // LineEdit / TextEdit (if used)
        theme.SetColor("font_color", "LineEdit", UiTokens.Vellum);
        theme.SetColor("font_placeholder_color", "LineEdit", UiTokens.Whisper);
        theme.SetFont("font", "LineEdit", BodyFont);
        theme.SetFontSize("font_size", "LineEdit", UiTokens.SizeBody);
        theme.SetStylebox("normal", "LineEdit", InputStyle());
        theme.SetStylebox("focus", "LineEdit", InputStyle(focus: true));
    }

    private static void ConfigureOptionAndPopupTheme(Godot.Theme theme)
    {
        theme.SetColor("font_color", "OptionButton", UiTokens.Vellum);
        theme.SetColor("font_hover_color", "OptionButton", UiTokens.Vellum);
        theme.SetColor("font_pressed_color", "OptionButton", UiTokens.Vellum);
        theme.SetFont("font", "OptionButton", BodyFont);
        theme.SetFontSize("font_size", "OptionButton", UiTokens.SizeBodySmall);
        theme.SetStylebox("normal", "OptionButton", ButtonStyle(ButtonState.Normal));
        theme.SetStylebox("hover", "OptionButton", ButtonStyle(ButtonState.Hover));
        theme.SetStylebox("pressed", "OptionButton", ButtonStyle(ButtonState.Pressed));
        theme.SetStylebox("focus", "OptionButton", ButtonStyle(ButtonState.Focus));

        // PopupMenu (OptionButton dropdown)
        theme.SetColor("font_color", "PopupMenu", UiTokens.Vellum);
        theme.SetColor("font_hover_color", "PopupMenu", UiTokens.Vellum);
        theme.SetColor("font_disabled_color", "PopupMenu", UiTokens.Charcoal);
        theme.SetFont("font", "PopupMenu", BodyFont);
        theme.SetFontSize("font_size", "PopupMenu", UiTokens.SizeBodySmall);
        theme.SetStylebox("panel", "PopupMenu", PanelRootStyle());
        theme.SetStylebox("hover", "PopupMenu", SurfaceStyle(UiTokens.Ice, UiTokens.RadiusSmall));
    }

    private static void ConfigureTooltipTheme(Godot.Theme theme)
    {
        theme.SetStylebox("panel", "TooltipPanel", TooltipStyle());
        theme.SetColor("font_color", "TooltipLabel", UiTokens.Vellum);
        theme.SetFont("font", "TooltipLabel", BodyFont);
        theme.SetFontSize("font_size", "TooltipLabel", UiTokens.SizeBodySmall);
    }

    // ---------- StyleBox factories (use these from any panel) ----------

    /// <summary>
    /// Style for full-width chrome bars (top bar, bottom bar). No corners, no radius —
    /// just a slate band with a single hairline rule on the edge facing the map.
    /// </summary>
    public static StyleBoxFlat ChromeBarStyle(bool bottom = false) => new()
    {
        BgColor = UiTokens.Slate,
        BorderColor = UiTokens.Hairline,
        BorderWidthTop = bottom ? 1 : 0,
        BorderWidthBottom = bottom ? 0 : 1,
        ContentMarginTop = UiTokens.Space2,
        ContentMarginBottom = UiTokens.Space2,
        ContentMarginLeft = UiTokens.Space4,
        ContentMarginRight = UiTokens.Space4,
    };

    /// <summary>
    /// Style for full-height rail panels (registry on the left, chronicle on the right).
    /// No corners — the panel edges abut the chrome bars to form a continuous frame.
    /// </summary>
    public static StyleBoxFlat RailPanelStyle(bool onLeft) => new()
    {
        BgColor = UiTokens.Slate,
        BorderColor = UiTokens.Hairline,
        BorderWidthLeft = onLeft ? 0 : 1,
        BorderWidthRight = onLeft ? 1 : 0,
        ContentMarginTop = UiTokens.Space3,
        ContentMarginBottom = UiTokens.Space3,
        ContentMarginLeft = UiTokens.Space3,
        ContentMarginRight = UiTokens.Space3,
    };

    /// <summary>
    /// A small floating overlay panel (map-area legend, small HUD widgets).
    /// Frost background with soft shadow, distinct from rail panels so it reads as "on top of the map."
    /// </summary>
    public static StyleBoxFlat OverlayPanelStyle() => new()
    {
        BgColor = UiTokens.WithAlpha(UiTokens.Frost, 0.92f),
        BorderColor = UiTokens.HairlineStrong,
        BorderWidthTop = 1,
        BorderWidthBottom = 1,
        BorderWidthLeft = 1,
        BorderWidthRight = 1,
        CornerRadiusTopLeft = UiTokens.RadiusSmall,
        CornerRadiusTopRight = UiTokens.RadiusSmall,
        CornerRadiusBottomLeft = UiTokens.RadiusSmall,
        CornerRadiusBottomRight = UiTokens.RadiusSmall,
        ShadowColor = UiTokens.CardShadow,
        ShadowSize = 6,
        ContentMarginTop = UiTokens.Space2,
        ContentMarginBottom = UiTokens.Space2,
        ContentMarginLeft = UiTokens.Space3,
        ContentMarginRight = UiTokens.Space3,
    };

    public static StyleBoxFlat PanelRootStyle() => new()
    {
        BgColor = UiTokens.Slate,
        BorderColor = UiTokens.Hairline,
        BorderWidthTop = 1,
        BorderWidthBottom = 1,
        BorderWidthLeft = 1,
        BorderWidthRight = 1,
        CornerRadiusTopLeft = UiTokens.RadiusLarge,
        CornerRadiusTopRight = UiTokens.RadiusLarge,
        CornerRadiusBottomLeft = UiTokens.RadiusLarge,
        CornerRadiusBottomRight = UiTokens.RadiusLarge,
        ContentMarginTop = UiTokens.Space4,
        ContentMarginBottom = UiTokens.Space4,
        ContentMarginLeft = UiTokens.Space4,
        ContentMarginRight = UiTokens.Space4,
    };

    public static StyleBoxFlat FloatingPanelStyle() => new()
    {
        BgColor = UiTokens.Slate,
        BorderColor = UiTokens.HairlineStrong,
        BorderWidthTop = 1,
        BorderWidthBottom = 1,
        BorderWidthLeft = 1,
        BorderWidthRight = 1,
        CornerRadiusTopLeft = UiTokens.RadiusLarge,
        CornerRadiusTopRight = UiTokens.RadiusLarge,
        CornerRadiusBottomLeft = UiTokens.RadiusLarge,
        CornerRadiusBottomRight = UiTokens.RadiusLarge,
        ShadowColor = UiTokens.CardShadow,
        ShadowSize = 12,
        ContentMarginTop = UiTokens.Space3,
        ContentMarginBottom = UiTokens.Space3,
        ContentMarginLeft = UiTokens.Space3,
        ContentMarginRight = UiTokens.Space3,
    };

    public static StyleBoxFlat CardStyle(Color accent, bool selected = false, bool highlighted = false)
    {
        Color border;
        if (highlighted)
        {
            border = UiTokens.Ember;
        }
        else if (selected)
        {
            border = UiTokens.WithAlpha(accent, 0.80f);
        }
        else
        {
            border = UiTokens.WithAlpha(accent, 0.45f);
        }

        return new StyleBoxFlat
        {
            BgColor = UiTokens.Frost,
            BorderColor = border,
            BorderWidthTop = highlighted ? 2 : 1,
            BorderWidthBottom = highlighted ? 2 : 1,
            BorderWidthLeft = highlighted ? 2 : 1,
            BorderWidthRight = highlighted ? 2 : 1,
            CornerRadiusTopLeft = UiTokens.RadiusMedium,
            CornerRadiusTopRight = UiTokens.RadiusMedium,
            CornerRadiusBottomLeft = UiTokens.RadiusMedium,
            CornerRadiusBottomRight = UiTokens.RadiusMedium,
            ShadowColor = UiTokens.CardShadow,
            ShadowSize = highlighted ? 10 : 6,
            ContentMarginTop = UiTokens.Space3,
            ContentMarginBottom = UiTokens.Space3,
            ContentMarginLeft = UiTokens.Space3,
            ContentMarginRight = UiTokens.Space3,
        };
    }

    public static StyleBoxFlat InsetStyle() => new()
    {
        BgColor = UiTokens.Basalt,
        BorderColor = UiTokens.Hairline,
        BorderWidthTop = 1,
        BorderWidthBottom = 1,
        BorderWidthLeft = 1,
        BorderWidthRight = 1,
        CornerRadiusTopLeft = UiTokens.RadiusSmall,
        CornerRadiusTopRight = UiTokens.RadiusSmall,
        CornerRadiusBottomLeft = UiTokens.RadiusSmall,
        CornerRadiusBottomRight = UiTokens.RadiusSmall,
        ContentMarginTop = UiTokens.Space2,
        ContentMarginBottom = UiTokens.Space2,
        ContentMarginLeft = UiTokens.Space2,
        ContentMarginRight = UiTokens.Space2,
    };

    public static StyleBoxFlat HeaderStripStyle(Color accent) => new()
    {
        BgColor = UiTokens.WithAlpha(accent, 0.12f),
        BorderColor = UiTokens.WithAlpha(accent, 0.40f),
        BorderWidthBottom = 1,
        CornerRadiusTopLeft = UiTokens.RadiusSmall,
        CornerRadiusTopRight = UiTokens.RadiusSmall,
        ContentMarginTop = UiTokens.Space2,
        ContentMarginBottom = UiTokens.Space2,
        ContentMarginLeft = UiTokens.Space3,
        ContentMarginRight = UiTokens.Space3,
    };

    public static StyleBoxFlat BadgeStyle(Color accent) => new()
    {
        BgColor = UiTokens.WithAlpha(accent, 0.18f),
        BorderColor = UiTokens.WithAlpha(accent, 0.55f),
        BorderWidthTop = 1,
        BorderWidthBottom = 1,
        BorderWidthLeft = 1,
        BorderWidthRight = 1,
        CornerRadiusTopLeft = UiTokens.RadiusSmall,
        CornerRadiusTopRight = UiTokens.RadiusSmall,
        CornerRadiusBottomLeft = UiTokens.RadiusSmall,
        CornerRadiusBottomRight = UiTokens.RadiusSmall,
        ContentMarginTop = 3,
        ContentMarginBottom = 3,
        ContentMarginLeft = UiTokens.Space2,
        ContentMarginRight = UiTokens.Space2,
    };

    public enum ChipTone
    {
        Neutral,
        Active,
        Warning,
        Critical,
    }

    public static StyleBoxFlat ChipStyle(ChipTone tone)
    {
        var color = tone switch
        {
            ChipTone.Active => UiTokens.Aurora,
            ChipTone.Warning => UiTokens.Ember,
            ChipTone.Critical => UiTokens.Blood,
            _ => UiTokens.Whisper,
        };
        return new StyleBoxFlat
        {
            BgColor = UiTokens.WithAlpha(color, 0.22f),
            BorderColor = UiTokens.WithAlpha(color, 0.55f),
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            CornerRadiusTopLeft = UiTokens.RadiusSmall,
            CornerRadiusTopRight = UiTokens.RadiusSmall,
            CornerRadiusBottomLeft = UiTokens.RadiusSmall,
            CornerRadiusBottomRight = UiTokens.RadiusSmall,
            ContentMarginTop = 3,
            ContentMarginBottom = 3,
            ContentMarginLeft = UiTokens.Space2,
            ContentMarginRight = UiTokens.Space2,
        };
    }

    public static Color ChipTextColor(ChipTone tone) => tone switch
    {
        ChipTone.Active => UiTokens.Aurora,
        ChipTone.Warning => UiTokens.Ember,
        ChipTone.Critical => UiTokens.Blood,
        _ => UiTokens.Parchment,
    };

    // ---------- Internals ----------

    private enum ButtonState { Normal, Hover, Pressed, Focus, Disabled }

    private static StyleBoxFlat ButtonStyle(ButtonState state)
    {
        var bg = state switch
        {
            ButtonState.Hover => UiTokens.Ice,
            ButtonState.Pressed => UiTokens.Ice,
            ButtonState.Focus => UiTokens.Frost,
            ButtonState.Disabled => UiTokens.WithAlpha(UiTokens.Frost, 0.55f),
            _ => UiTokens.Frost,
        };
        var border = state switch
        {
            ButtonState.Pressed => UiTokens.Ember,
            ButtonState.Focus => UiTokens.HairlineStrong,
            _ => UiTokens.Hairline,
        };
        return new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = border,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            CornerRadiusTopLeft = UiTokens.RadiusSmall,
            CornerRadiusTopRight = UiTokens.RadiusSmall,
            CornerRadiusBottomLeft = UiTokens.RadiusSmall,
            CornerRadiusBottomRight = UiTokens.RadiusSmall,
            ContentMarginTop = UiTokens.Space2,
            ContentMarginBottom = UiTokens.Space2,
            ContentMarginLeft = UiTokens.Space3,
            ContentMarginRight = UiTokens.Space3,
        };
    }

    private static StyleBoxFlat InputStyle(bool focus = false) => new()
    {
        BgColor = UiTokens.Basalt,
        BorderColor = focus ? UiTokens.Ember : UiTokens.Hairline,
        BorderWidthTop = 1,
        BorderWidthBottom = 1,
        BorderWidthLeft = 1,
        BorderWidthRight = 1,
        CornerRadiusTopLeft = UiTokens.RadiusSmall,
        CornerRadiusTopRight = UiTokens.RadiusSmall,
        CornerRadiusBottomLeft = UiTokens.RadiusSmall,
        CornerRadiusBottomRight = UiTokens.RadiusSmall,
        ContentMarginTop = UiTokens.Space2,
        ContentMarginBottom = UiTokens.Space2,
        ContentMarginLeft = UiTokens.Space3,
        ContentMarginRight = UiTokens.Space3,
    };

    private static StyleBoxFlat SurfaceStyle(Color bg, int radius) => new()
    {
        BgColor = bg,
        CornerRadiusTopLeft = radius,
        CornerRadiusTopRight = radius,
        CornerRadiusBottomLeft = radius,
        CornerRadiusBottomRight = radius,
        ContentMarginTop = UiTokens.Space2,
        ContentMarginBottom = UiTokens.Space2,
        ContentMarginLeft = UiTokens.Space3,
        ContentMarginRight = UiTokens.Space3,
    };

    private static StyleBoxFlat SliderGrooveStyle() => new()
    {
        BgColor = UiTokens.Basalt,
        CornerRadiusTopLeft = UiTokens.RadiusSmall,
        CornerRadiusTopRight = UiTokens.RadiusSmall,
        CornerRadiusBottomLeft = UiTokens.RadiusSmall,
        CornerRadiusBottomRight = UiTokens.RadiusSmall,
    };

    private static StyleBoxFlat SliderGrabberStyle(bool hover = false) => new()
    {
        BgColor = hover ? UiTokens.Parchment : UiTokens.Whisper,
        CornerRadiusTopLeft = UiTokens.RadiusSmall,
        CornerRadiusTopRight = UiTokens.RadiusSmall,
        CornerRadiusBottomLeft = UiTokens.RadiusSmall,
        CornerRadiusBottomRight = UiTokens.RadiusSmall,
    };

    private static StyleBoxFlat HairlineSeparator() => new()
    {
        BgColor = UiTokens.Hairline,
        ContentMarginTop = 0,
        ContentMarginBottom = 0,
        ContentMarginLeft = 0,
        ContentMarginRight = 0,
    };

    private static StyleBoxFlat TooltipStyle() => new()
    {
        BgColor = UiTokens.Basalt,
        BorderColor = UiTokens.HairlineStrong,
        BorderWidthTop = 1,
        BorderWidthBottom = 1,
        BorderWidthLeft = 1,
        BorderWidthRight = 1,
        CornerRadiusTopLeft = UiTokens.RadiusSmall,
        CornerRadiusTopRight = UiTokens.RadiusSmall,
        CornerRadiusBottomLeft = UiTokens.RadiusSmall,
        CornerRadiusBottomRight = UiTokens.RadiusSmall,
        ShadowColor = UiTokens.CardShadow,
        ShadowSize = 8,
        ContentMarginTop = UiTokens.Space2,
        ContentMarginBottom = UiTokens.Space2,
        ContentMarginLeft = UiTokens.Space3,
        ContentMarginRight = UiTokens.Space3,
    };

    private static SystemFont BuildFont(string[] fontNames)
    {
        var font = new SystemFont();
        var names = new string[fontNames.Length];
        System.Array.Copy(fontNames, names, fontNames.Length);
        font.FontNames = names;
        font.Antialiasing = TextServer.FontAntialiasing.Gray;
        font.SubpixelPositioning = TextServer.SubpixelPositioning.Auto;
        font.Hinting = TextServer.Hinting.Light;
        return font;
    }
}
