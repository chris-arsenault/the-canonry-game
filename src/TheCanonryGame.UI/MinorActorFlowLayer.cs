#pragma warning disable CA2213
using Engine.Projection;

using Godot;

namespace TheCanonryGame.UI;

/// <summary>
/// The unnamed labor made visible: fading motes where minor-actor cohorts
/// just worked - roads being tended, culture reinforced, lifecycle support -
/// tinted by culture. The kernel was always doing this; now the map shows a
/// world at work between the named deeds.
/// </summary>
public partial class MinorActorFlowLayer : Node2D
{
    private const int FadeTicks = 60;

    private readonly MinorActorPulse[] _buffer = new MinorActorPulse[768];
    private MinorActorFlowProjection? _flow;
    private EntityLayer? _entities;
    private Func<int, Color>? _cultureColor;

    public void Initialize(MinorActorFlowProjection flow, EntityLayer entities, Func<int, Color> cultureColor)
    {
        _flow = flow;
        _entities = entities;
        _cultureColor = cultureColor;
        ShowBehindParent = true;
    }

    public override void _Process(double delta)
    {
        if (IsVisibleInTree())
        {
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_flow is null || _entities is null)
        {
            return;
        }

        var currentTick = _flow.CurrentTick;
        var planeCode = _entities.CurrentPlaneCode;
        var count = _flow.CopyRecent(_buffer);
        for (var i = 0; i < count; i++)
        {
            var pulse = _buffer[i];
            var age = currentTick - pulse.Tick;
            if (pulse.PlaneCode != planeCode || age < 0 || age > FadeTicks)
            {
                continue;
            }

            var fade = 1f - (age / (float)FadeTicks);
            var color = _cultureColor?.Invoke(pulse.CultureId) ?? new Color(0.85f, 0.85f, 0.8f);
            color.A = 0.35f * fade;
            var center = _entities.HexCenter(new HexCoord(pulse.Q, pulse.R));
            var drift = new Vector2(
                6f * Mathf.Sin((pulse.Tick * 0.7f) + i),
                6f * Mathf.Cos((pulse.Tick * 1.1f) + i));
            DrawCircle(center + drift, 2.2f + (2f * fade), color);
        }
    }
}
