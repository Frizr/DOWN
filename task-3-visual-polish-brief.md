# Task 3: Visual Polish (Juiciness)

## Task Description
Implement the visual polish items from Phase B:
1. Add drop shadows under the player and enemies. (You can spawn a dark, semi-transparent ellipse sprite or use Godot's built-in Drop Shadow techniques beneath the characters).
2. Add dust particles. (Create a simple CPUParticles2D that emits when the player is running or dodging).
3. Implement a proper Hit Flash Shader for enemies. Update `remove_white.gdshader` to include a `flash_amount` uniform, and update `EnemyBase.cs` `FlashWhite()` to tween or set this uniform instead of using `Modulate = Colors.White * 4f`.

## Constraints
- Do not use external assets (create CPUParticles2D programmatically or create a new `.tscn` / `.cs` for it).
- Ensure the hit flash shader still correctly removes the white background for the AI generated sprites.
- Make sure dust particles only emit when the player is actually moving/dodging.

## Expected Outcome
The combat feels significantly more "juicy" and premium.
