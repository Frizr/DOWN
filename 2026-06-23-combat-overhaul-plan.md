# Combat Overhaul (Drakantos Style) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Overhaul the player controller and attack system to feature snappy movement, dodge-canceling, and a MOBA-style active skill kit (Basic Attack, Skill 1, Skill 2).

**Architecture:** We will increase friction for snappy movement, update the dodge logic in `PlayerController.cs` to cancel attack animations, and expand `AttackSystem.cs` to handle `TrySkill1()` and `TrySkill2()` with independent cooldowns. Input bindings for new skills will be registered dynamically in C# to avoid requiring the user to manually edit the Godot Project Settings.

**Tech Stack:** Godot 4.6 (.NET/C#)

## Global Constraints

- No external dependencies.
- Follow existing codebase style.
- Use C# standard practices.

---

### Task 1: Movement Snappiness, Dodge Canceling, & Input Mapping

**Files:**
- Modify: `D:\gamedev\down\Scripts\Core\PlayerController.cs`

**Interfaces:**
- Consumes: `AttackSystem.OnAnimationFinished()`, `AttackSystem.DisableAttack()`

- [ ] **Step 1: Increase Friction**
Modify `Friction` in `PlayerController.cs` to make movement snappy.

```csharp
// D:\gamedev\down\Scripts\Core\PlayerController.cs
	[ExportGroup("Movement")]
	[Export] public float MoveSpeed    = 180f;   // pixels per second in world-space
	[Export] public float SprintMult   = 1.65f;  // Sprint modifier (Shift)
	[Export] public float Friction     = 0.85f;  // Changed from 0.15f for snappy movement
```

- [ ] **Step 2: Cancel Attack on Dodge**
Modify `ReadDodge()` in `PlayerController.cs` to forcefully cancel ongoing attacks when a dodge is initiated. Add the cancellation logic right after setting `_isDodging = true`.

```csharp
// D:\gamedev\down\Scripts\Core\PlayerController.cs
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
			_attack.OnAnimationFinished();
			_attackAnimTimer = 0f;
		}

		// Grant i-frames
		_health.SetInvincible(DodgeIFrames);

		_camera?.Shake(ShakeOnDodge);
		PlayAnim("dodge");
	}
```

- [ ] **Step 3: Register Inputs Programmatically**
Add a method `EnsureInputMaps()` and call it in `_Ready()` of `PlayerController.cs` so we don't need manual Project Settings config for the new keys (Q and E).

```csharp
// D:\gamedev\down\Scripts\Core\PlayerController.cs (Add inside class, call in _Ready)
	private void EnsureInputMaps()
	{
		if (!InputMap.HasAction("skill_1"))
		{
			InputMap.AddAction("skill_1");
			var qKey = new InputEventKey { Keycode = Key.Q };
			InputMap.ActionAddEvent("skill_1", qKey);
		}
		if (!InputMap.HasAction("skill_2"))
		{
			InputMap.AddAction("skill_2");
			var eKey = new InputEventKey { Keycode = Key.E };
			InputMap.ActionAddEvent("skill_2", eKey);
		}
		if (!InputMap.HasAction("dodge"))
		{
			InputMap.AddAction("dodge");
			var spaceKey = new InputEventKey { Keycode = Key.Space };
			InputMap.ActionAddEvent("dodge", spaceKey);
		}
	}
```

Make sure to call `EnsureInputMaps();` as the first line inside `_Ready()`.

---

### Task 2: Active Skill System (Hero Kit)

**Files:**
- Modify: `D:\gamedev\down\Scripts\Core\AttackSystem.cs`
- Modify: `D:\gamedev\down\Scripts\Core\PlayerController.cs`

**Interfaces:**
- Produces: `TrySkill1()`, `TrySkill2()`

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
				Modulate = Colors.White;
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

// Update ApplyMovement speed calculation:
	float speed = MoveSpeed * (sprinting ? SprintMult : 1f) * _buffSpeedMult;
```
