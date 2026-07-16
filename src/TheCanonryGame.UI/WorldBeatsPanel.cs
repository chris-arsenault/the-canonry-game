#pragma warning disable CA1303
#pragma warning disable CA2000
#pragma warning disable CA2213
using Engine.Projection;

using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

/// <summary>
/// The ages of the world: a floating panel showing each of the two regime
/// tracks (World, Peoples) with the age holding it and that age's
/// telling, then a timeline of the transitions the world has earned.
/// </summary>
public partial class WorldBeatsPanel : PanelContainer
{
    [Signal]
    public delegate void CloseRequestedEventHandler();

    private static readonly string[] TrackLabels = ["The World", "The Peoples"];
    private static readonly string[] TrackColors = ["8FC7E8", "E8C27A"];

    private readonly Label[] _trackNames = new Label[2];
    private readonly RichTextLabel[] _trackBodies = new RichTextLabel[2];
    private RichTextLabel? _timeline;
    private UiProjectionState? _projection;
    private IProjectionSymbolCatalog? _symbols;
    private long _lastVersion = -1;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(520, 560);
        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", UiTokens.Space4);

        var header = new HBoxContainer();
        var title = new Label { Text = "The Ages of the World", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        title.AddThemeFontSizeOverride("font_size", UiTokens.SizeBody);
        header.AddChild(title);
        var close = new Button { Text = "✕" };
        close.Pressed += () => EmitSignal(SignalName.CloseRequested);
        header.AddChild(close);
        root.AddChild(header);

        for (var track = 0; track < 2; track++)
        {
            var name = new Label();
            name.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
            name.AddThemeColorOverride("font_color", new Color(TrackColors[track]));
            _trackNames[track] = name;
            root.AddChild(name);

            var body = new RichTextLabel
            {
                FitContent = true,
                BbcodeEnabled = true,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(0, 48),
            };
            _trackBodies[track] = body;
            root.AddChild(body);
        }

        var timelineTitle = new Label { Text = "How the world turned" };
        timelineTitle.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        root.AddChild(timelineTitle);
        _timeline = new RichTextLabel
        {
            FitContent = true,
            BbcodeEnabled = true,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 160),
        };
        root.AddChild(_timeline);

        AddChild(root);
    }

    public void Initialize(UiProjectionState projection, IProjectionSymbolCatalog? symbols)
    {
        _projection = projection;
        _symbols = symbols;
        _lastVersion = -1;
        Refresh();
    }

    public void Refresh()
    {
        if (_projection is null || _lastVersion == _projection.WorldBeatVersion)
        {
            return;
        }

        _lastVersion = _projection.WorldBeatVersion;
        for (var track = 0; track < 2 && track < _projection.WorldBeatAgeIds.Count; track++)
        {
            RefreshTrack(track);
        }

        RefreshTimeline();
    }

    private void RefreshTrack(int track)
    {
        var ageId = _projection!.WorldBeatAgeIds[track];
        if (ageId <= 0)
        {
            _trackNames[track].Text = $"{TrackLabels[track]} — quiet";
            _trackBodies[track].Text = "[color=6F7684]No age holds this track.[/color]";
            return;
        }

        var name = ProjectionDisplay.DisplayName(_symbols, ProjectionSymbolKind.Generator, ageId, $"age-{ageId}");
        var opened = _projection.WorldBeatOpenedTicks[track];
        _trackNames[track].Text = $"{TrackLabels[track]} — {name}  [t{opened}]";
        _trackBodies[track].Text = AgeDescription(ageId);
    }

    private string AgeDescription(int ageId)
    {
        if (_symbols is not null &&
            _symbols.TryResolve(ProjectionSymbolKind.Generator, ageId, out var symbol) &&
            !string.IsNullOrEmpty(symbol.Description))
        {
            return symbol.Description;
        }

        return "[color=6F7684]—[/color]";
    }

    private void RefreshTimeline()
    {
        var transitions = _projection!.WorldBeatTransitions;
        var text = new System.Text.StringBuilder();
        for (var i = transitions.Count - 1; i >= 0; i--)
        {
            var transition = transitions[i];
            var name = ProjectionDisplay.DisplayName(
                _symbols, ProjectionSymbolKind.Generator, transition.AgeId, $"age-{transition.AgeId}");
            var verb = transition.Opened ? "opens" : "closes";
            var color = TrackColors[System.Math.Clamp(transition.TrackCode, 0, 1)];
            text.Append("[color=6F7684]t")
                .Append(transition.Tick)
                .Append("[/color]  [color=")
                .Append(color)
                .Append(']')
                .Append(name)
                .Append("[/color] ")
                .Append(verb)
                .Append('\n');
        }

        _timeline!.Text = text.Length == 0 ? "[color=6F7684]The world has not yet turned.[/color]" : text.ToString();
    }
}
