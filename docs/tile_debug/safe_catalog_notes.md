# Safe Undead Tile Catalog Notes

## Why `Level01_TileArena` Failed

`Scenes/Levels/Level01_TileArena.tscn` was generated from a guessed catalog. Many `Ground_rocks.png` atlas cells were treated as walkable floor even though the 64px debug sheet shows they are cliffs, transition fragments, partial edge pieces, or cells with large transparent holes. The result looked like repeated cliff wallpaper instead of a readable combat arena.

Treat `Level01_TileArena.tscn` as experimental and visually incorrect until a manually verified catalog exists.

## Why 64x64 Is the Working Grid

For the current AI-assisted workflow, `Ground_rocks.png` is treated as a `64 x 64` atlas because:

- `Assets/Tiles/Undead/UndeadGround.tres` is configured with `texture_region_size = Vector2i(64, 64)`.
- The source sheet art is organized around 64px terrain chunks.
- The debug sheet `docs/tile_debug/ground_rocks_grid_64.png` is readable enough for manual coordinate review.

The 16px and 32px preview sheets remain useful for understanding sub-tile artwork, but the safe catalog uses 64px atlas coordinates.

## Safe Groups

The new `Assets/Tiles/Undead/undead_tile_catalog_safe.json` is intentionally strict.

Currently safe:

- `floor_detail_small`: small overlay details from `Details.png`.

Currently empty until manually confirmed:

- `ground_plain`
- `ground_rough`
- `cliff_border_top`
- `cliff_border_bottom`
- `cliff_border_left`
- `cliff_border_right`
- `corner_outer`
- `corner_inner`

These empty groups are intentional. Empty is safer than promoting cliff or fragment cells into gameplay terrain.

## Uncertain Groups

The catalog keeps two provisional groups for visual review:

- `maybe_ground`: Ground_rocks cells that look partly floor-like but may be transition or edge art.
- `maybe_border`: Ground_rocks cells that may be border pieces but still contain transparency, holes, fragments, or unclear orientation.

These groups must not be used for automatic arena generation until reviewed in Godot.

## Probe Scene

Open `Scenes/Tests/UndeadTileProbe.tscn` and visually inspect each row before generating any new arena. The probe displays:

- Empty safe terrain rows where no reliable coordinate has been selected.
- `floor_detail_small` candidates from `Details.png`.
- `maybe_ground` and `maybe_border` candidates from `Ground_rocks.png`.

Only after the probe is checked should `undead_tile_catalog_safe.json` be updated with confirmed safe terrain coordinates and used for a replacement arena.
