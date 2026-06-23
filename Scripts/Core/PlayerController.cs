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
	[Export] public float Friction     = 0.85f;  // Changed from 0.15f for snappy movement

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
	private AnimationPlayer _animPlayer;
	private IsometricCamera _camera;

	// ─── State ────────────────────────────────────────────────────────────────

	private Vector2 _moveDir     = Vector2.Zero;
	private Vector2 _dodgeDir    = Vector2.Zero;

	private bool  _isDodging      = false;
	private float _dodgeTimer     = 0f;
	private float _dodgeCoolTimer = 0f;
	private float _attackAnimTimer = 0f;
	private float _buffTimer      = 0f;
	private float _buffSpeedMult  = 1f;
	private Color _originalSelfModulate = Colors.White;
	private bool  _wasBuffed      = false;
	private bool  _isDead         = false;
	private bool  _hasPlayableBounds = false;
	private Rect2 _playableBounds;

	// Facing direction from the last non-zero input, used for directional animations.
	private Vector2 _facing = Vector2.Down;

	// ─── Lifecycle ────────────────────────────────────────────────────────────

	private AnimatedSprite2D _animSprite;
	private CpuParticles2D _dustParticles;

	private void EnsureInputMaps()
	{
		if (!InputMap.HasAction("skill_1"))
		{
			InputMap.AddAction("skill_1");
			var qKey = new InputEventKey { Keycode = Key.Key1 };
			InputMap.ActionAddEvent("skill_1", qKey);
		}
		if (!InputMap.HasAction("skill_2"))
		{
			InputMap.AddAction("skill_2");
			var eKey = new InputEventKey { Keycode = Key.Key2 };
			InputMap.ActionAddEvent("skill_2", eKey);
		}
		if (!InputMap.HasAction("dodge"))
		{
			InputMap.AddAction("dodge");
			var spaceKey = new InputEventKey { Keycode = Key.Space };
			InputMap.ActionAddEvent("dodge", spaceKey);
		}
	}

	public override void _Ready()
	{
		EnsureInputMaps();
		
		_health = GetNode<Health>("Health");
		_attack = GetNode<AttackSystem>("AttackSystem");
		_animSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

		// Locate camera (autoload or scene path)
		_camera = GetNodeOrNull<IsometricCamera>("/root/Main/Camera");

		// Wire health signals
		_health.DamageTaken += OnDamageTaken;
		_health.Died        += OnDied;

		// Wire attack signals → GameManager combo
		_attack.AttackStarted += OnAttackStarted;
		_attack.HitConnected += OnHitConnected;
		_attack.BuffTriggered += OnBuffTriggered;

		if (_animSprite != null)
			_animSprite.AnimationFinished += OnSpriteAnimationFinished;

		PlayAnim("idle");
		
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
		
		// Add dust particles
		_dustParticles = new CpuParticles2D();
		_dustParticles.Emitting = false;
		_dustParticles.Amount = 15;
		_dustParticles.Lifetime = 0.4f;
		_dustParticles.OneShot = false;
		_dustParticles.EmissionShape = CpuParticles2D.EmissionShapeEnum.Sphere;
		_dustParticles.EmissionSphereRadius = 8f;
		_dustParticles.Gravity = new Vector2(0, -20);
		_dustParticles.Direction = new Vector2(0, -1);
		_dustParticles.Spread = 45f;
		_dustParticles.InitialVelocityMin = 10f;
		_dustParticles.InitialVelocityMax = 30f;
		_dustParticles.ScaleAmountMin = 2f;
		_dustParticles.ScaleAmountMax = 4f;
		_dustParticles.Color = new Color(0.8f, 0.8f, 0.7f, 0.6f);
		_dustParticles.ZIndex = -1;
		_dustParticles.Position = new Vector2(0, 12);
		AddChild(_dustParticles);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDead)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			if (_dustParticles != null) _dustParticles.Emitting = false;
			return;
		}

		float dt = (float)delta;

		if (_buffTimer > 0f)
		{
			_buffTimer -= dt;
			if (_buffTimer <= 0f)
			{
				_buffSpeedMult = 1f;
				if (_wasBuffed)
				{
					SelfModulate = _originalSelfModulate;
					_wasBuffed = false;
				}
			}
		}

		ReadMovement();
		ReadAttack();
		ReadDodge();
		UpdateDodgeTimers(dt);
		UpdateAttackAnimationTimer(dt);

		if (_isDodging)
		{
			PerformDodge(dt);
			if (_dustParticles != null) _dustParticles.Emitting = true;
			return;
		}

		ApplyMovement(dt);
		
		if (_dustParticles != null)
			_dustParticles.Emitting = (Velocity.LengthSquared() > 0.01f);
	}

	private string GetDirectionSuffix()
	{
		float ax = Mathf.Abs(_facing.X);
		float ay = Mathf.Abs(_facing.Y);
		if (ay >= ax)
			return _facing.Y >= 0f ? "down" : "up";
		else
			return _facing.X >= 0f ? "right" : "left";
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Pause input is handled by GameManager so it still works while the tree is paused.
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
		if (_isDead || _isDodging)
			return;

		Vector2 attackFacing = GetAttackFacing();

		if (Input.IsActionJustPressed("skill_1"))
		{
			_attack.SetFacing(attackFacing);
			if (_attack.TrySkill1()) _facing = attackFacing;
		}
		else if (Input.IsActionJustPressed("skill_2"))
		{
			_attack.TrySkill2();
		}
		else if (Input.IsActionJustPressed("attack"))
		{
			_attack.SetFacing(attackFacing);
			if (_attack.TryAttack())
				_facing = attackFacing;
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

		// Animation Canceling
		if (_attack != null && _attack.IsAttacking)
		{
			_attack.DisableAttack();
			_attack.OnAnimationFinished();
			_attackAnimTimer = 0f;
		}

		// Grant i-frames
		_health.SetInvincible(DodgeIFrames);

		_camera?.Shake(ShakeOnDodge);
		PlayAnim("dodge");
	}

	// ─── Movement Application ────────────────────────────────────────────────

	private void ApplyMovement(float dt)
	{
		bool sprinting = Input.IsActionPressed("sprint");
		float speed    = MoveSpeed * (sprinting ? SprintMult : 1f) * _buffSpeedMult;

		if (_moveDir != Vector2.Zero)
		{
			Velocity = _moveDir * speed;
			if (!IsAttackAnimationLocked())
				PlayAnim(sprinting ? "run" : "walk");
		}
		else
		{
			// Friction deceleration
			Velocity = Velocity.Lerp(Vector2.Zero, 1f - Mathf.Pow(Friction, dt * 60f));
			if (!IsAttackAnimationLocked())
				PlayAnim("idle");
		}

		MoveAndSlide();
		ClampToPlayableBounds();
	}

	private void PerformDodge(float dt)
	{
		Velocity = _dodgeDir * DodgeSpeed;
		MoveAndSlide();
		ClampToPlayableBounds();
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

	private void UpdateAttackAnimationTimer(float dt)
	{
		if (_attackAnimTimer > 0f)
			_attackAnimTimer = Mathf.Max(0f, _attackAnimTimer - dt);
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

		float fallbackDuration = _attack?.AttackAnimLockDuration ?? 0.45f;
		bool animationStarted = PlayAnim("attack", true);
		_attackAnimTimer = animationStarted ? GetCurrentAnimationDuration(fallbackDuration) : fallbackDuration;
	}

	private void OnHitConnected(Node target, int damage)
	{
		if (_isDead)
			return;

		GameManager.Instance?.IncrementCombo();
		
		// Intended design: Players get a small "Hit Score" for landing attacks, 
		// and a larger "Kill Score" from EnemyBase.OnDied() when the enemy dies.
		int hitScore = damage * 5;
		GameManager.Instance?.AddScore(hitScore);
	}

	private void OnBuffTriggered(float speedMult, float duration)
	{
		_buffSpeedMult = speedMult;
		_buffTimer = duration;
		// Visual feedback
		if (!_wasBuffed)
		{
			_originalSelfModulate = SelfModulate;
			_wasBuffed = true;
		}
		SelfModulate = new Color(1.5f, 1.5f, 2.0f);
	}

	private void OnSpriteAnimationFinished()
	{
		if (_isDead)
			return;

		if (_attack != null && (_attack.IsAttacking || _currentAnim.StartsWith("attack")))
		{
			_attack.OnAnimationFinished();
			_attackAnimTimer = 0f;
		}

		if (_moveDir == Vector2.Zero && !_currentAnim.StartsWith("death"))
			PlayAnim("idle");
	}

	// ─── Animation Helper ─────────────────────────────────────────────────────

	private string _currentAnim = "";

	private Vector2 GetAttackFacing()
	{
		if (TryGetNearestEnemyDirection(150f, out Vector2 direction))
			return direction;

		return _facing == Vector2.Zero ? Vector2.Down : _facing;
	}

	private bool TryGetNearestEnemyDirection(float maxDistance, out Vector2 direction)
	{
		direction = Vector2.Zero;
		float bestDistanceSq = maxDistance * maxDistance;

		foreach (Node node in GetTree().GetNodesInGroup("enemy"))
		{
			if (node is not Node2D enemy || !GodotObject.IsInstanceValid(enemy))
				continue;

			Vector2 toEnemy = enemy.GlobalPosition - GlobalPosition;
			float distanceSq = toEnemy.LengthSquared();
			if (distanceSq <= 1f || distanceSq > bestDistanceSq)
				continue;

			bestDistanceSq = distanceSq;
			direction = toEnemy.Normalized();
		}

		return direction != Vector2.Zero;
	}

	/// <summary>Play animation only if it isn't already playing (prevents restart spam).</summary>
	public bool PlayAnim(string animName, bool force = false)
	{
		if (_animSprite == null)
			return false;

		string dirSuffix = GetDirectionSuffix();
		bool isIdleFallback = false;

		// Fallback for idle up/down which are missing from the spritesheet
		if (animName == "idle" && (dirSuffix == "up" || dirSuffix == "down"))
		{
			animName = "walk";
			isIdleFallback = true;
		}

		string resolvedAnim = animName + "_" + dirSuffix;

		// Additional fallback for missing animations like dodge or hurt
		if (!_animSprite.SpriteFrames.HasAnimation(resolvedAnim))
		{
			if (animName.StartsWith("hurt") || animName.StartsWith("dodge") || animName.StartsWith("death"))
				resolvedAnim = "idle_" + dirSuffix;
			else
				return false;
		}

		if (!force && _currentAnim == resolvedAnim && _animSprite.Animation == resolvedAnim)
		{
			if (!isIdleFallback && _animSprite.IsPlaying())
				return true;
			if (isIdleFallback && !_animSprite.IsPlaying())
				return true;
		}

		_currentAnim = resolvedAnim;
		_animSprite.Play(resolvedAnim);
		
		if (isIdleFallback)
		{
			_animSprite.Stop();
			_animSprite.Frame = 0;
		}

		return true;
	}

	private bool IsAttackAnimationLocked()
	{
		return (_attack != null && _attack.IsAttacking)
			|| _attackAnimTimer > 0f
			|| (_currentAnim.StartsWith("attack") && _animSprite?.IsPlaying() == true);
	}

	private void ClampToPlayableBounds()
	{
		if (!_hasPlayableBounds)
			return;

		GlobalPosition = new Vector2(
			Mathf.Clamp(GlobalPosition.X, _playableBounds.Position.X, _playableBounds.End.X),
			Mathf.Clamp(GlobalPosition.Y, _playableBounds.Position.Y, _playableBounds.End.Y)
		);
	}

	private async void ShowGameOverAfterDeathDelay(float delay)
	{
		await ToSignal(GetTree().CreateTimer(delay, false), SceneTreeTimer.SignalName.Timeout);
		GameManager.Instance?.SetState(GameManager.GameState.GameOver);
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

	// ─── Public Accessors ─────────────────────────────────────────────────────

	/// <summary>Current HP as a 0→1 percentage (for HUD health bar).</summary>
	public float HpPercent => _health.HpPercent;

	/// <summary>Whether the player is currently in a dodge roll.</summary>
	public bool IsDodging => _isDodging;

	/// <summary>Whether the player is dead and locked out of movement/combat.</summary>
	public bool IsDead => _isDead;

	public void SetPlayableBounds(Rect2 bounds)
	{
		_playableBounds = bounds;
		_hasPlayableBounds = true;
		ClampToPlayableBounds();
	}
}
