#pragma warning disable CA2000 // Godot nodes managed by scene tree
#pragma warning disable CA1305 // UI display strings
using System.Text;

using Godot;

using Engine.Projection;
using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

#pragma warning disable CA2213 // Godot nodes managed by scene tree
public partial class AttributeGraphPanel : PanelContainer
{
    private UiProjectionState? _projection;
    private IProjectionSymbolCatalog? _symbols;
    private Label? _titleLabel;
    private RichTextLabel? _content;

    public void Initialize(UiProjectionState? projection, IProjectionSymbolCatalog? symbols)
    {
        _projection = projection;
        _symbols = symbols;
        Refresh();
    }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(580, 460);
        AddThemeStyleboxOverride("panel", UiTheme.FloatingPanelStyle());

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", UiTokens.Space2);
        AddChild(vbox);

        var titleBar = new HBoxContainer();
        titleBar.AddThemeConstantOverride("separation", UiTokens.Space2);
        vbox.AddChild(titleBar);

        _titleLabel = new Label
        {
            Text = "Readings",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _titleLabel.AddThemeFontOverride("font", UiTheme.DisplayFont);
        _titleLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeTitleMedium);
        _titleLabel.AddThemeColorOverride("font_color", UiTokens.Vellum);
        titleBar.AddChild(_titleLabel);

        var closeBtn = new Button
        {
            Text = "x",
            CustomMinimumSize = new Vector2(26, 26),
            TooltipText = "Close",
        };
        closeBtn.Pressed += () => QueueFree();
        titleBar.AddChild(closeBtn);

        vbox.AddChild(new ColorRect
        {
            Color = UiTokens.Hairline,
            CustomMinimumSize = new Vector2(0, 1),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        });

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        vbox.AddChild(scroll);

        _content = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            ScrollActive = false,
        };
        _content.AddThemeFontOverride("normal_font", UiTheme.BodyFont);
        _content.AddThemeFontOverride("mono_font", UiTheme.MonoFont);
        _content.AddThemeFontSizeOverride("normal_font_size", UiTokens.SizeBodySmall);
        _content.AddThemeFontSizeOverride("mono_font_size", UiTokens.SizeBodySmall);
        _content.AddThemeColorOverride("default_color", UiTokens.Vellum);
        scroll.AddChild(_content);

        Refresh();
    }

    public void Refresh()
    {
        if (_projection is null || _content is null || _titleLabel is null)
        {
            return;
        }

        _titleLabel.Text = $"Readings  ·  t{_projection.CurrentTick}";

        var whisper = UiFormat.ColorHex(UiTokens.Whisper);
        var parchment = UiFormat.ColorHex(UiTokens.Parchment);
        var sb = new StringBuilder();
        foreach (var entity in _projection.Entities.Values
            .Where(entity => entity.StatusCode == UiProjectionStatusCodes.Active)
            .OrderBy(entity => ResolveEntityName(entity), StringComparer.Ordinal)
            .Take(24))
        {
            sb.AppendLine($"[color={parchment}]{ResolveEntityName(entity)}[/color]");
            if (entity.Attributes.Count == 0)
            {
                sb.AppendLine($"  [color={whisper}]no attribute events[/color]");
                continue;
            }

            foreach (var attribute in entity.Attributes.Values
                .OrderBy(attribute => ResolveAttributeName(attribute.AttributeId), StringComparer.Ordinal))
            {
                sb.AppendLine($"  {ResolveAttributeName(attribute.AttributeId),-16} [code]{attribute.NewValue,7:0.0}[/code]");
            }
        }

        _content.Text = sb.ToString();
    }

    private string ResolveEntityName(UiEntityProjection entity) =>
        _symbols?.ResolveEntityName(
            new ProjectionEntityNameKey(
                entity.EntityId,
                entity.KindId,
                entity.SubtypeId,
                entity.CultureId,
                entity.NameSeed),
            $"E{entity.EntityId}") ?? $"E{entity.EntityId}";

    private string ResolveAttributeName(int attributeId) =>
        _symbols?.ResolveDisplayName(
            ProjectionSymbolKind.Attribute,
            attributeId,
            $"Attribute {attributeId}") ?? $"Attribute {attributeId}";
}
