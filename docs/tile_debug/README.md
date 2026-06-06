# Undead Tile Debug Previews

`Scenes/Levels/Level01_TileArena.tscn` is experimental and is currently not visually correct. It should not be used as the active level or as proof that the tile catalog is valid.

The current `Assets/Tiles/Undead/undead_tile_catalog.json` was created before a reliable visual coordinate reference existed. Treat its categories as provisional until the preview images in this folder are manually inspected.

## Preview Images

These images preserve the source artwork visually, add a checkerboard behind transparent pixels, draw grid lines, and label cells as `(x,y)` from the top-left of each sheet.

| File | Source | Grid |
| --- | --- | --- |
| `ground_rocks_grid_16.png` | `Assets/Tiles/Undead/Ground_rocks.png` | `16 x 16` |
| `ground_rocks_grid_32.png` | `Assets/Tiles/Undead/Ground_rocks.png` | `32 x 32` |
| `ground_rocks_grid_64.png` | `Assets/Tiles/Undead/Ground_rocks.png` | `64 x 64` |
| `details_grid.png` | `Assets/Tiles/Undead/Details.png` | `64 x 64` |
| `objects_grid.png` | `Assets/Tiles/Undead/Objects.png` | `64 x 64` |

## Next Manual Pass

Inspect these grid previews and record safe coordinates for:

- walkable ground
- rough ground
- cliff top
- cliff face
- corners
- transitions
- decorative floor details

Only after that pass should `undead_tile_catalog.json` be corrected and a replacement arena be generated.
