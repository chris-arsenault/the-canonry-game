#pragma warning disable CA2213
using Engine.Projection;

using Godot;

namespace TheCanonryGame.UI;

/// <summary>
/// The chronicle's presence in the world: pulsing rings around the cast of
/// unfolding tales, tinted by heat - cold blue murmurs to ember-orange
/// legends - so a running story is visibly worth following on the map.
/// Added as a child of the EntityLayer and drawn behind its entities.
/// </summary>
public partial class ChronicleAuraLayer : Node2D
{
    private const double RedrawIntervalSeconds = 1d / 30d;

    private readonly List<(int EntityId, float Heat)> _auras = [];
    private EntityLayer? _entities;
    private double _time;
    private double _redrawElapsed;

    public void Initialize(EntityLayer entities)
    {
        _entities = entities;
        ShowBehindParent = true;
        SetProcess(false);
    }

    public void SetTales(IEnumerable<UiChronicleTale> tales)
    {
        ArgumentNullException.ThrowIfNull(tales);
        _auras.Clear();
        foreach (var tale in tales)
        {
            if (tale.Resolved)
            {
                continue;
            }

            foreach (var member in tale.Cast)
            {
                _auras.Add((member.EntityId, tale.Heat));
            }
        }

        SetProcess(_auras.Count > 0);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_auras.Count == 0 || !IsVisibleInTree())
        {
            return;
        }

        _redrawElapsed += delta;
        if (_redrawElapsed >= RedrawIntervalSeconds)
        {
            _time += _redrawElapsed;
            _redrawElapsed = 0d;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_entities is null)
        {
            return;
        }

        var hexSize = _entities.HexSize;
        for (var index = 0; index < _auras.Count; index++)
        {
            var (entityId, heat) = _auras[index];
            if (!_entities.TryGetEntityLocalPosition(entityId, out var position))
            {
                continue;
            }

            DrawAura(position, heat, index);
        }

        void DrawAura(Vector2 position, float heat, int index)
        {
            var warmth = Mathf.Clamp(heat / 10f, 0f, 1f);
            var color = new Color(0.45f, 0.65f, 0.9f).Lerp(new Color(0.98f, 0.55f, 0.22f), warmth);
            var pulse = 1f + (0.12f * Mathf.Sin(((float)_time * (1.2f + (warmth * 1.6f))) + (index * 0.9f)));
            var radius = hexSize * 0.62f * pulse;
            color.A = 0.55f + (0.25f * warmth);
            DrawArc(position, radius, 0f, Mathf.Tau, 40, color, 2.5f + (warmth * 2f), antialiased: true);
            color.A *= 0.4f;
            DrawArc(position, radius * 1.28f, 0f, Mathf.Tau, 40, color, 1.5f, antialiased: true);
        }
    }
}
