using Godot;

/// <summary>
/// EnemyBase — Agent 3: Enemy AI
/// Base CharacterBody2D for all enemies in DOWN.
/// Holds stats, owns the Health component, handles knockback,
/// and exposes virtual hooks for subclasses (Grunt, Brute, etc.).
///
/// Node type  : CharacterBody2D
/// Children   : Health (Node), AttackSystem (Node), Sprite2D,
///              AnimatedSprite2D, EnemyAI (Node),
///              DetectRange (Area2D), AttackRange (Area2D),
///              HitBox (Area2D), HurtBox (Area2D)
/// </summary>
public partial class EnemyBase : CharacterBody2D
{
	// ─── Inspector ────────────────────────────────────────────────────────────

	[ExportGroup("Stats")]
	[Export] public float MoveSpeed     = 80f;
	[Export] public int   ContactDamage = 8;     // Damage on body collision with player
	[Export] public float KnockbackForce = 220f;

	[ExportGroup("Score")]
	[Export] public int   KillScore     = 150;   // Base score awarded to GameManager on death

	[ExportGroup("Visual")]
	[Export] public float FlashDuration = 0.08f; // Seconds sprite flashes white on hit

	// ─── Signals ──────────────────────────────────────────────────────────────

	[Signal] public delegate void EnemyDiedEventHandler(EnemyBase enemy);
	[Signal] public delegate void PlayerDetectedEventHandler(Node2D player);
	[Signal] public delegate void PlayerLostEventHandler();

	// ─── References ───────────────────────────────────────────────────────────

	public Health          HP        { get; private set; }
	public AnimatedSprite2D Sprite   { get; private set; }
	public EnemyAI         AI        { get; private set; }

	// ─── State ────────────────────────────────────────────────────────────────

	private Vector2 _knockbackVelocity = Vector2.Zero;
	private bool    _isDead = false;
	private Vector2 _facing = Vector2.Down;
	private float   _animationLockTimer = 0f;
	private bool    _currentAnimStopped = false;

	// ─── Lifecycle ────────────────────────────────────────────────────────────

	public override void _Ready()
	{
		HP   = GetNode<Health>("Health");
		Sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		AI   = GetNode<EnemyAI>("EnemyAI");

		HP.DamageTaken += OnDamageTaken;
		HP.Died        += OnDied;

		OnReady();   // hook for subclasses
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDead)
			return;

		float dt = (float)delta;
		if (_animationLockTimer > 0f)
			_animationLockTimer -= dt;

		// Apply knockback decay (exponential friction)
		if (_knockbackVelocity != Vector2.Zero)
		{
			_knockbackVelocity = _knockbackVelocity.Lerp(Vector2.Zero, 12f * dt);
			if (_knockbackVelocity.Length() < 2f)
				_knockbackVelocity = Vector2.Zero;
		}

		// AI sets Velocity; we only add knockback on top
		Velocity += _knockbackVelocity;
		MoveAndSlide();

		// Isometric Y-sort depth
		ZIndex = (int)(Position.X + Position.Y);
	}

	// ─── Public API ───────────────────────────────────────────────────────────

	/// <summary>Apply an impulse knockback away from a source position.</summary>
	public void ApplyKnockback(Vector2 fromPosition)
	{
		Vector2 dir = (GlobalPosition - fromPosition).Normalized();
		_knockbackVelocity += dir * KnockbackForce;
	}

	public void FaceDirection(Vector2 direction)
	{
		if (direction.LengthSquared() > 0.001f)
			_facing = direction.Normalized();
	}

	/// <summary>Is this enemy alive?</summary>
	public bool IsAlive => !_isDead;

	// ─── Virtual Hooks ───────────────────────────────────────────────────────

	/// <summary>Called at end of _Ready — override in subclasses for extra setup.</summary>
	protected virtual void OnReady() { }

	/// <summary>Override to customise on-hit reaction (e.g. Brute staggers less).</summary>
	protected virtual void OnDamageTaken(int amount, int currentHp)
	{
		FlashWhite();
		PlayAnim("hurt", true);
		GD.Print($"[Combat] Enemy took {amount} damage. HP: {currentHp}/{HP.MaxHealth}");
	}

	/// <summary>Override for custom death behaviour (e.g. explosion, loot drop).</summary>
	protected virtual void OnDied()
	{
		_isDead = true;
		Velocity = Vector2.Zero;
		_knockbackVelocity = Vector2.Zero;
		PlayAnim("death", true);

		GameManager.Instance?.AddScore(KillScore);

		EmitSignal(SignalName.EnemyDied, this);

		GetTree().CreateTimer(GetCurrentAnimationDuration(1.2f)).Timeout += () => QueueFree();

		GD.Print($"[Combat] Enemy died. +{KillScore} pts.");
	}

	// ─── Animation Helpers ────────────────────────────────────────────────────

	private string _currentAnim = "";

	public void PlayAnim(string name, bool force = false)
	{
		if (Sprite == null || Sprite.SpriteFrames == null)
			return;

		string resolvedName = ResolveAnimName(name);
		if (string.IsNullOrEmpty(resolvedName))
			return;

		bool stopOnFirstFrame = ShouldStopFallbackIdle(name, resolvedName);
		if (_animationLockTimer > 0f && !force && !IsCurrentAction(name))
			return;
		if (_currentAnim == resolvedName && _currentAnimStopped == stopOnFirstFrame && !force)
			return;

		_currentAnim = resolvedName;
		_currentAnimStopped = stopOnFirstFrame;

		Sprite.Play(resolvedName);
		if (force || stopOnFirstFrame)
			Sprite.Frame = 0;
		if (stopOnFirstFrame)
		{
			Sprite.Stop();
		}

		if (IsCurrentAction(name))
			_animationLockTimer = GetCurrentAnimationDuration(0.35f);
	}

	private string ResolveAnimName(string name)
	{
		var frames = Sprite.SpriteFrames;
		if (frames.HasAnimation(name))
			return name;

		string action = name;
		string direction = GetFacingDirection();
		int separator = name.IndexOf('_');
		if (separator >= 0)
		{
			action = name[..separator];
			direction = name[(separator + 1)..];
		}

		string directionalName = $"{action}_{direction}";
		if (frames.HasAnimation(directionalName))
			return directionalName;

		string downName = $"{action}_down";
		if (frames.HasAnimation(downName))
			return downName;

		if (action != "walk")
		{
			string walkDirectionalName = $"walk_{direction}";
			if (frames.HasAnimation(walkDirectionalName))
				return walkDirectionalName;
			if (frames.HasAnimation("walk_down"))
				return "walk_down";
		}

		if (action != "idle")
		{
			string idleDirectionalName = $"idle_{direction}";
			if (frames.HasAnimation(idleDirectionalName))
				return idleDirectionalName;
			if (frames.HasAnimation("idle_down"))
				return "idle_down";
		}

		foreach (StringName animationName in frames.GetAnimationNames())
			return animationName.ToString();

		return string.Empty;
	}

	private string GetFacingDirection()
	{
		float ax = Mathf.Abs(_facing.X);
		float ay = Mathf.Abs(_facing.Y);

		if (ay >= ax)
			return _facing.Y >= 0f ? "down" : "up";

		return _facing.X >= 0f ? "right" : "left";
	}

	private static bool IsCurrentAction(string name)
	{
		return name.StartsWith("attack") || name.StartsWith("hurt") || name.StartsWith("death");
	}

	private static bool ShouldStopFallbackIdle(string requestedName, string resolvedName)
	{
		return requestedName.StartsWith("idle") && !resolvedName.StartsWith("idle");
	}

	private float GetCurrentAnimationDuration(float fallback)
	{
		if (Sprite?.SpriteFrames == null || string.IsNullOrEmpty(_currentAnim))
			return fallback;
		if (!Sprite.SpriteFrames.HasAnimation(_currentAnim))
			return fallback;

		int frameCount = Sprite.SpriteFrames.GetFrameCount(_currentAnim);
		double speed = Sprite.SpriteFrames.GetAnimationSpeed(_currentAnim);
		if (frameCount <= 0 || speed <= 0)
			return fallback;

		return Mathf.Max((float)(frameCount / speed), fallback);
	}

	/// <summary>Briefly modulate sprite to white on hit (classic damage flash).</summary>
	private async void FlashWhite()
	{
		CanvasItem sprite = Sprite;
		if (sprite == null)
			sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		if (sprite == null) return;

		Color original = sprite.Modulate;
		sprite.Modulate = Colors.White * 4f;  // Over-bright = white flash
		await ToSignal(
			GetTree().CreateTimer(FlashDuration, false),
			SceneTreeTimer.SignalName.Timeout
		);
		sprite.Modulate = original;
	}
}
