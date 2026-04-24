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
/// Left sidebar listing all entities. Click an entity to open its floating card.
/// </summary>
#pragma warning disable CA2213 // Godot nodes managed by scene tree
public partial class EntityBrowser : PanelContainer
{
    private SimulationRunner? _sim;
    private VBoxContainer? _listContainer;
    private readonly Dictionary<EntityId, Button> _entityButtons = [];

    /// <summary>Emitted when the user clicks an entity in the browser.</summary>
    [Signal]
    public delegate void EntitySelectedEventHandler(int entityIdValue);

    public void Initialize(SimulationRunner sim)
    {
        _sim = sim;
    }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(220, 0);
        SizeFlagsVertical = SizeFlags.ExpandFill;

        var vbox = new VBoxContainer();
        AddChild(vbox);

        var title = new Label
        {
            Text = "Entities",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 16);
        vbox.AddChild(title);
        vbox.AddChild(new HSeparator());

        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        vbox.AddChild(scroll);

        _listContainer = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        scroll.AddChild(_listContainer);
    }

    private static string? FormatLastActionSubtitle(Entity entity, int currentTick)
    {
        if (entity.LastActionName is null || entity.LastActionTick is null)
        {
            return null;
        }

        var age = System.Math.Max(0, currentTick - entity.LastActionTick.Value);
        var ageText = age == 0 ? "now" : $"{age}t ago";
        return $"    {entity.LastActionName} · {ageText}";
    }

    /// <summary>Rebuild the entity list from current simulation state.</summary>
    public void Refresh()
    {
        if (_sim is null || _listContainer is null) return;

        // Clear existing buttons
        foreach (var child in _listContainer.GetChildren())
        {
            child.QueueFree();
        }

        _entityButtons.Clear();

        // Add entities grouped by kind
        foreach (var kind in _sim.DomainConfig.Kinds.Keys.OrderBy(kind => kind.Value, System.StringComparer.Ordinal))
        {
            var entities = _sim.Graph.GetEntitiesByKind(kind);
            if (entities.Count == 0) continue;

            // Kind header
            var kindLabel = new Label
            {
                Text = kind.ToString(),
            };
            kindLabel.AddThemeFontSizeOverride("font_size", 12);
            kindLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
            _listContainer.AddChild(kindLabel);

            foreach (var entityId in entities)
            {
                var entity = _sim.Graph.GetEntity(entityId);
                if (entity is null) continue;

                var statusSuffix = entity.Status == EntityStatus.Active ? "" : " [dead]";
                var btn = new Button
                {
                    Text = $"  {entity.Name}{statusSuffix}",
                    Alignment = HorizontalAlignment.Left,
                    Flat = true,
                };

                var capturedId = entityId; // Capture for lambda
                btn.Pressed += () => EmitSignal(SignalName.EntitySelected, capturedId.Value);
                _listContainer.AddChild(btn);
                _entityButtons[entityId] = btn;

                var subtitleText = FormatLastActionSubtitle(entity, _sim.Clock.CurrentTick);
                if (subtitleText is not null)
                {
                    var subtitle = new Label
                    {
                        Text = subtitleText,
                    };
                    subtitle.AddThemeFontSizeOverride("font_size", 10);
                    subtitle.AddThemeColorOverride("font_color", new Color(0.65f, 0.65f, 0.65f));
                    _listContainer.AddChild(subtitle);
                }
            }
        }
    }
}
