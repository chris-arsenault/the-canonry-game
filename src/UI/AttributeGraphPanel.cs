#pragma warning disable CA2000 // Godot nodes managed by scene tree
#pragma warning disable CA1305 // UI display strings
using System.Collections.Generic;
using System.Linq;

using Godot;

using Engine.Attributes;
using Engine.Graph;
using Engine.Simulation;

namespace TheCanonryGame.UI;

/// <summary>
/// Multi-series graph panel. Up to three (entity, attribute) pairs plotted against time.
/// History is captured here each tick — no engine dependency on per-attribute queues.
/// </summary>
#pragma warning disable CA2213 // Godot nodes managed by scene tree
public partial class AttributeGraphPanel : PanelContainer
{
    private const int MaxSeries = 3;
    private const int MaxSamples = 200;

    private static readonly Color[] Palette =
    [
        new(0.9f, 0.3f, 0.3f),
        new(0.3f, 0.7f, 0.95f),
        new(0.35f, 0.9f, 0.45f),
    ];

    private SimulationRunner? _sim;
    private Label? _titleLabel;
    private Control? _graphArea;
    private readonly SeriesSlot[] _slots = new SeriesSlot[MaxSeries];
    private int _lastEntityListCount = -1;

    private sealed class SeriesSlot
    {
        public OptionButton EntityPicker { get; init; } = null!;
        public OptionButton AttributePicker { get; init; } = null!;
        public Label ValueLabel { get; init; } = null!;
        public EntityId? EntityId { get; set; }
        public string? AttributeName { get; set; }
        public Queue<(int Tick, float Value)> History { get; } = new();

        public bool IsConfigured => EntityId is not null && AttributeName is not null;

        public void Clear()
        {
            EntityId = null;
            AttributeName = null;
            History.Clear();
        }
    }

    public void Initialize(SimulationRunner sim)
    {
        _sim = sim;
        foreach (var slot in _slots)
        {
            slot?.Clear();
        }

        RebuildEntityDropdowns();
    }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(560, 440);

        var vbox = new VBoxContainer();
        AddChild(vbox);

        var titleBar = new HBoxContainer();
        vbox.AddChild(titleBar);

        _titleLabel = new Label
        {
            Text = "Attribute Graph",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 14);
        titleBar.AddChild(_titleLabel);

        var closeBtn = new Button { Text = "X", CustomMinimumSize = new Vector2(28, 28) };
        closeBtn.Pressed += () => QueueFree();
        titleBar.AddChild(closeBtn);

        vbox.AddChild(new HSeparator());

        for (var i = 0; i < MaxSeries; i++)
        {
            _slots[i] = BuildSlotRow(vbox, i);
        }

        vbox.AddChild(new HSeparator());

        _graphArea = new Control
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 240),
        };
        _graphArea.Draw += DrawGraph;
        vbox.AddChild(_graphArea);

        RebuildEntityDropdowns();
    }

    public void Refresh()
    {
        if (_sim is null) return;

        if (_sim.Graph.AllEntities.Count != _lastEntityListCount)
        {
            RebuildEntityDropdowns();
        }

        SampleAll(_sim.Clock.CurrentTick);
        UpdateValueLabels();

        if (_titleLabel is not null)
        {
            _titleLabel.Text = $"Attribute Graph (tick {_sim.Clock.CurrentTick})";
        }

        _graphArea?.QueueRedraw();
    }

    private SeriesSlot BuildSlotRow(VBoxContainer parent, int index)
    {
        var row = new HBoxContainer();
        parent.AddChild(row);

        var swatch = new ColorRect
        {
            Color = Palette[index],
            CustomMinimumSize = new Vector2(12, 16),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        row.AddChild(swatch);

        var entityPicker = new OptionButton
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        entityPicker.ItemSelected += (selected) => OnEntityPicked(index, (int)selected);
        row.AddChild(entityPicker);

        var attrPicker = new OptionButton
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        attrPicker.ItemSelected += (selected) => OnAttrPicked(index, (int)selected);
        row.AddChild(attrPicker);

        var valueLabel = new Label
        {
            Text = "—",
            CustomMinimumSize = new Vector2(70, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        valueLabel.AddThemeColorOverride("font_color", Palette[index]);
        row.AddChild(valueLabel);

        var clearBtn = new Button { Text = "×", CustomMinimumSize = new Vector2(28, 28) };
        clearBtn.Pressed += () => ClearSlot(index);
        row.AddChild(clearBtn);

        return new SeriesSlot
        {
            EntityPicker = entityPicker,
            AttributePicker = attrPicker,
            ValueLabel = valueLabel,
        };
    }

    private void RebuildEntityDropdowns()
    {
        if (_sim is null) return;
        _lastEntityListCount = _sim.Graph.AllEntities.Count;

        var items = _sim.Graph.AllEntities
            .Where(kv => kv.Value.Status == EntityStatus.Active)
            .OrderBy(kv => kv.Value.Kind)
            .ThenBy(kv => kv.Value.Name, System.StringComparer.Ordinal)
            .Select(kv => (Id: kv.Key, Label: $"{kv.Value.Name} ({kv.Value.Kind})"))
            .ToList();

        foreach (var slot in _slots)
        {
            if (slot?.EntityPicker is null) continue;

            var previous = slot.EntityId;
            slot.EntityPicker.Clear();
            slot.EntityPicker.AddItem("— pick entity —", -1);

            var selectedIdx = 0;
            for (var i = 0; i < items.Count; i++)
            {
                slot.EntityPicker.AddItem(items[i].Label, items[i].Id.Value);
                if (previous is not null && items[i].Id == previous.Value)
                {
                    selectedIdx = i + 1;
                }
            }

            slot.EntityPicker.Selected = selectedIdx;
            RebuildAttributeDropdown(slot);
        }
    }

    private void RebuildAttributeDropdown(SeriesSlot slot)
    {
        if (_sim is null) return;

        slot.AttributePicker.Clear();
        slot.AttributePicker.AddItem("— pick attribute —", -1);

        if (slot.EntityId is null)
        {
            slot.AttributePicker.Selected = 0;
            slot.AttributePicker.Disabled = true;
            return;
        }

        var entity = _sim.Graph.GetEntity(slot.EntityId.Value);
        if (entity is null)
        {
            slot.AttributePicker.Selected = 0;
            slot.AttributePicker.Disabled = true;
            return;
        }

        slot.AttributePicker.Disabled = false;
        var names = entity.Attributes.All
            .OfType<ScalarValue>()
            .Select(s => s.Name)
            .OrderBy(n => n, System.StringComparer.Ordinal)
            .ToList();

        var selectedIdx = 0;
        for (var i = 0; i < names.Count; i++)
        {
            slot.AttributePicker.AddItem(names[i], i);
            if (slot.AttributeName == names[i])
            {
                selectedIdx = i + 1;
            }
        }

        if (selectedIdx == 0)
        {
            slot.AttributeName = null;
        }

        slot.AttributePicker.Selected = selectedIdx;
    }

    private void OnEntityPicked(int slotIndex, int itemIndex)
    {
        if (_sim is null) return;
        var slot = _slots[slotIndex];

        var newId = itemIndex > 0 ? (EntityId?)new EntityId(slot.EntityPicker.GetItemId(itemIndex)) : null;

        if (newId == slot.EntityId) return;

        slot.EntityId = newId;
        slot.AttributeName = null;
        slot.History.Clear();
        RebuildAttributeDropdown(slot);
    }

    private void OnAttrPicked(int slotIndex, int itemIndex)
    {
        var slot = _slots[slotIndex];
        if (itemIndex <= 0)
        {
            slot.AttributeName = null;
            slot.History.Clear();
            return;
        }

        var newName = slot.AttributePicker.GetItemText(itemIndex);
        if (newName == slot.AttributeName) return;

        slot.AttributeName = newName;
        slot.History.Clear();
    }

    private void ClearSlot(int index)
    {
        var slot = _slots[index];
        slot.Clear();
        slot.EntityPicker.Selected = 0;
        RebuildAttributeDropdown(slot);
        UpdateValueLabels();
        _graphArea?.QueueRedraw();
    }

    private void SampleAll(int currentTick)
    {
        if (_sim is null) return;

        foreach (var slot in _slots)
        {
            if (!slot.IsConfigured) continue;

            var entity = _sim.Graph.GetEntity(slot.EntityId!.Value);
            if (entity is null)
            {
                slot.Clear();
                continue;
            }

            if (entity.Attributes[slot.AttributeName!] is not ScalarValue scalar) continue;

            slot.History.Enqueue((currentTick, scalar.Value));
            while (slot.History.Count > MaxSamples) slot.History.Dequeue();
        }
    }

    private void UpdateValueLabels()
    {
        foreach (var slot in _slots)
        {
            if (slot.History.Count > 0)
            {
                var latest = slot.History.Last().Value;
                slot.ValueLabel.Text = $"{latest:F1}";
            }
            else
            {
                slot.ValueLabel.Text = slot.IsConfigured ? "…" : "—";
            }
        }
    }

    private void DrawGraph()
    {
        if (_sim is null || _graphArea is null) return;

        var size = _graphArea.Size;
        const float margin = 42f;
        var graphW = size.X - margin * 2;
        var graphH = size.Y - margin * 2;

        _graphArea.DrawRect(new Rect2(margin, margin, graphW, graphH), new Color(0.08f, 0.08f, 0.1f));

        var active = _slots.Where(s => s.History.Count >= 2).ToList();
        var font = ThemeDB.FallbackFont;

        if (active.Count == 0)
        {
            if (font is not null)
            {
                _graphArea.DrawString(font, new Vector2(margin + 10, margin + graphH / 2),
                    "Pick an entity and attribute to start plotting.",
                    HorizontalAlignment.Left, -1, 12, new Color(0.5f, 0.5f, 0.55f));
            }

            return;
        }

        var minTick = int.MaxValue;
        var maxTick = int.MinValue;
        var maxVal = 0f;
        var minVal = 0f;
        foreach (var slot in active)
        {
            foreach (var (tick, value) in slot.History)
            {
                if (tick < minTick) minTick = tick;
                if (tick > maxTick) maxTick = tick;
                if (value > maxVal) maxVal = value;
                if (value < minVal) minVal = value;
            }
        }

        if (maxTick <= minTick) return;
        if (Mathf.IsEqualApprox(maxVal, minVal)) maxVal = minVal + 1f;

        var tickRange = (float)(maxTick - minTick);
        var valueRange = maxVal - minVal;

        if (font is not null)
        {
            var axisColor = new Color(0.55f, 0.55f, 0.6f);
            _graphArea.DrawString(font, new Vector2(4, margin + 4), $"{maxVal:F1}",
                HorizontalAlignment.Left, -1, 10, axisColor);
            _graphArea.DrawString(font, new Vector2(4, margin + graphH),
                $"{minVal:F1}", HorizontalAlignment.Left, -1, 10, axisColor);
            _graphArea.DrawString(font, new Vector2(margin, margin + graphH + 14),
                $"t{minTick}", HorizontalAlignment.Left, -1, 10, axisColor);
            _graphArea.DrawString(font, new Vector2(margin + graphW - 30, margin + graphH + 14),
                $"t{maxTick}", HorizontalAlignment.Left, -1, 10, axisColor);
        }

        for (var i = 0; i < _slots.Length; i++)
        {
            var slot = _slots[i];
            if (slot.History.Count < 2) continue;

            var color = Palette[i];
            var points = new List<Vector2>(slot.History.Count);
            foreach (var (tick, value) in slot.History)
            {
                var x = margin + ((tick - minTick) / tickRange * graphW);
                var y = margin + graphH - ((value - minVal) / valueRange * graphH);
                points.Add(new Vector2(x, y));
            }

            _graphArea.DrawPolyline(points.ToArray(), color, 1.5f, antialiased: true);

            if (font is not null && _sim.Graph.GetEntity(slot.EntityId!.Value) is { } entity)
            {
                var labelText = $"{entity.Name}.{slot.AttributeName}";
                _graphArea.DrawString(font, points[^1] + new Vector2(4, -4), labelText,
                    HorizontalAlignment.Left, -1, 10, color);
            }
        }
    }
}
