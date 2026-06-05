# Map Tile Workflow Notes

These notes summarize what the project should follow for readable Godot maps.

## Sources Studied

- Godot Using TileMaps: https://docs.godotengine.org/en/4.5/tutorials/2d/using_tilemaps.html
- Godot Using TileSets: https://docs.godotengine.org/en/stable/tutorials/2d/using_tilesets.html
- Godot TileMapLayer API: https://docs.godotengine.org/en/stable/classes/class_tilemaplayer.html
- Godot TileSetAtlasSource API: https://docs.godotengine.org/en/4.6/classes/class_tilesetatlassource.html
- Kenney Isometric Miniature Dungeon: https://kenney.nl/assets/isometric-miniature-dungeon

## Practical Conclusions

- Use `TileMapLayer` as the real map system. The current project still fakes some tilemap work by placing many `Sprite2D` nodes, which is readable enough for props but not ideal for floor/wall maps.
- Use separate layers:
  - `GroundLayer` for base floor.
  - `PathLayer` for roads/plaza.
  - `WallLayer` for bounds, walls, gates, and blocked silhouettes.
  - `DetailLayer` for cracks, rocks, bones, and visual noise.
  - `PropLayer` or YSort decorations for large objects and characters.
- Use a saved external `TileSet` resource instead of rebuilding tile data every run. This makes levels reusable and editable in Godot.
- For roads, cliffs, walls, and floor boundaries, use terrain/autotile where the asset sheet has full edge and corner coverage.
- If a tileset does not contain enough edge/corner variants, do not force terrain mode. Use hand-authored patterns or a simpler rectangular layout.
- Keep visual noise low near player spawn. Tile variation should support readability, not replace composition.
- Use collision/navigation data on tiles where possible instead of separate invisible collision rectangles for everything.

## What This Means For This Project

The best next step is to replace the runtime sprite-grid floor in `TilemapSetup.cs` with real `TileMapLayer` nodes backed by a `.tres` `TileSet` resource. The current generated arena can remain as a prototype, but the permanent version should be a proper tilemap:

1. Create `Assets/TileSets/arena_undead_tileset.tres`.
2. Add atlas sources for:
   - local `Assets/Tiles/Undead/Ground_rocks.png`
   - local `Assets/Tiles/CursedLand/Ground.png`
   - Kenney dungeon structural tiles where useful
3. Create scene layers under `World`:
   - `GroundTileMap`
   - `PathTileMap`
   - `CursedTileMap`
   - `WallTileMap`
   - `DetailTileMap`
4. Keep large props/NPCs in YSort, not inside floor tile layers.
5. Make the first map by painting or generating cells with `TileMapLayer.SetCell(...)`.

## Current Status

- Kenney Isometric Miniature Dungeon has been installed under `Assets/External/`.
- License is included in the imported folder.
- `TilemapSetup.cs` already references Kenney props for arena dressing.
- The project still needs a real reusable `TileSet` and `TileMapLayer` migration for clean map authoring.
