#pragma warning disable CA2213
using Engine.Projection;

using Godot;

namespace TheCanonryGame.UI;

/// <summary>
/// Journeys made visible: fading lines along the hops named entities just
/// took, tinted by kind. A trek across the ice reads as a path being drawn,
/// not a token teleporting between hexes.
/// </summary>
public partial class EntityTrailLayer : Node2D
{
    private const int FadeTicks = 90;

    private readonly EntityTrailSegment[] _buffer = new EntityTrailSegment[4096];
    private EntityTrailProjection? _trails;
    private EntityLayer? _entities;
    private Func<int, Color>? _entityColor;

    public void Initialize(EntityTrailProjection trails, EntityLayer entities, Func<int, Color> entityColor)
    {
        _trails = trails;
        _entities = entities;
        _entityColor = entityColor;
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
        if (_trails is null || _entities is null)
        {
            return;
        }

        var currentTick = _trails.CurrentTick;
        var planeCode = _entities.CurrentPlaneCode;
        var count = _trails.CopyRecent(_buffer);
        for (var i = 0; i < count; i++)
        {
            var segment = _buffer[i];
            var age = currentTick - segment.Tick;
            if (segment.PlaneCode != planeCode || age < 0 || age > FadeTicks)
            {
                continue;
            }

            var fade = 1f - (age / (float)FadeTicks);
            var color = _entityColor?.Invoke(segment.EntityId) ?? new Color(0.85f, 0.85f, 0.8f);
            color.A = 0.30f * fade;
            var from = _entities.HexCenter(new HexCoord(segment.FromQ, segment.FromR));
            var to = _entities.HexCenter(new HexCoord(segment.ToQ, segment.ToR));
            DrawLine(from, to, color, 1.2f + (1.6f * fade), antialiased: true);
        }
    }
}
