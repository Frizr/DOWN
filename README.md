# `D O W N` — ☠️Isometric Combat Purgatory☠️

![DOWN Banner](Assets/DOWN.png)

[![Godot Engine](https://img.shields.io/badge/Godot-4.x%20%28.NET%2F%23C%29-478CBF?style=for-the-badge&logo=godot-engine&logoColor=white)](https://godotengine.org)
[![Project Status](https://img.shields.io/badge/Status-Under%20Development-orange?style=for-the-badge&logo=gitkraken)](https://github.com)
[![Language](https://img.shields.io/badge/Language-C%23-green?style=for-the-badge&logo=c-sharp)](https://dotnet.microsoft.com)

> [!CAUTION]
> **DEVELOPMENT STATUS: ACTIVE WORK IN PROGRESS**
>
> This game is under active, intensive development. The core isometric physics engine, player movement mechanics, basic enemy AI, real-time combat system, and game state architecture have been fully implemented. However, visual assets, level design, and overall polish are currently being expanded.

---

## 📖 Overview: Descend Into the Void

**DOWN** is a dark, punishing 2D isometric action-combat game developed with the **Godot Engine 4.x (C# / .NET)**. Navigating a bleak, mathematically projected isometric purgatory, players must engage in quick tactical combat, execute frame-perfect dodge rolls with invincibility frames (i-frames), and stack up high scores by chaining together combos in an intense arena survival experience. 

It is a project designed to showcase high-performance C# scripting in Godot, decoupling patterns, and responsive game-feel implementation.

---

## technical Highlights 

This project was built with a strong focus on clean architecture, performance, and satisfying gameplay mechanics. Below are the key engineering highlights:

*   **Custom Isometric Vector Math**: Implemented a standalone 2D-to-isometric coordinate projection system (`IsometricUtils.cs`), avoiding external dependencies and showcasing custom vector manipulation.
*   **State Machine Architecture (FSM)**: Designed a modular and decoupled Finite State Machine for enemy AI (`EnemyAI.cs` & `EnemyBase.cs`), separating patrol, chase, and telegraphed combat states cleanly.
*   **Event-Driven Systems**: Leveraged C# Action delegates (`System.Action`) to handle player damage, enemy deaths, and score multiplier triggers. This keeps the HUD and `GameManager` loosely coupled from the physics/combat entities.
*   **Invincibility-Frame (i-Frame) Engine**: Developed a frame-perfect invincibility system (`Health.cs`) linked with the player's dodge roll to allow precision evasion.
*   **Screen Shake & Feedback Loops**: Programmed procedural, decay-based camera shake (`IsometricCamera.cs`) that scales dynamically based on the damage dealt or received.

---

## ⚡ Core Features

1. **Isometric Engine & Custom Projection**
   - **8-Directional Movement**: Flat WASD/arrow key inputs are seamlessly projected onto a custom 2.5D isometric screen-space coordinate system.
   - **Dynamic Camera**: Smoothly tracks player movement and responds dynamically with camera shake effects upon hitting or receiving damage.

2. **Fluid Movement & Dodge Mechanics**
   - **Friction-Based Deceleration**: Controls feel heavy yet responsive, utilizing friction-based sliding for realistic weight.
   - **Sprint**: Hold Shift to accelerate across the battlefield.
   - **Dodge Roll**: Active invincibility frames (i-frames) allow players to phase through enemy attacks safely.

3. **Dynamic Combat & Combo System**
   - **Precise Hitbox/Hurtbox**: Real-time overlapping checking for accurate weapon swings and damage detection.
   - **Combo Multipliers**: Fast and aggressive play is rewarded. Keep your hit streak alive without getting damaged or timing out to trigger multiplier tiers (×1.5, ×2.0, and up to ×3.0 score rewards).

4. **Enemy AI (Grunt System)**
   - **Finite State Machine (FSM)**: Enemies patrol, detect, chase, and choreograph visual telegraph warnings before attacking the player.

5. **Tactical Game Manager**
   - **Autoload Singleton**: Features global game states (`MainMenu`, `Playing`, `Paused`, `GameOver`) allowing smooth transition phases, scoring records, and pausing mechanics.

---

## 📂 Project Structure

```bash
DOWN/
├── .godot/                  # Godot internal cache and metadata
├── Assets/                  # Visual and audio assets
│   ├── Audio/               # Sound effects (.wav) and music (.mp3)
│   ├── Sprites/             # Player, Enemy, and visual sprites
│   └── Tiles/               # Map tiles for the isometric grid layout
├── Levels/                  # Game levels and arenas
├── Scenes/                  # Packed scene files (.tscn)
│   ├── Main.tscn            # The main game arena and loop
│   ├── Player.tscn          # Player node and scripts
│   ├── Enemy.tscn           # Enemy Base node
│   ├── HUD.tscn             # Head-Up Display (Health Bar, Combo, Score UI)
│   └── DeathScreen.tscn     # GameOver overlay screen
├── Scripts/                 # C# source code files
│   ├── Audio/               # Future Audio Manager expansion folder
│   ├── Player/              # Future Player-specific scripting folder
│   ├── Enemy/               # Future Enemy sub-type scripting folder
│   ├── UI/                  # Future UI/Menu scripting folder
│   └── Core/                # CORE ENGINE LOGIC & SYSTEMS
│       ├── GameManager.cs       # Singleton managing game states, combo, & scores
│       ├── PlayerController.cs  # Player input, isometric movement, & dodge roll
│       ├── EnemyBase.cs         # Blueprint template for enemy nodes
│       ├── EnemyAI.cs           # State machine controlling patrol, chase, & attack behavior
│       ├── EnemyGrunt.cs        # Grunt enemy logic overrides
│       ├── Health.cs            # HP, damage mechanics, and invincibility triggers
│       ├── AttackSystem.cs      # Attack routing, timing, and hitbox checks
│       ├── CombatTrigger.cs     # Area2D-based attack intersection logic
│       ├── IsometricCamera.cs   # Camera tracking and procedural shaking
│       ├── IsometricUtils.cs    # Coordinate transform calculations (2D <-> Iso)
│       ├── LevelManager.cs      # Spawning waves, difficulty progression, & level changes
│       ├── TilemapSetup.cs      # Renders and initializes the isometric grid map
│       ├── PlaceholderSetup.cs  # Direct 2D node draw commands for temporary graphics
│       ├── HUD.cs               # Live status UI updater
│       └── DeathScreen.cs       # Post-game screen behavior and reset options
├── DOWN.csproj              # C# / Mono project configuration
├── DOWN.sln                 # IDE Solution file (VS Code / Visual Studio)
└── project.godot            # Main Godot project configuration
```

> [!NOTE]
> Directory structures under `Scripts/Player`, `Scripts/Enemy`, `Scripts/UI`, and `Scripts/Audio` are placeholders reserved for refactoring and code organization as features scale out of the main `Core/` loop.

---

## 🚀 Getting Started

### Prerequisites
1. **Godot Engine 4.x (.NET / Mono Edition)**: Standard Godot version will not compile the C# code. Make sure you use the .NET edition.
2. **.NET SDK (6.0 or 8.0)** installed on your machine.
3. A C#-supported editor (e.g., **VS Code** with *C# Dev Kit* or **Visual Studio 2022**).

### Installation & Run Steps
1. Clone or download this repository.
2. Open **Godot Engine (.NET Edition)**.
3. Import the project by navigating to the cloned directory and selecting `project.godot`.
4. **Build the C# Solution**: Click the **Build** button in the top-right corner of the Godot editor.
5. Open `Scenes/Main.tscn` and press **F5** (or click the Play button) to start playing!

---

## 🕹️ Controls Layout

| Action | Keyboard / Mouse Inputs | Description |
| :--- | :--- | :--- |
| **Move** | `W` `A` `S` `D` / `Arrow Keys` | Move character in 8 isometric directions |
| **Sprint** | Hold `Left Shift` | Boost movement speed |
| **Attack** | `Left Click` / `Space` | Attack in the character's facing direction |
| **Dodge Roll** | `Space` / Dodge Button | Roll forward with temporary invincibility (*i-frames*) |
| **Tactical Pause** | `Esc` / `Tab` | Toggle pause menu overlay |

---

## 📈 Development Roadmap

- [x] Core Isometric Projection Engine (2.5D coordinate mapping)
- [x] Enemy AI Finite State Machine (Patrol & Chase behavior)
- [x] Action Combat System (Hitbox setup, combo calculations, and i-frames)
- [ ] Visual Assets Integration (Replace code-drawn placeholders with sprite art)
- [ ] Isometric Tilemap Design & Level Hazards
- [ ] Audio Systems (Interactive background music & combat SFX)
- [ ] Diversified Enemy Roster (Ranged archers, Heavy brutes, Boss encounters)
