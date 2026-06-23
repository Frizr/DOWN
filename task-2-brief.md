### Task 2: Active Skill System (Hero Kit)

**Files:**
- Modify: `D:\gamedev\down\Scripts\Core\AttackSystem.cs`
- Modify: `D:\gamedev\down\Scripts\Core\PlayerController.cs`

**Interfaces:**
- Produces: `TrySkill1()`, `TrySkill2()` in AttackSystem

- [ ] **Step 1: Add Cooldowns to AttackSystem**
Add state variables for Skill 1 and Skill 2 in `AttackSystem.cs`.

```csharp
// D:\gamedev\down\Scripts\Core\AttackSystem.cs
	[ExportGroup("Timing (seconds)")]
	// ... existing exports ...
	[Export] public float Skill1Cooldown = 4.0f;
	[Export] public float Skill2Cooldown = 8.0f;

	// State
	private float _skill1Timer = 0f;
	private float _skill2Timer = 0f;
```

Update `_Process()` to tick these timers down:
```csharp
// D:\gamedev\down\Scripts\Core\AttackSystem.cs (Inside _Process)
		if (_skill1Timer > 0f) _skill1Timer -= dt;
		if (_skill2Timer > 0f) _skill2Timer -= dt;
```

- [ ] **Step 2: Implement TrySkill1 (AoE Cleave)**
Add the `TrySkill1` method to `AttackSystem.cs`. It activates a wider, heavier attack.

```csharp
// D:\gamedev\down\Scripts\Core\AttackSystem.cs
	public bool TrySkill1()
	{
		if (_disabled || IsOwnerDead() || _skill1Timer > 0f || IsAttacking)
			return false;

		IsAttacking = true;
		_skill1Timer = Skill1Cooldown;
		_attackTimer = AttackAnimLockDuration * 1.5f; // Longer lock for heavy skill

		EmitSignal(SignalName.AttackStarted, 2); // Pass 2 to simulate heavy hit
		
		// Temporarily widen hitbox
		if (_hitBoxShape?.Shape is RectangleShape2D rect)
		{
			Vector2 originalSize = rect.Size;
			rect.Size = new Vector2(120f, 120f); // Massive AoE
			ActivateHitBox(HeavyDamage * 2);
			
			// Reset size after delay
			GetTree().CreateTimer(HitBoxDuration).Timeout += () => {
				rect.Size = originalSize;
			};
		}
		else
		{
			ActivateHitBox(HeavyDamage * 2);
		}

		return true;
	}
```

- [ ] **Step 3: Implement TrySkill2 (Speed Buff)**
Add `TrySkill2` method to `AttackSystem.cs`. This emits a buff signal. First, add the signal:

```csharp
// D:\gamedev\down\Scripts\Core\AttackSystem.cs
	[Signal] public delegate void BuffTriggeredEventHandler(float speedMultiplier, float duration);

	public bool TrySkill2()
	{
		if (_disabled || IsOwnerDead() || _skill2Timer > 0f || IsAttacking)
			return false;

		_skill2Timer = Skill2Cooldown;
		
		// Emit buff to PlayerController
		EmitSignal(SignalName.BuffTriggered, 1.8f, 3.0f);
		return true;
	}
```

- [ ] **Step 4: Connect Skills to PlayerController**
In `PlayerController.cs`, read the inputs and handle the buff.

```csharp
// D:\gamedev\down\Scripts\Core\PlayerController.cs (Add buff state variables)
	private float _buffTimer = 0f;
	private float _buffSpeedMult = 1f;

// Inside _Ready():
	_attack.Connect(AttackSystem.SignalName.BuffTriggered, Callable.From<float, float>(OnBuffTriggered));

// Add the handler method:
	private void OnBuffTriggered(float speedMult, float duration)
	{
		_buffSpeedMult = speedMult;
		_buffTimer = duration;
		// Visual feedback
		Modulate = new Color(1.5f, 1.5f, 2.0f); 
	}

// Update _PhysicsProcess to tick the buff timer:
		if (_buffTimer > 0f)
		{
			_buffTimer -= (float)delta;
			if (_buffTimer <= 0f)
			{
				_buffSpeedMult = 1f;
				Modulate = Colors.White; // Need to check if it's currently correct. Usually new Color(1,1,1) is white
			}
		}

// Update ReadAttack() to also check for skills:
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
			if (_attack.TryAttack()) _facing = attackFacing;
		}
	}

// Update ApplyMovement speed calculation in PlayerController:
		float speed = MoveSpeed * (sprinting ? SprintMult : 1f) * _buffSpeedMult;
```
