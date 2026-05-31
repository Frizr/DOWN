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

	// Plain, clean, flat gray undead ground tile (cliff-free)
	private static readonly Vector2I PlainGroundCoords = new(13, 14);

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

		// Initialize clean TileSet and add only the plain floor tile source
		var tileSet = new TileSet { TileSize = new Vector2I(TileSize, TileSize) };
		var source = new TileSetAtlasSource
		{
			Texture = texture,
			TextureRegionSize = new Vector2I(TextureRegionSize, TextureRegionSize)
		};
		source.CreateTile(PlainGroundCoords);
		tileSet.AddSource(source, FloorSourceId);

		tileMap.TileSet = tileSet;
		tileMap.Position = MapOrigin;
		tileMap.Scale = TileMapScale;
		tileMap.Clear();

		// Fill the entire rectangular map cleanly with a single plain floor tile (no noise, no wallpaper)
		for (int y = 0; y < GridHeight; y++)
		{
			for (int x = 0; x < GridWidth; x++)
			{
				tileMap.SetCell(new Vector2I(x, y), FloorSourceId, PlainGroundCoords);
			}
		}

		SetupEntities();
		SetupDecorations();

		var player = FindChild("Player", true, false) as Node2D;
		GD.Print("[TilemapSetup] Static map generation completed with single safe ground tile.");
	}

	private void SetupEntities()
	{
		var player = FindChild("Player", true, false) as Node2D;
		var enemy = FindChild("Enemy", true, false) as Node2D;

		// Place Player at the exact center of the flat floor
		if (player != null)
		{
			player.Position = CellToWorld(GridWidth / 2, GridHeight / 2);
			player.AddToGroup("player");
		}
		else
		{
			GD.PushWarning("[TilemapSetup] Could not find Player node.");
		}

		// Place Enemy near Player but safely offset
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

		// ─── FIXED, SPARSE VISUAL PROPS PLACEMENT ───────────────────────────

		// 1. Grave Cluster (Top-Left, centered around 8, 8)
		AddDecor(decorationsRoot, "Lich_shadow1.png", 8, 8, 1.3f);
		AddDecor(decorationsRoot, "Grave_shadow1_1.png", 6, 7, 1.4f);
		AddDecor(decorationsRoot, "Grave_shadow1_2.png", 7, 10, 1.4f);
		AddDecor(decorationsRoot, "Grave_shadow2_4.png", 10, 7, 1.4f);

		// 2. Bones/Skull Cluster (Top-Right, centered around 50, 8)
		AddDecor(decorationsRoot, "Pile_sculls_shadow1.png", 50, 8, 0.9f);
		AddDecor(decorationsRoot, "Bones_shadow1_1.png", 48, 7, 1.3f);
		AddDecor(decorationsRoot, "Bones_shadow2_2.png", 52, 9, 1.3f);
		AddDecor(decorationsRoot, "Bones_shadow3_5.png", 49, 10, 1.3f);

		// 3. Crystal/Rock Cluster (Bottom-Right, centered around 50, 30)
		AddDecor(decorationsRoot, "Rock_shadow1_1.png", 50, 30, 1.0f);
		AddDecor(decorationsRoot, "Crystal_shadow1_1.png", 48, 29, 1.0f);
		AddDecor(decorationsRoot, "Crystal_shadow2_1.png", 52, 31, 1.0f);
		AddDecor(decorationsRoot, "Thorn_plant_shadow1_1.png", 49, 32, 0.85f);

		// 4. Dead Tree/Rock Cluster (Bottom-Left, centered around 10, 30)
		AddDecor(decorationsRoot, "Dead_tree_shadow1_1.png", 10, 30, 1.1f);
		AddDecor(decorationsRoot, "Rock_shadow2_3.png", 8, 31, 1.0f);
		AddDecor(decorationsRoot, "Rock_shadow3_1.png", 12, 29, 1.0f);
		AddDecor(decorationsRoot, "Broken_tree_shadow1_1.png", 11, 32, 1.0f);
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

	private static void AddDecor(Node2D parent, string file, int cellX, int cellY, float scale)
	{
		var texture = ResourceLoader.Load<Texture2D>(ObjectTexturePrefix + file);
		if (texture == null)
		{
			// Safe fallback
			texture = ResourceLoader.Load<Texture2D>(ObjectTexturePrefix + "Rock_shadow1_1.png");
			if (texture == null) return;
		}

		var sprite = new Sprite2D();
		sprite.Name = "Decor_" + file.Replace(".png", "");

		// CRITICAL SAFE ORDER: Add node to parent FIRST, then set its transform properties!
		parent.AddChild(sprite);

		sprite.Texture = texture;
		sprite.Centered = true;
		sprite.Position = CellToWorld(cellX, cellY);
		sprite.Scale = new Vector2(scale, scale);
		sprite.ZIndex = cellY;
	}

	private static Vector2 CellToWorld(int cellX, int cellY)
	{
		return MapOrigin + new Vector2(
			(cellX + 0.5f) * WorldTileSize,
			(cellY + 0.5f) * WorldTileSize
		);
	}
}
