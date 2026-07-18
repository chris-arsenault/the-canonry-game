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
    private const int MaximumVisibleMotes = 192;

    private readonly MinorActorPulse[] _buffer = new MinorActorPulse[768];
    private readonly HashSet<long> _drawnCells = [];
    private MinorActorFlowProjection? _flow;
    private EntityLayer? _entities;
    private Func<int, Color>? _cultureColor;
    private FlowLayerState? _lastState;

    public void Initialize(MinorActorFlowProjection flow, EntityLayer entities, Func<int, Color> cultureColor)
    {
        _flow = flow;
        _entities = entities;
        _cultureColor = cultureColor;
        ShowBehindParent = true;
    }

    public void Present()
    {
        if (_flow is null || _entities is null)
        {
            return;
        }

        var state = new FlowLayerState(_flow.Version, _flow.CurrentTick, _entities.CurrentPlaneCode);
        if (_lastState == state)
        {
            return;
        }

        _lastState = state;
        QueueRedraw();
    }

    public void Invalidate() => _lastState = null;

    public override void _Draw()
    {
        if (_flow is null || _entities is null)
        {
            return;
        }

        var currentTick = _flow.CurrentTick;
        var planeCode = _entities.CurrentPlaneCode;
        var count = _flow.CopyRecent(_buffer, planeCode, currentTick - FadeTicks);
        _drawnCells.Clear();
        var visibleMotes = 0;
        for (var i = 0; i < count && visibleMotes < MaximumVisibleMotes; i++)
        {
            var pulse = _buffer[i];
            var age = currentTick - pulse.Tick;
            if (age < 0 || age > FadeTicks)
            {
                continue;
            }

            var cellKey = ((long)(uint)pulse.Q << 32) | (uint)pulse.R;
            if (!_drawnCells.Add(cellKey))
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
            visibleMotes++;
        }
    }

    private readonly record struct FlowLayerState(long Version, int Tick, int PlaneCode);
}
