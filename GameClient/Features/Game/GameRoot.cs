using Godot;

namespace GameClient.Features.Game;

public partial class GameRoot : Node2D
{
    [Export] private NodePath _playerViewPath = "PlayerView";
    [Export] private NodePath _cameraPath = "Camera2D";

    private Node _playerView;
    private Camera2D _camera;
    private Node2D _followTarget;

    public override void _Ready()
    {
        _playerView = GetNode(_playerViewPath);
        _camera = GetNode<Camera2D>(_cameraPath);
        if (_camera != null)
            _camera.Enabled = true;
    }

    public override void _Process(double delta)
    {
        if (_playerView == null || _camera == null) return;

        // Acquire a follow target lazily: pick the first Node2D child under PlayerView
        if (_followTarget == null || !_followTarget.IsInsideTree())
        {
            foreach (var child in _playerView.GetChildren())
            {
                if (child is Node2D n2d)
                {
                    _followTarget = n2d;
                    break;
                }
            }
        }

        if (_followTarget != null)
        {
            _camera.GlobalPosition = ((Node2D)_followTarget).GlobalPosition;
        }
    }
}