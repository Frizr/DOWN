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

	private enum TerrainType
	{
		Wall,   // Impassable cliff/cave wall
		Open,   // Walkable open central battlefield
		Rough,  // Walkable rough/cracked ground
		Dark    // Walkable dark path/cave corners
	}

	private TerrainType[,] _layout;

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

		GenerateLayout();

		for (int y = 0; y < GridHeight; y++)
		{
			for (int x = 0; x < GridWidth; x++)
			{
				tileMap.SetCell(new Vector2I(x, y), FloorSourceId, PickFloorTile(x, y));
			}
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

	private static void CreateTiles(TileSetAtlasSource source, Vector2I[] coords)
	{
		foreach (Vector2I atlasCoords in coords)
			source.CreateTile(atlasCoords);
	}

	private void GenerateLayout()
	{
		_layout = new TerrainType[GridWidth, GridHeight];

		// Define player clearing parameters
		float centerX = (GridWidth - 1) / 2f;
		float centerY = (GridHeight - 1) / 2f;

		for (int y = 0; y < GridHeight; y++)
		{
			for (int x = 0; x < GridWidth; x++)
			{
				// 1. Irregular cave border: top/bottom/left/right
				// Use deterministic sine and cosine combinations for natural-looking boundaries
				float leftMargin = 3f + Mathf.Sin(y * 0.4f) * 1.5f + Mathf.Cos(y * 0.15f) * 1.0f;
				float rightMargin = GridWidth - 4f - Mathf.Sin(y * 0.35f) * 1.5f - Mathf.Cos(y * 0.2f) * 1.0f;
				float topMargin = 3f + Mathf.Sin(x * 0.4f) * 1.5f + Mathf.Cos(x * 0.15f) * 1.0f;
				float bottomMargin = GridHeight - 4f - Mathf.Sin(x * 0.35f) * 1.5f - Mathf.Cos(x * 0.2f) * 1.0f;

				if (x < leftMargin || x > rightMargin || y < topMargin || y > bottomMargin)
				{
					_layout[x, y] = TerrainType.Wall;
					continue;
				}

				// 2. Central open clearing (battlefield)
				// An ellipse in the center
				float dx = (x - centerX) / 14f;
				float dy = (y - centerY) / 9f;
				bool isCenterClearing = (dx * dx) + (dy * dy) < 1.0f;

				if (isCenterClearing)
				{
					_layout[x, y] = TerrainType.Open;
					continue;
				}

				// 3. Walkable paths to other directions
				// Paths connecting the center to left/right/top/bottom
				bool inHorizontalPath = Mathf.Abs(y - centerY) <= 3f && x >= 4 && x < GridWidth - 4;
				bool inVerticalPath = Mathf.Abs(x - centerX) <= 3f && y >= 4 && y < GridHeight - 4;

				// Extra winding paths
				bool northwestPath = x > 5 && x < 25 && y > 5 && y < 20 && Mathf.Abs((x + 2) - (y * 2)) <= 3;
				bool southeastPath = x > 36 && x < 56 && y > 21 && y < 36 && Mathf.Abs((x - 26) - y) <= 2;

				if (inHorizontalPath || inVerticalPath || northwestPath || southeastPath)
				{
					// Blend paths between Open and Rough ground based on distance from center
					float distFromCenter = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
					if (distFromCenter < 18f)
						_layout[x, y] = TerrainType.Open;
					else if (distFromCenter < 25f)
						_layout[x, y] = TerrainType.Rough;
					else
						_layout[x, y] = TerrainType.Dark;
					continue;
				}

				// Default outer walkable area is Rough or Dark ground
				int edgeDist = Math.Min(Math.Min(x, y), Math.Min(GridWidth - 1 - x, GridHeight - 1 - y));
				if (edgeDist < 8)
					_layout[x, y] = TerrainType.Dark;
				else
					_layout[x, y] = TerrainType.Rough;
			}
		}

		// 4. Inject quadrant-based cliff pillars inside the map
		Vector2[] pillars = new Vector2[]
		{
			new(16f, 11f),
			new(44f, 11f),
			new(16f, 29f),
			new(44f, 29f)
		};

		foreach (var pil in pillars)
		{
			for (int y = 0; y < GridHeight; y++)
			{
				for (int x = 0; x < GridWidth; x++)
				{
					float px = x - pil.X;
					float py = y - pil.Y;
					// Add organic variation
					float angle = Mathf.Atan2(py, px);
					float radiusVariation = 1.0f + Mathf.Sin(angle * 5f) * 0.6f + Mathf.Cos(angle * 3f) * 0.4f;
					float pillarRadius = 3.5f + radiusVariation;

					if ((px * px) + (py * py) < pillarRadius * pillarRadius)
					{
						// Ensure pillar does not overwrite the player's immediate start clearing
						float dxToCenter = x - centerX;
						float dyToCenter = y - centerY;
						float distToCenter = Mathf.Sqrt(dxToCenter * dxToCenter + dyToCenter * dyToCenter);
						if (distToCenter > 10f)
						{
							_layout[x, y] = TerrainType.Wall;
						}
					}
				}
			}
		}
	}

	private Vector2I PickFloorTile(int x, int y)
	{
		TerrainType type = _layout[x, y];

		if (type == TerrainType.Wall)
		{
			// Cliff/Wall Autotiling / Neighbor alignment
			// If cell below is walkable ground, this is a Cliff Face!
			bool isFace = false;
			if (y < GridHeight - 1 && _layout[x, y + 1] != TerrainType.Wall)
			{
				isFace = true;
			}

			if (isFace)
			{
				// Cliff Face: vertical rocky wall
				int index = (x % 8) + 12; // indices 12 to 19 in CliffAtlasCoords represent faces
				return CliffAtlasCoords[index % CliffAtlasCoords.Length];
			}
			else
			{
				// Cliff Top: flat surface
				int index = x % 12; // indices 0 to 11 in CliffAtlasCoords represent tops
				return CliffAtlasCoords[index % CliffAtlasCoords.Length];
			}
		}

		// Ground Clustering: group ground tiles in 3x3 chunks to make patterns contiguous and clean
		int blockX = x / 3;
		int blockY = y / 3;
		int blockHash = HashCell(blockX, blockY);
		int cellHash = HashCell(x, y);

		Vector2I[] coords;
		switch (type)
		{
			case TerrainType.Open:
				coords = OpenGroundAtlasCoords;
				break;
			case TerrainType.Dark:
				coords = DarkGroundAtlasCoords;
				break;
			default:
				coords = RoughGroundAtlasCoords;
				break;
		}

		int baseIndex = blockHash % coords.Length;

		// 25% of cells have a local detail variation (cracked rock or unique shade)
		if (cellHash % 100 < 25)
		{
			int varIndex = (baseIndex + 1 + (cellHash % 3)) % coords.Length;
			return coords[varIndex];
		}

		return coords[baseIndex];
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

		float centerX = (GridWidth - 1) / 2f;
		float centerY = (GridHeight - 1) / 2f;

		// Deterministically spawn visual decorations across the map
		for (int y = 0; y < GridHeight; y++)
		{
			for (int x = 0; x < GridWidth; x++)
			{
				TerrainType type = _layout[x, y];

				// Do not spawn decorations directly on Cliff Walls
				if (type == TerrainType.Wall)
					continue;

				// SAFETY CLEAN: Clear zone around Player/Enemy start
				float dx = x - centerX;
				float dy = y - centerY;
				float distToSpawn = Mathf.Sqrt(dx * dx + dy * dy);
				if (distToSpawn <= 6.5f)
					continue;

				int cellHash = HashCell(x, y);

				// 1. GRAVEYARD ZONE (Top-Left: x in [6..24], y in [5..15])
				if (x >= 6 && x <= 24 && y >= 5 && y <= 15)
				{
					if (cellHash % 100 < 15)
					{
						int roll = cellHash % 10;
						if (roll < 5)
						{
							// Spawn Grave
							string file = $"Grave_shadow1_{(cellHash % 17) + 1}.png";
							AddDecorationAtCell(decorationsRoot, file, x, y, 1.4f);
						}
						else if (roll < 7)
						{
							// Spawn Skull Pile
							string file = $"Pile_sculls_shadow{(cellHash % 3) + 1}.png";
							AddDecorationAtCell(decorationsRoot, file, x, y, 0.9f);
						}
						else if (roll < 9)
						{
							// Spawn Ruin
							string file = $"Ruin_shadow{(cellHash % 3) + 1}_{(cellHash % 5) + 1}.png";
							AddDecorationAtCell(decorationsRoot, file, x, y, 1.1f);
						}
						else
						{
							// Spawn Dead Tree
							string file = $"Dead_tree_shadow{(cellHash % 2 == 0 ? "1_1" : "3_2")}.png";
							AddDecorationAtCell(decorationsRoot, file, x, y, 1.1f);
						}
					}
					continue;
				}

				// 2. CRYSTAL CAVES (Bottom-Right: x in [36..54], y in [24..34])
				if (x >= 36 && x <= 54 && y >= 24 && y <= 34)
				{
					if (cellHash % 100 < 12)
					{
						int roll = cellHash % 10;
						if (roll < 6)
						{
							// Spawn Crystal
							string file = $"Crystal_shadow{(cellHash % 3) + 1}_{(cellHash % 4) + 1}.png";
							AddDecorationAtCell(decorationsRoot, file, x, y, 1.0f);
						}
						else if (roll < 8)
						{
							// Spawn Thorn Plant
							string file = GetThornPlantFile(cellHash);
							AddDecorationAtCell(decorationsRoot, file, x, y, 0.85f);
						}
						else
						{
							// Spawn Dark Plant
							string file = GetPlantFile(cellHash);
							AddDecorationAtCell(decorationsRoot, file, x, y, 0.85f);
						}
					}
					continue;
				}

				// 3. SCENIC BOUNDARIES (Near Cliff Pillars and Cave Walls)
				bool nearWall = false;
				for (int ny = -1; ny <= 1; ny++)
				{
					for (int nx = -1; nx <= 1; nx++)
					{
						int cx = x + nx;
						int cy = y + ny;
						if (cx >= 0 && cx < GridWidth && cy >= 0 && cy < GridHeight)
						{
							if (_layout[cx, cy] == TerrainType.Wall)
							{
								nearWall = true;
								break;
							}
						}
					}
					if (nearWall) break;
				}

				if (nearWall)
				{
					if (cellHash % 100 < 10)
					{
						int roll = cellHash % 10;
						if (roll < 4)
						{
							// Spawn Rock
							string file = $"Rock_shadow{(cellHash % 3) + 1}_{(cellHash % 5) + 1}.png";
							AddDecorationAtCell(decorationsRoot, file, x, y, 1.0f);
						}
						else if (roll < 7)
						{
							// Spawn Dead/Broken Tree
							string file = GetTreeFile(cellHash);
							AddDecorationAtCell(decorationsRoot, file, x, y, 1.0f);
						}
						else
						{
							// Spawn Thorn Plant / Bones
							string file = (roll < 9) ? GetThornPlantFile(cellHash) : GetBonesFile(cellHash);
							AddDecorationAtCell(decorationsRoot, file, x, y, 0.9f);
						}
					}
					continue;
				}

				// 4. GENERAL BATTLEFIELD DECORATIONS (Scattered open bones/skulls/cracked rocks)
				if (cellHash % 1000 < 25)
				{
					int roll = cellHash % 10;
					if (roll < 5)
					{
						// Spawn Bones
						string file = GetBonesFile(cellHash);
						AddDecorationAtCell(decorationsRoot, file, x, y, 1.3f);
					}
					else if (roll < 7)
					{
						// Spawn Small Grave
						string file = $"Grave_shadow2_{(cellHash % 8) + 1}.png";
						AddDecorationAtCell(decorationsRoot, file, x, y, 1.1f);
					}
					else if (roll < 9)
					{
						// Spawn Thorn/Mossy Plant
						string file = GetPlantFile(cellHash);
						AddDecorationAtCell(decorationsRoot, file, x, y, 0.8f);
					}
					else
					{
						// Spawn Rock
						string file = $"Rock_shadow1_{(cellHash % 4) + 1}.png";
						AddDecorationAtCell(decorationsRoot, file, x, y, 0.9f);
					}
				}
			}
		}
	}

	private static string GetThornPlantFile(int hash)
	{
		int group = (hash % 3) + 1;
		if (group == 2)
		{
			// Note the typo 'palnt' in group 2 files
			return $"Thorn_palnt_shadow2_{(hash % 6) + 1}.png";
		}
		return $"Thorn_plant_shadow{group}_{(hash % 6) + 1}.png";
	}

	private static string GetPlantFile(int hash)
	{
		int type = hash % 3;
		if (type == 0)
			return $"Plant_shadow1_{(hash % 5) + 1}.png";
		if (type == 1)
			return $"Plant__shadow2_{(hash % 5) + 1}.png"; // Double underscore
		return $"Plant_shadow3_{(hash % 5) + 1}.png";
	}

	private static string GetTreeFile(int hash)
	{
		int type = hash % 3;
		if (type == 0)
			return $"Dead_tree_shadow1_{(hash % 3) + 1}.png";
		if (type == 1)
			return $"Dead_tree_shadow3_{(hash % 3) + 1}.png";
		
		int brokenType = hash % 3;
		if (brokenType == 0)
			return $"Broken_tree_shadow1_{(hash % 7) + 1}.png";
		if (brokenType == 1)
			return $"Broken_tree_shadow2_{(hash % 7) + 1}.png";
		return $"Broken_ tree_shadow3_{(hash % 7) + 1}.png"; // Space after underscore
	}

	private static string GetBonesFile(int hash)
	{
		int group = (hash % 3) + 1;
		int index = (hash % 18) + 1;
		if (group == 2 && index == 14)
		{
			index = 13; // Avoid missing file Bones_shadow2_14.png
		}
		return $"Bones_shadow{group}_{index}.png";
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

	private static void AddDecorationAtCell(Node2D parent, string file, int cellX, int cellY, float scale)
	{
		var texture = ResourceLoader.Load<Texture2D>(ObjectTexturePrefix + file);
		if (texture == null)
		{
			// Fallback in case of asset index mismatch
			texture = ResourceLoader.Load<Texture2D>(ObjectTexturePrefix + "Rock_shadow1_1.png");
			if (texture == null) return;
		}

		var sprite = new Sprite2D
		{
			Name = "Decor_" + file.Replace(".png", ""),
			Texture = texture,
			Centered = true,
			Position = CellToWorld(cellX, cellY),
			Scale = new Vector2(scale, scale),
			ZIndex = cellY
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
}
