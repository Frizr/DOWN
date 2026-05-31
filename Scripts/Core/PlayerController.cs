using Godot;

/// <summary>
/// PlayerController — Agent 2: Player Controller
/// Isometric 8-directional CharacterBody2D movement, dodge roll with
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

	// Facing direction from the last non-zero input, used for directional animations.
	private Vector2 _facing = Vector2.Down;

	// ─── Isometric Conversion Constants (mirrors IsometricUtils) ─────────────
	//  We inline the projection here so PlayerController has no static dependency.
	// Isometric basis vectors: D/A key → northeast/southwest, S/W key → southeast/northwest
	private static readonly Vector2 IsoRight = new Vector2( 1f,  0.5f).Normalized(); // D key
	private static readonly Vector2 IsoDown  = new Vector2(-1f,  0.5f).Normalized(); // S key

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
		_attack.HitConnected += OnHitConnected;

		PlayAnim("idle");
		GD.Print("[PlayerController] Ready.");
	}

	public override void _PhysicsProcess(double delta)
	{
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

	/// <summary>Read WASD/arrow keys and project into isometric screen-space.</summary>
	private void ReadMovement()
	{
		// Flat input axis
		float inputX = Input.GetAxis("move_left",  "move_right");
		float inputY = Input.GetAxis("move_up",    "move_down");

		if (inputX == 0f && inputY == 0f)
		{
			_moveDir = Vector2.Zero;
			return;
		}

		// Project flat input onto the isometric axes
		// Right-key  → move northeast in iso space
		// Down-key   → move southeast in iso space
		Vector2 inputFacing = new Vector2(inputX, inputY).Normalized();
		Vector2 dir = (IsoRight * inputX + IsoDown * inputY).Normalized();
		_moveDir = dir;
		_facing  = inputFacing;
	}

	private void ReadAttack()
	{
		if (Input.IsActionJustPressed("attack"))
			_attack.TryAttack();
	}

	private void ReadDodge()
	{
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
		GD.Print("[Player] Dodge roll.");
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
			if (!_attack.IsAttacking)
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
		_camera?.Shake(ShakeOnHit);
		GameManager.Instance?.ResetCombo();

		PlayAnim("hurt");
		GD.Print($"[Player] Hit for {amount} | HP: {currentHp}/{_health.MaxHealth}");
	}

	private void OnDied()
	{
		PlayAnim("death");
		GameManager.Instance?.SetState(GameManager.GameState.GameOver);
		GD.Print("[Player] Died.");
	}

	private void OnHitConnected(Node target, int damage)
	{
		GameManager.Instance?.IncrementCombo();
		GameManager.Instance?.AddScore(damage * 10);
		GD.Print($"[Player] Hit {target.Name} for {damage}.");
	}

	// ─── Animation Helper ─────────────────────────────────────────────────────

	private string _currentAnim = "";

	/// <summary>Play animation only if it isn't already playing (prevents restart spam).</summary>
	private void PlayAnim(string animName)
	{
		if (_sprite == null || _sprite.SpriteFrames == null)
			return;

		string resolvedAnim = ResolveDirectionalAnim(animName);
		if (_currentAnim == resolvedAnim)
			return;
		if (!_sprite.SpriteFrames.HasAnimation(resolvedAnim))
			return;

		_currentAnim = resolvedAnim;
		_sprite.Play(resolvedAnim);
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

	// ─── Public Accessors ─────────────────────────────────────────────────────

	/// <summary>Current HP as a 0→1 percentage (for HUD health bar).</summary>
	public float HpPercent => _health.HpPercent;

	/// <summary>Whether the player is currently in a dodge roll.</summary>
	public bool IsDodging => _isDodging;
}
