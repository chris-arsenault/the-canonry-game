#pragma warning disable CA1303
#pragma warning disable CA2000
#pragma warning disable CA2213
using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

public partial class LegendPanel : PanelContainer
{
    public override void _Ready()
    {
        AssetLibrary.Load();
        MouseFilter = MouseFilterEnum.Ignore;
        AddThemeStyleboxOverride("panel", UiTheme.OverlayPanelStyle());

        var column = GetNode<VBoxContainer>("Column");
        column.AddThemeConstantOverride("separation", UiTokens.Space1);

        var title = GetNode<Label>("Column/TitleLabel");
        title.AddThemeFontOverride("font", UiTheme.BodyFont);
        title.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
        title.AddThemeColorOverride("font_color", UiTokens.Parchment);

        AddLegendRow(column, "penguin", UiTokens.KindPenguin, "penguin");
        AddLegendRow(column, "settlement", UiTokens.KindSettlement, "settlement");
        AddLegendRow(column, "faction", UiTokens.KindFaction, "faction");
        AddLegendRow(column, "artifact", UiTokens.KindArtifact, "artifact");
        AddLegendRow(column, "orca", UiTokens.KindOrca, "orca");
    }

    private static void AddLegendRow(VBoxContainer parent, string kindId, Color accent, string labelText)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", UiTokens.Space2);
        parent.AddChild(row);

        Control glyph;
        var sprite = AssetLibrary.KindMapIcon(kindId)
            ?? AssetLibrary.KindSprite(kindId);
        if (sprite is not null)
        {
            glyph = new TextureRect
            {
                Texture = sprite,
                CustomMinimumSize = new Vector2(22, 22),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = MouseFilterEnum.Ignore,
            };
        }
        else
        {
            glyph = new KindGlyphRect
            {
                KindId = kindId,
                Accent = accent,
                CustomMinimumSize = new Vector2(18, 18),
            };
        }

        row.AddChild(glyph);

        var label = new Label { Text = labelText };
        label.AddThemeFontOverride("font", UiTheme.BodyFont);
        label.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        label.AddThemeColorOverride("font_color", UiTokens.Parchment);
        row.AddChild(label);
    }
}
