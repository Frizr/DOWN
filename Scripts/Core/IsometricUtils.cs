using Godot;

/// <summary>
/// IsometricUtils — Agent 1: Isometric Engine
/// Pure-static coordinate conversion helpers.
/// 
/// Coordinate systems used in this project:
///   World  — flat 2D logical grid space (X = right, Y = down)
///   Screen — actual pixel position on screen (after isometric projection)
///   Tile   — integer grid column / row (col, row)
///
/// Tile dimensions (edit these to match your art):
///   TILE_WIDTH  = 64 px  (full diamond width)
///   TILE_HEIGHT = 32 px  (full diamond height, 2:1 ratio)
/// </summary>
public static class IsometricUtils
{
    // ─── Tile Constants ───────────────────────────────────────────────────────

    /// <summary>Full pixel width of one isometric tile diamond.</summary>
    public const float TileWidth  = 64f;

    /// <summary>Full pixel height of one isometric tile diamond (2:1 ratio = TileWidth / 2).</summary>
    public const float TileHeight = 32f;

    /// <summary>Half width — used in all projection formulas.</summary>
    public const float HalfW = TileWidth  / 2f;   // 32

    /// <summary>Half height — used in all projection formulas.</summary>
    public const float HalfH = TileHeight / 2f;   // 16

    // ─── World ↔ Screen ───────────────────────────────────────────────────────

    /// <summary>
    /// Convert a flat world position to isometric screen pixel position.
    /// Use this when you need to place a sprite at a logical world coordinate.
    /// 
    /// Formula (standard 2:1 dimetric projection):
    ///   screen.x = (world.x - world.y) * HalfW
    ///   screen.y = (world.x + world.y) * HalfH
    /// </summary>
    public static Vector2 WorldToScreen(Vector2 worldPos)
    {
        return new Vector2(
            (worldPos.X - worldPos.Y) * HalfW,
            (worldPos.X + worldPos.Y) * HalfH
        );
    }

    /// <summary>
    /// Convert an isometric screen pixel position back to flat world space.
    /// Use this for mouse picking / click-to-move.
    ///
    /// Inverse of WorldToScreen:
    ///   world.x = (screen.x / HalfW + screen.y / HalfH) / 2
    ///   world.y = (screen.y / HalfH - screen.x / HalfW) / 2
    /// </summary>
    public static Vector2 ScreenToWorld(Vector2 screenPos)
    {
        return new Vector2(
            (screenPos.X / HalfW + screenPos.Y / HalfH) / 2f,
            (screenPos.Y / HalfH - screenPos.X / HalfW) / 2f
        );
    }

    // ─── Tile ↔ World ─────────────────────────────────────────────────────────

    /// <summary>
    /// Convert an integer tile grid coordinate (col, row) to world space.
    /// Tiles are unit squares in world space, so this is just a cast.
    /// Pass the result to WorldToScreen to get pixel coords.
    /// </summary>
    public static Vector2 TileToWorld(Vector2I tilePos)
    {
        return new Vector2(tilePos.X, tilePos.Y);
    }

    /// <summary>
    /// Convert a world-space position to the nearest tile grid coordinate.
    /// Uses floor() so partial positions snap to the tile that contains them.
    /// </summary>
    public static Vector2I WorldToTile(Vector2 worldPos)
    {
        return new Vector2I(Mathf.FloorToInt(worldPos.X), Mathf.FloorToInt(worldPos.Y));
    }

    // ─── Convenience Shortcuts ────────────────────────────────────────────────

    /// <summary>
    /// Directly convert a tile grid coordinate to screen pixel position.
    /// Equivalent to WorldToScreen(TileToWorld(tilePos)).
    /// </summary>
    public static Vector2 TileToScreen(Vector2I tilePos)
    {
        return WorldToScreen(TileToWorld(tilePos));
    }

    /// <summary>
    /// Convert a screen pixel position to the nearest tile grid coordinate.
    /// Useful for mouse hover highlighting.
    /// </summary>
    public static Vector2I ScreenToTile(Vector2 screenPos)
    {
        return WorldToTile(ScreenToWorld(screenPos));
    }

    // ─── Depth / Y-Sort ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns a Y-sort value for a sprite at a given world position.
    /// Higher Y-sort = drawn on top (further "south" in the isometric view).
    /// Feed this to Node2D.ZIndex or YSort node ordering.
    /// </summary>
    public static float GetDepth(Vector2 worldPos)
    {
        // Sum of X+Y gives the rendering depth in standard isometric projection
        return worldPos.X + worldPos.Y;
    }

    // ─── Debug Helper ─────────────────────────────────────────────────────────

    /// <summary>
    /// Print coordinate round-trip to the Godot Output panel.
    /// Handy for verifying that conversions are lossless.
    /// </summary>
    public static void DebugRoundTrip(Vector2 worldPos)
    {
        Vector2 screen    = WorldToScreen(worldPos);
        Vector2 recovered = ScreenToWorld(screen);
        GD.Print($"[IsoUtils] World {worldPos} → Screen {screen} → Recovered {recovered}");
    }
}
