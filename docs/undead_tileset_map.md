# Undead Tileset Map

This catalog is an inspection pass for `Assets/Tiles/Undead`. It is not a level
generation plan. Coordinates use atlas cell coordinates `[col, row]`, counted
from the top-left of the source image. Pixel regions are derived as:

`x = col * 64`, `y = row * 64`, `w = 64`, `h = 64`.

## Effective Tile Size

The effective terrain tile size is `64 x 64` pixels.

Observed sheet dimensions:

| Sheet | Dimensions | 64 px grid note |
| --- | ---: | --- |
| `Ground_rocks.png` | `496 x 592` | Full cells are columns `0-6`, rows `0-8`. Column `7` is partial width, row `9` is partial height. |
| `Details.png` | `576 x 176` | Full cells are columns `0-8`, rows `0-1`. Row `2` is partial height. |
| `Objects.png` | `768 x 704` | Full 64 px grid, but this sheet is props/objects, not terrain topology. |
| `Water_coasts.png` | `1056 x 256` | Water/coast sheet, not categorized in this terrain catalog yet. |
| `water_detilazation.png` | `688 x 576` | Water detail sheet, not categorized in this terrain catalog yet. |
| `water_detilazation_v2.png` | `688 x 576` | Water detail sheet, not categorized in this terrain catalog yet. |

## Important Finding

`Ground_rocks.png` does not contain a strong set of full, clean, plain walkable
ground cells. Most cells are cliffs, cliff caps, edge transitions, or terrain
chunks that include cliff faces. Previous procedural attempts treated some of
these as ordinary floor tiles, which explains the unstructured results.

For later generation, use the confident groups below only for their named role.
Do not use cliff face or transition cells as plain floor.

## TileSet Resource

Created TileSet resource:

`res://Assets/Tiles/Undead/UndeadGround.tres`

This TileSet uses `res://Assets/Tiles/Undead/Ground_rocks.png` as atlas source
`0`, with `texture_region_size = Vector2i(64, 64)` and `tile_size =
Vector2i(64, 64)`. It defines the full 64 px cells from columns `0-6` and rows
`0-8`. It intentionally excludes partial atlas cells in column `7` and row `9`.

Painting intent for source `0`:

| Intent | Atlas coordinates |
| --- | --- |
| Floor painting | No confident plain floor cells yet. Use only human-confirmed `maybe.ground_plain` cells until a better floor atlas is identified. |
| Cliff placement | `cliff_top`: `[2,0]`, `[5,0]`, `[6,0]`, `[4,1]`, `[5,1]`, `[5,2]`, `[6,2]`; `cliff_face`: `[0,4]`, `[1,4]`, `[2,4]`, `[3,4]`, `[4,4]`, `[5,4]`, `[6,4]`, `[0,6]`, `[1,6]`, `[2,6]`, `[3,6]`, `[4,6]`, `[5,6]`, `[6,6]`, `[0,7]`, `[1,7]`, `[2,7]`, `[3,7]`, `[4,7]`, `[5,7]`, `[6,7]`, `[0,8]`, `[1,8]`, `[2,8]`, `[3,8]`, `[4,8]`, `[5,8]`, `[6,8]` |
| Edge/corner placement | `corner_outer`: `[0,0]`, `[4,0]`, `[0,1]`, `[4,2]`; `corner_inner`: `[2,1]`, `[3,1]`, `[1,3]`, `[2,3]`; `transition`: `[0,2]`, `[1,2]`, `[2,2]`, `[3,2]`, `[4,2]`, `[0,3]`, `[4,3]`, `[5,3]`, `[0,5]`, `[2,5]`, `[3,5]`, `[6,5]` |

## Confident Terrain Groups

### Ground Plain

No confident full-cell plain ground tiles were found in `Ground_rocks.png`.

### Ground Rough

No confident standalone rough walkable ground tiles were found. Several cells
show rough top surfaces, but they are tied to cliff transitions and are listed
under `maybe`.

### Ground Dark

No confident dark walkable ground tiles were found. The dark/black areas in the
grid preview are mostly transparency or cliff-face art, not a floor surface.

### Cliff Top

These cells appear to be cliff-top or cliff-lip pieces:

| Sheet | Coordinates |
| --- | --- |
| `Ground_rocks.png` | `[2,0]`, `[5,0]`, `[6,0]`, `[4,1]`, `[5,1]`, `[5,2]`, `[6,2]` |

### Cliff Face

These cells are reliable vertical cliff/wall face pieces:

| Sheet | Coordinates |
| --- | --- |
| `Ground_rocks.png` | `[0,4]`, `[1,4]`, `[2,4]`, `[3,4]`, `[4,4]`, `[5,4]`, `[6,4]`, `[0,6]`, `[1,6]`, `[2,6]`, `[3,6]`, `[4,6]`, `[5,6]`, `[6,6]`, `[0,7]`, `[1,7]`, `[2,7]`, `[3,7]`, `[4,7]`, `[5,7]`, `[6,7]`, `[0,8]`, `[1,8]`, `[2,8]`, `[3,8]`, `[4,8]`, `[5,8]`, `[6,8]` |

### Outer Corners

These cells look like convex cliff or cap corners:

| Sheet | Coordinates |
| --- | --- |
| `Ground_rocks.png` | `[0,0]`, `[4,0]`, `[0,1]`, `[4,2]` |

### Inner Corners

These cells look like concave cliff notches:

| Sheet | Coordinates |
| --- | --- |
| `Ground_rocks.png` | `[2,1]`, `[3,1]`, `[1,3]`, `[2,3]` |

### Transitions

These cells combine walkable-looking top surface with cliff edge/face material.
They should only be used as edge/transition pieces, not as plain floor fill:

| Sheet | Coordinates |
| --- | --- |
| `Ground_rocks.png` | `[0,2]`, `[1,2]`, `[2,2]`, `[3,2]`, `[4,2]`, `[0,3]`, `[4,3]`, `[5,3]`, `[0,5]`, `[2,5]`, `[3,5]`, `[6,5]` |

### Floor Details

`Details.png` is a decal/detail sheet. These cells are suitable as overlay
details on top of a valid ground layer:

| Sheet | Coordinates |
| --- | --- |
| `Details.png` | `[0,0]`, `[1,0]`, `[2,0]`, `[3,0]`, `[4,0]`, `[5,0]`, `[6,0]`, `[7,0]`, `[8,0]`, `[0,1]`, `[1,1]`, `[2,1]`, `[3,1]`, `[4,1]`, `[5,1]`, `[6,1]`, `[7,1]`, `[8,1]` |

## Maybe / Needs Human Confirmation

These are not safe enough for automatic use yet.

| Category | Sheet | Coordinates | Reason |
| --- | --- | --- | --- |
| `ground_plain` | `Ground_rocks.png` | `[1,1]`, `[0,3]`, `[1,3]` | Contains broad flat-looking surface, but also edge/cliff content. |
| `ground_rough` | `Ground_rocks.png` | `[0,2]`, `[1,2]`, `[2,3]`, `[3,3]`, `[4,3]`, `[5,3]`, `[6,3]` | Rough surface is visible, but cells are not clean standalone fill tiles. |
| `ground_dark` | `Ground_rocks.png` | `[4,5]`, `[5,5]` | Dark material may be shadow or non-walkable cliff art. |
| `cliff_top` | `Ground_rocks.png` | `[1,0]`, `[3,0]`, `[1,5]`, `[4,5]` | Looks like edge/cap material but orientation is ambiguous. |
| `corner_outer` | `Ground_rocks.png` | `[6,1]`, `[6,3]` | Could be cap/corner pieces or standalone rocky masses. |
| `corner_inner` | `Ground_rocks.png` | `[3,3]`, `[5,3]` | Concavity is plausible but not certain. |
| `transition` | `Ground_rocks.png` | `[6,1]`, `[6,2]`, `[2,5]`, `[4,5]`, `[5,5]` | Mixed material; needs editor validation. |
| `floor_detail` | `Details.png` | `[0,2]`, `[1,2]`, `[2,2]`, `[3,2]`, `[4,2]`, `[5,2]`, `[6,2]`, `[7,2]`, `[8,2]` | Bottom row is only 48 px tall in the source sheet. |

## Object Sheets

`Objects.png` and `Objects_separately/` are prop/decor sources, not base terrain
cells. The separated folder is more reliable for manual or scripted prop
placement because names indicate object family, such as:

- `Bones_*`
- `Grave_*`
- `Rock_*`
- `Ruin_*`
- `Tree_*`
- `Dead_tree_*`
- `Broken_tree_*`
- `Plant_*`
- `Thorn_plant_*`
- `Crystal_*`
- `Lich_*`
- `Scull_door_*`

Do not treat `Objects.png` atlas cells as terrain categories without a separate
object placement catalog.
