#pragma warning disable CA1303
#pragma warning disable CA2000
#pragma warning disable CA2213
using Engine.Projection;

using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

/// <summary>
/// The chronicle journal: a floating window with the world's tales. Left
/// nav lists chronicle titles (unfolding first, then endings); the right
/// pane narrates the selected chronicle - cast, act, beats, and outcome -
/// with every cast name linking to its entity.
/// </summary>
public partial class ChronicleJournal : PanelContainer
{
    [Signal]
    public delegate void EntitySelectedEventHandler(int entityIdValue);

    [Signal]
    public delegate void CloseRequestedEventHandler();

    private const string TaleColor = "C9A2E0";
    private const string DimColor = "6F7684";
    private const string ConnectorColor = "B8B5A6";
    private const string EntityColor = "9BC4E8";
    private const string NarrativeColor = "D8D4C4";

    private readonly List<UiChronicleTaleView> _views = [];
    private VBoxContainer? _navList;
    private RichTextLabel? _detail;
    private Label? _headerLabel;
    private int _selectedSequence = -1;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(920, 560);
        AddThemeStyleboxOverride("panel", UiTheme.FloatingPanelStyle());

        var outer = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        outer.AddThemeConstantOverride("separation", UiTokens.Space2);
        AddChild(outer);

        var headerRow = new HBoxContainer();
        _headerLabel = new Label { Text = "Chronicle Journal", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _headerLabel.AddThemeFontOverride("font", UiTheme.DisplayFont);
        _headerLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeTitleMedium);
        _headerLabel.AddThemeColorOverride("font_color", UiTokens.Vellum);
        headerRow.AddChild(_headerLabel);
        var closeButton = new Button { Text = "✕", CustomMinimumSize = new Vector2(28, 28) };
        closeButton.Pressed += () => EmitSignal(SignalName.CloseRequested);
        headerRow.AddChild(closeButton);
        outer.AddChild(headerRow);

        var hairline = new ColorRect { CustomMinimumSize = new Vector2(0, 1), Color = UiTokens.Hairline };
        outer.AddChild(hairline);

        outer.AddChild(BuildSplit());
        RebuildNav();
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
        _detail.AddThemeFontSizeOverride("normal_font_size", UiTokens.SizeBodySmall);
        _detail.AddThemeFontSizeOverride("bold_font_size", UiTokens.SizeTitleSmall);
        _detail.AddThemeColorOverride("default_color", UiTokens.Vellum);
        _detail.MetaClicked += OnDetailMetaClicked;
        detailScroll.AddChild(_detail);
        split.AddChild(detailScroll);
        return split;
    }

    public void SetTaleViews(IReadOnlyList<UiChronicleTaleView> views)
    {
        ArgumentNullException.ThrowIfNull(views);
        _views.Clear();
        _views.AddRange(views);
        if (_selectedSequence < 0 && _views.Count > 0)
        {
            _selectedSequence = _views[0].Tale.Sequence;
        }

        RebuildNav();
        RenderDetail();
    }

    private void OnDetailMetaClicked(Variant meta)
    {
        if (int.TryParse(meta.AsString(), System.Globalization.CultureInfo.InvariantCulture, out var entityId) && entityId > 0)
        {
            EmitSignal(SignalName.EntitySelected, entityId);
        }
    }

    /// <summary>The interest measure: heat, named for the telling.</summary>
    internal static string HeatBand(float heat) => heat switch
    {
        < 1.5f => "cold embers",
        < 3.5f => "murmuring",
        < 5.5f => "burning",
        < 8f => "roaring",
        _ => "legendary",
    };

    internal static string HeatGlyphs(float heat) => heat switch
    {
        < 1.5f => "·",
        < 3.5f => "✦",
        < 5.5f => "✦✦",
        < 8f => "✦✦✦",
        _ => "✦✦✦✦",
    };

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

        var unfolding = _views.Where(static view => !view.Tale.Resolved)
            .OrderByDescending(static view => view.Tale.Heat)
            .ToList();
        var ended = _views.Where(static view => view.Tale.Resolved).ToList();
        if (_headerLabel is not null)
        {
            _headerLabel.Text = $"Chronicle Journal  —  {unfolding.Count} unfolding";
        }

        AddNavSection("Unfolding", unfolding);
        AddNavSection("Endings", ended);
    }

    private void AddNavSection(string header, List<UiChronicleTaleView> views)
    {
        if (_navList is null || views.Count == 0)
        {
            return;
        }

        var label = new Label { Text = header };
        label.AddThemeFontOverride("font", UiTheme.BodyFont);
        label.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
        label.AddThemeColorOverride("font_color", UiTokens.Whisper);
        _navList.AddChild(label);

        foreach (var view in views)
        {
            var sequence = view.Tale.Sequence;
            var button = new Button
            {
                Text = view.Tale.Resolved ? view.Title : $"{HeatGlyphs(view.Tale.Heat)} {view.Title}",
                Alignment = HorizontalAlignment.Left,
                ClipText = true,
                ToggleMode = true,
                ButtonPressed = sequence == _selectedSequence,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                TooltipText = view.Tale.ArchetypeName,
            };
            button.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
            button.Pressed += () =>
            {
                _selectedSequence = sequence;
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

        var view = _views.Find(candidate => candidate.Tale.Sequence == _selectedSequence);
        if (view is null)
        {
            _detail.Text = $"[color=#{DimColor}]Select a chronicle. The ice is watching.[/color]";
            return;
        }

        _detail.Text = string.Join('\n', DetailLines(view));
    }

    private static List<string> DetailLines(UiChronicleTaleView view)
    {
        var tale = view.Tale;
        var years = (tale.LastEventTick - tale.StartTick) / 50f;
        var lines = new List<string>
        {
            $"[b][color=#{TaleColor}]{view.Title}[/color][/b]",
            tale.Resolved
                ? $"[color=#{DimColor}]{tale.ArchetypeName} · ended after {years:0.#} years · {tale.StruckBeats} beats struck[/color]"
                : $"[color=#{DimColor}]{tale.ArchetypeName} · {years:0.#} years and unfolding · " +
                  $"[/color][color=#{TaleColor}]{HeatGlyphs(tale.Heat)} {HeatBand(tale.Heat)}[/color]",
            string.Empty,
            $"[color=#{ConnectorColor}]The cast[/color]",
        };
        foreach (var member in tale.Cast)
        {
            lines.Add($"    [url={member.EntityId}][color=#{EntityColor}]{member.DisplayName}[/color][/url]");
        }

        lines.Add(string.Empty);
        lines.Add($"[color=#{ConnectorColor}]The telling[/color]");
        AppendTimeline(lines, view);

        if (tale.Resolved && view.OutcomeNarrative.Length > 0)
        {
            lines.Add(string.Empty);
            lines.Add($"[color=#{ConnectorColor}]How it ended[/color]");
            lines.Add($"    [i][color=#{NarrativeColor}]{view.OutcomeNarrative}[/color][/i]");
        }

        return lines;
    }

    /// <summary>
    /// One chronological timeline: struck beats carry the telling; the raw
    /// record events that surround each beat sit indented and dimmed beneath
    /// it, so cause and telling read together instead of as two lists.
    /// </summary>
    private static void AppendTimeline(List<string> lines, UiChronicleTaleView view)
    {
        if (view.Beats.Count == 0)
        {
            lines.Add($"    [color=#{DimColor}]The ice holds its breath - nothing yet worth telling.[/color]");
            return;
        }

        var ordered = view.Beats
            .OrderBy(static entry => entry.Tick)
            .ThenBy(static entry => entry.EntryType == UiChronicleEntryType.BeatStruck ? 1 : 0)
            .ToList();
        var anyBeat = ordered.Exists(static entry => entry.EntryType == UiChronicleEntryType.BeatStruck);
        if (!anyBeat)
        {
            lines.Add($"    [color=#{DimColor}]The ice holds its breath - nothing yet worth telling.[/color]");
        }

        foreach (var entry in ordered)
        {
            if (entry.EntryType == UiChronicleEntryType.BeatStruck)
            {
                lines.Add($"    {entry.Text}");
            }
            else
            {
                lines.Add($"        [color=#{DimColor}]·[/color] {entry.Text}");
            }
        }
    }
}
