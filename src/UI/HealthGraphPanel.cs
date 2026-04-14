#pragma warning disable CA2000 // Godot nodes managed by scene tree
#pragma warning disable CA1305 // UI display strings
using System.Collections.Generic;
using System.Linq;

using Godot;

using Engine.Graph;
using Engine.Kinds;
using Engine.Simulation;

namespace TheCanonryGame.UI;

/// <summary>
/// Panel showing health metric over time for filtered entities.
/// Draws a simple line graph per entity.
/// </summary>
#pragma warning disable CA2213 // Godot nodes managed by scene tree
public partial class HealthGraphPanel : PanelContainer
{
    private SimulationRunner? _sim;
    private readonly List<Color> _palette =
    [
        new(0.9f, 0.3f, 0.3f), new(0.3f, 0.6f, 0.9f), new(0.3f, 0.9f, 0.4f),
        new(0.9f, 0.8f, 0.2f), new(0.7f, 0.3f, 0.9f), new(0.9f, 0.5f, 0.2f),
    ];

    private Label? _titleLabel;
    private Control? _graphArea;

    /// <summary>Entity kind to filter by.</summary>
    public EntityKind FilterKind { get; set; } = EntityKind.Settlement;

    public void Initialize(SimulationRunner sim)
    {
        _sim = sim;
    }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(500, 300);

        var vbox = new VBoxContainer();
        AddChild(vbox);

        var titleBar = new HBoxContainer();
        vbox.AddChild(titleBar);

        _titleLabel = new Label { Text = "Health Over Time", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _titleLabel.AddThemeFontSizeOverride("font_size", 14);
        titleBar.AddChild(_titleLabel);

        var closeBtn = new Button { Text = "X", CustomMinimumSize = new Vector2(28, 28) };
        closeBtn.Pressed += () => QueueFree();
        titleBar.AddChild(closeBtn);

        vbox.AddChild(new HSeparator());

        _graphArea = new Control
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 220),
        };
        _graphArea.Draw += DrawGraph;
        vbox.AddChild(_graphArea);
    }

    public void Refresh()
    {
        _graphArea?.QueueRedraw();
        if (_titleLabel is not null)
        {
            _titleLabel.Text = $"{FilterKind} Health Over Time (tick {_sim?.Clock.CurrentTick ?? 0})";
        }
    }

    private void DrawGraph()
    {
        if (_sim is null || _graphArea is null) return;

        var size = _graphArea.Size;
        var margin = 40f;
        var graphW = size.X - margin * 2;
        var graphH = size.Y - margin * 2;

        // Draw background
        _graphArea.DrawRect(new Rect2(margin, margin, graphW, graphH), new Color(0.1f, 0.1f, 0.12f));

        // Collect entities with health history
        var entities = new List<(Entity Entity, (int Tick, float Value)[] History)>();
        foreach (var eid in _sim.Graph.GetEntitiesByKind(FilterKind))
        {
            var entity = _sim.Graph.GetEntity(eid);
            if (entity is null) continue;
            if (entity.HealthHistory.Count < 2) continue;
            entities.Add((entity, entity.HealthHistory.ToArray()));
        }

        if (entities.Count == 0) return;

        // Find tick range and value range
        var minTick = int.MaxValue;
        var maxTick = int.MinValue;
        var maxVal = 0f;
        foreach (var (_, history) in entities)
        {
            foreach (var (tick, value) in history)
            {
                if (tick < minTick) minTick = tick;
                if (tick > maxTick) maxTick = tick;
                if (value > maxVal) maxVal = value;
            }
        }

        if (maxTick <= minTick) return;
        if (maxVal <= 0f) maxVal = 1f;

        var tickRange = (float)(maxTick - minTick);

        // Draw axis labels
        var font = ThemeDB.FallbackFont;
        if (font is not null)
        {
            _graphArea.DrawString(font, new Vector2(margin, margin - 5), $"{maxVal:F0}", HorizontalAlignment.Left, -1, 10, new Color(0.5f, 0.5f, 0.5f));
            _graphArea.DrawString(font, new Vector2(margin, margin + graphH + 12), $"0", HorizontalAlignment.Left, -1, 10, new Color(0.5f, 0.5f, 0.5f));
            _graphArea.DrawString(font, new Vector2(margin + graphW - 30, margin + graphH + 12), $"t{maxTick}", HorizontalAlignment.Left, -1, 10, new Color(0.5f, 0.5f, 0.5f));
        }

        // Draw lines per entity
        var colorIdx = 0;
        foreach (var (entity, history) in entities)
        {
            var color = _palette[colorIdx % _palette.Count];
            colorIdx++;

            var points = new List<Vector2>();
            foreach (var (tick, value) in history)
            {
                var x = margin + ((tick - minTick) / tickRange * graphW);
                var y = margin + graphH - (value / maxVal * graphH);
                points.Add(new Vector2(x, y));
            }

            if (points.Count >= 2)
            {
                _graphArea.DrawPolyline(points.ToArray(), color, 1.5f);
            }

            // Label at end of line
            if (points.Count > 0 && font is not null)
            {
                var last = points[^1];
                _graphArea.DrawString(font, last + new Vector2(3, -3), entity.Name,
                    HorizontalAlignment.Left, -1, 9, color);
            }
        }
    }
}
