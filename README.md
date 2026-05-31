# DOWN - 💀 Dark Isometric Arena Survival

![DOWN Banner](Assets/DOWN.png)

[![Godot Engine](https://img.shields.io/badge/Godot-4.x%20%28.NET%2F%23C%29-478CBF?style=for-the-badge&logo=godot-engine&logoColor=white)](https://godotengine.org)
[![Language](https://img.shields.io/badge/Language-C%23-green?style=for-the-badge&logo=c-sharp)](https://dotnet.microsoft.com)
[![Status](https://img.shields.io/badge/Status-In%20Development-orange?style=for-the-badge)](https://github.com/Frizr/DOWN)

A dark, high-paced top-down isometric 2D arena survival game built from the ground up using **Godot Engine 4.x (.NET/C#)**. Plunged into an ancient, decaying graveyard, your sole goal is to survive an endless onslaught of the undead.

Success demands mastering precision dodge rolls to exploit tight invincibility frames, chaining together 3-hit combo finishers, and keeping your combo streak alive long enough to multiply your score into the high-score stratosphere.

---

## 📸 Screenshots

![Gameplay Screenshot 1](Assets/screenshot1.png)

![Gameplay Screenshot 2](Assets/screenshot2.png)

---


## 🕹️ Controls Guide

Control your character using a standard keyboard and mouse setup, optimized for fast-paced action:

| Action | Input | Tactical Description |
| :--- | :--- | :--- |
| **Move** | `W` `A` `S` `D` | Walk fluidly across the map in 8 isometric directions. |
| **Sprint** | Hold `Left Shift` | Accelerate by **×1.65** to cover ground or escape encirclements. |
| **Dodge Roll** | `Space` | Roll in your moving direction. Grants **i-frames for 0.3 seconds**, making you completely immune to damage. Has a 0.8s cooldown. |
| **Attack** | `Left Mouse Click` | Swing your sword. Input chains a **3-hit combo** — the third hit deals heavy damage. |
| **Camera Zoom** | `Q` (in) / `E` (out) | Adjust camera zoom distance to scout the map or focus on combat. |
| **Pause** | `Esc` / `Tab` | Freeze the game with the tactical pause overlay. |

---

## ⚔️ Combat Mechanics (Deep Dive)

### 3-Hit Combo Attack Chain
Every left-click advances the combo step (1 → 2 → 3 → reset):
- **Hits 1 and 2 (Light):** Deal **15 damage** per hit with a **0.35s** cooldown between inputs.
- **Hit 3 (Heavy Finisher):** Deals **40 damage**, with a slower **0.6s** cooldown. This is the kill-shot — save it for committed targets.
- **Combo Window:** You have **0.55 seconds** after each hit to chain the next. Miss the window and the combo resets, forcing you to start from hit 1 again.
- **Directional Hitboxes:** The sword's attack `Area2D` physically shifts its position and shape depending on your facing direction:
  - Horizontal attack (left/right): `40×20px` rect, offset **±28px** on the X axis.
  - Vertical attack (up/down): `20×40px` rect, offset **±28px** on the Y axis.

### Hitstop (Freeze Frame Effect)
Every time your weapon connects with an enemy, the engine briefly freezes (`Engine.TimeScale = 0.05`) for **0.06 seconds** before restoring to normal speed. This micro-freeze is what makes hits feel impactful and satisfying — the same technique used in games like Dead Cells and Hades.

### Dodge Roll & I-Frames
- **i-Frame Window:** 0.3 seconds of full invincibility during the roll.
- **Direction:** You roll in the direction you are currently holding. If you are stationary, you roll in the direction you are facing.
- **Cooldown:** 0.8 seconds — you cannot spam-dodge.
- **Camera Feedback:** Every dodge triggers a subtle **camera shake** (intensity 4) to give the roll weight and physicality.

### Physics Hit Detection
The `HitBox` (an `Area2D`) activates for exactly **0.12 seconds** per swing. It checks for overlapping bodies and areas in the same physics frame and calls `TakeDamage()` on any enemy `Health` component found in the hit target's node tree. Each enemy can only be registered as a hit target **once per swing** — no double-dipping.

---

## 🔢 Score & Combo System

Managed by the `GameManager` global singleton:

| Combo Hits (streak) | Score Multiplier |
| :--- | :--- |
| 0 – 4 hits | ×1.0 (base) |
| 5 – 9 hits | ×1.5 |
| 10 – 19 hits | ×2.0 |
| 20+ hits | ×3.0 |

- Your combo **resets after 3 seconds** of no successful hits — you must keep pressure on enemies to maintain your multiplier.
- Taking any damage from an enemy also breaks your combo.
- When the run ends, if your score exceeds your saved best, the Death Screen displays **"NEW BEST"** and persists the high score to `user://save.cfg` using Godot's `ConfigFile` API. Otherwise it shows the classic **"YOU DIED"**.

---

## 🤖 Enemy AI System (Finite State Machine)

Enemy behavior is driven by `EnemyAI.cs`, a strict four-state FSM:

```
Idle ──── (detects player) ──────────────► Aggro
  ▲                                           │
  │                         (player escapes)  │
Patrol ◄── (no waypoints) ─────────────────── ◄─ (in melee range) ──► Attack
```

| State | Behavior |
| :--- | :--- |
| **Idle** | Stands in place. Scans for the player within `180px`. Transitions to Patrol if waypoints are set. |
| **Patrol** | Walks toward the next waypoint. Waits for **1.5s** at each stop. Immediately breaks into Aggro if the player enters range. |
| **Aggro** | Chases the player using `NavigationAgent2D` navmesh pathfinding. Recalculates path every **0.25s**. Loses track if player escapes beyond `280px`. |
| **Attack** | Stops moving and executes a melee strike if the cooldown has expired (default **1.2s**). Returns to Aggro if the player moves out of `AttackRadius` range. |

---

## 💀 Map Layout & Visual Props

The arena is a fully handcrafted `60×40` tile grid using the **Undead** tileset from Craftpix. The map layout is static (no procedural noise) to guarantee a clean, stable gameplay space:

* **Graveyard Zone (Top-Left):** A ruined archway, broken stone pillars, a central lich monument, moss-covered gravestones, and piled skulls. Sets the dark and cursed tone of the map.
* **Crystal Caves (Bottom-Right):** Clusters of glowing emerald crystals and twisted thorn plants. Used to visually mark the lower boundary of the arena.
* **Dead Forest (Bottom-Left):** Skeletal dead trees, large rock boulders, and snapped branches. Acts as the natural boundary of the left flank.
* **Battlefield Remnants (Top-Right & Outer Edges):** Giant ancient bone remains and scattered skeletal wreckage. Serves as the ruined perimeter fencing of the outer arena edge.
* **Open Center:** The absolute center is intentionally left free of visual obstacles, ensuring both the player spawn at `(30, 20)` and the enemy spawn at `(34, 21)` have a clear, immediately readable combat arena.

---

## ⚙️ Codebase Breakdown

Each script is tightly scoped to a single responsibility:

| Script | Role |
| :--- | :--- |
| `GameManager.cs` | Global autoload singleton. Owns game state (`Playing`, `Paused`, `GameOver`), score, combo multiplier tiers, and their timed decay. |
| `PlayerController.cs` | Reads `W/A/S/D` input, routes movement to `CharacterBody2D`, handles sprint multiplier, dodge roll velocity and i-frame grants, and connects signals for damage/death. |
| `EnemyBase.cs` | Blueprint `CharacterBody2D` shared by all enemy types. Handles velocity movement, damage flashing, knockback physics, and death signal emission. |
| `EnemyAI.cs` | 4-state FSM (`Idle → Patrol → Aggro → Attack`) using `NavigationAgent2D` for live pathfinding. Resolves player reference via group lookup. |
| `AttackSystem.cs` | Manages the 3-hit combo chain. Activates/deactivates the `HitBox` Area2D per swing, resolves `Health` components via ancestor tree walk, and triggers hitstop `Engine.TimeScale` freeze. |
| `Health.cs` | Stores current and max HP. Exposes `TakeDamage()` and `SetInvincible()` APIs. Emits `DamageTaken` and `Died` signals. |
| `IsometricCamera.cs` | Smooth-follows the player position. Applies procedural, exponentially-decaying screen shake on damage or dodge events. |
| `TilemapSetup.cs` | On `_Ready`, fills the TileMap with a single clean flat floor tile, then manually spawns all themed prop clusters as `Sprite2D` nodes under `World/Decorations`. |
| `DeathScreen.cs` | Listens for `GameManager.StateChanged` → `GameOver`. Reads final score, compares with persisted high score in `user://save.cfg`, animates cinematic entry (fade + scale), and handles Restart / Menu buttons. |
| `HUD.cs` | Receives `ScoreChanged` and `ComboChanged` signals from `GameManager`. Updates health bar, combo streak label, and multiplier indicator in real time. |

---

## 🚀 Getting Started

### Prerequisites
1. **Godot Engine 4.x (.NET / Mono Edition)** — the standard Godot build will not compile C# scripts.
2. **.NET SDK 6.0 or 8.0** installed on your system.
3. A C#-compatible editor such as **VS Code** (with the *C# Dev Kit* extension) or **Visual Studio 2022**.

### Setup & Run
1. Clone or download this repository to a local folder.
2. Open **Godot Engine (.NET Edition)** and import the project via `project.godot`.
3. Click the **Build** (hammer) button in the top-right corner to compile the C# assembly.
4. In the FileSystem tab, open `Scenes/Main.tscn`.
5. Press **F5** to launch. The game starts immediately in the arena.

---

## 📂 Project Directory

```bash
DOWN/
├── Assets/                  # Visual and audio assets
│   ├── Audio/               # SFX (.wav) & background music (.mp3)
│   ├── Sprites/             # Animated sprite sheets for Player & Enemy
│   ├── Tiles/               # Isometric floor & decoration tiles
│   └── reference.png        # In-game arena reference screenshot
├── Scenes/                  # Godot packed scene files (.tscn)
│   ├── Main.tscn            # Main game arena entry point
│   ├── Player.tscn          # Player CharacterBody2D and all sub-nodes
│   ├── Enemy.tscn           # Enemy AI agent with NavigationAgent2D
│   ├── HUD.tscn             # In-game health, score, and combo overlay
│   └── DeathScreen.tscn     # End-of-run score and highscore screen
├── Scripts/
│   └── Core/
│       ├── GameManager.cs       # Global singleton: states, score, combos
│       ├── PlayerController.cs  # Input, movement, dodge, sprint
│       ├── EnemyBase.cs         # Base class for all enemy types
│       ├── EnemyAI.cs           # FSM: Idle → Patrol → Aggro → Attack
│       ├── Health.cs            # HP, i-frames, and death signals
│       ├── AttackSystem.cs      # 3-hit chain, hitbox, hitstop
│       ├── IsometricCamera.cs   # Follow camera and screen shake
│       ├── TilemapSetup.cs      # Static floor + decoration spawning
│       ├── HUD.cs               # Live UI updates
│       └── DeathScreen.cs       # Game-over screen and score persistence
├── DOWN.csproj              # C# project configuration
└── project.godot            # Godot engine project file
```

---

## 📈 Development Roadmap

- [x] Top-down 8-directional movement with friction-based deceleration
- [x] Sprint and dodge roll with configurable i-frames
- [x] 3-hit melee combo chain with a heavy finisher
- [x] Directional hitboxes with physics-based hit detection
- [x] Hitstop freeze-frame on hit connection
- [x] Enemy FSM: Idle / Patrol / Aggro / Attack states
- [x] NavigationAgent2D pathfinding for enemy chasing
- [x] Combo multiplier system (×1.5 / ×2.0 / ×3.0) with 3s decay timer
- [x] Death screen with high score persistence (`user://save.cfg`)
- [x] Handcrafted undead arena map with themed prop clusters
- [ ] Audio: background ambient music and combat SFX
- [ ] Expanded enemy roster (ranged attackers, heavy brutes)
- [ ] Boss encounter with multi-phase attack patterns
- [ ] Multiple arena maps with unique visual themes
- [ ] Player ability upgrades between runs
