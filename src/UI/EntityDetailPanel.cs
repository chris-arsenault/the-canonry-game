#pragma warning disable CA2000 // Godot nodes managed by scene tree
#pragma warning disable CA1305 // UI display strings
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Godot;

using Engine.Domain.Config;
using Engine.Graph;
using Engine.Simulation;
using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

/// <summary>
/// Side panel showing details for a selected entity: attributes, relationships, effects.
/// Updates live as the simulation ticks.
/// </summary>
#pragma warning disable CA2213 // Godot nodes managed by scene tree
public partial class EntityDetailPanel : PanelContainer
{
    private SimulationRunner? _sim;
    private EntityId? _selectedEntityId;
    private RichTextLabel? _content;
    private Label? _titleLabel;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(320, 0);
        SizeFlagsVertical = SizeFlags.ExpandFill;

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", UiTokens.Space2);
        AddChild(vbox);

        _titleLabel = new Label
        {
            Text = "Select an entity",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _titleLabel.AddThemeFontOverride("font", UiTheme.DisplayFont);
        _titleLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeTitleMedium);
        _titleLabel.AddThemeColorOverride("font_color", UiTokens.Vellum);
        vbox.AddChild(_titleLabel);

        vbox.AddChild(new ColorRect
        {
            Color = UiTokens.Hairline,
            CustomMinimumSize = new Vector2(0, 1),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        });

        var scroll = new ScrollContainer
        {
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
    }

    public void Initialize(SimulationRunner sim)
    {
        _sim = sim;
    }

    public void SelectEntity(EntityId entityId)
    {
        _selectedEntityId = entityId;
        Refresh();
    }

    public void ClearSelection()
    {
        _selectedEntityId = null;
        if (_titleLabel is not null) _titleLabel.Text = "Select an entity";
        if (_content is not null) _content.Text = "";
    }

    /// <summary>Refresh the display with current simulation state.</summary>
    public void Refresh()
    {
        if (_sim is null || _selectedEntityId is null || _content is null || _titleLabel is null)
        {
            return;
        }

        var entity = _sim.Graph.GetEntity(_selectedEntityId.Value);
        if (entity is null)
        {
            _titleLabel.Text = "Entity not found";
            _content.Text = "";
            return;
        }

        _titleLabel.Text = $"{entity.Name}  ·  {entity.Kind}";

        var whisper = Main.ColorHex(UiTokens.Whisper);
        var parchment = Main.ColorHex(UiTokens.Parchment);
        var aurora = Main.ColorHex(UiTokens.Aurora);
        var blood = Main.ColorHex(UiTokens.Blood);

        var sb = new StringBuilder();

        // Header info — small-caps ledger block
        sb.AppendLine($"[color={parchment}]{entity.Subtype}  ·  {_sim.DomainConfig.GetCultureDisplayName(entity.Culture)}  ·  {entity.Status}[/color]");
        sb.AppendLine($"[code][color={whisper}]({entity.Coordinates.Q}, {entity.Coordinates.R})  ·  age {_sim.Clock.CurrentTick - entity.CreatedAt}t[/color][/code]");

        // Attributes
        sb.AppendLine($"\n[color={parchment}]ATTRIBUTES[/color]");
        var attrNames = entity.Attributes.Names.OrderBy(n => n, System.StringComparer.Ordinal);
        foreach (var attrName in attrNames)
        {
            var baseValue = entity.Attributes.GetValue(attrName);
            var effectMod = _sim.Effects.ComputeEffectModifier(_selectedEntityId.Value, attrName);
            var trend = (entity.Attributes[attrName] as Engine.Attributes.ScalarValue)?.Trend ?? 0f;
            var trendArrow = trend > 0.01f ? $"[color={aurora}]▲[/color]" : trend < -0.01f ? $"[color={blood}]▼[/color]" : "—";

            if (System.Math.Abs(effectMod) > 0.01f)
            {
                sb.AppendLine($"  {_sim.DomainConfig.GetAttributeDisplayName(entity, attrName)}: [b]{baseValue:F1}[/b] [color={whisper}](+{effectMod:F1} effects)[/color] {trendArrow}");
            }
            else
            {
                sb.AppendLine($"  {_sim.DomainConfig.GetAttributeDisplayName(entity, attrName)}: [b]{baseValue:F1}[/b] {trendArrow}");
            }
        }

        // Active Relationships
        sb.AppendLine($"\n[color={parchment}]RELATIONSHIPS[/color]");
        var outgoing = _sim.Graph.OutgoingRelationships(_selectedEntityId.Value).ToList();
        var incoming = _sim.Graph.IncomingRelationships(_selectedEntityId.Value).ToList();

        if (outgoing.Count == 0 && incoming.Count == 0)
        {
            sb.AppendLine($"  [color={whisper}]— none —[/color]");
        }

        foreach (var rel in outgoing)
        {
            var targetName = _sim.Graph.GetEntity(rel.TargetId)?.Name ?? rel.TargetId.ToString();
            sb.AppendLine($"  → {rel.Kind} → {targetName} [color={whisper}]({rel.Strength:F2})[/color]");
        }

        foreach (var rel in incoming)
        {
            var sourceName = _sim.Graph.GetEntity(rel.SourceId)?.Name ?? rel.SourceId.ToString();
            sb.AppendLine($"  ← {rel.Kind} ← {sourceName} [color={whisper}]({rel.Strength:F2})[/color]");
        }

        // Active Effects
        var effects = _sim.Effects.GetAllEffects(_selectedEntityId.Value);
        if (effects.Count > 0)
        {
            sb.AppendLine($"\n[color={parchment}]ACTIVE EFFECTS[/color]");
            foreach (var effect in effects)
            {
                var sign = effect.Value >= 0 ? "+" : "";
                sb.AppendLine($"  {_sim.DomainConfig.GetAttributeDisplayName(entity, effect.AttributeName)}: {sign}{effect.Value:F1} [color={whisper}][{effect.EffectType}] from {effect.SourceDescription}[/color]");
            }
        }

        _content.Text = sb.ToString();
    }
}
