using Godot;

namespace TheCanonryGame.UI;

public sealed class PhysicalMap3DCameraController
{
    private const float MinimumPitch = 0.42f;
    private const float MaximumPitch = 1.42f;
    private const float OrbitSensitivity = 0.006f;
    private const float ZoomFactor = 1.12f;
    private readonly Camera3D _camera;
    private Vector3 _focus;
    private float _yaw = -0.65f;
    private float _pitch = 0.92f;
    private float _minimumSize = 60f;
    private float _maximumSize = 12000f;
    private bool _orbiting;
    private bool _panning;

    public PhysicalMap3DCameraController(Camera3D camera)
    {
        ArgumentNullException.ThrowIfNull(camera);
        _camera = camera;
        _camera.Projection = Camera3D.ProjectionType.Orthogonal;
        _camera.Near = 0.1f;
        _camera.Far = 50000f;
    }

    public Camera3D Camera => _camera;

    public void Reset(int radius, float hexSize, float maximumAbsoluteHeight)
    {
        _focus = Vector3.Zero;
        _yaw = -0.65f;
        _pitch = 0.92f;
        var worldDiameter = Math.Max(1f, radius * hexSize * 3.5f);
        _minimumSize = Math.Max(hexSize * 2f, 24f);
        _maximumSize = Math.Max(worldDiameter * 1.6f, _minimumSize);
        _camera.Size = Math.Clamp(worldDiameter, _minimumSize, _maximumSize);
        _camera.Far = Math.Max(50000f, worldDiameter * 4f + maximumAbsoluteHeight * 20f);
        UpdateTransform();
    }

    public void MakeCurrent()
    {
        _camera.MakeCurrent();
        UpdateTransform();
    }

    public bool HandlePointerInput(InputEvent input)
    {
        if (input is InputEventMouseButton mouseButton)
        {
            return HandleMouseButton(mouseButton);
        }

        if (input is InputEventMouseMotion motion)
        {
            return HandleMouseMotion(motion);
        }

        return false;
    }

    public bool Pan(Vector2 direction, float delta)
    {
        if (direction == Vector2.Zero)
        {
            return false;
        }

        var panDirection = WorldPanDirection(direction, _yaw);
        var speed = Math.Max(40f, _camera.Size * 0.65f);
        _focus += panDirection.Normalized() * speed * delta;
        UpdateTransform();
        return true;
    }

    internal static Vector3 WorldPanDirection(Vector2 inputDirection, float yaw)
    {
        var right = new Vector3(MathF.Cos(yaw), 0f, -MathF.Sin(yaw));
        var forward = new Vector3(-MathF.Sin(yaw), 0f, -MathF.Cos(yaw));
        return (right * inputDirection.X) - (forward * inputDirection.Y);
    }

    private bool HandleMouseButton(InputEventMouseButton mouse)
    {
        switch (mouse.ButtonIndex)
        {
            case MouseButton.WheelUp when mouse.Pressed:
                SetSize(_camera.Size / ZoomFactor);
                return true;
            case MouseButton.WheelDown when mouse.Pressed:
                SetSize(_camera.Size * ZoomFactor);
                return true;
            case MouseButton.Right:
                _orbiting = mouse.Pressed;
                return true;
            case MouseButton.Middle:
                _panning = mouse.Pressed;
                return true;
            default:
                return false;
        }
    }

    private bool HandleMouseMotion(InputEventMouseMotion motion)
    {
        if (_orbiting)
        {
            _yaw -= motion.Relative.X * OrbitSensitivity;
            _pitch = Math.Clamp(
                _pitch + motion.Relative.Y * OrbitSensitivity,
                MinimumPitch,
                MaximumPitch);
            UpdateTransform();
            return true;
        }

        if (_panning)
        {
            var right = new Vector3(MathF.Cos(_yaw), 0f, -MathF.Sin(_yaw));
            var forward = new Vector3(-MathF.Sin(_yaw), 0f, -MathF.Cos(_yaw));
            var scale = _camera.Size / Math.Max(1f, _camera.GetViewport().GetVisibleRect().Size.Y);
            _focus += (-right * motion.Relative.X + forward * motion.Relative.Y) * scale;
            UpdateTransform();
            return true;
        }

        return false;
    }

    private void SetSize(float size)
    {
        _camera.Size = Math.Clamp(size, _minimumSize, _maximumSize);
        UpdateTransform();
    }

    private void UpdateTransform()
    {
        var horizontal = MathF.Cos(_pitch);
        var direction = new Vector3(
            horizontal * MathF.Sin(_yaw),
            MathF.Sin(_pitch),
            horizontal * MathF.Cos(_yaw));
        var distance = Math.Max(2000f, _camera.Size * 2.2f);
        _camera.Position = _focus + direction * distance;
        _camera.LookAt(_focus, Vector3.Up);
    }
}
