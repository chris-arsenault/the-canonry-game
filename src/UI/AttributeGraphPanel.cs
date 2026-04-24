#pragma warning disable CA2000 // Godot nodes managed by scene tree
#pragma warning disable CA1305 // UI display strings
using System.Collections.Generic;
using System.Linq;

using Godot;

using Engine.Domain.Config;
using Engine.Attributes;
using Engine.Graph;
using Engine.Kinds;
using Engine.Simulation;
using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

/// <summary>
/// Multi-series graph panel. Up to three (EntityKind, attribute) pairs plotted against time.
/// Each configured slot draws one line per live entity of that kind that has the attribute,
/// all sharing the slot's palette color. History is captured here each tick — no engine
/// dependency on per-attribute queues.
/// </summary>
#pragma warning disable CA2213 // Godot nodes managed by scene tree
public partial class AttributeGraphPanel : PanelContainer
{
    private const int MaxSeries = 3;
    private const int MaxSamples = 200;

    // Three discriminable series colors pulled from the token palette.
    // Ember is hot (series 1), Aurora is cool (series 2), Gilt is warm-neutral (series 3).
    private static readonly Color[] Palette =
    [
        UiTokens.Ember,
        UiTokens.Aurora,
        UiTokens.Gilt,
    ];

    private SimulationRunner? _sim;
    private Label? _titleLabel;
    private Control? _graphArea;
    private readonly SeriesSlot[] _slots = new SeriesSlot[MaxSeries];
    private int _lastEntityRosterHash;

    private sealed class SeriesSlot
    {
        public OptionButton KindPicker { get; init; } = null!;
        public OptionButton AttributePicker { get; init; } = null!;
        public Label CountLabel { get; init; } = null!;
        public EntityKind? Kind { get; set; }
        public string? AttributeName { get; set; }
        public List<string> AvailableAttributeNames { get; } = [];

        /// <summary>Per-entity sample history for this slot. Entries pruned when entity no longer matches.</summary>
        public Dictionary<EntityId, Queue<(int Tick, float Value)>> History { get; } = [];

        public bool IsConfigured => Kind is not null && AttributeName is not null;

        public void Clear()
        {
            Kind = null;
            AttributeName = null;
            History.Clear();
            AvailableAttributeNames.Clear();
        }
    }

    public void Initialize(SimulationRunner sim)
    {
        _sim = sim;
        foreach (var slot in _slots)
        {
            slot?.Clear();
        }

        RebuildKindDropdowns();
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
            Text = "×",
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

        for (var i = 0; i < MaxSeries; i++)
        {
            _slots[i] = BuildSlotRow(vbox, i);
        }

        vbox.AddChild(new ColorRect
        {
            Color = UiTokens.Hairline,
            CustomMinimumSize = new Vector2(0, 1),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        });

        _graphArea = new Control
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 260),
        };
        _graphArea.Draw += DrawGraph;
        vbox.AddChild(_graphArea);

        RebuildKindDropdowns();
    }

    public void Refresh()
    {
        if (_sim is null) return;

        var rosterHash = ComputeRosterHash();
        if (rosterHash != _lastEntityRosterHash)
        {
            _lastEntityRosterHash = rosterHash;
            RebuildKindDropdowns();
        }

        SampleAll(_sim.Clock.CurrentTick);
        UpdateCountLabels();

        if (_titleLabel is not null)
        {
            _titleLabel.Text = $"Readings  ·  t{_sim.Clock.CurrentTick}";
        }

        _graphArea?.QueueRedraw();
    }

    private SeriesSlot BuildSlotRow(VBoxContainer parent, int index)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", UiTokens.Space2);
        parent.AddChild(row);

        var swatch = new ColorRect
        {
            Color = Palette[index],
            CustomMinimumSize = new Vector2(3, 18),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        row.AddChild(swatch);

        var kindPicker = new OptionButton
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        kindPicker.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        kindPicker.ItemSelected += (selected) => OnKindPicked(index, (int)selected);
        row.AddChild(kindPicker);

        var attrPicker = new OptionButton
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        attrPicker.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        attrPicker.ItemSelected += (selected) => OnAttrPicked(index, (int)selected);
        row.AddChild(attrPicker);

        var countLabel = new Label
        {
            Text = "—",
            CustomMinimumSize = new Vector2(70, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        countLabel.AddThemeFontOverride("font", UiTheme.MonoFont);
        countLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
        countLabel.AddThemeColorOverride("font_color", Palette[index]);
        row.AddChild(countLabel);

        var clearBtn = new Button
        {
            Text = "×",
            CustomMinimumSize = new Vector2(26, 26),
            TooltipText = "Clear slot",
        };
        clearBtn.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        clearBtn.Pressed += () => ClearSlot(index);
        row.AddChild(clearBtn);

        return new SeriesSlot
        {
            KindPicker = kindPicker,
            AttributePicker = attrPicker,
            CountLabel = countLabel,
        };
    }

    private int ComputeRosterHash()
    {
        if (_sim is null) return 0;

        // Cheap structural hash: count active entities per kind. Changes when entities are
        // added, removed, or die — enough to trigger a dropdown rebuild.
        var hash = 17;
        foreach (var kind in _sim.DomainConfig.Kinds.Keys.OrderBy(kind => kind.Value, System.StringComparer.Ordinal))
        {
            var ids = _sim.Graph.GetEntitiesByKind(kind);
            var alive = 0;
            foreach (var id in ids)
            {
                if (_sim.Graph.GetEntity(id)?.Status == EntityStatus.Active) alive++;
            }

            hash = hash * 31 + System.StringComparer.Ordinal.GetHashCode(kind.Value) * 1009 + alive;
        }

        return hash;
    }

    private void RebuildKindDropdowns()
    {
        if (_sim is null) return;

        var kindsWithActive = new List<EntityKind>();
        foreach (var kind in _sim.DomainConfig.Kinds.Keys.OrderBy(kind => kind.Value, System.StringComparer.Ordinal))
        {
            if (HasAnyActive(kind)) kindsWithActive.Add(kind);
        }

        foreach (var slot in _slots)
        {
            if (slot?.KindPicker is null) continue;

            var previous = slot.Kind;
            slot.KindPicker.Clear();
            slot.KindPicker.AddItem("— pick kind —", -1);

            var selectedIdx = 0;
            for (var i = 0; i < kindsWithActive.Count; i++)
            {
                var kind = kindsWithActive[i];
                slot.KindPicker.AddItem(kind.ToString(), i);
                if (previous == kind)
                {
                    selectedIdx = i + 1;
                }
            }

            slot.KindPicker.Selected = selectedIdx;
            if (selectedIdx == 0 && previous is not null)
            {
                slot.Clear();
            }

            RebuildAttributeDropdown(slot);
        }
    }

    private bool HasAnyActive(EntityKind kind)
    {
        if (_sim is null) return false;
        foreach (var id in _sim.Graph.GetEntitiesByKind(kind))
        {
            if (_sim.Graph.GetEntity(id)?.Status == EntityStatus.Active) return true;
        }

        return false;
    }

    private void RebuildAttributeDropdown(SeriesSlot slot)
    {
        if (_sim is null) return;

        slot.AttributePicker.Clear();
        slot.AttributePicker.AddItem("— pick attribute —", -1);

        if (slot.Kind is null)
        {
            slot.AttributePicker.Selected = 0;
            slot.AttributePicker.Disabled = true;
            return;
        }

        slot.AttributePicker.Disabled = false;
        var names = CollectScalarAttributeNames(slot.Kind.Value);
        slot.AvailableAttributeNames.Clear();
        slot.AvailableAttributeNames.AddRange(names);

        var selectedIdx = 0;
        for (var i = 0; i < names.Count; i++)
        {
            var displayName = _sim.DomainConfig.GetAttributeDisplayName(slot.Kind.Value, cultureId: null, names[i]);
            slot.AttributePicker.AddItem(displayName, i);
            if (slot.AttributeName == names[i])
            {
                selectedIdx = i + 1;
            }
        }

        if (selectedIdx == 0)
        {
            slot.AttributeName = null;
            slot.History.Clear();
        }

        slot.AttributePicker.Selected = selectedIdx;
    }

    private List<string> CollectScalarAttributeNames(EntityKind kind)
    {
        if (_sim is null) return [];

        var names = new HashSet<string>();
        foreach (var id in _sim.Graph.GetEntitiesByKind(kind))
        {
            var entity = _sim.Graph.GetEntity(id);
            if (entity?.Status != EntityStatus.Active) continue;
            foreach (var attr in entity.Attributes.All)
            {
                if (attr is ScalarValue scalar) names.Add(scalar.Name);
            }
        }

        return [.. names.OrderBy(n => n, System.StringComparer.Ordinal)];
    }

    private void OnKindPicked(int slotIndex, int itemIndex)
    {
        if (_sim is null) return;
        var slot = _slots[slotIndex];
        var kindsWithActive = _sim.DomainConfig.Kinds.Keys
            .Where(HasAnyActive)
            .OrderBy(kind => kind.Value, System.StringComparer.Ordinal)
            .ToList();

        EntityKind? newKind = itemIndex > 0
            ? kindsWithActive[itemIndex - 1]
            : null;

        if (newKind == slot.Kind) return;

        slot.Kind = newKind;
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

        var nameIndex = itemIndex - 1;
        if (nameIndex < 0 || nameIndex >= slot.AvailableAttributeNames.Count)
        {
            slot.AttributeName = null;
            slot.History.Clear();
            return;
        }

        var newName = slot.AvailableAttributeNames[nameIndex];
        if (newName == slot.AttributeName) return;

        slot.AttributeName = newName;
        slot.History.Clear();
    }

    private void ClearSlot(int index)
    {
        var slot = _slots[index];
        slot.Clear();
        slot.KindPicker.Selected = 0;
        RebuildAttributeDropdown(slot);
        UpdateCountLabels();
        _graphArea?.QueueRedraw();
    }

    private void SampleAll(int currentTick)
    {
        if (_sim is null) return;

        foreach (var slot in _slots)
        {
            if (!slot.IsConfigured) continue;

            var currentIds = new HashSet<EntityId>();
            foreach (var id in _sim.Graph.GetEntitiesByKind(slot.Kind!.Value))
            {
                var entity = _sim.Graph.GetEntity(id);
                if (entity?.Status != EntityStatus.Active) continue;
                if (entity.Attributes[slot.AttributeName!] is not ScalarValue scalar) continue;

                currentIds.Add(id);
                if (!slot.History.TryGetValue(id, out var queue))
                {
                    queue = new Queue<(int, float)>();
                    slot.History[id] = queue;
                }

                queue.Enqueue((currentTick, scalar.Value));
                while (queue.Count > MaxSamples) queue.Dequeue();
            }

            // Prune histories for entities that no longer qualify (died or lost the attribute).
            var stale = slot.History.Keys.Where(id => !currentIds.Contains(id)).ToList();
            foreach (var id in stale)
            {
                slot.History.Remove(id);
            }
        }
    }

    private void UpdateCountLabels()
    {
        foreach (var slot in _slots)
        {
            if (!slot.IsConfigured)
            {
                slot.CountLabel.Text = "—";
                continue;
            }

            var lineCount = slot.History.Count(kv => kv.Value.Count >= 2);
            slot.CountLabel.Text = lineCount == 1 ? "1 line" : $"{lineCount} lines";
        }
    }

    private void DrawGraph()
    {
        if (_sim is null || _graphArea is null) return;

        var size = _graphArea.Size;
        const float margin = 48f;
        var graphW = size.X - margin * 2;
        var graphH = size.Y - margin * 2;

        // Plot surface: deepest surface (Ink) with a single hairline frame.
        _graphArea.DrawRect(new Rect2(margin, margin, graphW, graphH), UiTokens.Ink);
        _graphArea.DrawRect(new Rect2(margin, margin, graphW, graphH), UiTokens.Hairline, filled: false);

        var bodyFont = UiTheme.BodyFont;
        var monoFont = UiTheme.MonoFont;
        var anyLines = _slots.Any(s => s.History.Any(kv => kv.Value.Count >= 2));

        if (!anyLines)
        {
            _graphArea.DrawString(bodyFont, new Vector2(margin + UiTokens.Space3, margin + graphH / 2),
                "Pick a kind and attribute to begin plotting.",
                HorizontalAlignment.Left, -1, UiTokens.SizeBody, UiTokens.Whisper);
            return;
        }

        var minTick = int.MaxValue;
        var maxTick = int.MinValue;
        var maxVal = float.NegativeInfinity;
        var minVal = float.PositiveInfinity;
        foreach (var slot in _slots)
        {
            foreach (var queue in slot.History.Values)
            {
                foreach (var (tick, value) in queue)
                {
                    if (tick < minTick) minTick = tick;
                    if (tick > maxTick) maxTick = tick;
                    if (value > maxVal) maxVal = value;
                    if (value < minVal) minVal = value;
                }
            }
        }

        if (maxTick <= minTick) return;
        if (Mathf.IsEqualApprox(maxVal, minVal)) maxVal = minVal + 1f;
        if (minVal > 0f && minVal < maxVal * 0.1f) minVal = 0f;

        var tickRange = (float)(maxTick - minTick);
        var valueRange = maxVal - minVal;

        // Faint gridlines at quartiles — Basalt on Ink for subtle tabular feel.
        for (var q = 1; q <= 3; q++)
        {
            var y = margin + graphH * q / 4f;
            _graphArea.DrawLine(new Vector2(margin, y), new Vector2(margin + graphW, y),
                UiTokens.Hairline, 1f);
        }

        // Axis labels in mono for tabular alignment.
        _graphArea.DrawString(monoFont, new Vector2(UiTokens.Space2, margin + UiTokens.Space2),
            $"{maxVal:F1}", HorizontalAlignment.Left, -1, UiTokens.SizeCaption, UiTokens.Parchment);
        _graphArea.DrawString(monoFont, new Vector2(UiTokens.Space2, margin + graphH),
            $"{minVal:F1}", HorizontalAlignment.Left, -1, UiTokens.SizeCaption, UiTokens.Parchment);
        _graphArea.DrawString(monoFont, new Vector2(margin, margin + graphH + 16),
            $"t{minTick}", HorizontalAlignment.Left, -1, UiTokens.SizeCaption, UiTokens.Whisper);
        _graphArea.DrawString(monoFont, new Vector2(margin + graphW - 40, margin + graphH + 16),
            $"t{maxTick}", HorizontalAlignment.Left, -1, UiTokens.SizeCaption, UiTokens.Whisper);

        for (var i = 0; i < _slots.Length; i++)
        {
            var slot = _slots[i];
            if (!slot.IsConfigured) continue;

            var color = Palette[i];
            var displayName = _sim.DomainConfig.GetAttributeDisplayName(slot.Kind!.Value, cultureId: null, slot.AttributeName!);
            var legendText = $"{slot.Kind} · {displayName}".ToUpperInvariant();
            _graphArea.DrawString(bodyFont, new Vector2(margin + UiTokens.Space2 + i * 180, margin + UiTokens.Space3),
                legendText, HorizontalAlignment.Left, -1, UiTokens.SizeMicro, color);

            foreach (var (entityId, queue) in slot.History)
            {
                if (queue.Count < 2) continue;

                var points = new List<Vector2>(queue.Count);
                foreach (var (tick, value) in queue)
                {
                    var x = margin + ((tick - minTick) / tickRange * graphW);
                    var y = margin + graphH - ((value - minVal) / valueRange * graphH);
                    points.Add(new Vector2(x, y));
                }

                _graphArea.DrawPolyline(points.ToArray(), color, 1.5f, antialiased: true);

                if (_sim.Graph.GetEntity(entityId) is { } entity)
                {
                    _graphArea.DrawString(bodyFont, points[^1] + new Vector2(UiTokens.Space1, -2), entity.Name,
                        HorizontalAlignment.Left, -1, UiTokens.SizeMicro, color);
                }
            }
        }
    }
}
