#pragma warning disable CA1303
#pragma warning disable CA2000
#pragma warning disable CA2213
using Engine.Projection;

using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

/// <summary>
/// The ages strip: three always-visible chips - Ice, Peoples, Memory - each
/// showing the age currently holding that regime track. A blank track reads
/// as quiet. Clicking the strip opens the fuller ages panel.
/// </summary>
public partial class WorldBeatsStrip : PanelContainer
{
    [Signal]
    public delegate void ToggleRequestedEventHandler();

    private static readonly string[] TrackLabels = ["Ice", "Peoples", "Memory"];
    private static readonly string[] TrackColors = ["8FC7E8", "E8C27A", "C9A2E0"];

    private readonly Label[] _chips = new Label[3];
    private UiProjectionState? _projection;
    private IProjectionSymbolCatalog? _symbols;
    private long _lastVersion = -1;

    public override void _Ready()
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", UiTokens.Space3);
        for (var track = 0; track < 3; track++)
        {
            var chip = new Label
            {
                Text = $"{TrackLabels[track]}: —",
                MouseFilter = MouseFilterEnum.Ignore,
            };
            chip.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
            chip.AddThemeColorOverride("font_color", new Color(TrackColors[track]));
            _chips[track] = chip;
            row.AddChild(chip);
        }

        AddChild(row);
        MouseFilter = MouseFilterEnum.Stop;
        TooltipText = "Open the ages of the world";
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
        for (var track = 0; track < 3 && track < _projection.WorldBeatAgeIds.Count; track++)
        {
            var ageId = _projection.WorldBeatAgeIds[track];
            var name = ageId > 0
                ? ProjectionDisplay.DisplayName(_symbols, ProjectionSymbolKind.Generator, ageId, $"age-{ageId}")
                : "quiet";
            _chips[track].Text = $"{TrackLabels[track]}: {name}";
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            EmitSignal(SignalName.ToggleRequested);
        }
    }
}
