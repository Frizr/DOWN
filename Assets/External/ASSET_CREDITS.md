# External Asset Credits

This folder contains third-party assets that are safe to use in this project.

## Kenney Isometric Miniature Dungeon

- Folder: `Assets/External/KenneyIsometricMiniatureDungeon/`
- Source: https://kenney.nl/assets/isometric-miniature-dungeon
- License: Creative Commons Zero 1.0 Universal (CC0)
- License file in project: `Assets/External/KenneyIsometricMiniatureDungeon/License.txt`
- Imported zip kept for traceability: `Assets/External/kenney_isometric-miniature-dungeon.zip`

Current project usage:

- `Scripts/Core/TilemapSetup.cs` uses Kenney dungeon props for readable arena structure:
  - west stone gate
  - shrine door
  - broken ruin wall
  - plaza half-walls
  - columns
  - chest, barrels, and crates for the guard post

Design note:

The existing local Undead and CursedLand assets still provide the main undead theme. Kenney assets are currently used as clean structural set dressing because their isometric pieces read more clearly as walls, gates, and dungeon props.
