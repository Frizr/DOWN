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

	/// <summary>Is this enemy alive?</summary>
	public bool IsAlive => !_isDead;

	// ─── Virtual Hooks ───────────────────────────────────────────────────────

	/// <summary>Called at end of _Ready — override in subclasses for extra setup.</summary>
	protected virtual void OnReady() { }

	/// <summary>Override to customise on-hit reaction (e.g. Brute staggers less).</summary>
	protected virtual void OnDamageTaken(int amount, int currentHp)
	{
		FlashWhite();
		PlayAnim("hurt");
		GD.Print($"[{Name}] Hit {amount} | HP {currentHp}/{HP.MaxHealth}");
	}

	/// <summary>Override for custom death behaviour (e.g. explosion, loot drop).</summary>
	protected virtual void OnDied()
	{
		_isDead = true;
		PlayAnim("death");

		GameManager.Instance?.AddScore(KillScore);

		EmitSignal(SignalName.EnemyDied, this);

		// Remove from scene after death anim (~1 s)
		GetTree().CreateTimer(1.2f).Timeout += () => QueueFree();

		GD.Print($"[{Name}] Died. +{KillScore} pts.");
	}

	// ─── Animation Helpers ────────────────────────────────────────────────────

	private string _currentAnim = "";

	public void PlayAnim(string name)
	{
		if (Sprite == null || Sprite.SpriteFrames == null || _currentAnim == name)
			return;
		if (!Sprite.SpriteFrames.HasAnimation(name))
			return;
		_currentAnim = name;
		Sprite.Play(name);
	}

	/// <summary>Briefly modulate sprite to white on hit (classic damage flash).</summary>
	private async void FlashWhite()
	{
		var sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		if (sprite == null) return;

		sprite.Modulate = Colors.White * 4f;  // Over-bright = white flash
		await ToSignal(
			GetTree().CreateTimer(FlashDuration, false),
			SceneTreeTimer.SignalName.Timeout
		);
		sprite.Modulate = Colors.White;
	}
}
