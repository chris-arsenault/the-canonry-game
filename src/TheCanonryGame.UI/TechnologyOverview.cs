#pragma warning disable CA1303
#pragma warning disable CA2000
#pragma warning disable CA2213
using Godot;

using TheCanonryGame.Game.Runtime;
using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

/// <summary>
/// Floating culture-by-culture technology ledger. The left pane selects a
/// culture; the right pane shows its live claims and effective grant totals.
/// </summary>
public partial class TechnologyOverview : PanelContainer
{
    [Signal]
    public delegate void CloseRequestedEventHandler();

    private const string DimColor = "6F7684";
    private const string ConnectorColor = "B8B5A6";
    private const string BonusColor = "78C9B2";
    private const string NarrativeColor = "D8D4C4";

    private IReadOnlyList<GameCultureTechnologyOverview> _cultures = [];
    private VBoxContainer? _navList;
    private RichTextLabel? _detail;
    private Label? _headerLabel;
    private int _selectedCultureId;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(920, 560);
        AddThemeStyleboxOverride("panel", UiTheme.FloatingPanelStyle());

        var outer = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        outer.AddThemeConstantOverride("separation", UiTokens.Space2);
        AddChild(outer);

        var headerRow = new HBoxContainer();
        _headerLabel = new Label
        {
            Text = "Technology Overview",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _headerLabel.AddThemeFontOverride("font", UiTheme.DisplayFont);
        _headerLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeTitleMedium);
        _headerLabel.AddThemeColorOverride("font_color", UiTokens.Vellum);
        headerRow.AddChild(_headerLabel);

        var closeButton = new Button { Text = "✕", CustomMinimumSize = new Vector2(28, 28) };
        closeButton.Pressed += () => EmitSignal(SignalName.CloseRequested);
        headerRow.AddChild(closeButton);
        outer.AddChild(headerRow);

        outer.AddChild(new ColorRect
        {
            CustomMinimumSize = new Vector2(0, 1),
            Color = UiTokens.Hairline,
        });
        outer.AddChild(BuildSplit());
        RebuildNav();
    }

    internal void SetOverview(GameTechnologyOverview overview)
    {
        ArgumentNullException.ThrowIfNull(overview);
        _cultures = overview.Cultures;
        if ((_selectedCultureId <= 0 || !_cultures.Any(CultureIsSelected)) && _cultures.Count > 0)
        {
            _selectedCultureId = _cultures[0].CultureId;
        }

        RebuildNav();
        RenderDetail();
    }

    private HSplitContainer BuildSplit()
    {
        var split = new HSplitContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };

        var navScroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(300, 0),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        _navList = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _navList.AddThemeConstantOverride("separation", UiTokens.Space1);
        navScroll.AddChild(_navList);
        split.AddChild(navScroll);

        var detailScroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        _detail = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SelectionEnabled = true,
        };
        _detail.AddThemeFontOverride("normal_font", UiTheme.BodyFont);
        _detail.AddThemeFontOverride("bold_font", UiTheme.DisplayFont);
        _detail.AddThemeFontOverride("mono_font", UiTheme.MonoFont);
        _detail.AddThemeFontSizeOverride("normal_font_size", UiTokens.SizeBodySmall);
        _detail.AddThemeFontSizeOverride("bold_font_size", UiTokens.SizeTitleSmall);
        _detail.AddThemeFontSizeOverride("mono_font_size", UiTokens.SizeBodySmall);
        _detail.AddThemeColorOverride("default_color", UiTokens.Vellum);
        detailScroll.AddChild(_detail);
        split.AddChild(detailScroll);
        return split;
    }

    private void RebuildNav()
    {
        if (_navList is null)
        {
            return;
        }

        foreach (var child in _navList.GetChildren())
        {
            child.QueueFree();
        }

        if (_headerLabel is not null)
        {
            _headerLabel.Text = $"Technology Overview  —  {_cultures.Count} cultures";
        }

        foreach (var culture in _cultures)
        {
            var cultureId = culture.CultureId;
            var button = new Button
            {
                Text = $"{culture.DisplayName}  ({culture.ClaimedTechs})",
                Alignment = HorizontalAlignment.Left,
                ClipText = true,
                ToggleMode = true,
                ButtonPressed = cultureId == _selectedCultureId,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                TooltipText = $"{culture.ClaimedTechs} controlled technologies",
            };
            button.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
            button.AddThemeColorOverride("font_color", UiTokens.CultureColor(culture.StableName));
            button.Pressed += () =>
            {
                _selectedCultureId = cultureId;
                RebuildNav();
                RenderDetail();
            };
            _navList.AddChild(button);
        }
    }

    private void RenderDetail()
    {
        if (_detail is null)
        {
            return;
        }

        var culture = _cultures.FirstOrDefault(CultureIsSelected);
        if (culture is null)
        {
            _detail.Text = $"[color=#{DimColor}]No culture knowledge is available yet.[/color]";
            return;
        }

        _detail.Text = string.Join('\n', DetailLines(culture));
    }

    private static List<string> DetailLines(GameCultureTechnologyOverview culture)
    {
        var accent = UiTokens.CultureColor(culture.StableName).ToHtml(false);
        var tierText = culture.MaxTier >= 0 ? $"highest tier {culture.MaxTier}" : "no tier reached";
        var lines = new List<string>
        {
            $"[b][color=#{accent}]{culture.DisplayName}[/color][/b]",
            $"[color=#{DimColor}]{culture.ClaimedTechs} controlled technologies · {tierText}[/color]",
            string.Empty,
            $"[color=#{ConnectorColor}]Total effective bonuses[/color]",
        };

        if (culture.Bonuses.Count == 0)
        {
            lines.Add($"    [color=#{DimColor}]No technology grants are active.[/color]");
        }
        else
        {
            foreach (var bonus in culture.Bonuses)
            {
                lines.Add(
                    $"    [color=#{NarrativeColor}]{bonus.Label}[/color]  " +
                    $"[color=#{BonusColor}][font_size={UiTokens.SizeBodySmall}]×{bonus.Multiplier:0.###}[/font_size][/color]");
            }
        }

        lines.Add(string.Empty);
        lines.Add($"[color=#{ConnectorColor}]Controlled technologies[/color]");
        if (culture.Technologies.Count == 0)
        {
            lines.Add($"    [color=#{DimColor}]Its semantic field does not currently hold a technology site.[/color]");
            return lines;
        }

        foreach (var technology in culture.Technologies)
        {
            lines.Add(string.Empty);
            lines.Add($"[b]{technology.Name}[/b]");
            lines.Add($"[color=#{DimColor}]Tier {technology.Tier} · {technology.Discipline}[/color]");
            lines.Add($"    [color=#{NarrativeColor}]{technology.GrantDescription}[/color]");
        }

        return lines;
    }

    private bool CultureIsSelected(GameCultureTechnologyOverview culture) =>
        culture.CultureId == _selectedCultureId;
}
