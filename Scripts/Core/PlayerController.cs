using Godot;

/// <summary>
/// PlayerController — Agent 2: Player Controller
/// Top-down 8-directional CharacterBody2D movement, dodge roll with
/// i-frames, attack input routing, and camera/GameManager integration.
///
/// Node type : CharacterBody2D
/// Children  : Health (Node), AttackSystem (Node), Sprite2D, AnimatedSprite2D,
///             HitBox (Area2D + CollisionShape2D), HurtBox (Area2D + CollisionShape2D)
/// </summary>
public partial class PlayerController : CharacterBody2D
{
	// ─── Inspector ────────────────────────────────────────────────────────────

	[ExportGroup("Movement")]
	[Export] public float MoveSpeed    = 180f;   // pixels per second in world-space
	[Export] public float SprintMult   = 1.65f;  // Sprint modifier (Shift)
	[Export] public float Friction     = 0.15f;  // Lower = slidier feel

	[ExportGroup("Dodge Roll")]
	[Export] public float DodgeSpeed    = 400f;  // Peak dodge velocity
	[Export] public float DodgeDuration = 0.25f; // Seconds of dodge travel
	[Export] public float DodgeCooldown = 0.8f;  // Seconds before next dodge
	[Export] public float DodgeIFrames  = 0.3f;  // Full invincible window

	[ExportGroup("Camera Shake")]
	[Export] public float ShakeOnHit    = 14f;
	[Export] public float ShakeOnDodge  = 4f;

	// ─── Node References ──────────────────────────────────────────────────────

	private Health          _health;
	private AttackSystem    _attack;
	private AnimatedSprite2D _sprite;
	private IsometricCamera _camera;

	// ─── State ────────────────────────────────────────────────────────────────

	private Vector2 _moveDir     = Vector2.Zero;
	private Vector2 _dodgeDir    = Vector2.Zero;

	private bool  _isDodging      = false;
	private float _dodgeTimer     = 0f;
	private float _dodgeCoolTimer = 0f;
	private bool  _isDead         = false;

	// Facing direction from the last non-zero input, used for directional animations.
	private Vector2 _facing = Vector2.Down;

	// ─── Lifecycle ────────────────────────────────────────────────────────────

	public override void _Ready()
	{
		_health = GetNode<Health>("Health");
		_attack = GetNode<AttackSystem>("AttackSystem");
		_sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

		// Locate camera (autoload or scene path)
		_camera = GetNodeOrNull<IsometricCamera>("/root/Main/Camera");

		// Wire health signals
		_health.DamageTaken += OnDamageTaken;
		_health.Died        += OnDied;

		// Wire attack signals → GameManager combo
		_attack.AttackStarted += OnAttackStarted;
		_attack.HitConnected += OnHitConnected;

		if (_sprite != null)
			_sprite.AnimationFinished += OnSpriteAnimationFinished;

		PlayAnim("idle");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDead)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		float dt = (float)delta;

		ReadMovement();
		ReadAttack();
		ReadDodge();
		UpdateDodgeTimers(dt);

		if (_isDodging)
		{
			PerformDodge(dt);
			return;
		}

		ApplyMovement(dt);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Pause toggle — always available even during combat
		if (@event.IsActionPressed("ui_cancel"))
			GameManager.Instance?.TogglePause();
	}

	// ─── Input Reading ────────────────────────────────────────────────────────

	/// <summary>Read WASD/arrow keys as direct screen-space movement.</summary>
	private void ReadMovement()
	{
		float inputX = Input.GetAxis("move_left",  "move_right");
		float inputY = Input.GetAxis("move_up",    "move_down");

		if (inputX == 0f && inputY == 0f)
		{
			_moveDir = Vector2.Zero;
			return;
		}

		Vector2 dir = new Vector2(inputX, inputY).Normalized();
		_moveDir = dir;
		_facing  = dir;
	}

	private void ReadAttack()
	{
		if (_isDead)
			return;

		if (Input.IsActionJustPressed("attack"))
		{
			_attack.SetFacing(_facing);
			_attack.TryAttack();
		}
	}

	private void ReadDodge()
	{
		if (_isDead)
			return;
		if (!Input.IsActionJustPressed("dodge"))
			return;
		if (_dodgeCoolTimer > 0f)
			return;

		// Dodge in current facing direction; fall back to facing if stationary
		_dodgeDir     = _moveDir != Vector2.Zero ? _moveDir : _facing;
		_isDodging    = true;
		_dodgeTimer   = DodgeDuration;
		_dodgeCoolTimer = DodgeCooldown;

		// Grant i-frames
		_health.SetInvincible(DodgeIFrames);

		_camera?.Shake(ShakeOnDodge);
		PlayAnim("dodge");
	}

	// ─── Movement Application ────────────────────────────────────────────────

	private void ApplyMovement(float dt)
	{
		bool sprinting = Input.IsActionPressed("sprint");
		float speed    = MoveSpeed * (sprinting ? SprintMult : 1f);

		if (_moveDir != Vector2.Zero)
		{
			Velocity = _moveDir * speed;
			PlayAnim(sprinting ? "run" : "walk");
		}
		else
		{
			// Friction deceleration
			Velocity = Velocity.Lerp(Vector2.Zero, 1f - Mathf.Pow(Friction, dt * 60f));
			bool movementAnimIsStillPlaying = IsMovementAnimation(_currentAnim);
			if (_attack.IsAttacking && movementAnimIsStillPlaying)
				_attack.OnAnimationFinished();
			if (!_attack.IsAttacking || movementAnimIsStillPlaying)
				PlayAnim("idle");
		}

		MoveAndSlide();
	}

	private void PerformDodge(float dt)
	{
		Velocity = _dodgeDir * DodgeSpeed;
		MoveAndSlide();
	}

	private void UpdateDodgeTimers(float dt)
	{
		if (_dodgeCoolTimer > 0f)
			_dodgeCoolTimer -= dt;

		if (_isDodging)
		{
			_dodgeTimer -= dt;
			if (_dodgeTimer <= 0f)
			{
				_isDodging = false;
				Velocity   = Vector2.Zero;
			}
		}
	}

	// ─── Signal Handlers ──────────────────────────────────────────────────────

	private void OnDamageTaken(int amount, int currentHp)
	{
		if (_isDead || currentHp <= 0)
			return;

		_camera?.Shake(ShakeOnHit);
		GameManager.Instance?.ResetCombo();

		PlayAnim("hurt");
		GD.Print($"[Combat] Player took {amount} damage. HP: {currentHp}/{_health.MaxHealth}");
	}

	private void OnDied()
	{
		if (_isDead)
			return;

		_isDead = true;
		_isDodging = false;
		_moveDir = Vector2.Zero;
		_dodgeDir = Vector2.Zero;
		Velocity = Vector2.Zero;
		_attack?.DisableAttack();

		bool deathAnimationStarted = PlayAnim("death");
		ShowGameOverAfterDeathDelay(deathAnimationStarted ? GetCurrentAnimationDuration(0.8f) : 0.5f);
		GD.Print("[Combat] Player died.");
	}

	private void OnAttackStarted(int comboStep)
	{
		if (_isDead)
			return;

		PlayAnim("attack");
	}

	private void OnHitConnected(Node target, int damage)
	{
		if (_isDead)
			return;

		GameManager.Instance?.IncrementCombo();
		GameManager.Instance?.AddScore(damage * 10);
	}

	private void OnSpriteAnimationFinished()
	{
		if (_isDead)
			return;

		if (_attack != null && (_attack.IsAttacking || _currentAnim.StartsWith("attack")))
			_attack.OnAnimationFinished();

		if (_moveDir == Vector2.Zero && !_currentAnim.StartsWith("death"))
			PlayAnim("idle");
	}

	// ─── Animation Helper ─────────────────────────────────────────────────────

	private string _currentAnim = "";

	/// <summary>Play animation only if it isn't already playing (prevents restart spam).</summary>
	private bool PlayAnim(string animName)
	{
		if (_sprite == null || _sprite.SpriteFrames == null)
			return false;

		string resolvedAnim = ResolveDirectionalAnim(animName);
		if (_currentAnim == resolvedAnim)
			return true;
		if (!_sprite.SpriteFrames.HasAnimation(resolvedAnim))
			return false;

		_currentAnim = resolvedAnim;
		_sprite.Play(resolvedAnim);
		return true;
	}

	private string ResolveDirectionalAnim(string baseAnim)
	{
		if (baseAnim.Contains('_'))
			return baseAnim;

		string directionalAnim = $"{baseAnim}_{GetFacingDirection()}";
		if (_sprite.SpriteFrames.HasAnimation(directionalAnim))
			return directionalAnim;

		return baseAnim;
	}

	private string GetFacingDirection()
	{
		float ax = Mathf.Abs(_facing.X);
		float ay = Mathf.Abs(_facing.Y);

		if (ay >= ax)
			return _facing.Y >= 0f ? "down" : "up";

		return _facing.X >= 0f ? "right" : "left";
	}

	private static bool IsMovementAnimation(string animName)
	{
		return animName.StartsWith("walk") || animName.StartsWith("run");
	}

	private async void ShowGameOverAfterDeathDelay(float delay)
	{
		await ToSignal(GetTree().CreateTimer(delay, false), SceneTreeTimer.SignalName.Timeout);
		GameManager.Instance?.SetState(GameManager.GameState.GameOver);
	}

	private float GetCurrentAnimationDuration(float fallback)
	{
		if (_sprite?.SpriteFrames == null || string.IsNullOrEmpty(_currentAnim))
			return fallback;
		if (!_sprite.SpriteFrames.HasAnimation(_currentAnim))
			return fallback;

		int frameCount = _sprite.SpriteFrames.GetFrameCount(_currentAnim);
		double speed = _sprite.SpriteFrames.GetAnimationSpeed(_currentAnim);
		if (frameCount <= 0 || speed <= 0)
			return fallback;

		return Mathf.Max((float)(frameCount / speed), fallback);
	}

	// ─── Public Accessors ─────────────────────────────────────────────────────

	/// <summary>Current HP as a 0→1 percentage (for HUD health bar).</summary>
	public float HpPercent => _health.HpPercent;

	/// <summary>Whether the player is currently in a dodge roll.</summary>
	public bool IsDodging => _isDodging;

	/// <summary>Whether the player is dead and locked out of movement/combat.</summary>
	public bool IsDead => _isDead;
}
