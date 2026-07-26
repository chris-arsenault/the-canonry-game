#pragma warning disable CA2213
using Engine.Projection;

using Godot;

namespace TheCanonryGame.UI;

/// <summary>
/// Persistent unnamed cohorts and the fading work they leave behind. Cultural
/// fronts, road crews, and campaign columns remain visible between work events.
/// </summary>
public partial class MinorActorFlowLayer : Node2D
{
    private const int FadeTicks = 12;
    private const int MaximumVisibleActors = 256;
    private const int MaximumVisibleMotes = 192;

    private readonly MinorActorViewPoint[] _actorBuffer = new MinorActorViewPoint[4096];
    private readonly MinorActorWorkPoint[] _workBuffer = new MinorActorWorkPoint[768];
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
        DrawActiveActors(planeCode);
        DrawRecentWork(currentTick, planeCode);
    }

    private void DrawActiveActors(int planeCode)
    {
        if (_flow is null)
        {
            return;
        }

        var actorCount = _flow.CopyActive(_actorBuffer, planeCode);
        for (var i = 0; i < actorCount && i < MaximumVisibleActors; i++)
        {
            DrawActor(_actorBuffer[i]);
        }
    }

    private void DrawRecentWork(int currentTick, int planeCode)
    {
        if (_flow is null || _entities is null)
        {
            return;
        }

        var count = _flow.CopyRecentWork(_workBuffer, planeCode, currentTick - FadeTicks);
        _drawnCells.Clear();
        var visibleMotes = 0;
        for (var i = 0; i < count && visibleMotes < MaximumVisibleMotes; i++)
        {
            var work = _workBuffer[i];
            var age = currentTick - work.Tick;
            if (age < 0 || age > FadeTicks)
            {
                continue;
            }

            var cellKey = ((long)(uint)work.Q << 32) | (uint)work.R;
            if (!_drawnCells.Add(cellKey))
            {
                continue;
            }

            var fade = 1f - (age / (float)FadeTicks);
            var color = _cultureColor?.Invoke(work.CultureId) ?? new Color(0.85f, 0.85f, 0.8f);
            color.A = 0.35f * fade;
            var center = _entities.HexCenter(new HexCoord(work.Q, work.R));
            var drift = new Vector2(
                6f * Mathf.Sin((work.Tick * 0.7f) + i),
                6f * Mathf.Cos((work.Tick * 1.1f) + i));
            DrawCircle(center + drift, 2.2f + (2f * fade), color);
            visibleMotes++;
        }
    }

    private void DrawActor(MinorActorViewPoint actor)
    {
        if (_entities is null)
        {
            return;
        }

        var center = _entities.HexCenter(new HexCoord(actor.Q, actor.R));
        var color = _cultureColor?.Invoke(actor.CultureId) ?? new Color(0.85f, 0.85f, 0.8f);
        color.A = actor.PostureCode == Engine.Constants.MinorActorPostureCodes.Disrupted
            ? 0.35f
            : 0.8f;
        var size = actor.RoleCode switch
        {
            Engine.Constants.MinorActorRoleCodes.CampaignColumn => 5.5f,
            Engine.Constants.MinorActorRoleCodes.RoadCrew => 4.5f,
            _ => 3.8f,
        };

        DrawMovementTrail(actor, center, color);
        DrawActorMarker(actor.RoleCode, center, color, size);
    }

    private void DrawMovementTrail(MinorActorViewPoint actor, Vector2 center, Color color)
    {
        if (_entities is null ||
            (actor.PreviousQ == actor.Q && actor.PreviousR == actor.R))
        {
            return;
        }

        var previous = _entities.HexCenter(new HexCoord(actor.PreviousQ, actor.PreviousR));
        DrawLine(previous, center, new Color(color, 0.3f), 1.2f);
    }

    private void DrawActorMarker(int roleCode, Vector2 center, Color color, float size)
    {
        switch (roleCode)
        {
            case Engine.Constants.MinorActorRoleCodes.RoadCrew:
                DrawRoadCrew(center, color, size);
                break;
            case Engine.Constants.MinorActorRoleCodes.CampaignColumn:
                DrawCampaignColumn(center, color, size);
                break;
            default:
                DrawCulturalFront(center, color, size);
                break;
        }
    }

    private void DrawRoadCrew(Vector2 center, Color color, float size)
    {
        DrawRect(
            new Rect2(center - new Vector2(size, size), new Vector2(size * 2f, size * 2f)),
            color);
    }

    private void DrawCampaignColumn(Vector2 center, Color color, float size)
    {
        DrawCircle(center, size, color);
        DrawCircle(center, size * 0.42f, new Color(0.08f, 0.08f, 0.08f, color.A));
    }

    private void DrawCulturalFront(Vector2 center, Color color, float size)
    {
        var points = new[]
        {
            center + new Vector2(0f, -size),
            center + new Vector2(size, size),
            center + new Vector2(-size, size),
        };
        DrawColoredPolygon(points, color);
    }

    private readonly record struct FlowLayerState(long Version, int Tick, int PlaneCode);
}
