using Godot;

/// <summary>
/// Shared prototype arena layout data used by systems that need a safe fallback
/// before TilemapSetup has generated scene nodes.
/// </summary>
public static class LevelLayoutData
{
	public static readonly Vector2 DefaultEnemySpawn = new(1040f, 360f);

	public static readonly Vector2[] FallbackEnemySpawns =
	{
		new(420f, 320f),
		new(800f, 192f),
		new(1080f, 380f),
		new(520f, 620f),
		new(960f, 640f),
		new(320f, 520f),
		new(1120f, 224f),
		new(1152f, 672f)
	};
}
