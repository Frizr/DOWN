using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class TilemapSetup : Node
{
	// ─── Node paths ───
	private const string WorldPath = "World";
	private const string TileMapPath = "World/TileMap";
	private const string GeneratedMapPath = "World/GeneratedMap";
	private const string SpawnPointsPath = "World/SpawnPoints";
	private const string ArenaBoundsPath = "World/ArenaBounds";
	private const string YSortPath = "World/YSort";
	private const string CameraPath = "Camera";

	// ─── Asset roots ───
	private const string UndeadObjectsPath = "res://Assets/Tiles/Undead/Objects_separately";
	private const string CursedObjectsPath = "res://Assets/Tiles/CursedLand/Objects_separetely";
	private const string UndeadAnimationPath = "res://Assets/Tiles/Undead";
	private const string UndeadGroundPath = "res://Assets/Tiles/Undead/Ground_rocks.png";
	private const string UndeadDetailsPath = "res://Assets/Tiles/Undead/Details.png";
	private const string CursedGroundPath = "res://Assets/Tiles/CursedLand/Ground.png";
	private const string CursedDetailsPath = "res://Assets/Tiles/CursedLand/details.png";
	private const string UndeadWaterPath = "res://Assets/Tiles/Undead/Water_coasts.png";
	private const string KenneyDungeonAnglePath = "res://Assets/External/KenneyIsometricMiniatureDungeon/Angle";
	private const string NpcFramesPath = "res://Assets/SpriteFrames";

	// ─── Layout constants ───
	private static readonly Vector2 PlayerSpawn = new(640f, 420f);
	private static readonly Vector2 MapSize = new(1280f, 832f);
	private static readonly Vector2 CameraZoom = new(2.05f, 2.05f);
	private static readonly Rect2 FallbackArenaRect = new(new Vector2(-960f, -560f), MapSize);
	private const int ArenaWallThickness = 96;
	private const int GroundTileSize = 64;
	private const uint PlayerCollisionLayer = 1;
	private const uint ArenaCollisionLayer = 4;
	private const float PlayerVisualClearRadius = 220f;
	private static readonly bool ShowArenaDebug = false;
	private static readonly Color ArenaDebugColor = new(1f, 0.82f, 0.25f, 0.75f);
	private static bool _hasActivePlayableBounds = false;
	private static Rect2 _activePlayableBounds = FallbackArenaRect;

	private readonly RandomNumberGenerator _rng = new();
	private Rect2 _mapRect = FallbackArenaRect;
	private Rect2 _arenaRect = FallbackArenaRect;
	private Node2D _cursedLayer;
	private Node2D _animatedLayer;

	// ─── Tile atlas regions (ground spritesheet sub-rects) ───
	private readonly struct TileAtlasRegion
	{
		public TileAtlasRegion(string sourcePath, Rect2 region, float weight = 1f)
		{
			SourcePath = sourcePath;
			Region = region;
			Weight = weight;
		}

		public string SourcePath { get; }
		public Rect2 Region { get; }
		public float Weight { get; }
	}

	// Undead ground: solid rocky earth variants
	private static readonly TileAtlasRegion[] UndeadGroundTiles =
	{
		new(UndeadGroundPath, new Rect2(192, 64, 64, 64), 3f),
		new(UndeadGroundPath, new Rect2(256, 64, 64, 64), 3f),
		new(UndeadGroundPath, new Rect2(320, 64, 64, 64), 2f),
		new(UndeadGroundPath, new Rect2(192, 128, 64, 64), 2f),
		new(UndeadGroundPath, new Rect2(256, 128, 64, 64), 2f),
		new(UndeadGroundPath, new Rect2(384, 128, 64, 64), 1f)
	};

	// Undead ground: darker/muddier variants for background
	private static readonly TileAtlasRegion[] UndeadDarkGroundTiles =
	{
		new(UndeadGroundPath, new Rect2(0, 0, 64, 64), 3f),
		new(UndeadGroundPath, new Rect2(64, 0, 64, 64), 3f),
		new(UndeadGroundPath, new Rect2(128, 0, 64, 64), 2f),
		new(UndeadGroundPath, new Rect2(0, 64, 64, 64), 2f),
		new(UndeadGroundPath, new Rect2(64, 64, 64, 64), 2f),
		new(UndeadGroundPath, new Rect2(128, 64, 64, 64), 1f)
	};

	// Water/coast tiles for swamp background
	private static readonly TileAtlasRegion[] UndeadWaterTiles =
	{
		new(UndeadWaterPath, new Rect2(0, 0, 64, 64), 2f),
		new(UndeadWaterPath, new Rect2(64, 0, 64, 64), 2f),
		new(UndeadWaterPath, new Rect2(128, 0, 64, 64), 1f),
		new(UndeadWaterPath, new Rect2(0, 64, 64, 64), 1f)
	};

	// Undead floor details (cracks, scratches)
	private static readonly TileAtlasRegion[] UndeadDetailTiles =
	{
		new(UndeadDetailsPath, new Rect2(0, 0, 64, 64), 1f),
		new(UndeadDetailsPath, new Rect2(64, 0, 64, 64), 1f),
		new(UndeadDetailsPath, new Rect2(256, 0, 64, 64), 1f),
		new(UndeadDetailsPath, new Rect2(384, 0, 64, 64), 1f)
	};

	// Cursed ground tiles
	private static readonly TileAtlasRegion[] CursedGroundTiles =
	{
		new(CursedGroundPath, new Rect2(128, 128, 64, 64), 3f),
		new(CursedGroundPath, new Rect2(192, 128, 64, 64), 3f),
		new(CursedGroundPath, new Rect2(256, 128, 64, 64), 2f),
		new(CursedGroundPath, new Rect2(128, 192, 64, 64), 2f),
		new(CursedGroundPath, new Rect2(192, 192, 64, 64), 2f),
		new(CursedGroundPath, new Rect2(256, 192, 64, 64), 1f)
	};

	// Cursed floor details
	private static readonly TileAtlasRegion[] CursedDetailTiles =
	{
		new(CursedDetailsPath, new Rect2(0, 0, 64, 64), 1f),
		new(CursedDetailsPath, new Rect2(64, 0, 64, 64), 1f),
		new(CursedDetailsPath, new Rect2(0, 64, 64, 64), 1f),
		new(CursedDetailsPath, new Rect2(64, 64, 64, 64), 1f)
	};

	// ─── Ambient animations ───
	private readonly struct AmbientAnimation
	{
		public AmbientAnimation(string fileName, int columns, int rows, float speed, float minScale, float maxScale)
		{
			FileName = fileName;
			Columns = columns;
			Rows = rows;
			Speed = speed;
			MinScale = minScale;
			MaxScale = maxScale;
		}

		public string FileName { get; }
		public int Columns { get; }
		public int Rows { get; }
		public float Speed { get; }
		public float MinScale { get; }
		public float MaxScale { get; }
	}

	private static readonly AmbientAnimation AnimatedDeadTreeLarge = new("Animation2.png", 6, 3, 5.5f, 1.0f, 1.35f);
	private static readonly AmbientAnimation AnimatedDeadTreeSmall = new("Animation3.png", 6, 3, 5.5f, 1.25f, 1.75f);
	private static readonly AmbientAnimation AnimatedLich = new("Animation4.png", 6, 3, 7.5f, 0.82f, 1.05f);
	private static readonly AmbientAnimation AnimatedSkullGate = new("Animation5.png", 6, 3, 6.5f, 0.95f, 1.18f);
	private static readonly AmbientAnimation AnimatedCultist = new("Animation6.png", 6, 3, 7.5f, 0.9f, 1.12f);

	// ─── NPC soldier spawns ───
	private readonly struct NpcSoldierSpawn
	{
		public NpcSoldierSpawn(string framesFile, Vector2 position, Vector2 patrolOffset, float scale = 1.45f)
		{
			FramesFile = framesFile;
			Position = position;
			PatrolOffset = patrolOffset;
			Scale = scale;
		}

		public string FramesFile { get; }
		public Vector2 Position { get; }
		public Vector2 PatrolOffset { get; }
		public float Scale { get; }
	}

	private static readonly NpcSoldierSpawn[] SoldierSpawns =
	{
		new("char_001_frames.tres", PlayerSpawn + new Vector2(-420f, -120f), new Vector2(36f, 0f), 1.35f),
		new("char_002_frames.tres", PlayerSpawn + new Vector2(430f, 118f), new Vector2(-36f, 0f), 1.35f)
	};

	// ─── Main entry ───
	public override void _Ready()
	{
		_rng.Randomize();
		_mapRect = new Rect2(PlayerSpawn - MapSize * 0.5f, MapSize);
		_arenaRect = _mapRect.Grow(-70f);
		_activePlayableBounds = _arenaRect;
		_hasActivePlayableBounds = true;

		DisableBrokenTileMap();
		BuildGeneratedMap();
		SetupArenaOverlay();
		SetupArenaBounds();
		SetupSpawnPoints();
		SetupPlayerEnemyPositions();
		SetupCameraFocus();
		ApplyWaveTheme(1);
	}

	public static bool TryGetActivePlayableBounds(out Rect2 bounds)
	{
		bounds = _activePlayableBounds;
		return _hasActivePlayableBounds;
	}

	public void ApplyWaveTheme(int wave)
	{
		if (_cursedLayer == null || !IsInstanceValid(_cursedLayer))
			_cursedLayer = GetNodeOrNull<Node2D>(GeneratedMapPath + "/CursedLayer");

		if (_cursedLayer == null)
			return;

		float intensity = Mathf.Clamp(wave / 8f, 0.12f, 1f);
		_cursedLayer.Visible = true;
		_cursedLayer.Modulate = new Color(1f, 1f, 1f, Mathf.Lerp(0.16f, 0.86f, intensity));
	}

	// ─── Disable legacy TileMap node ───
	private void DisableBrokenTileMap()
	{
		var tileMap = GetNodeOrNull<TileMapLayer>(TileMapPath)
			?? FindChild("TileMap", true, false) as TileMapLayer;

		if (tileMap == null)
			return;

		tileMap.Clear();
		tileMap.Visible = false;
	}

	// ═══════════════════════════════════════════════════════════════
	//  MAP GENERATION – real asset based layers
	// ═══════════════════════════════════════════════════════════════

	private void BuildGeneratedMap()
	{
		Node2D world = GetNodeOrNull<Node2D>(WorldPath);
		if (world == null)
			return;

		// Clean up any previous generated nodes
		world.GetNodeOrNull<Node>("BaseGround")?.QueueFree();
		world.GetNodeOrNull<Node>("BaseGroundTiles")?.QueueFree();
		world.GetNodeOrNull<Node>("GeneratedMap")?.QueueFree();
		world.GetNodeOrNull<Node2D>("Decorations")?.CallDeferred(Node.MethodName.QueueFree);

		var generatedMap = new Node2D { Name = "GeneratedMap" };
		world.AddChild(generatedMap);

		// Cursed overlay layer (intensity controlled by wave)
		_cursedLayer = new Node2D { Name = "CursedLayer", ZIndex = -80 };
		_animatedLayer = new Node2D { Name = "AnimatedLandmarks", YSortEnabled = true };
		generatedMap.AddChild(_cursedLayer);
		generatedMap.AddChild(_animatedLayer);

		BuildLayeredMap(generatedMap);
		AddAnimatedLandmarks();
		SetupNpcSoldiers();
	}

	/// <summary>
	/// Builds the 6-layer map structure with real tile assets.
	/// </summary>
	private void BuildLayeredMap(Node2D generatedMap)
	{
		// Create the 6 required layers
		var layer00 = new Node2D { Name = "00_Background", ZIndex = -160 };
		var layer01 = new Node2D { Name = "01_BaseTerrain", ZIndex = -130 };
		var layer02 = new Node2D { Name = "02_ReadablePath", ZIndex = -120 };
		var layer03 = new Node2D { Name = "03_FloorDetails", ZIndex = -110 };
		var layer04 = new Node2D { Name = "04_BackProps_NoCollision", YSortEnabled = true };
		var layer05 = new Node2D { Name = "05_BlockingProps_WithCollision", YSortEnabled = true };

		generatedMap.AddChild(layer00);
		generatedMap.AddChild(layer01);
		generatedMap.AddChild(layer02);
		generatedMap.AddChild(layer03);
		generatedMap.AddChild(layer04);
		generatedMap.AddChild(layer05);

		Fill_00_Background(layer00);
		Fill_01_BaseTerrain(layer01);
		Fill_02_ReadablePath(layer02);
		Fill_03_FloorDetails(layer03);
		Fill_04_BackProps(layer04);
		Fill_05_BlockingProps(layer05);
	}

	// ─── LAYER 00: Background – dark swamp/void/water ───
	private void Fill_00_Background(Node2D parent)
	{
		// Solid dark backdrop behind everything (subtle, only visible at map edges)
		AddFilledRect(parent, "DarkVoidBackdrop", _mapRect.Grow(100f),
			new Color(0.03f, 0.06f, 0.05f), -162);

		// Tile the entire map area with dark ground tiles (low opacity, swamp feel)
		AddTiledFloor(parent, "SwampDarkGround", _mapRect,
			UndeadDarkGroundTiles, GroundTileSize, -161,
			new Color(0.35f, 0.45f, 0.38f, 0.85f));

		// Scatter water/coast tiles around the outer ring for swamp feel
		Rect2 outerNorth = new(_mapRect.Position, new Vector2(_mapRect.Size.X, 120f));
		Rect2 outerSouth = new(new Vector2(_mapRect.Position.X, _mapRect.End.Y - 120f), new Vector2(_mapRect.Size.X, 120f));
		Rect2 outerWest = new(_mapRect.Position, new Vector2(120f, _mapRect.Size.Y));
		Rect2 outerEast = new(new Vector2(_mapRect.End.X - 120f, _mapRect.Position.Y), new Vector2(120f, _mapRect.Size.Y));

		AddTiledFloor(parent, "SwampWaterN", outerNorth, UndeadWaterTiles, GroundTileSize, -160,
			new Color(0.30f, 0.40f, 0.35f, 0.60f));
		AddTiledFloor(parent, "SwampWaterS", outerSouth, UndeadWaterTiles, GroundTileSize, -160,
			new Color(0.30f, 0.40f, 0.35f, 0.60f));
		AddTiledFloor(parent, "SwampWaterW", outerWest, UndeadWaterTiles, GroundTileSize, -160,
			new Color(0.30f, 0.40f, 0.35f, 0.55f));
		AddTiledFloor(parent, "SwampWaterE", outerEast, UndeadWaterTiles, GroundTileSize, -160,
			new Color(0.30f, 0.40f, 0.35f, 0.55f));
	}

	// ─── LAYER 01: BaseTerrain – main playable floor ───
	private void Fill_01_BaseTerrain(Node2D parent)
	{
		// Main island: full tile coverage with undead ground
		Rect2 mainIsland = new(PlayerSpawn + new Vector2(-460f, -235f), new Vector2(920f, 470f));
		AddTiledFloor(parent, "MainIslandGround", mainIsland,
			UndeadGroundTiles, GroundTileSize, -132,
			new Color(0.92f, 0.88f, 0.72f, 1.0f));

		// Sub-areas with slight color variation for readable biome zones

		// NW graveyard area – slightly green/mossy tint
		Rect2 graveyardArea = new(PlayerSpawn + new Vector2(-460f, -235f), new Vector2(240f, 180f));
		AddTiledFloor(parent, "GraveyardFloor", graveyardArea,
			UndeadGroundTiles, GroundTileSize, -131,
			new Color(0.68f, 0.82f, 0.70f, 0.45f));

		// NE shrine area – cool blue/teal tint
		Rect2 shrineArea = new(PlayerSpawn + new Vector2(220f, -235f), new Vector2(240f, 170f));
		AddTiledFloor(parent, "ShrineFloor", shrineArea,
			UndeadGroundTiles, GroundTileSize, -131,
			new Color(0.60f, 0.78f, 0.82f, 0.40f));

		// SE cursed area – red/flesh tint with cursed tiles
		Rect2 cursedArea = new(PlayerSpawn + new Vector2(200f, 80f), new Vector2(260f, 155f));
		AddTiledFloor(parent, "CursedFloor", cursedArea,
			CursedGroundTiles, GroundTileSize, -131,
			new Color(0.90f, 0.65f, 0.58f, 0.65f));

		// SW outpost area – warm/sandy tint
		Rect2 outpostArea = new(PlayerSpawn + new Vector2(-460f, 65f), new Vector2(230f, 170f));
		AddTiledFloor(parent, "OutpostFloor", outpostArea,
			UndeadGroundTiles, GroundTileSize, -131,
			new Color(0.90f, 0.82f, 0.60f, 0.40f));
	}

	// ─── LAYER 02: ReadablePath – main path/combat area ───
	private void Fill_02_ReadablePath(Node2D parent)
	{
		// Central combat plaza – brighter tile patch so player knows "fight here"
		Rect2 centralPlaza = new(PlayerSpawn + new Vector2(-200f, -130f), new Vector2(400f, 260f));
		AddTiledFloor(parent, "CentralPlazaTiles", centralPlaza,
			UndeadGroundTiles, GroundTileSize, -122,
			new Color(1.15f, 1.08f, 0.86f, 0.70f));

		// East-west road – bright tile strip
		Rect2 ewRoad = new(PlayerSpawn + new Vector2(-460f, -44f), new Vector2(920f, 88f));
		AddTiledFloor(parent, "EastWestRoadTiles", ewRoad,
			UndeadGroundTiles, GroundTileSize, -121,
			new Color(1.10f, 1.02f, 0.82f, 0.55f));

		// North-south road – bright tile strip
		Rect2 nsRoad = new(PlayerSpawn + new Vector2(-44f, -235f), new Vector2(88f, 470f));
		AddTiledFloor(parent, "NorthSouthRoadTiles", nsRoad,
			UndeadGroundTiles, GroundTileSize, -121,
			new Color(1.05f, 0.98f, 0.80f, 0.50f));

		// Subtle shadow under the path edges using a soft tint polygon (NOT the main visual)
		AddFilledRect(parent, "PlazaShadowSubtle", centralPlaza.Grow(12f),
			new Color(0.08f, 0.06f, 0.04f, 0.12f), -124);
	}

	// ─── LAYER 03: FloorDetails – sparse cracks only ───
	private void Fill_03_FloorDetails(Node2D parent)
	{
		// Very sparse undead cracks across the main island (low fill = not cluttered)
		Rect2 mainIsland = new(PlayerSpawn + new Vector2(-420f, -200f), new Vector2(840f, 400f));
		AddScatteredTiles(parent, "SparseFloorCracks", mainIsland,
			UndeadDetailTiles, GroundTileSize, 0.015f, -108);

		// A few cursed detail tiles in the SE cursed area
		Rect2 cursedArea = new(PlayerSpawn + new Vector2(220f, 95f), new Vector2(220f, 130f));
		AddScatteredTiles(parent, "CursedFloorMarks", cursedArea,
			CursedDetailTiles, GroundTileSize, 0.025f, -107);
	}

	// ─── LAYER 04: BackProps (no collision) – graves, bones, chests ───
	private void Fill_04_BackProps(Node2D parent)
	{
		// NW graveyard cluster: 6 graves in a 2x3 grid
		Vector2[] graveOffsets =
		{
			new(-380f, -195f), new(-320f, -195f),
			new(-380f, -145f), new(-320f, -145f),
			new(-380f, -95f),  new(-320f, -95f)
		};
		for (int i = 0; i < graveOffsets.Length; i++)
			AddStaticLandmark(parent, UndeadObjectsPath + "/Grave_shadow1_" + (1 + i) + ".png",
				PlayerSpawn + graveOffsets[i], 0.62f, "Grave_" + i);

		// Bone pile near SW outpost
		AddStaticLandmark(parent, UndeadObjectsPath + "/Bones_shadow2_7.png",
			PlayerSpawn + new Vector2(-340f, 155f), 0.50f, "BonePile_SW");

		// Small skull pile at SE boundary
		AddStaticLandmark(parent, UndeadObjectsPath + "/Pile_sculls_shadow1.png",
			PlayerSpawn + new Vector2(350f, 195f), 0.48f, "SkullPile_SE");

		// Lich shrine decoration in NE
		AddStaticLandmark(parent, UndeadObjectsPath + "/Lich_shadow2.png",
			PlayerSpawn + new Vector2(380f, -165f), 0.50f, "LichShrine_NE");

		// Kenney chest at SW outpost
		AddStaticLandmark(parent, KenneyDungeonAnglePath + "/chestClosed_S.png",
			PlayerSpawn + new Vector2(-385f, 178f), 0.20f, "Chest_SW");

		// Kenney barrels near chest
		AddStaticLandmark(parent, KenneyDungeonAnglePath + "/barrelsStacked_S.png",
			PlayerSpawn + new Vector2(-420f, 200f), 0.18f, "Barrels_SW");

		// Kenney broken wall in SE cursed
		AddStaticLandmark(parent, KenneyDungeonAnglePath + "/stoneWallBroken_S.png",
			PlayerSpawn + new Vector2(410f, 165f), 0.20f, "BrokenWall_SE");

		// Crystal in NE shrine
		AddStaticLandmark(parent, UndeadObjectsPath + "/Crystal_shadow1_1.png",
			PlayerSpawn + new Vector2(340f, -180f), 0.55f, "Crystal_NE");

		// Dead arm reaching from the ground (cursed zone flavor)
		AddStaticLandmark(parent, UndeadObjectsPath + "/Dead_arm_shadow1_1.png",
			PlayerSpawn + new Vector2(290f, 135f), 0.48f, "DeadArm_SE");
	}

	// ─── LAYER 05: BlockingProps (with collision) – trees, cliffs, gates, ruins ───
	private void Fill_05_BlockingProps(Node2D parent)
	{
		// ── West gate (skull door) ──
		AddBlockingLandmark(parent, UndeadObjectsPath + "/Scull_door_shadow2.png",
			PlayerSpawn + new Vector2(-445f, -16f), 0.78f, "WestGate",
			new Vector2(82f, 48f), new Vector2(0f, 20f));

		// ── NW dead tree ──
		AddBlockingLandmark(parent, UndeadObjectsPath + "/Dead_tree_shadow3_3.png",
			PlayerSpawn + new Vector2(-410f, -210f), 0.72f, "DeadTree_NW",
			new Vector2(50f, 40f), new Vector2(0f, 24f));

		// ── NE broken tree ──
		AddBlockingLandmark(parent, UndeadObjectsPath + "/Broken_tree_shadow2_2.png",
			PlayerSpawn + new Vector2(380f, -210f), 0.70f, "BrokenTree_NE",
			new Vector2(50f, 40f), new Vector2(0f, 24f));

		// ── SE ruin ──
		AddBlockingLandmark(parent, UndeadObjectsPath + "/Ruin_shadow3_5.png",
			PlayerSpawn + new Vector2(420f, 190f), 0.70f, "Ruin_SE",
			new Vector2(78f, 48f), new Vector2(0f, 20f));

		// ── Cursed large plant (SE, blocks passage) ──
		AddBlockingLandmark(parent, CursedObjectsPath + "/Jaws_plant_shadow1_1.png",
			PlayerSpawn + new Vector2(440f, 130f), 0.60f, "CursedPlant_SE",
			new Vector2(46f, 36f), new Vector2(0f, 14f));

		// ── Kenney gate in NE shrine area ──
		AddBlockingLandmark(parent, KenneyDungeonAnglePath + "/stoneWallGateClosed_S.png",
			PlayerSpawn + new Vector2(330f, -220f), 0.28f, "KenneyGate_NE",
			new Vector2(60f, 30f), new Vector2(0f, 12f));

		// ── North edge rock line (chokepoint blockers) ──
		AddBlockingRockLine(parent, PlayerSpawn + new Vector2(-350f, -238f),
			new Vector2(110f, 0f), 7, "NorthEdgeRock");

		// ── South edge rock line ──
		AddBlockingRockLine(parent, PlayerSpawn + new Vector2(-350f, 238f),
			new Vector2(110f, 0f), 7, "SouthEdgeRock");

		// ── West thorn wall ──
		AddBlockingThornWall(parent, PlayerSpawn + new Vector2(-470f, -150f),
			new Vector2(0f, 65f), 5, "WestThornWall");

		// ── East thorn wall ──
		AddBlockingThornWall(parent, PlayerSpawn + new Vector2(470f, -130f),
			new Vector2(0f, 65f), 5, "EastThornWall");

		// ── Blocking trees near SW outpost ──
		AddBlockingLandmark(parent, UndeadObjectsPath + "/Dead_tree_shadow1_1.png",
			PlayerSpawn + new Vector2(-430f, 160f), 0.65f, "DeadTree_SW",
			new Vector2(44f, 36f), new Vector2(0f, 22f));

		// ── Thorn plant at cursed zone entrance ──
		AddBlockingLandmark(parent, UndeadObjectsPath + "/Thorn_plant_shadow3_2.png",
			PlayerSpawn + new Vector2(200f, 100f), 0.58f, "ThornPlant_CursedEntry",
			new Vector2(38f, 30f), new Vector2(0f, 12f));
	}

	// ─── Animated landmarks (in AnimatedLandmarks layer, Y-sorted) ───
	private void AddAnimatedLandmarks()
	{
		if (_animatedLayer == null)
			return;

		AddAnimatedAmbient(_animatedLayer, AnimatedSkullGate,
			PlayerSpawn + new Vector2(-480f, -20f), 0, "AnimSkullGate");
		AddAnimatedAmbient(_animatedLayer, AnimatedLich,
			PlayerSpawn + new Vector2(390f, -170f), 1, "AnimLichShrine");
		AddAnimatedAmbient(_animatedLayer, AnimatedCultist,
			PlayerSpawn + new Vector2(430f, 200f), 2, "AnimCursedSentinel");
		AddAnimatedAmbient(_animatedLayer, AnimatedDeadTreeLarge,
			PlayerSpawn + new Vector2(-460f, -240f), 0, "AnimDeadTree_NW");
		AddAnimatedAmbient(_animatedLayer, AnimatedDeadTreeSmall,
			PlayerSpawn + new Vector2(460f, -230f), 1, "AnimDeadTree_NE");
	}

	// ═══════════════════════════════════════════════════════════════
	//  TILE HELPERS – real sprite-based tile placement
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Fill an area with tiled ground sprites. Every grid cell within the area gets a tile.
	/// </summary>
	private void AddTiledFloor(Node2D parent, string prefix, Rect2 area,
		IReadOnlyList<TileAtlasRegion> tiles, int tileSize, int zIndex, Color modulate)
	{
		if (tiles.Count == 0) return;

		int minX = Mathf.FloorToInt(area.Position.X / tileSize);
		int maxX = Mathf.CeilToInt(area.End.X / tileSize);
		int minY = Mathf.FloorToInt(area.Position.Y / tileSize);
		int maxY = Mathf.CeilToInt(area.End.Y / tileSize);
		int index = 0;

		for (int y = minY; y < maxY; y++)
		{
			for (int x = minX; x < maxX; x++)
			{
				Vector2 position = new(x * tileSize + tileSize * 0.5f, y * tileSize + tileSize * 0.5f);
				if (!area.HasPoint(position))
					continue;

				var selected = PickTile(tiles, x, y, index);
				var texture = GD.Load<Texture2D>(selected.SourcePath);
				if (texture == null) continue;

				var sprite = new Sprite2D
				{
					Name = prefix + index++,
					Texture = new AtlasTexture { Atlas = texture, Region = selected.Region },
					Centered = true,
					Position = position,
					ZIndex = zIndex,
					Modulate = modulate
				};
				sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
				parent.AddChild(sprite);
			}
		}
	}

	/// <summary>
	/// Scatter tiles sparsely within an area (for detail/crack layers).
	/// Uses deterministic hash so placement is stable.
	/// </summary>
	private void AddScatteredTiles(Node2D parent, string prefix, Rect2 area,
		IReadOnlyList<TileAtlasRegion> tiles, int tileSize, float fillChance, int zIndex)
	{
		if (tiles.Count == 0) return;

		int minX = Mathf.FloorToInt(area.Position.X / tileSize) - 1;
		int maxX = Mathf.CeilToInt(area.End.X / tileSize) + 1;
		int minY = Mathf.FloorToInt(area.Position.Y / tileSize) - 1;
		int maxY = Mathf.CeilToInt(area.End.Y / tileSize) + 1;
		int index = 0;

		for (int y = minY; y <= maxY; y++)
		{
			for (int x = minX; x <= maxX; x++)
			{
				Vector2 position = new(x * tileSize + tileSize * 0.5f, y * tileSize + tileSize * 0.5f);
				if (!area.HasPoint(position))
					continue;
				if (position.DistanceTo(PlayerSpawn) < PlayerVisualClearRadius)
					continue;
				if (!ShouldPlaceStructuredTile(x, y, fillChance))
					continue;

				var selected = PickTile(tiles, x, y, index);
				var texture = GD.Load<Texture2D>(selected.SourcePath);
				if (texture == null) continue;

				var sprite = new Sprite2D
				{
					Name = prefix + index++,
					Texture = new AtlasTexture { Atlas = texture, Region = selected.Region },
					Centered = true,
					Position = position + GetStructuredTileOffset(x, y),
					ZIndex = zIndex,
					Rotation = GetStructuredTileRotation(x, y)
				};
				sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
				parent.AddChild(sprite);
			}
		}
	}

	// ═══════════════════════════════════════════════════════════════
	//  PROP HELPERS
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Place a rock line with blocking collision along an edge.
	/// </summary>
	private void AddBlockingRockLine(Node2D parent, Vector2 start, Vector2 step, int count, string prefix)
	{
		for (int i = 0; i < count; i++)
		{
			Vector2 pos = start + step * i + new Vector2(0f, (i % 2) * 10f);
			string rockAsset = UndeadObjectsPath + "/Rock_shadow" + (1 + i % 3) + "_1.png";
			AddBlockingLandmark(parent, rockAsset, pos, 0.68f,
				prefix + i, new Vector2(48f, 32f), new Vector2(0f, 14f));
		}
	}

	/// <summary>
	/// Place a thorn wall with blocking collision along an edge.
	/// </summary>
	private void AddBlockingThornWall(Node2D parent, Vector2 start, Vector2 step, int count, string prefix)
	{
		for (int i = 0; i < count; i++)
		{
			Vector2 pos = start + step * i + new Vector2((i % 2) * 8f, 0f);
			string thornAsset = UndeadObjectsPath + "/Thorn_plant_shadow1_" + (1 + i % 3) + ".png";
			AddBlockingLandmark(parent, thornAsset, pos, 0.62f,
				prefix + i, new Vector2(36f, 28f), new Vector2(0f, 12f));
		}
	}

	/// <summary>
	/// Add a static sprite with no collision.
	/// </summary>
	private static void AddStaticLandmark(Node2D parent, string path, Vector2 position, float scale, string name)
	{
		var texture = GD.Load<Texture2D>(path);
		if (texture == null)
			return;

		var sprite = new Sprite2D
		{
			Name = name,
			Texture = texture,
			Centered = true,
			Position = position,
			ZIndex = Mathf.RoundToInt(position.Y)
		};
		sprite.Scale = new Vector2(scale, scale);
		parent.AddChild(sprite);
	}

	/// <summary>
	/// Add a sprite + StaticBody2D collision (for blocking props).
	/// </summary>
	private static void AddBlockingLandmark(Node2D parent, string path, Vector2 position,
		float scale, string name, Vector2 collisionSize, Vector2 collisionOffset)
	{
		var root = new Node2D
		{
			Name = name,
			Position = position,
			ZIndex = Mathf.RoundToInt(position.Y)
		};
		parent.AddChild(root);

		var texture = GD.Load<Texture2D>(path);
		if (texture != null)
		{
			var sprite = new Sprite2D
			{
				Name = name + "Visual",
				Texture = texture,
				Centered = true,
				Scale = new Vector2(scale, scale)
			};
			root.AddChild(sprite);
		}

		var body = new StaticBody2D
		{
			Name = name + "Blocker",
			Position = collisionOffset,
			CollisionLayer = ArenaCollisionLayer,
			CollisionMask = PlayerCollisionLayer
		};
		root.AddChild(body);

		body.AddChild(new CollisionShape2D
		{
			Name = name + "Shape",
			Shape = new RectangleShape2D { Size = collisionSize }
		});
	}

	// ═══════════════════════════════════════════════════════════════
	//  ANIMATED AMBIENT
	// ═══════════════════════════════════════════════════════════════

	private void AddAnimatedAmbient(Node2D parent, AmbientAnimation animation, Vector2 position, int row, string name)
	{
		var frames = CreateAmbientFrames(animation, Mathf.Clamp(row, 0, animation.Rows - 1));
		if (frames == null)
			return;

		var sprite = new AnimatedSprite2D
		{
			Name = name,
			SpriteFrames = frames,
			Animation = "idle",
			Autoplay = "idle",
			Centered = true,
			Position = position,
			ZIndex = Mathf.RoundToInt(position.Y)
		};

		float scale = _rng.RandfRange(animation.MinScale, animation.MaxScale);
		sprite.Scale = new Vector2(scale, scale);
		parent.AddChild(sprite);
		sprite.Play("idle");
	}

	private SpriteFrames CreateAmbientFrames(AmbientAnimation animation, int row)
	{
		var texture = GD.Load<Texture2D>(UndeadAnimationPath + "/" + animation.FileName);
		if (texture == null)
			return null;

		int frameWidth = texture.GetWidth() / animation.Columns;
		int frameHeight = texture.GetHeight() / animation.Rows;
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		frames.AddAnimation("idle");
		frames.SetAnimationLoop("idle", true);
		frames.SetAnimationSpeed("idle", animation.Speed);

		for (int i = 0; i < animation.Columns; i++)
		{
			var atlas = new AtlasTexture
			{
				Atlas = texture,
				Region = new Rect2(i * frameWidth, row * frameHeight, frameWidth, frameHeight)
			};
			frames.AddFrame("idle", atlas);
		}

		return frames;
	}

	// ═══════════════════════════════════════════════════════════════
	//  NPC SOLDIERS
	// ═══════════════════════════════════════════════════════════════

	private void SetupNpcSoldiers()
	{
		var ySortRoot = GetNodeOrNull<Node2D>(YSortPath);
		if (ySortRoot == null)
			return;

		ySortRoot.GetNodeOrNull<Node>("NPCs")?.QueueFree();

		var npcRoot = new Node2D { Name = "NPCs", YSortEnabled = true };
		ySortRoot.AddChild(npcRoot);

		for (int i = 0; i < SoldierSpawns.Length; i++)
			AddNpcSoldier(npcRoot, SoldierSpawns[i], i);
	}

	private void AddNpcSoldier(Node2D parent, NpcSoldierSpawn spawn, int index)
	{
		var frames = GD.Load<SpriteFrames>(NpcFramesPath + "/" + spawn.FramesFile);
		if (frames == null)
			return;

		var npc = new NPCController
		{
			Name = "SoldierNPC" + (index + 1),
			Position = spawn.Position,
			WalkSpeed = 28f + index * 3f,
			WaitTime = 1.2f + index * 0.25f,
			CollisionLayer = 0,
			CollisionMask = 0
		};

		var sprite = new AnimatedSprite2D
		{
			Name = "AnimatedSprite2D",
			SpriteFrames = frames,
			Animation = "idle_down",
			Autoplay = "idle_down",
			Centered = true,
			Position = Vector2.Zero,
			Scale = new Vector2(spawn.Scale, spawn.Scale)
		};
		npc.AddChild(sprite);

		var collision = new CollisionShape2D
		{
			Name = "BodyShape",
			Shape = new CapsuleShape2D
			{
				Radius = 10f,
				Height = 28f
			},
			Position = new Vector2(0f, 8f)
		};
		npc.AddChild(collision);

		parent.AddChild(npc);
		npc.AddToGroup("friendly_npc");

		var patrolA = new Marker2D
		{
			Name = "PatrolA",
			Position = spawn.Position
		};
		var patrolB = new Marker2D
		{
			Name = "PatrolB",
			Position = spawn.Position + spawn.PatrolOffset
		};
		parent.AddChild(patrolA);
		parent.AddChild(patrolB);

		npc.Waypoints = new NodePath[]
		{
			npc.GetPathTo(patrolA),
			npc.GetPathTo(patrolB)
		};
	}

	// ═══════════════════════════════════════════════════════════════
	//  ARENA / SPAWNS / CAMERA
	// ═══════════════════════════════════════════════════════════════

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

	private void SetupSpawnPoints()
	{
		Node2D spawnRoot = GetNodeOrNull<Node2D>(SpawnPointsPath);
		if (spawnRoot == null)
		{
			var world = GetNodeOrNull<Node2D>(WorldPath);
			spawnRoot = new Node2D { Name = "SpawnPoints" };
			world?.AddChild(spawnRoot);
		}

		foreach (Node child in spawnRoot.GetChildren())
			child.QueueFree();

		Vector2 center = _arenaRect.GetCenter();
		Vector2[] spawnPositions =
		{
			new(_arenaRect.Position.X + 180f, center.Y),
			new(_arenaRect.End.X - 180f, center.Y),
			new(center.X, _arenaRect.Position.Y + 145f),
			new(center.X, _arenaRect.End.Y - 145f),
			new(_arenaRect.Position.X + 260f, _arenaRect.Position.Y + 210f),
			new(_arenaRect.End.X - 260f, _arenaRect.Position.Y + 210f),
			new(_arenaRect.Position.X + 275f, _arenaRect.End.Y - 210f),
			new(_arenaRect.End.X - 275f, _arenaRect.End.Y - 210f)
		};

		for (int i = 0; i < spawnPositions.Length; i++)
		{
			var point = new Node2D { Name = "SpawnPoint" + i, Position = spawnPositions[i] };
			point.AddToGroup("spawn_point");
			spawnRoot.AddChild(point);
		}
	}

	private void SetupPlayerEnemyPositions()
	{
		var player = FindChild("Player", true, false) as Node2D;
		if (player != null)
		{
			player.Position = PlayerSpawn;
			player.AddToGroup("player");
			if (player is PlayerController playerController)
				playerController.SetPlayableBounds(_arenaRect);

			if (player is CollisionObject2D playerCollision)
				playerCollision.CollisionMask |= ArenaCollisionLayer;
		}
		else
		{
			GD.PushWarning("[TilemapSetup] Could not find Player node.");
		}
	}

	private void SetupCameraFocus()
	{
		var camera = GetNodeOrNull<Camera2D>(CameraPath);
		if (camera == null)
			return;

		camera.Zoom = CameraZoom;
		camera.GlobalPosition = PlayerSpawn;
		camera.Enabled = true;
		camera.MakeCurrent();
		camera.LimitLeft = Mathf.FloorToInt(_mapRect.Position.X);
		camera.LimitTop = Mathf.FloorToInt(_mapRect.Position.Y);
		camera.LimitRight = Mathf.CeilToInt(_mapRect.End.X);
		camera.LimitBottom = Mathf.CeilToInt(_mapRect.End.Y);

		if (camera is IsometricCamera isoCamera)
		{
			isoCamera.SetZoom(CameraZoom.X);
			isoCamera.SetBounds(_mapRect);
			isoCamera.SetTarget(FindChild("Player", true, false) as Node2D);
			isoCamera.SnapTo(PlayerSpawn);
		}
	}

	// ═══════════════════════════════════════════════════════════════
	//  LOW-LEVEL PRIMITIVES
	// ═══════════════════════════════════════════════════════════════

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

	/// <summary>
	/// Add a subtle colored polygon. Only used for shadow tints under real tiles,
	/// never as the main visible design element.
	/// </summary>
	private static void AddFilledRect(Node2D parent, string name, Rect2 rect, Color color, int zIndex)
	{
		var polygon = new Polygon2D
		{
			Name = name,
			Color = color,
			Position = rect.GetCenter(),
			ZIndex = zIndex,
			Polygon = new Vector2[]
			{
				new(-rect.Size.X * 0.5f, -rect.Size.Y * 0.5f),
				new(rect.Size.X * 0.5f, -rect.Size.Y * 0.5f),
				new(rect.Size.X * 0.5f, rect.Size.Y * 0.5f),
				new(-rect.Size.X * 0.5f, rect.Size.Y * 0.5f)
			}
		};
		parent.AddChild(polygon);
	}

	// ─── Deterministic hash helpers for tile variety ───
	private static bool ShouldPlaceStructuredTile(int x, int y, float fillChance)
	{
		int threshold = Mathf.Clamp(Mathf.RoundToInt(fillChance * 100f), 1, 100);
		int hash = Mathf.Abs(x * 73856093 ^ y * 19349663 ^ 83492791) % 100;
		return hash < threshold;
	}

	private static Vector2 GetStructuredTileOffset(int x, int y)
	{
		int hashX = Mathf.Abs(x * 92821 ^ y * 68917) % 13;
		int hashY = Mathf.Abs(x * 31337 ^ y * 11719) % 11;
		return new Vector2(hashX - 6f, hashY - 5f);
	}

	private static float GetStructuredTileRotation(int x, int y)
	{
		int hash = Mathf.Abs(x * 19937 ^ y * 44497) % 7;
		return (hash - 3) * 0.018f;
	}

	private TileAtlasRegion PickTile(IReadOnlyList<TileAtlasRegion> tiles, int x, int y, int index)
	{
		float totalWeight = 0f;
		foreach (var tile in tiles)
			totalWeight += Mathf.Max(0.01f, tile.Weight);

		int hash = Mathf.Abs(x * 374761393 ^ y * 668265263 ^ index * 982451653);
		float roll = hash / (float)int.MaxValue * totalWeight;
		foreach (var tile in tiles)
		{
			roll -= Mathf.Max(0.01f, tile.Weight);
			if (roll <= 0f)
				return tile;
		}

		return tiles[0];
	}
}
