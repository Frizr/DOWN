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
	[Export] public float KnockbackForce = 1200f;

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
	public Node2D Visuals { get; private set; }
	public EnemyAI         AI        { get; private set; }

	// ─── State ────────────────────────────────────────────────────────────────

	private Vector2 _knockbackVelocity = Vector2.Zero;
	private bool    _isDead = false;
	private Vector2 _facing = Vector2.Down;

	// ─── Lifecycle ────────────────────────────────────────────────────────────

	public override void _Ready()
	{
		HP   = GetNode<Health>("Health");
		Visuals = GetNodeOrNull<Node2D>("Visuals");
		AI   = GetNode<EnemyAI>("EnemyAI");

		HP.DamageTaken += OnDamageTaken;
		HP.Died        += OnDied;

		OnReady();   // hook for subclasses
		
		// Add shadow
		Sprite2D shadow = new Sprite2D();
		GradientTexture2D grad = new GradientTexture2D();
		grad.Fill = GradientTexture2D.FillEnum.Radial;
		grad.FillFrom = new Vector2(0.5f, 0.5f);
		grad.FillTo = new Vector2(0.5f, 0.0f);
		grad.Gradient = new Gradient();
		grad.Gradient.Colors = new[] { new Color(0, 0, 0, 0.4f), new Color(0, 0, 0, 0f) };
		grad.Gradient.Offsets = new[] { 0f, 1f };
		grad.Width = 24;
		grad.Height = 12;
		shadow.Texture = grad;
		shadow.ZIndex = -1;
		shadow.Position = new Vector2(0, 10);
		AddChild(shadow);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDead)
			return;

		float dt = (float)delta;

		// Apply knockback decay (exponential friction)
		if (_knockbackVelocity != Vector2.Zero)
		{
			_knockbackVelocity = _knockbackVelocity.Lerp(Vector2.Zero, 6f * dt);
			if (_knockbackVelocity.Length() < 2f)
				_knockbackVelocity = Vector2.Zero;
		}

		// AI sets Velocity; we only add knockback on top
		Velocity += _knockbackVelocity;
		MoveAndSlide();

		// Face movement direction
		if (Velocity.LengthSquared() > 0.001f)
		{
			FaceDirection(Velocity);
		}
	}

	// ─── Public API ───────────────────────────────────────────────────────────

	/// <summary>Apply an impulse knockback away from a source position.</summary>
	public void ApplyKnockback(Vector2 fromPosition)
	{
		Vector2 dir = (GlobalPosition - fromPosition).Normalized();
		_knockbackVelocity += dir * KnockbackForce;
	}

	private AnimatedSprite2D _animSprite;
	private string _currentAnim = "";

	public void FaceDirection(Vector2 direction)
	{
		if (direction.LengthSquared() > 0.001f)
		{
			_facing = direction.Normalized();
			if (!string.IsNullOrEmpty(_currentAnim))
			{
				// Keep playing current animation but update direction
				string baseAnim = _currentAnim.Split('_')[0];
				PlayAnim(baseAnim, true);
			}
		}
	}

	/// <summary>Is this enemy alive?</summary>
	public bool IsAlive => !_isDead;

	// ─── Virtual Hooks ───────────────────────────────────────────────────────

	/// <summary>Called at end of _Ready — override in subclasses for extra setup.</summary>
	protected virtual void OnReady() 
	{ 
		_animSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (_animSprite != null)
			_animSprite.AnimationFinished += OnSpriteAnimationFinished;
			
		CanvasItem visuals = Visuals;
		if (visuals != null && visuals.Material is ShaderMaterial vMat)
		{
			visuals.Material = (ShaderMaterial)vMat.Duplicate();
		}
		else if (_animSprite != null && _animSprite.Material is ShaderMaterial aMat)
		{
			_animSprite.Material = (ShaderMaterial)aMat.Duplicate();
		}
	}

	private void OnSpriteAnimationFinished()
	{
		if (_isDead) return;
		if (_currentAnim.StartsWith("attack") || _currentAnim.StartsWith("hurt"))
		{
			PlayAnim("idle", true);
		}
	}

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

	private string GetDirectionSuffix()
	{
		float ax = Mathf.Abs(_facing.X);
		float ay = Mathf.Abs(_facing.Y);
		if (ay >= ax)
			return _facing.Y >= 0f ? "down" : "up";
		else
			return _facing.X >= 0f ? "right" : "left";
	}

	public void PlayAnim(string name, bool force = false)
	{
		if (_animSprite == null) return;

		bool isIdle = name == "idle";
		string baseName = isIdle ? "walk" : name; // Fallback idle to walk
		string resolvedAnim = baseName + "_" + GetDirectionSuffix();

		if (!_animSprite.SpriteFrames.HasAnimation(resolvedAnim))
		{
			if (baseName == "hurt" || baseName == "death")
				resolvedAnim = "walk_" + GetDirectionSuffix(); // Fallback
			else
				return;
		}

		if (!force && _currentAnim == resolvedAnim && _animSprite.IsPlaying())
		{
			if (isIdle) _animSprite.Stop(); // Just stop if we should be idling
			return;
		}

		_currentAnim = resolvedAnim;
		_animSprite.Play(resolvedAnim);
		if (isIdle)
			_animSprite.Stop();
	}

	private float GetCurrentAnimationDuration(float fallback)
	{
		if (_animSprite == null || string.IsNullOrEmpty(_currentAnim))
			return fallback;
		if (!_animSprite.SpriteFrames.HasAnimation(_currentAnim))
			return fallback;

		var fps = _animSprite.SpriteFrames.GetAnimationSpeed(_currentAnim);
		var frameCount = _animSprite.SpriteFrames.GetFrameCount(_currentAnim);
		if (fps > 0f && frameCount > 0)
			return (float)(frameCount / fps);
			
		return fallback;
	}

	/// <summary>Briefly modulate sprite to white on hit (classic damage flash).</summary>
	private async void FlashWhite()
	{
		CanvasItem visuals = Visuals;
		if (visuals == null) return;

		ShaderMaterial mat = visuals.Material as ShaderMaterial ?? _animSprite?.Material as ShaderMaterial;
		if (mat != null)
		{
			mat.SetShaderParameter("flash_amount", 1.0f);
			await ToSignal(
				GetTree().CreateTimer(FlashDuration, false),
				SceneTreeTimer.SignalName.Timeout
			);
			if (IsInstanceValid(this))
			{
				mat.SetShaderParameter("flash_amount", 0.0f);
			}
		}
		else
		{
			Color original = visuals.Modulate;
			visuals.Modulate = Colors.White * 4f;  // Over-bright = white flash
			await ToSignal(
				GetTree().CreateTimer(FlashDuration, false),
				SceneTreeTimer.SignalName.Timeout
			);
			if (IsInstanceValid(this))
			{
				visuals.Modulate = original;
			}
		}
	}
}
