using Godot;

[GlobalClass]
public partial class TilemapSetup : Node
{
	private const string WorldPath = "World";
	private const string TileMapPath = "World/TileMap";
	private const string DecorationsPath = "World/Decorations";
	private const string ArenaBoundsPath = "World/ArenaBounds";
	private const string ArenaOverlayPath = "World/ArenaOverlay";
	private const string CameraPath = "Camera";
	private const string BackgroundTexturePath = "res://Assets/Tiles/Undead/Demo/Undead_land_background.png";

	private static readonly Vector2 PlayerSpawn = new(640f, 420f);
	private static readonly Vector2 EnemySpawn = new(760f, 420f);
	private static readonly Vector2 BaseGroundPosition = new(640f, 360f);
	private static readonly Vector2 BaseGroundScale = new(1.8f, 1.8f);
	private static readonly Vector2 CameraZoom = new(2.4f, 2.4f);
	private static readonly Rect2 ArenaRect = new(new Vector2(420f, 300f), new Vector2(520f, 300f));
	private static readonly Color ArenaOverlayColor = new(0f, 0f, 0f, 0.14f);
	private const float ArenaWallThickness = 48f;
	private const uint ArenaCollisionLayer = 2;

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

		var overlay = world.GetNodeOrNull<Sprite2D>("ArenaOverlay");
		if (overlay == null)
		{
			overlay = new Sprite2D { Name = "ArenaOverlay" };
			world.AddChild(overlay);
		}

		overlay.Texture = CreateSolidTexture(ArenaOverlayColor);
		overlay.Centered = true;
		overlay.Position = ArenaRect.GetCenter();
		overlay.Scale = ArenaRect.Size;
		overlay.ZIndex = -90;
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

		float left = ArenaRect.Position.X;
		float top = ArenaRect.Position.Y;
		float right = ArenaRect.End.X;
		float bottom = ArenaRect.End.Y;
		float centerX = ArenaRect.GetCenter().X;
		float centerY = ArenaRect.GetCenter().Y;

		AddArenaWall(boundsRoot, "TopWall", new Vector2(centerX, top - ArenaWallThickness * 0.5f), new Vector2(ArenaRect.Size.X + ArenaWallThickness * 2f, ArenaWallThickness));
		AddArenaWall(boundsRoot, "BottomWall", new Vector2(centerX, bottom + ArenaWallThickness * 0.5f), new Vector2(ArenaRect.Size.X + ArenaWallThickness * 2f, ArenaWallThickness));
		AddArenaWall(boundsRoot, "LeftWall", new Vector2(left - ArenaWallThickness * 0.5f, centerY), new Vector2(ArenaWallThickness, ArenaRect.Size.Y));
		AddArenaWall(boundsRoot, "RightWall", new Vector2(right + ArenaWallThickness * 0.5f, centerY), new Vector2(ArenaWallThickness, ArenaRect.Size.Y));
	}

	private void SetupPlayerEnemyPositions()
	{
		var player = FindChild("Player", true, false) as Node2D;
		if (player != null)
		{
			player.Position = PlayerSpawn;
			player.AddToGroup("player");
		}
		else
		{
			GD.PushWarning("[TilemapSetup] Could not find Player node.");
		}

		var enemy = FindChild("Enemy", true, false) as Node2D;
		if (enemy != null)
		{
			enemy.Position = EnemySpawn;
			enemy.AddToGroup("enemy");
		}
		else
		{
			GD.PushWarning("[TilemapSetup] Could not find Enemy node.");
		}
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

	private static void AddArenaWall(Node2D parent, string name, Vector2 position, Vector2 size)
	{
		var body = new StaticBody2D
		{
			Name = name,
			Position = position,
			CollisionLayer = ArenaCollisionLayer,
			CollisionMask = 0
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
