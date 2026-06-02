using Godot;

public partial class NPCController : CharacterBody2D
{
	[ExportGroup("Movement")]
	[Export] public float WalkSpeed = 50f;
	[Export] public float WaitTime  = 2f;

	[ExportGroup("Patrol")]
	[Export] public NodePath[] Waypoints = new NodePath[] { };

	private AnimatedSprite2D _sprite;
	private int    _waypointIndex = 0;
	private float  _waitTimer     = 0f;
	private bool   _waiting       = false;
	private string _currentAnim   = "";
	private Vector2 _facing       = Vector2.Down;

	public override void _Ready()
	{
		_sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		PlayAnim("idle");
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		if (Waypoints.Length == 0) { PlayAnim("idle"); return; }

		if (_waiting)
		{
			_waitTimer -= dt;
			PlayAnim("idle");
			if (_waitTimer <= 0f) { _waiting = false; _waypointIndex = (_waypointIndex + 1) % Waypoints.Length; }
			return;
		}

		var wp  = GetNodeOrNull<Node2D>(Waypoints[_waypointIndex]);
		if (wp == null)
		{
			Velocity = Vector2.Zero;
			PlayAnim("idle");
			_waypointIndex = (_waypointIndex + 1) % Waypoints.Length;
			return;
		}

		Vector2 dir = wp.GlobalPosition - GlobalPosition;

		if (dir.Length() < 6f)
		{
			Velocity = Vector2.Zero;
			_waiting   = true;
			_waitTimer = WaitTime;
			return;
		}

		_facing  = dir.Normalized();
		Velocity = _facing * WalkSpeed;
		MoveAndSlide();
		PlayAnim("walk");
	}

	private void PlayAnim(string anim)
	{
		if (_sprite == null || _sprite.SpriteFrames == null) return;
		float ax = Mathf.Abs(_facing.X), ay = Mathf.Abs(_facing.Y);
		string dir = ay >= ax ? (_facing.Y >= 0 ? "_down" : "_up") : (_facing.X > 0 ? "_right" : "_left");
		string full = anim + dir;
		if (!_sprite.SpriteFrames.HasAnimation(full)) full = anim;
		if (_currentAnim == full || !_sprite.SpriteFrames.HasAnimation(full)) return;
		_currentAnim = full;
		_sprite.Play(full);
	}
}
