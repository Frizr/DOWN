# DOWN Implementation Audit

Date: 2026-06-02

## Scope

Reviewed the current Godot 4.6 .NET project structure, core C# scripts, scene wiring, and build health for the `DOWN` prototype.

## Verification

- Ran `dotnet build D:\gamedev\down\DOWN.sln`.
- Result: build succeeded with 0 errors and 0 warnings.

## Fixed

### Multi-goblin wave spawning is now active

Files:
- `Scripts/Core/LevelManager.cs`
- `Scenes/Main.tscn`

Problem:
- `LevelManager` existed in the scene but did not have `EnemyScene` assigned.
- `StartLevel()` was not called, so the game only had the single manually placed goblin.

Fix:
- `LevelManager` now auto-loads `res://Scenes/Enemy.tscn` if the scene is not assigned.
- `Scenes/Main.tscn` now assigns `EnemyScene`.
- Added `AutoStart` and `AutoStartDelay`.
- Added fallback spawn positions so waves work even before hand-authored spawn point nodes are added.
- Default wave size is now 4 goblins, with endless waves enabled by `MaxWaves = 0`.

### Camera and arena feel less cropped

Files:
- `Scripts/Core/TilemapSetup.cs`
- `Scenes/Main.tscn`

Problem:
- The camera started at a tight zoom, making the arena feel cut off.
- The preferred playable rect was smaller than the intended battle area.

Fix:
- Reduced starting camera zoom to show more of the arena.
- Increased the background scale slightly.
- Expanded the preferred arena rectangle.

### Death screen is now wired

Files:
- `Scenes/DeathScreen.tscn`
- `Scenes/Main.tscn`
- `Scripts/Core/DeathScreen.cs`

Problem:
- `DeathScreen.tscn` only had an empty root `CanvasLayer`.
- It was not instanced in `Scenes/Main.tscn`.
- The script assumed an animation existed and could throw if the animation was absent.

Fix:
- Built a working `DeathScreen.tscn` with labels and Restart/Menu buttons.
- Instanced it in `Scenes/Main.tscn`.
- Made animation playback optional in `DeathScreen.cs`.

### Player hurtbox was far away from the player

File: `Scenes/Player.tscn`

Problem:
- `HurtBox` and `HurtBoxShape` positions placed the player hurtbox far from the visible player body.

Fix:
- Recentered the hurtbox under the player body.

### Health regeneration never healed

File: `Scripts/Core/Health.cs`

Problem:
- Regen used `Mathf.RoundToInt(RegenPerSec * delta)`.
- At normal frame rates, `2 hp/sec * 0.016 sec` rounds to `0`, so regen never increases HP.

Fix:
- Added a fractional regen accumulator.
- HP now heals once enough fractional regen has accumulated into at least 1 full HP.
- Regen accumulation resets after taking damage.

### NPC waypoint lookup could crash

File: `Scripts/Core/NPCController.cs`

Problem:
- NPC patrol used `GetNode<Node2D>(Waypoints[_waypointIndex])`.
- If a waypoint path is missing or deleted, Godot throws and can stop the script.

Fix:
- Changed lookup to `GetNodeOrNull<Node2D>()`.
- Missing waypoints now make the NPC idle and advance to the next waypoint safely.

## Issues Found

### Death screen scene is incomplete

Files:
- `Scenes/DeathScreen.tscn`
- `Scripts/Core/DeathScreen.cs`
- `Scenes/Main.tscn`

Status: fixed.

Problem:
- `Scenes/DeathScreen.tscn` currently only contains a root `CanvasLayer`.
- It does not attach `DeathScreen.cs`.
- It does not contain the nodes required by the script: `Root`, labels, buttons, and `AnimationPlayer`.
- `Scenes/Main.tscn` does not instance the death screen scene.

Impact:
- The HUD can show a simple `GAME OVER` banner, but the full restart/menu death screen is not actually wired.

Recommended fix:
- Build the full `DeathScreen.tscn` node tree.
- Attach `Scripts/Core/DeathScreen.cs`.
- Instance it in `Scenes/Main.tscn`.

### LevelManager is present but not active

Files:
- `Scenes/Main.tscn`
- `Scripts/Core/LevelManager.cs`

Status: fixed.

Problem:
- `LevelManager` exists in `Scenes/Main.tscn`, but `EnemyScene` is not assigned.
- `StartLevel()` is not called anywhere in the current flow.
- `SpawnPoints` exists but contains no actual spawn point nodes.

Impact:
- Wave spawning will not run unless manually configured or triggered.
- The current prototype depends on the single pre-placed `Enemy` instance.

Recommended fix:
- Assign `EnemyScene = res://Scenes/Enemy.tscn`.
- Add spawn point children to `World/SpawnPoints` and the `spawn_point` group.
- Decide whether waves should start automatically or only after a trigger.

### Player HurtBox position looks wrong

File: `Scenes/Player.tscn`

Status: fixed.

Problem:
- `HurtBox` has `position = Vector2(73, 52)` and `HurtBoxShape` has `position = Vector2(-18, 45)`.
- The combined hurtbox is far away from the player body.

Impact:
- If enemy hitboxes use area collision later, player damage may miss or feel unfair.
- Current enemy damage is applied directly through `EnemyAI`, so this may not break the current prototype yet.

Recommended fix:
- Move `HurtBox` back to the player origin and keep the shape centered near the body.
- Then validate melee collision with visible collision shapes.

### Score logic may double-reward combat

Files:
- `Scripts/Core/PlayerController.cs`
- `Scripts/Core/EnemyBase.cs`

Observation:
- `PlayerController.OnHitConnected()` awards `damage * 10` on every hit.
- `EnemyBase.OnDied()` also awards `KillScore`.

Impact:
- This may be intended as hit score plus kill score.
- If score should only reward kills, this is inflated.

Recommended fix:
- Decide the design: hit score, kill score, or both.
- If both are intended, rename/comment the hit reward so it is clear.

## Next Implementation Queue

1. Playtest camera zoom and arena bounds in the visible Godot window.
2. Clarify score rules, then adjust score rewards if needed.
3. Add real spawn point nodes to `World/SpawnPoints` once the final arena layout is locked.
4. Add more enemy variants using the existing goblin sprite sheets.
5. Add a quick in-game smoke test checklist for movement, attack, damage, death, restart, and wave spawning.
