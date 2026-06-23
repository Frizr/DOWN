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
