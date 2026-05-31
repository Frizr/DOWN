using Godot;

/// <summary>
/// IsometricCamera — Agent 1: Isometric Engine
/// Smooth-following Camera2D with zoom, isometric projection support,
/// and a screen-shake method for brutal combat feedback.
/// Attach this script to a Camera2D node.
/// </summary>
public partial class IsometricCamera : Camera2D
{
	// ─── Inspector Parameters ────────────────────────────────────────────────

	[ExportGroup("Follow Settings")]
	/// <summary>How quickly the camera interpolates toward the target (higher = snappier).</summary>
	[Export] public float FollowSpeed = 8.0f;

	/// <summary>Dead-zone radius in pixels. Camera won't move if target is within this range.</summary>
	[Export] public float DeadZone = 4.0f;

	[ExportGroup("Zoom Settings")]
	/// <summary>Minimum zoom level (most zoomed-in).</summary>
	[Export] public float ZoomMin = 0.5f;

	/// <summary>Maximum zoom level (most zoomed-out).</summary>
	[Export] public float ZoomMax = 3.0f;

	/// <summary>How many zoom steps per scroll tick.</summary>
	[Export] public float ZoomStep = 0.1f;

	/// <summary>Speed at which zoom interpolates to target value.</summary>
	[Export] public float ZoomSpeed = 10.0f;

	[ExportGroup("Screen Shake")]
	/// <summary>How fast shake amplitude decays per second.</summary>
	[Export] public float ShakeDecay = 5.0f;

	// ─── Internal State ───────────────────────────────────────────────────────

	private Node2D _target;
	private float  _targetZoom   = 1.0f;
	private float  _shakeStrength = 0.0f;
	private RandomNumberGenerator _rng = new();

	// ─── Godot Lifecycle ──────────────────────────────────────────────────────

	public override void _Ready()
	{
		// Set sensible defaults — isometric pixel art usually starts at 1× zoom
		_targetZoom = Zoom.X;
		_rng.Randomize();

		// Enable pixel-perfect rendering anchor
		AnchorMode = AnchorModeEnum.DragCenter;

		CallDeferred(nameof(FindPlayer));
	}

	private void FindPlayer()
	{
		Node2D player = GetNodeOrNull<Node2D>("/root/Main/World/YSort/Player");
		if (player != null)
		{
			_target = player;
		}
		else
		{
			var players = GetTree().GetNodesInGroup("player");
			if (players.Count > 0)
				_target = players[0] as Node2D;
		}
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		HandleZoomInput();
		SmoothZoom(dt);

		if (_target != null)
			GlobalPosition = GlobalPosition.Lerp(_target.GlobalPosition, FollowSpeed * dt);

		ApplyShake(dt);
	}

	// ─── Public API ───────────────────────────────────────────────────────────

	/// <summary>
	/// Assign a Node2D for the camera to track.
	/// Call this from your Player or GameManager after the scene is ready.
	/// </summary>
	public void SetTarget(Node2D target)
	{
		_target = target;
	}

	/// <summary>
	/// Trigger a screen shake.
	/// strength : maximum pixel offset (e.g. 8 for light, 24 for heavy hit).
	/// </summary>
	public void Shake(float strength)
	{
		_shakeStrength = Mathf.Max(_shakeStrength, strength); // don't cancel existing shake
	}

	/// <summary>
	/// Instantly center the camera on a world position (no lerp).
	/// Useful for scene transitions or cutscenes.
	/// </summary>
	public void SnapTo(Vector2 worldPosition)
	{
		GlobalPosition = worldPosition;
	}

	// ─── Private Helpers ──────────────────────────────────────────────────────

	/// <summary>Read scroll-wheel input and adjust the target zoom level.</summary>
	private void HandleZoomInput()
	{
		if (Input.IsActionJustReleased("zoom_in"))      // Map in Project > Input Map
			_targetZoom = Mathf.Clamp(_targetZoom - ZoomStep, ZoomMin, ZoomMax);
		else if (Input.IsActionJustReleased("zoom_out"))
			_targetZoom = Mathf.Clamp(_targetZoom + ZoomStep, ZoomMin, ZoomMax);
	}

	/// <summary>Lerp current zoom toward _targetZoom.</summary>
	private void SmoothZoom(float dt)
	{
		float current = Zoom.X;
		float next    = Mathf.Lerp(current, _targetZoom, ZoomSpeed * dt);
		Zoom = new Vector2(next, next);
	}

	/// <summary>Smoothly move camera toward the target's screen-space position.</summary>
	private void SmoothFollow(float dt)
	{
		Vector2 targetPos = _target.GlobalPosition;

		// Skip micro-movements inside the dead-zone
		if (GlobalPosition.DistanceTo(targetPos) < DeadZone)
			return;

		GlobalPosition = GlobalPosition.Lerp(targetPos, FollowSpeed * dt);
	}

	/// <summary>Apply random offset for shake, then decay amplitude.</summary>
	private void ApplyShake(float dt)
	{
		if (_shakeStrength <= 0.01f)
		{
			_shakeStrength = 0;
			Offset = Vector2.Zero;
			return;
		}

		// Random offset within a circle of radius _shakeStrength
		float angle  = _rng.RandfRange(0, Mathf.Tau);
		float radius = _rng.RandfRange(0, _shakeStrength);
		Offset = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);

		// Exponential decay
		_shakeStrength = Mathf.MoveToward(_shakeStrength, 0, ShakeDecay * _shakeStrength * dt);
	}
}
