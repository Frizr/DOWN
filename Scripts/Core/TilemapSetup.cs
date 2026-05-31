using Godot;
using System;

[GlobalClass]
public partial class TilemapSetup : Node
{
	private const string TileMapPath = "World/TileMap";
	private const string DecorationsPath = "World/Decorations";
	private const string FloorTexturePath = "res://Assets/Tiles/Undead/Ground_rocks.png";
	private const string ObjectTexturePrefix = "res://Assets/Tiles/Undead/Objects_separately/";
	private const int TileSize = 16;
	private const int TextureRegionSize = 16;
	private const int GridWidth = 60;
	private const int GridHeight = 40;
	private const int FloorSourceId = 0;
	private const float TileWorldScale = 4f;
	private const float WorldTileSize = TileSize * TileWorldScale;
	private static readonly Vector2 TileMapScale = new(TileWorldScale, TileWorldScale);
	private static readonly Vector2 MapOrigin = new(
		-(GridWidth * WorldTileSize) / 2f,
		-(GridHeight * WorldTileSize) / 2f
	);

	private static readonly Vector2I[] OpenGroundAtlasCoords =
	{
		new(5, 7), new(6, 7), new(7, 7), new(8, 7),
		new(9, 7), new(10, 7), new(11, 7), new(12, 7),
		new(13, 14), new(14, 14), new(15, 14), new(16, 14),
		new(17, 14), new(18, 14), new(19, 14), new(20, 14),
	};

	private static readonly Vector2I[] RoughGroundAtlasCoords =
	{
		new(0, 14), new(1, 14), new(2, 14), new(3, 14), new(4, 14),
		new(21, 14), new(22, 14), new(23, 14), new(24, 14), new(25, 14),
		new(26, 14), new(27, 14), new(0, 15), new(1, 15), new(2, 15),
		new(18, 15), new(19, 15), new(20, 15), new(21, 15), new(22, 15),
	};

	private static readonly Vector2I[] DarkGroundAtlasCoords =
	{
		new(0, 22), new(1, 22), new(2, 22), new(3, 22), new(4, 22),
		new(5, 22), new(6, 22), new(7, 22), new(8, 22), new(9, 22),
		new(10, 22), new(11, 22), new(12, 22), new(13, 22), new(14, 22),
		new(15, 22), new(16, 22), new(17, 22),
	};

	private static readonly Vector2I[] CliffAtlasCoords =
	{
		new(5, 0), new(6, 0), new(7, 0), new(8, 0),
		new(9, 0), new(10, 0), new(11, 0), new(12, 0),
		new(13, 0), new(14, 0), new(15, 0), new(16, 0),
		new(5, 1), new(6, 1), new(7, 1), new(8, 1),
		new(9, 1), new(10, 1), new(11, 1), new(12, 1),
	};

	private static readonly DecorationSpec[] Decorations =
	{
		new("Dead_tree_shadow1_1.png", 6, 5, 1.0f),
		new("Dead_tree_shadow3_2.png", 52, 6, 1.0f),
		new("Broken_tree_shadow2_4.png", 8, 32, 1.0f),
		new("Broken_tree_shadow1_5.png", 50, 34, 1.0f),
		new("Rock_shadow1_1.png", 4, 14, 1.0f),
		new("Rock_shadow2_3.png", 55, 16, 1.0f),
		new("Rock_shadow3_5.png", 14, 5, 1.0f),
		new("Rock_shadow1_4.png", 45, 31, 1.0f),
		new("Grave_shadow1_2.png", 12, 29, 1.4f),
		new("Grave_shadow1_7.png", 15, 30, 1.4f),
		new("Grave_shadow2_4.png", 42, 7, 1.4f),
		new("Grave_shadow3_9.png", 46, 9, 1.4f),
		new("Bones_shadow1_1.png", 20, 11, 1.5f),
		new("Bones_shadow2_8.png", 38, 12, 1.5f),
		new("Bones_shadow3_15.png", 18, 27, 1.5f),
		new("Bones_shadow1_14.png", 48, 26, 1.5f),
		new("Pile_sculls_shadow1.png", 10, 18, 0.75f),
		new("Pile_sculls_shadow3.png", 51, 22, 0.75f),
		new("Crystal_shadow1_1.png", 7, 23, 1.0f),
		new("Crystal_shadow2_4.png", 54, 28, 1.0f),
		new("Plant_shadow1_2.png", 5, 9, 0.85f),
		new("Plant__shadow2_4.png", 24, 34, 0.85f),
		new("Thorn_plant_shadow3_4.png", 36, 5, 0.85f),
		new("Thorn_palnt_shadow2_5.png", 55, 35, 0.85f),
	};

	public override void _Ready()
	{
		var tileMap = GetNodeOrNull<TileMapLayer>(TileMapPath)
			?? FindChild("TileMap", true, false) as TileMapLayer;
		if (tileMap == null)
		{
			GD.PushWarning("[TilemapSetup] Could not find TileMapLayer named TileMap.");
			return;
		}

		var texture = ResourceLoader.Load<Texture2D>(FloorTexturePath);
		if (texture == null)
		{
			GD.PushWarning("[TilemapSetup] Could not load floor texture: " + FloorTexturePath);
			return;
		}

		var tileSet = CreateTileSet(texture);
		tileMap.TileSet = tileSet;
		tileMap.Position = MapOrigin;
		tileMap.Scale = TileMapScale;
		tileMap.Clear();

		for (int y = 0; y < GridHeight; y++)
		{
			for (int x = 0; x < GridWidth; x++)
				tileMap.SetCell(new Vector2I(x, y), FloorSourceId, PickFloorTile(x, y));
		}

		SetupEntities();
		SetupDecorations();

		var player = FindChild("Player", true, false) as Node2D;
		var camera = GetTree().Root.GetNodeOrNull<Node2D>("/root/Main/YSort/Player/Camera");
		GD.Print("[TilemapSetup] Tilemap position: " + tileMap.Position);
		GD.Print("[TilemapSetup] Player position: " + (player != null ? player.Position.ToString() : "<missing>"));
		GD.Print("[TilemapSetup] Camera position: " + (camera != null ? camera.GlobalPosition.ToString() : "<missing>"));
	}

	private static TileSet CreateTileSet(Texture2D texture)
	{
		var tileSet = new TileSet
		{
			TileSize = new Vector2I(TileSize, TileSize)
		};

		var source = new TileSetAtlasSource
		{
			Texture = texture,
			TextureRegionSize = new Vector2I(TextureRegionSize, TextureRegionSize)
		};

		CreateTiles(source, OpenGroundAtlasCoords);
		CreateTiles(source, RoughGroundAtlasCoords);
		CreateTiles(source, DarkGroundAtlasCoords);
		CreateTiles(source, CliffAtlasCoords);

		tileSet.AddSource(source, FloorSourceId);
		return tileSet;
	}

	private static Vector2I PickFloorTile(int x, int y)
	{
		if (IsCliffCell(x, y))
			return PickFrom(CliffAtlasCoords, x, y);

		if (IsDarkEdgePatch(x, y))
			return PickFrom(DarkGroundAtlasCoords, x, y);

		if (IsOpenCell(x, y))
			return PickFrom(OpenGroundAtlasCoords, x, y);

		return PickFrom(RoughGroundAtlasCoords, x, y);
	}

	private static void CreateTiles(TileSetAtlasSource source, Vector2I[] coords)
	{
		foreach (Vector2I atlasCoords in coords)
			source.CreateTile(atlasCoords);
	}

	private static bool IsOpenCell(int x, int y)
	{
		float centerX = (GridWidth - 1) / 2f;
		float centerY = (GridHeight - 1) / 2f;
		float dx = (x - centerX) / 19f;
		float dy = (y - centerY) / 12f;
		bool centralClearing = (dx * dx) + (dy * dy) < 1f;

		float horizontalPathY = centerY + Mathf.Sin(x * 0.28f) * 2.2f;
		bool horizontalPath = x > 4 && x < GridWidth - 5 && Mathf.Abs(y - horizontalPathY) <= 2.2f;

		float verticalPathX = centerX + Mathf.Sin(y * 0.34f) * 2.5f;
		bool verticalPath = y > 4 && y < GridHeight - 5 && Mathf.Abs(x - verticalPathX) <= 2f;

		bool northwestPath = x > 5 && x < 25 && y > 5 && y < 20 && Mathf.Abs((x + 2) - (y * 2)) <= 3;
		bool southeastPath = x > 36 && x < 56 && y > 21 && y < 36 && Mathf.Abs((x - 26) - y) <= 2;

		return centralClearing || horizontalPath || verticalPath || northwestPath || southeastPath;
	}

	private static bool IsCliffCell(int x, int y)
	{
		int edgeDistance = Math.Min(Math.Min(x, y), Math.Min(GridWidth - 1 - x, GridHeight - 1 - y));
		if (edgeDistance <= 2)
			return true;

		if (IsOpenCell(x, y))
			return false;

		return edgeDistance < 8 && HashCell(x, y) % 10 < 4;
	}

	private static bool IsDarkEdgePatch(int x, int y)
	{
		if (IsOpenCell(x, y))
			return false;

		int edgeDistance = Math.Min(Math.Min(x, y), Math.Min(GridWidth - 1 - x, GridHeight - 1 - y));
		return edgeDistance < 11 || HashCell(x, y) % 17 == 0;
	}

	private static Vector2I PickFrom(Vector2I[] coords, int x, int y)
	{
		return coords[HashCell(x, y) % coords.Length];
	}

	private static int HashCell(int x, int y)
	{
		unchecked
		{
			return ((x * 73856093) ^ (y * 19349663) ^ 83492791) & int.MaxValue;
		}
	}

	private void SetupEntities()
	{
		var player = FindChild("Player", true, false) as Node2D;
		var enemy = FindChild("Enemy", true, false) as Node2D;

		if (player != null)
		{
			player.Position = CellToWorld(GridWidth / 2, GridHeight / 2);
			player.AddToGroup("player");
		}
		else
		{
			GD.PushWarning("[TilemapSetup] Could not find Player node.");
		}

		if (enemy != null)
		{
			enemy.Position = CellToWorld((GridWidth / 2) + 4, (GridHeight / 2) + 1);
			enemy.AddToGroup("enemy");
		}
		else
		{
			GD.PushWarning("[TilemapSetup] Could not find Enemy node.");
		}
	}

	private void SetupDecorations()
	{
		Node2D decorationsRoot = GetOrCreateDecorationsRoot();
		foreach (Node child in decorationsRoot.GetChildren())
			child.QueueFree();

		foreach (DecorationSpec spec in Decorations)
			AddDecoration(decorationsRoot, spec);
	}

	private Node2D GetOrCreateDecorationsRoot()
	{
		var root = GetNodeOrNull<Node2D>(DecorationsPath);
		if (root != null)
			return root;

		var world = GetNodeOrNull<Node2D>("World");
		root = new Node2D { Name = "Decorations", YSortEnabled = true };
		world?.AddChild(root);
		return root;
	}

	private static void AddDecoration(Node2D parent, DecorationSpec spec)
	{
		var texture = ResourceLoader.Load<Texture2D>(ObjectTexturePrefix + spec.File);
		if (texture == null)
		{
			GD.PushWarning("[TilemapSetup] Missing decoration texture: " + spec.File);
			return;
		}

		var sprite = new Sprite2D
		{
			Name = "Decor_" + spec.File.Replace(".png", ""),
			Texture = texture,
			Centered = true,
			Position = CellToWorld(spec.CellX, spec.CellY),
			Scale = new Vector2(spec.Scale, spec.Scale),
			ZIndex = spec.CellY
		};
		parent.AddChild(sprite);
	}

	private static Vector2 CellToWorld(int cellX, int cellY)
	{
		return MapOrigin + new Vector2(
			(cellX + 0.5f) * WorldTileSize,
			(cellY + 0.5f) * WorldTileSize
		);
	}

	private readonly struct DecorationSpec
	{
		public readonly string File;
		public readonly int CellX;
		public readonly int CellY;
		public readonly float Scale;

		public DecorationSpec(string file, int cellX, int cellY, float scale)
		{
			File = file;
			CellX = cellX;
			CellY = cellY;
			Scale = scale;
		}
	}
}
