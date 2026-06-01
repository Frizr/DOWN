using Godot;

[GlobalClass]
public partial class TilemapSetup : Node
{
	private const string WorldPath = "World";
	private const string TileMapPath = "World/TileMap";
	private const string DecorationsPath = "World/Decorations";
	private const string ArenaBoundsPath = "World/ArenaBounds";
	private const string CameraPath = "Camera";
	private const string BackgroundTexturePath = "res://Assets/Tiles/Undead/Demo/Undead_land_background.png";

	private static readonly Vector2 PlayerSpawn = new(640f, 420f);
	private static readonly Vector2 BaseGroundPosition = new(640f, 360f);
	private static readonly Vector2 BaseGroundScale = new(1.8f, 1.8f);
	private static readonly Vector2 CameraZoom = new(2.4f, 2.4f);
	private const int ArenaTileSize = 32;
	private const int ArenaMargin = 64;
	private static readonly Rect2 PreferredArenaRect = new(new Vector2(330f, 250f), new Vector2(760f, 460f));
	private static readonly Rect2 FallbackArenaRect = PreferredArenaRect;
	private static readonly Color ArenaDebugColor = new(1f, 0.82f, 0.25f, 0.75f);
	private const int ArenaWallThickness = 96;
	private const uint PlayerCollisionLayer = 1;
	private const uint ArenaCollisionLayer = 4;
	private const float MinimumEnemySpawnDistance = 230f;
	private static readonly bool ShowArenaDebug = false;
	private static readonly bool ShowSpawnDebugMarker = false;
	private static readonly bool LogSpawnSelection = false;
	private Rect2 _arenaRect = FallbackArenaRect;


	private static readonly Vector2[] EnemySpawnPoints =
	{
		new(420f, 320f),
		new(800f, 192f),
		new(1080f, 380f),
		new(520f, 620f),
		new(960f, 640f),
		new(1216f, 520f),
		new(320f, 520f),
		new(1120f, 224f),
		new(224f, 224f),
		new(1152f, 672f)
	};

	public override void _Ready()
	{
		DisableBrokenTileMap();
		SetupBaseGround();
		SetupArenaOverlay();
		SetupArenaBounds();
		SetupPlayerEnemyPositions();
		SetupCameraFocus();
		SetupDecorations();
	}

	private void DisableBrokenTileMap()
	{
		var tileMap = GetNodeOrNull<TileMapLayer>(TileMapPath)
			?? FindChild("TileMap", true, false) as TileMapLayer;

		if (tileMap == null)
			return;

		tileMap.Clear();
		tileMap.Visible = false;
	}

	private void SetupBaseGround()
	{
		Node2D world = GetNodeOrNull<Node2D>(WorldPath);
		if (world == null)
			return;

		var baseGround = world.GetNodeOrNull<Sprite2D>("BaseGround");
		if (baseGround == null)
		{
			baseGround = new Sprite2D { Name = "BaseGround" };
			world.AddChild(baseGround);
		}

		var backgroundTexture = GD.Load<Texture2D>(BackgroundTexturePath);
		if (backgroundTexture == null)
		{
			GD.PushWarning("[TilemapSetup] Missing background texture: " + BackgroundTexturePath);
			return;
		}

		_arenaRect = CalculateArenaRect(backgroundTexture);

		baseGround.Texture = backgroundTexture;
		baseGround.Centered = true;
		baseGround.Position = BaseGroundPosition;
		baseGround.Scale = BaseGroundScale;
		baseGround.ZIndex = -100;
	}

	private void SetupArenaOverlay()
	{
		Node2D world = GetNodeOrNull<Node2D>(WorldPath);
		if (world == null)
			return;

		world.GetNodeOrNull<Node>("ArenaOverlay")?.QueueFree();
		if (!ShowArenaDebug)
			return;

		var debugRect = new Line2D
		{
			Name = "ArenaOverlay",
			Closed = true,
			DefaultColor = ArenaDebugColor,
			Width = 2f,
			ZIndex = 250
		};

		debugRect.AddPoint(_arenaRect.Position);
		debugRect.AddPoint(new Vector2(_arenaRect.End.X, _arenaRect.Position.Y));
		debugRect.AddPoint(_arenaRect.End);
		debugRect.AddPoint(new Vector2(_arenaRect.Position.X, _arenaRect.End.Y));
		world.AddChild(debugRect);
	}

	private void SetupArenaBounds()
	{
		var boundsRoot = GetNodeOrNull<Node2D>(ArenaBoundsPath);
		if (boundsRoot == null)
		{
			var world = GetNodeOrNull<Node2D>(WorldPath);
			if (world == null)
				return;

			boundsRoot = new Node2D { Name = "ArenaBounds" };
			world.AddChild(boundsRoot);
		}

		foreach (Node child in boundsRoot.GetChildren())
			child.QueueFree();

		float left = _arenaRect.Position.X;
		float top = _arenaRect.Position.Y;
		float right = _arenaRect.End.X;
		float bottom = _arenaRect.End.Y;
		float centerX = _arenaRect.GetCenter().X;
		float centerY = _arenaRect.GetCenter().Y;

		AddArenaWall(boundsRoot, "TopWall", new Vector2(centerX, top - ArenaWallThickness * 0.5f), new Vector2(_arenaRect.Size.X + ArenaWallThickness * 2f, ArenaWallThickness));
		AddArenaWall(boundsRoot, "BottomWall", new Vector2(centerX, bottom + ArenaWallThickness * 0.5f), new Vector2(_arenaRect.Size.X + ArenaWallThickness * 2f, ArenaWallThickness));
		AddArenaWall(boundsRoot, "LeftWall", new Vector2(left - ArenaWallThickness * 0.5f, centerY), new Vector2(ArenaWallThickness, _arenaRect.Size.Y + ArenaWallThickness * 2f));
		AddArenaWall(boundsRoot, "RightWall", new Vector2(right + ArenaWallThickness * 0.5f, centerY), new Vector2(ArenaWallThickness, _arenaRect.Size.Y + ArenaWallThickness * 2f));
	}

	private void SetupPlayerEnemyPositions()
	{
		var player = FindChild("Player", true, false) as Node2D;
		if (player != null)
		{
			player.Position = PlayerSpawn;
			player.AddToGroup("player");
			if (player is CollisionObject2D playerCollision)
				playerCollision.CollisionMask |= ArenaCollisionLayer;
		}
		else
		{
			GD.PushWarning("[TilemapSetup] Could not find Player node.");
		}

		var enemy = FindChild("Enemy", true, false) as Node2D;
		if (enemy != null)
		{
			Vector2 enemySpawn = PickEnemySpawnPoint();
			enemy.Position = enemySpawn;
			enemy.AddToGroup("enemy");
			MaybeShowSpawnDebugMarker(enemySpawn);

			if (LogSpawnSelection)
				GD.Print($"[TilemapSetup] Enemy spawn: {enemySpawn}");
		}
		else
		{
			GD.PushWarning("[TilemapSetup] Could not find Enemy node.");
		}
	}

	private Vector2 PickEnemySpawnPoint()
	{
		var rng = new RandomNumberGenerator();
		rng.Randomize();

		int startIndex = (int)rng.RandiRange(0, EnemySpawnPoints.Length - 1);
		for (int offset = 0; offset < EnemySpawnPoints.Length; offset++)
		{
			Vector2 candidate = EnemySpawnPoints[(startIndex + offset) % EnemySpawnPoints.Length];
			if (IsSafeEnemySpawn(candidate))
				return candidate;
		}

		return new Vector2(864f, 544f);
	}

	private bool IsSafeEnemySpawn(Vector2 position)
	{
		return _arenaRect.HasPoint(position)
			&& position.DistanceTo(PlayerSpawn) >= MinimumEnemySpawnDistance;
	}

	private void MaybeShowSpawnDebugMarker(Vector2 position)
	{
		if (!ShowSpawnDebugMarker)
			return;

		Node2D world = GetNodeOrNull<Node2D>(WorldPath);
		if (world == null)
			return;

		var marker = new Sprite2D
		{
			Name = "EnemySpawnDebugMarker",
			Texture = CreateSolidTexture(new Color(1f, 0.2f, 0.1f, 0.8f)),
			Centered = true,
			Position = position,
			Scale = new Vector2(8f, 8f),
			ZIndex = 200
		};
		world.AddChild(marker);
	}

	private void SetupCameraFocus()
	{
		var camera = GetNodeOrNull<Camera2D>(CameraPath);
		if (camera == null)
			return;

		camera.Zoom = CameraZoom;
		camera.GlobalPosition = PlayerSpawn;
	}

	private void SetupDecorations()
	{
		Node2D decorationsRoot = GetOrCreateDecorationsRoot();
		foreach (Node child in decorationsRoot.GetChildren())
			child.QueueFree();
	}

	private Node2D GetOrCreateDecorationsRoot()
	{
		var root = GetNodeOrNull<Node2D>(DecorationsPath);
		if (root != null)
			return root;

		var world = GetNodeOrNull<Node2D>(WorldPath);
		root = new Node2D { Name = "Decorations", YSortEnabled = true };
		world?.AddChild(root);
		return root;
	}

	private static Rect2 CalculateArenaRect(Texture2D texture)
	{
		Vector2 scaledSize = texture.GetSize() * BaseGroundScale;
		Vector2 backgroundTopLeft = BaseGroundPosition - scaledSize * 0.5f;
		Rect2 backgroundRect = new(backgroundTopLeft, scaledSize);
		if (ContainsRect(backgroundRect, PreferredArenaRect))
			return PreferredArenaRect;

		Vector2 playableTopLeft = backgroundTopLeft + new Vector2(ArenaMargin, ArenaMargin);
		Vector2 playableEnd = backgroundTopLeft + scaledSize - new Vector2(ArenaMargin, ArenaMargin);

		Vector2 snappedTopLeft = new(SnapUpToTile(playableTopLeft.X), SnapUpToTile(playableTopLeft.Y));
		Vector2 snappedEnd = new(SnapDownToTile(playableEnd.X), SnapDownToTile(playableEnd.Y));
		Vector2 snappedSize = snappedEnd - snappedTopLeft;

		if (snappedSize.X < ArenaTileSize || snappedSize.Y < ArenaTileSize)
			return FallbackArenaRect;

		return new Rect2(snappedTopLeft, snappedSize);
	}

	private static bool ContainsRect(Rect2 outer, Rect2 inner)
	{
		return inner.Position.X >= outer.Position.X
			&& inner.Position.Y >= outer.Position.Y
			&& inner.End.X <= outer.End.X
			&& inner.End.Y <= outer.End.Y;
	}

	private static float SnapUpToTile(float value)
	{
		return Mathf.Ceil(value / ArenaTileSize) * ArenaTileSize;
	}

	private static float SnapDownToTile(float value)
	{
		return Mathf.Floor(value / ArenaTileSize) * ArenaTileSize;
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

		var shape = new CollisionShape2D
		{
			Name = name + "Shape",
			Shape = new RectangleShape2D { Size = size }
		};
		body.AddChild(shape);
	}

	private static ImageTexture CreateSolidTexture(Color color)
	{
		var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
		image.Fill(color);
		return ImageTexture.CreateFromImage(image);
	}
}
