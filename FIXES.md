# DOWN Fixes

Date: 2026-06-02

## Build And Verification

- `dotnet build D:\gamedev\down\DOWN.sln` passes.
- Result: 0 errors, 0 warnings.
- Godot 4.6.2 .NET project starts successfully.

## Gameplay Fixes

### Health regen fixed

File:

- `Scripts/Core/Health.cs`

What was fixed:

- HP regen previously rounded per-frame healing to 0, so regen often did not work.
- Added a regen accumulator so fractional healing builds up over time.
- Regen accumulator resets after taking damage.

### NPC waypoint crash fixed

File:

- `Scripts/Core/NPCController.cs`

What was fixed:

- NPC patrol could crash if a waypoint path was missing.
- Missing waypoint paths are now handled safely.
- NPC idles and continues to the next waypoint instead of throwing an error.

### Multi-goblin waves enabled

Files:

- `Scripts/Core/LevelManager.cs`
- `Scenes/Main.tscn`

What was fixed:

- `LevelManager` was present but not fully active.
- `EnemyScene` is now assigned in `Main.tscn`.
- `LevelManager` can auto-load `res://Scenes/Enemy.tscn` as a fallback.
- Waves now auto-start after a short delay.
- Default wave size is 4 goblins.
- `MaxWaves = 0`, so waves continue after each wave is cleared.
- Added fallback spawn positions so goblins can spawn even without manual spawn point nodes.

### Arena and camera improved

Files:

- `Scripts/Core/TilemapSetup.cs`
- `Scenes/Main.tscn`
- `Scripts/Core/IsometricCamera.cs`

What was fixed:

- Camera was too zoomed in, making the arena feel cropped.
- Starting zoom is now wider.
- Arena background scale was increased.
- Preferred playable arena rectangle was expanded.
- Camera now clamps to the background bounds so moving sideways does not show empty/cut-off map edges.
- Follow-up fix: playable bounds now follow much more of the Undead background instead of the smaller manual rectangle.
- Follow-up fix: default camera zoom is now `0.7`, with a lower min zoom, so the visible area is wider and side movement does not feel immediately blocked.
- Follow-up fix: Undead background is now tiled 3x3 at runtime, so wide debug windows and camera movement do not reveal gray empty space around the single background image.
- Follow-up fix: camera is forced enabled/current-ready at runtime and snapped to the player after bounds are assigned.
- Follow-up fix: the Undead background center is now tied to the player spawn position, so the player cannot start outside the visible map.
- Follow-up fix: `Scenes/Main.tscn` now includes a fallback `BaseGround` sprite centered on the player spawn, so the editor/debug window has map art behind the player even before runtime setup replaces it with the 3x3 tiled background.
- Follow-up fix: camera and player bounds now use a smaller safe arena inside the Undead background, not the full outer edge of the 3x3 tiled background.
- Follow-up fix: camera zoom was tightened to reduce visible outside edge during movement.

### Player attack damage fixed

Files:

- `Scripts/Core/AttackSystem.cs`
- `Scenes/Player.tscn`
- `Scripts/Core/PlayerController.cs`

What was fixed:

- Attack animation could play without damage if the short collision window missed the goblin hurtbox.
- Player attack hitbox is now larger and active slightly longer.
- Added a directional melee fallback check against enemies near the player, so close-range attacks still apply damage even when the Area2D overlap is too strict.
- Follow-up fix: movement no longer immediately overwrites the attack animation while the player is attacking.
- Follow-up fix: attack input now faces the nearest close enemy before playing the sword animation, so the swing direction better matches the goblin being hit.
- Follow-up fix: added a visible `SlashVisual` line effect that appears in the active sword-hit direction while the hitbox is live.
- Follow-up fix: sword attack animations are no longer looped, so Godot can finish the animation cleanly.
- Follow-up fix: attack state now stays locked for nearly the full sword swing, so moving upward with `W` does not cancel the visible attack animation early.
- Follow-up fix: added custom generated slash and hit-impact spritesheets under `Assets/Generated/Effects`, then wired them into `AttackSystem` so sword swings and successful hits have dedicated animated effects without external assets.
- Follow-up fix: player movement now uses a dedicated attack-animation lock, so holding `W` or another movement key cannot override the sword animation until the attack finishes.
- Follow-up fix: the safe arena rectangle is now pulled further inside the Undead background, so camera/player bounds stop before the outer visual edge.
- Follow-up fix: removed the static `BaseGround` fallback from `Main.tscn`; runtime now owns the background tiles so duplicate ground/tilemap visuals do not appear.

### Death screen wired

Files:

- `Scenes/DeathScreen.tscn`
- `Scenes/Main.tscn`
- `Scripts/Core/DeathScreen.cs`

What was fixed:

- `DeathScreen.tscn` was almost empty.
- Added a working death screen UI with:
  - title label
  - final score label
  - high score label
  - restart button
  - menu button
- Death screen is now instanced in `Scenes/Main.tscn`.
- Script now checks whether animation exists before playing it.

### Player hurtbox fixed

File:

- `Scenes/Player.tscn`

What was fixed:

- Player `HurtBox` was positioned far away from the player body.
- Hurtbox is now centered near the visible player body.

## Documentation Added

Files:

- `IMPLEMENTATION_AUDIT.md`
- `FIXES.md`

What was added:

- `IMPLEMENTATION_AUDIT.md` documents audit findings, fixed issues, remaining notes, and next implementation queue.
- `FIXES.md` summarizes only what has been fixed.

## Still Worth Playtesting

- Confirm the new camera zoom feels good in the visible Godot window.
- Confirm goblin waves spawn at comfortable distances.
- Confirm death screen restart/menu works during manual play.
- Decide whether score should reward every hit, only kills, or both.

### Duplicate background/tilemap fixed

File:

- `Scripts/Core/TilemapSetup.cs`

What was fixed:

- Runtime background setup no longer creates a 3x3 grid of the same `Undead_land_background.png`.
- Any old `World/BaseGroundTiles` runtime root is removed if it still exists from a previous run.
- The arena now uses one centered `World/BaseGround` sprite and calculates bounds from that single background.
- Follow-up fix: playable bounds are now a smaller inner rectangle, while the camera can still use the full background area.
- Follow-up fix: `PlayerController` now clamps the player position to the playable rectangle after movement/dodge, so the player cannot slip outside even if collision bounds are late or missed.

### Multi-agent polish pass

Files:

- `Scripts/Core/TilemapSetup.cs`
- `Scripts/Core/LevelManager.cs`
- `Scripts/Core/PlayerController.cs`
- `Scripts/Core/GeneratedAudioFeedback.cs`
- `Scenes/Main.tscn`
- `Assets/Generated/Effects/`
- `Assets/Generated/Audio/`

What was fixed or improved:

- Project-manager audit identified map/background ownership, playable bounds, combat animation feel, enemy readability, and audio polish as the highest-priority lanes.
- Game developer pass made the background/playable bounds more authoritative from the real background rect, cleans duplicate background sprites, exposes active playable bounds, and makes wave fallback spawns respect the playable area.
- Player attack facing now only commits after `TryAttack()` succeeds, avoiding facing changes when the attack is still on cooldown.
- Player attack animation lock now has a reliable fallback timer so movement animations do not steal accepted sword attacks.
- Assets/VFX pass replaced the generated slash and hit-impact spritesheets in-place while preserving the `AttackSystem` frame contract.
- Sound pass added self-generated WAV SFX and an isolated `GeneratedAudioFeedback` node that listens to existing combat, health, enemy death, and wave signals.
- Final verification: `dotnet build D:\gamedev\down\DOWN.sln` passes with 0 errors and 0 warnings.

### Large generated map and level theme flow

Files:

- `Scripts/Core/TilemapSetup.cs`
- `Scripts/Core/LevelManager.cs`

What was fixed or improved:

- Replaced the single-background arena with a runtime `World/GeneratedMap` root.
- The playable area is now much larger: 3600x2400 world pixels with an inner collision/playable rectangle.
- The old broken `TileMap`, `BaseGround`, and `BaseGroundTiles` ownership path is cleared at runtime so the duplicate-map issue should not return.
- Undead is now the main biome at game start, using the existing Undead demo landmark plus many object sprites from `Assets/Tiles/Undead/Objects_separately`.
- Cursed Land is now generated as a separate biome layer using `Assets/Tiles/CursedLand/Objects_separetely`.
- Cursed Land starts hidden and fades in through `LevelManager` as waves rise, giving the game a clearer progression: undead graveyard first, then spreading cursed corruption.
- Spawn points are generated automatically around the larger arena, and wave spawning still respects playable bounds plus minimum player distance.
- Final verification: `dotnet build D:\gamedev\down\DOWN.sln` passes with 0 errors and 0 warnings.

Free asset candidates checked:

- Free Undead Pixel Tileset Top Down: https://free-game-assets.itch.io/free-undead-tileset-top-down-pixel-art
- CC1.0 Top Down Dungeon Tileset: https://quintino-pixels.itch.io/cc-10-top-down-dungeon-tileset
- Free top-down 16x16 pack: https://anokolisa.itch.io/dungeon-crawler-pixel-art-asset-pack

Current choice:

- No new asset was downloaded yet because the project already has matching Undead and CursedLand folders. The code now actually uses both.

### Animated CraftPix-style map pass

File:

- `Scripts/Core/TilemapSetup.cs`

What was fixed or improved:

- Map size was expanded again to 5200x3400 world pixels so the arena feels more like a real top-down level instead of a small test room.
- Removed the Undead demo screenshot as the main landmark path. The runtime map is now built from actual object and animation assets, not a pasted preview image.
- Added structured ground zones: main traversal lane, north graveyard, south ruins, and a cursed road preview.
- Added animated ambient landmarks from the downloaded Undead sheets:
  - `Animation2.png` and `Animation3.png` for moving dead-tree/root props.
  - `Animation4.png` for lich/necromancer shrine props.
  - `Animation5.png` for skull gate props.
  - `Animation6.png` for cursed sentinel/cultist props.
- Animated props are generated as `AnimatedSprite2D` nodes from 6-frame sprite-sheet rows and placed on a Y-sorted landmark layer.
- Static decoration density was increased with more ruins, graves, skull piles, thorns, rocks, and cursed objects.
- Final verification: `dotnet build D:\gamedev\down\DOWN.sln` passes with 0 errors and 0 warnings.

### Ground visual cleanup

File:

- `Scripts/Core/TilemapSetup.cs`

What was fixed:

- The generated map no longer relies on large semi-transparent rectangle patches for the visible ground, which made the level look like debug blocks.
- Ground is now built from atlas regions in the downloaded CraftPix sheets:
  - `Assets/Tiles/Undead/Ground_rocks.png`
  - `Assets/Tiles/Undead/Details.png`
  - `Assets/Tiles/CursedLand/Ground.png`
  - `Assets/Tiles/CursedLand/details.png`
- The polished `Assets/Tiles/Undead/Demo/Undead_land_background.png` is now used as the main starting-area backdrop, scaled behind the player so the first screen looks like the intended CraftPix reference.
- Tile-generated ground still extends outside the demo backdrop so the map remains large.
- Final verification: `dotnet build D:\gamedev\down\DOWN.sln` passes with 0 errors and 0 warnings.

### Dense first-screen map pass

File:

- `Scripts/Core/TilemapSetup.cs`

What was fixed:

- The first screen was still too empty because the visible base was mostly fallback color with sparse details.
- The Undead demo backdrop is now tiled across the generated map at native scale, so the whole arena has CraftPix-style ground detail instead of a flat green field.
- Undead ground/detail fill rates were increased, and cursed detail density was raised for later wave progression.
- Undead and cursed object counts were increased so the map reads as an actual graveyard/cursed land instead of scattered props on an empty plane.
- Final verification: `dotnet build D:\gamedev\down\DOWN.sln` passes with 0 errors and 0 warnings.

### Ground layer visibility fix

File:

- `Scripts/Core/TilemapSetup.cs`

What was fixed:

- The map still looked flat because `UndeadBaseFallback` was a `Polygon2D` at default `ZIndex = 0`, which covered the tiled CraftPix backdrop and ground sprites underneath it.
- The fallback color layer is now explicitly drawn at `ZIndex = -140`, behind the demo backdrop, ground tiles, detail tiles, decorations, and animated props.
- Final verification: `dotnet build D:\gamedev\down\DOWN.sln` passes with 0 errors and 0 warnings.

### Demo backdrop tiling rollback

File:

- `Scripts/Core/TilemapSetup.cs`

What was fixed:

- `Undead_land_background.png` was incorrectly repeated across the whole arena even though it is a complete demo scene image, not a seamless tile. That created the maze-like square pattern shown in the screenshot.
- The demo image is now placed once as the main first-screen backdrop at `1.65x` scale.
- Full-map `Ground_rocks` and cursed ground tile spam were removed from the first-screen build path because those atlas regions contain cliffs/walls and read as noisy blocks when scattered everywhere.
- Extra decoration and animated ambient counts were reduced so the map has readable set dressing instead of visual clutter.
- Final verification: `dotnet build D:\gamedev\down\DOWN.sln` passes with 0 errors and 0 warnings.

### Demo backdrop removed from runtime

File:

- `Scripts/Core/TilemapSetup.cs`

What was fixed:

- The runtime map no longer loads or references `Assets/Tiles/Undead/Demo/Undead_land_background.png`.
- The first-screen ground is now generated as a procedural undead/cursed soil texture named `ProceduralUndeadGround`, then decorated with the separate object and detail assets.
- This avoids both previous failures: an empty flat color field and a pasted CraftPix demo screenshot.
- Final verification: `dotnet build D:\gamedev\down\DOWN.sln` passes with 0 errors and 0 warnings.

### Compact arena pass

File:

- `Scripts/Core/TilemapSetup.cs`

What was fixed:

- The generated map was still too wide and sparse for the first viewport.
- Runtime map size was reduced from `5200x3400` to `1800x1100` world pixels.
- Camera zoom was tightened from `0.85x` to `1.15x`, so the player sees a denser arena instead of a huge empty field.
- Large circular ground blobs were replaced with smaller mottled patches to avoid obvious placeholder circles.
- Undead object density was increased around the playable arena, while key set pieces were placed by hand near the player for a clearer first-screen composition.
- Final verification: `dotnet build D:\gamedev\down\DOWN.sln` passes with 0 errors and 0 warnings.
