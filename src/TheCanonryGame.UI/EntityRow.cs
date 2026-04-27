#pragma warning disable CA1303
#pragma warning disable CA1305
#pragma warning disable CA2000
#pragma warning disable CA2213
using Godot;

using Engine.Projection;
using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

public partial class EntityRow : VBoxContainer
{
    private Control? _glyphSlot;
    private Button? _button;
    private Label? _subtitleLabel;
    private string? _glyphKey;
    private int _entityId;

    [Signal]
    public delegate void EntitySelectedEventHandler(int entityIdValue);

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 0);
        GetNode<HBoxContainer>("Row").AddThemeConstantOverride("separation", UiTokens.Space1);

        _glyphSlot = GetNode<Control>("Row/GlyphSlot");
        _button = GetNode<Button>("Row/EntityButton");
        _subtitleLabel = GetNode<Label>("SubtitleLabel");

        _button.AddThemeFontOverride("font", UiTheme.BodyFont);
        _button.AddThemeFontSizeOverride("font_size", UiTokens.SizeBody);
        _button.AddThemeColorOverride("font_hover_color", UiTokens.Vellum);
        _button.Pressed += () => EmitSignal(SignalName.EntitySelected, _entityId);

        _subtitleLabel.AddThemeFontOverride("font", UiTheme.BodyFont);
        _subtitleLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        _subtitleLabel.AddThemeColorOverride("font_color", UiTokens.Whisper);
    }

    public void BindTo(UiEntityProjection entity, IProjectionSymbolCatalog? symbols, int currentTick)
    {
        ArgumentNullException.ThrowIfNull(entity);

        _entityId = entity.EntityId;
        var isHistorical = entity.StatusCode != EntityStatusCodes.Active;
        var kindStableName = ProjectionDisplay.StableName(symbols, ProjectionSymbolKind.Kind, entity.KindId, $"kind-{entity.KindId}");
        var subtypeStableName = ProjectionDisplay.NullableStableName(symbols, ProjectionSymbolKind.Subtype, entity.SubtypeId);
        var entityName = ProjectionDisplay.EntityName(symbols, entity);

        BindGlyph(kindStableName, subtypeStableName, isHistorical);

        if (_button is not null)
        {
            _button.Text = isHistorical ? $" {entityName}  historical" : $" {entityName}";
            _button.AddThemeColorOverride("font_color", isHistorical ? UiTokens.Whisper : UiTokens.Vellum);
        }

        if (_subtitleLabel is not null)
        {
            var subtitle = FormatLastActionSubtitle(entity, symbols, currentTick);
            _subtitleLabel.Text = subtitle ?? string.Empty;
            _subtitleLabel.Visible = subtitle is not null;
        }
    }

    private void BindGlyph(string kindStableName, string? subtypeStableName, bool isHistorical)
    {
        if (_glyphSlot is null)
        {
            return;
        }

        var key = $"{kindStableName}:{subtypeStableName}:{isHistorical}";
        if (key == _glyphKey)
        {
            return;
        }

        foreach (var child in _glyphSlot.GetChildren())
        {
            child.QueueFree();
        }

        Control glyph;
        var spriteTex = AssetLibrary.SubtypeMapIcon(kindStableName, subtypeStableName)
            ?? AssetLibrary.KindSprite(kindStableName);
        if (spriteTex is not null)
        {
            glyph = new TextureRect
            {
                Texture = spriteTex,
                CustomMinimumSize = new Vector2(22, 24),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                Modulate = isHistorical ? UiTokens.WithAlpha(Colors.White, 0.45f) : Colors.White,
                MouseFilter = MouseFilterEnum.Ignore,
            };
        }
        else
        {
            var accent = UiTokens.KindAccent(kindStableName);
            glyph = new KindGlyphRect
            {
                KindId = kindStableName,
                Accent = isHistorical ? UiTokens.Whisper : accent,
                CustomMinimumSize = new Vector2(18, 20),
            };
        }

        _glyphSlot.AddChild(glyph);
        _glyphKey = key;
    }

    private static string? FormatLastActionSubtitle(
        UiEntityProjection entity,
        IProjectionSymbolCatalog? symbols,
        int currentTick)
    {
        var last = entity.LastProcessExecution;
        if (last is null)
        {
            return null;
        }

        var processName = ProjectionDisplay.DisplayName(
            symbols,
            ProjectionSymbolKind.Process,
            last.ProcessId,
            $"Process {last.ProcessId}");
        var age = System.Math.Max(0, currentTick - last.Tick);
        var ageText = age == 0 ? "now" : $"{age}t ago";
        return $"    {processName} | {ageText}";
    }
}
