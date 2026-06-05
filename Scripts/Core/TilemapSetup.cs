using Godot;

[GlobalClass]
public partial class TilemapSetup : Node
{
	private const string WorldPath = "World";
	private const string TileMapPath = "World/TileMap";
	private const string GeneratedMapNodeName = "GeneratedMap";
	private const string LegacyArenaBoundsPath = "World/ArenaBounds";
	private const string ManualLevelPath = "World/Level01";
	private const string ManualPlayerSpawnPath = "World/Level01/SpawnPoints/PlayerSpawn";
	private const string ManualEnemySpawnPath = "World/Level01/SpawnPoints/EnemySpawn";
	private const string CameraPath = "Camera";

	private const uint PlayerCollisionLayer = 1;
	private const uint ArenaCollisionLayer = 4;
	private const int ArenaWallThickness = 96;

	private static readonly Vector2 DefaultPlayerSpawn = new(640f, 420f);
	private static readonly Vector2 DefaultEnemySpawn = new(920f, 420f);
	private static readonly Vector2 CameraZoom = new(2.05f, 2.05f);
	private static readonly Rect2 ManualFallbackArenaRect = new(new Vector2(160f, 120f), new Vector2(960f, 560f));

	private static bool _hasActivePlayableBounds = false;
	private static Rect2 _activePlayableBounds = ManualFallbackArenaRect;

	private Rect2 _playableBounds = ManualFallbackArenaRect;
	private Rect2 _cameraBounds = ManualFallbackArenaRect;

	public override void _Ready()
	{
		DisableLegacyVisuals();
		ResolvePlayableBounds();
		EnsureSimpleBoundsIfNeeded();
		RegisterManualSpawnPoints();
		SetupPlayerEnemyPositions();
		SetupCameraFocus();
	}

	public static bool TryGetActivePlayableBounds(out Rect2 bounds)
	{
		bounds = _activePlayableBounds;
		return _hasActivePlayableBounds;
	}

	public void ApplyWaveTheme(int wave)
	{
		// Manual levels own their visuals; waves no longer mutate generated map layers.
	}

	private void DisableLegacyVisuals()
	{
		var tileMap = GetNodeOrNull<TileMapLayer>(TileMapPath)
			?? FindChild("TileMap", true, false) as TileMapLayer;

		if (tileMap != null)
		{
			tileMap.Clear();
			tileMap.Visible = false;
		}

		var world = GetNodeOrNull<Node2D>(WorldPath);
		if (world == null)
			return;

		world.GetNodeOrNull<Node>("BaseGround")?.QueueFree();
		world.GetNodeOrNull<Node>("BaseGroundTiles")?.QueueFree();
		world.GetNodeOrNull<Node>(GeneratedMapNodeName)?.QueueFree();
	}

	private void ResolvePlayableBounds()
	{
		var level = GetManualLevel();
		if (level != null && TryGetPlayableRectFromBounds(level.GetNodeOrNull<Node2D>("Bounds"), out Rect2 manualBounds))
		{
			_playableBounds = manualBounds;
			_cameraBounds = manualBounds;
		}
		else
		{
			_playableBounds = ManualFallbackArenaRect;
			_cameraBounds = ManualFallbackArenaRect;
		}

		_activePlayableBounds = _playableBounds;
		_hasActivePlayableBounds = true;
	}

	private void EnsureSimpleBoundsIfNeeded()
	{
		var boundsRoot = GetManualLevel()?.GetNodeOrNull<Node2D>("Bounds");
		if (boundsRoot == null)
			boundsRoot = GetOrCreateFallbackBoundsRoot();

		if (boundsRoot == null || HasStaticBounds(boundsRoot))
			return;

		AddArenaWall(boundsRoot, "TopWall",
			new Vector2(_playableBounds.GetCenter().X, _playableBounds.Position.Y - ArenaWallThickness * 0.5f),
			new Vector2(_playableBounds.Size.X + ArenaWallThickness * 2f, ArenaWallThickness));
		AddArenaWall(boundsRoot, "BottomWall",
			new Vector2(_playableBounds.GetCenter().X, _playableBounds.End.Y + ArenaWallThickness * 0.5f),
			new Vector2(_playableBounds.Size.X + ArenaWallThickness * 2f, ArenaWallThickness));
		AddArenaWall(boundsRoot, "LeftWall",
			new Vector2(_playableBounds.Position.X - ArenaWallThickness * 0.5f, _playableBounds.GetCenter().Y),
			new Vector2(ArenaWallThickness, _playableBounds.Size.Y + ArenaWallThickness * 2f));
		AddArenaWall(boundsRoot, "RightWall",
			new Vector2(_playableBounds.End.X + ArenaWallThickness * 0.5f, _playableBounds.GetCenter().Y),
			new Vector2(ArenaWallThickness, _playableBounds.Size.Y + ArenaWallThickness * 2f));
	}

	private void RegisterManualSpawnPoints()
	{
		var enemySpawn = GetNodeOrNull<Node2D>(ManualEnemySpawnPath);
		enemySpawn?.AddToGroup("spawn_point");
	}

	private void SetupPlayerEnemyPositions()
	{
		Vector2 playerSpawn = GetSpawnPosition(ManualPlayerSpawnPath, DefaultPlayerSpawn);
		Vector2 enemySpawn = GetSpawnPosition(ManualEnemySpawnPath, DefaultEnemySpawn);

		var player = FindChild("Player", true, false) as Node2D;
		if (player != null)
		{
			player.GlobalPosition = playerSpawn;
			player.AddToGroup("player");

			if (player is PlayerController playerController)
				playerController.SetPlayableBounds(_playableBounds);

			if (player is CollisionObject2D playerCollision)
				playerCollision.CollisionMask |= ArenaCollisionLayer;
		}
		else
		{
			GD.PushWarning("[TilemapSetup] Could not find Player node.");
		}

		var enemy = FindExistingEnemy();
		if (enemy != null)
		{
			enemy.GlobalPosition = enemySpawn;
			enemy.AddToGroup("enemy");
		}
	}

	private void SetupCameraFocus()
	{
		var camera = GetNodeOrNull<Camera2D>(CameraPath);
		if (camera == null)
			return;

		var player = FindChild("Player", true, false) as Node2D;
		Vector2 focus = player?.GlobalPosition ?? GetSpawnPosition(ManualPlayerSpawnPath, DefaultPlayerSpawn);

		camera.Zoom = CameraZoom;
		camera.GlobalPosition = focus;
		camera.Enabled = true;
		camera.MakeCurrent();
		camera.LimitLeft = Mathf.FloorToInt(_cameraBounds.Position.X);
		camera.LimitTop = Mathf.FloorToInt(_cameraBounds.Position.Y);
		camera.LimitRight = Mathf.CeilToInt(_cameraBounds.End.X);
		camera.LimitBottom = Mathf.CeilToInt(_cameraBounds.End.Y);

		if (camera is IsometricCamera isoCamera)
		{
			isoCamera.SetZoom(CameraZoom.X);
			isoCamera.SetBounds(_cameraBounds);
			isoCamera.SetTarget(player);
			isoCamera.SnapTo(focus);
		}
	}

	private Node2D GetManualLevel()
	{
		return GetNodeOrNull<Node2D>(ManualLevelPath)
			?? FindChild("Level01", true, false) as Node2D;
	}

	private Node2D GetOrCreateFallbackBoundsRoot()
	{
		var boundsRoot = GetNodeOrNull<Node2D>(LegacyArenaBoundsPath);
		if (boundsRoot != null)
			return boundsRoot;

		var world = GetNodeOrNull<Node2D>(WorldPath);
		if (world == null)
			return null;

		boundsRoot = new Node2D { Name = "ArenaBounds" };
		world.AddChild(boundsRoot);
		return boundsRoot;
	}

	private Vector2 GetSpawnPosition(string path, Vector2 fallback)
	{
		var spawn = GetNodeOrNull<Node2D>(path);
		return spawn?.GlobalPosition ?? fallback;
	}

	private Node2D FindExistingEnemy()
	{
		foreach (Node node in GetTree().GetNodesInGroup("enemy"))
		{
			if (node is Node2D enemy && IsInstanceValid(enemy))
				return enemy;
		}

		return FindChild("Enemy", true, false) as Node2D;
	}

	private static bool HasStaticBounds(Node2D boundsRoot)
	{
		foreach (Node child in boundsRoot.GetChildren())
		{
			if (child is StaticBody2D)
				return true;
		}

		return false;
	}

	private static bool TryGetPlayableRectFromBounds(Node2D boundsRoot, out Rect2 rect)
	{
		rect = ManualFallbackArenaRect;
		if (boundsRoot == null)
			return false;

		float? top = null;
		float? bottom = null;
		float? left = null;
		float? right = null;
		float? topCenter = null;
		float? bottomCenter = null;
		float? leftCenter = null;
		float? rightCenter = null;

		foreach (Node bodyNode in boundsRoot.GetChildren())
		{
			if (bodyNode is not StaticBody2D body)
				continue;

			foreach (Node shapeNode in body.GetChildren())
			{
				if (shapeNode is not CollisionShape2D collision || collision.Disabled)
					continue;
				if (collision.Shape is not RectangleShape2D rectangle)
					continue;

				Vector2 center = collision.GlobalPosition;
				Vector2 scale = collision.GlobalScale;
				Vector2 size = new(
					Mathf.Abs(rectangle.Size.X * scale.X),
					Mathf.Abs(rectangle.Size.Y * scale.Y));

				if (size.X >= size.Y)
				{
					if (topCenter == null || center.Y < topCenter.Value)
					{
						topCenter = center.Y;
						top = center.Y + size.Y * 0.5f;
					}

					if (bottomCenter == null || center.Y > bottomCenter.Value)
					{
						bottomCenter = center.Y;
						bottom = center.Y - size.Y * 0.5f;
					}
				}
				else
				{
					if (leftCenter == null || center.X < leftCenter.Value)
					{
						leftCenter = center.X;
						left = center.X + size.X * 0.5f;
					}

					if (rightCenter == null || center.X > rightCenter.Value)
					{
						rightCenter = center.X;
						right = center.X - size.X * 0.5f;
					}
				}
			}
		}

		if (left == null || right == null || top == null || bottom == null)
			return false;
		if (right.Value <= left.Value || bottom.Value <= top.Value)
			return false;

		rect = new Rect2(new Vector2(left.Value, top.Value), new Vector2(right.Value - left.Value, bottom.Value - top.Value));
		return true;
	}

	private static void AddArenaWall(Node2D parent, string name, Vector2 position, Vector2 size)
	{
		var body = new StaticBody2D
		{
			Name = name,
			Position = position,
			CollisionLayer = ArenaCollisionLayer,
			CollisionMask = PlayerCollisionLayer
		};
		parent.AddChild(body);

		body.AddChild(new CollisionShape2D
		{
			Name = name + "Shape",
			Shape = new RectangleShape2D { Size = size }
		});
	}
}
