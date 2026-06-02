using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class TilemapSetup : Node
{
	private const string WorldPath = "World";
	private const string TileMapPath = "World/TileMap";
	private const string GeneratedMapPath = "World/GeneratedMap";
	private const string DecorationsPath = "World/Decorations";
	private const string SpawnPointsPath = "World/SpawnPoints";
	private const string ArenaBoundsPath = "World/ArenaBounds";
	private const string YSortPath = "World/YSort";
	private const string CameraPath = "Camera";

	private const string UndeadObjectsPath = "res://Assets/Tiles/Undead/Objects_separately";
	private const string CursedObjectsPath = "res://Assets/Tiles/CursedLand/Objects_separetely";
	private const string UndeadAnimationPath = "res://Assets/Tiles/Undead";
	private const string UndeadGroundPath = "res://Assets/Tiles/Undead/Ground_rocks.png";
	private const string UndeadDetailsPath = "res://Assets/Tiles/Undead/Details.png";
	private const string CursedGroundPath = "res://Assets/Tiles/CursedLand/Ground.png";
	private const string CursedDetailsPath = "res://Assets/Tiles/CursedLand/details.png";
	private const string NpcFramesPath = "res://Assets/SpriteFrames";

	private static readonly Vector2 PlayerSpawn = new(640f, 420f);
	private static readonly Vector2 MapSize = new(1280f, 832f);
	private static readonly Vector2 CameraZoom = new(1.25f, 1.25f);
	private static readonly Rect2 FallbackArenaRect = new(new Vector2(-960f, -560f), MapSize);
	private static readonly Color ArenaDebugColor = new(1f, 0.82f, 0.25f, 0.75f);
	private const int ArenaWallThickness = 96;
	private const int GroundTileSize = 64;
	private const uint PlayerCollisionLayer = 1;
	private const uint ArenaCollisionLayer = 4;
	private const float MinimumEnemySpawnDistance = 260f;
	private static readonly bool ShowArenaDebug = false;
	private static bool _hasActivePlayableBounds = false;
	private static Rect2 _activePlayableBounds = FallbackArenaRect;

	private readonly RandomNumberGenerator _rng = new();
	private Rect2 _mapRect = FallbackArenaRect;
	private Rect2 _arenaRect = FallbackArenaRect;
	private Node2D _cursedLayer;
	private Node2D _animatedLayer;

	private static readonly string[] UndeadDecorationAssets =
	{
		"Bones_shadow1_1.png", "Bones_shadow2_1.png", "Bones_shadow3_1.png",
		"Grave_shadow1_1.png", "Grave_shadow1_10.png", "Grave_shadow1_14.png",
		"Dead_tree_shadow1_1.png", "Dead_tree_shadow3_1.png",
		"Broken_tree_shadow1_1.png", "Broken_tree_shadow2_1.png",
		"Crystal_shadow1_1.png", "Crystal_shadow2_1.png", "Dead_arm_shadow1_1.png",
		"Ruin_shadow1_1.png", "Ruin_shadow2_1.png", "Thorn_plant_shadow1_1.png",
		"Pile_sculls_shadow1.png", "Rock_shadow1_1.png", "Scull_door_shadow1.png"
	};

	private static readonly string[] CursedDecorationAssets =
	{
		"Eye_plant_shadow1_1.png", "Eye_plant_shadow2_1.png",
		"Jaws_plant_shadow1_1.png", "Jaws_plant_shadow2_1.png",
		"Many_eyes_plant_shadow1_1.png", "Meat_flower_shadow1_1.png",
		"Pustules_shadow1_1.png", "Fetus_shadow1_1.png",
		"Rock1_shadow1_1.png", "Rock2_shadow2_1.png", "Rock3_shadow1_1.png"
	};

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
		new("char_001_frames.tres", PlayerSpawn + new Vector2(-190f, 70f), new Vector2(80f, 0f)),
		new("char_002_frames.tres", PlayerSpawn + new Vector2(-70f, 135f), new Vector2(-70f, 55f)),
		new("char_003_frames.tres", PlayerSpawn + new Vector2(155f, 90f), new Vector2(75f, -45f)),
		new("char_004_frames.tres", PlayerSpawn + new Vector2(265f, -70f), new Vector2(-90f, 0f), 1.5f),
		new("char_005_frames.tres", PlayerSpawn + new Vector2(-315f, -45f), new Vector2(0f, 90f), 1.38f),
		new("char_006_frames.tres", PlayerSpawn + new Vector2(35f, -165f), new Vector2(105f, 35f), 1.42f)
	};

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

	private static readonly TileAtlasRegion[] UndeadGroundTiles =
	{
		new(UndeadGroundPath, new Rect2(192, 64, 64, 64), 3f),
		new(UndeadGroundPath, new Rect2(256, 64, 64, 64), 3f),
		new(UndeadGroundPath, new Rect2(320, 64, 64, 64), 2f),
		new(UndeadGroundPath, new Rect2(192, 128, 64, 64), 2f),
		new(UndeadGroundPath, new Rect2(256, 128, 64, 64), 2f),
		new(UndeadGroundPath, new Rect2(384, 128, 64, 64), 1f)
	};

	private static readonly TileAtlasRegion[] UndeadDetailTiles =
	{
		new(UndeadDetailsPath, new Rect2(0, 0, 64, 64), 1f),
		new(UndeadDetailsPath, new Rect2(64, 0, 64, 64), 1f),
		new(UndeadDetailsPath, new Rect2(256, 0, 64, 64), 1f),
		new(UndeadDetailsPath, new Rect2(384, 0, 64, 64), 1f)
	};

	private static readonly TileAtlasRegion[] CursedGroundTiles =
	{
		new(CursedGroundPath, new Rect2(128, 128, 64, 64), 3f),
		new(CursedGroundPath, new Rect2(192, 128, 64, 64), 3f),
		new(CursedGroundPath, new Rect2(256, 128, 64, 64), 2f),
		new(CursedGroundPath, new Rect2(128, 192, 64, 64), 2f),
		new(CursedGroundPath, new Rect2(192, 192, 64, 64), 2f),
		new(CursedGroundPath, new Rect2(256, 192, 64, 64), 1f)
	};

	private static readonly TileAtlasRegion[] CursedDetailTiles =
	{
		new(CursedDetailsPath, new Rect2(0, 0, 64, 64), 1f),
		new(CursedDetailsPath, new Rect2(64, 0, 64, 64), 1f),
		new(CursedDetailsPath, new Rect2(0, 64, 64, 64), 1f),
		new(CursedDetailsPath, new Rect2(64, 64, 64, 64), 1f)
	};

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

		float intensity = Mathf.Clamp((wave - 1) / 8f, 0f, 1f);
		_cursedLayer.Visible = intensity > 0.01f;
		_cursedLayer.Modulate = new Color(1f, 1f, 1f, Mathf.Lerp(0.12f, 0.95f, intensity));
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

	private void BuildGeneratedMap()
	{
		Node2D world = GetNodeOrNull<Node2D>(WorldPath);
		if (world == null)
			return;

		world.GetNodeOrNull<Node>("BaseGround")?.QueueFree();
		world.GetNodeOrNull<Node>("BaseGroundTiles")?.QueueFree();
		world.GetNodeOrNull<Node>("GeneratedMap")?.QueueFree();

		var generatedMap = new Node2D { Name = "GeneratedMap" };
		world.AddChild(generatedMap);

		var groundLayer = new Node2D { Name = "GroundLayer", ZIndex = -120 };
		_cursedLayer = new Node2D { Name = "CursedLayer", ZIndex = -80 };
		_animatedLayer = new Node2D { Name = "AnimatedLandmarks", YSortEnabled = true };
		generatedMap.AddChild(groundLayer);
		generatedMap.AddChild(_cursedLayer);
		generatedMap.AddChild(_animatedLayer);

		AddFilledRect(groundLayer, "UndeadBaseFallback", _mapRect, new Color(0.18f, 0.21f, 0.19f), -140);
		AddProceduralGroundTexture(groundLayer);
		AddStructuredTileLayout(groundLayer);
		AddMapLandmarks();

		AddCursedRegion(_cursedLayer);
		SetupDecorations();
		SetupNpcSoldiers();
	}

	private void AddProceduralGroundTexture(Node2D parent)
	{
		const int textureWidth = 1280;
		const int textureHeight = 832;
		var image = Image.CreateEmpty(textureWidth, textureHeight, false, Image.Format.Rgba8);
		var random = new RandomNumberGenerator();
		random.Seed = 7327;

		for (int y = 0; y < textureHeight; y++)
		{
			for (int x = 0; x < textureWidth; x++)
			{
				float grain = random.RandfRange(-0.025f, 0.025f);
				float wave = Mathf.Sin(x * 0.021f) * 0.015f + Mathf.Cos(y * 0.017f) * 0.012f;
				image.SetPixel(x, y, new Color(0.18f + grain + wave, 0.215f + grain, 0.19f + grain * 0.8f, 1f));
			}
		}

		AddGroundBlobs(image, random, textureWidth, textureHeight, 140, new Color(0.225f, 0.24f, 0.215f, 1f), 6f, 28f);
		AddGroundBlobs(image, random, textureWidth, textureHeight, 110, new Color(0.13f, 0.16f, 0.15f, 1f), 8f, 34f);
		AddGroundBlobs(image, random, textureWidth, textureHeight, 70, new Color(0.29f, 0.29f, 0.25f, 1f), 4f, 14f);
		PaintSoftRect(image, new Rect2(PlayerSpawn + new Vector2(-510f, -42f), new Vector2(1020f, 84f)), new Color(0.36f, 0.34f, 0.27f, 1f), 0.48f);
		PaintSoftRect(image, new Rect2(PlayerSpawn + new Vector2(-46f, -300f), new Vector2(92f, 600f)), new Color(0.34f, 0.32f, 0.25f, 1f), 0.42f);
		PaintSoftEllipse(image, PlayerSpawn, new Vector2(185f, 128f), new Color(0.39f, 0.37f, 0.29f, 1f), 0.52f);
		PaintSoftEllipse(image, PlayerSpawn + new Vector2(-390f, -230f), new Vector2(170f, 118f), new Color(0.25f, 0.27f, 0.23f, 1f), 0.40f);
		PaintSoftEllipse(image, PlayerSpawn + new Vector2(350f, 190f), new Vector2(185f, 122f), new Color(0.24f, 0.22f, 0.20f, 1f), 0.42f);
		PaintSoftEllipse(image, PlayerSpawn + new Vector2(-350f, 185f), new Vector2(140f, 96f), new Color(0.34f, 0.32f, 0.26f, 1f), 0.38f);
		PaintSoftEllipse(image, PlayerSpawn + new Vector2(410f, -215f), new Vector2(165f, 112f), new Color(0.18f, 0.24f, 0.21f, 1f), 0.46f);

		var texture = ImageTexture.CreateFromImage(image);

		var sprite = new Sprite2D
		{
			Name = "ProceduralUndeadGround",
			Texture = texture,
			Centered = true,
			Position = _mapRect.GetCenter(),
			Scale = new Vector2(_mapRect.Size.X / textureWidth, _mapRect.Size.Y / textureHeight),
			ZIndex = -119
		};
		sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		parent.AddChild(sprite);
	}

	private void PaintSoftRect(Image image, Rect2 worldRect, Color color, float strength)
	{
		int left = Mathf.Clamp(Mathf.FloorToInt(worldRect.Position.X - _mapRect.Position.X), 0, image.GetWidth() - 1);
		int right = Mathf.Clamp(Mathf.CeilToInt(worldRect.End.X - _mapRect.Position.X), 0, image.GetWidth() - 1);
		int top = Mathf.Clamp(Mathf.FloorToInt(worldRect.Position.Y - _mapRect.Position.Y), 0, image.GetHeight() - 1);
		int bottom = Mathf.Clamp(Mathf.CeilToInt(worldRect.End.Y - _mapRect.Position.Y), 0, image.GetHeight() - 1);
		float feather = 34f;

		for (int y = top; y <= bottom; y++)
		{
			for (int x = left; x <= right; x++)
			{
				float worldX = x + _mapRect.Position.X;
				float worldY = y + _mapRect.Position.Y;
				float distanceToEdge = Mathf.Min(
					Mathf.Min(worldX - worldRect.Position.X, worldRect.End.X - worldX),
					Mathf.Min(worldY - worldRect.Position.Y, worldRect.End.Y - worldY)
				);
				float alpha = Mathf.Clamp(distanceToEdge / feather, 0f, 1f) * strength;
				image.SetPixel(x, y, image.GetPixel(x, y).Lerp(color, alpha));
			}
		}
	}

	private void PaintSoftEllipse(Image image, Vector2 worldCenter, Vector2 radius, Color color, float strength)
	{
		int left = Mathf.Clamp(Mathf.FloorToInt(worldCenter.X - radius.X - _mapRect.Position.X), 0, image.GetWidth() - 1);
		int right = Mathf.Clamp(Mathf.CeilToInt(worldCenter.X + radius.X - _mapRect.Position.X), 0, image.GetWidth() - 1);
		int top = Mathf.Clamp(Mathf.FloorToInt(worldCenter.Y - radius.Y - _mapRect.Position.Y), 0, image.GetHeight() - 1);
		int bottom = Mathf.Clamp(Mathf.CeilToInt(worldCenter.Y + radius.Y - _mapRect.Position.Y), 0, image.GetHeight() - 1);

		for (int y = top; y <= bottom; y++)
		{
			for (int x = left; x <= right; x++)
			{
				float dx = (x + _mapRect.Position.X - worldCenter.X) / radius.X;
				float dy = (y + _mapRect.Position.Y - worldCenter.Y) / radius.Y;
				float distance = dx * dx + dy * dy;
				if (distance > 1f)
					continue;

				float alpha = Mathf.Clamp(1f - distance, 0f, 1f) * strength;
				image.SetPixel(x, y, image.GetPixel(x, y).Lerp(color, alpha));
			}
		}
	}

	private static void AddGroundBlobs(Image image, RandomNumberGenerator random, int width, int height, int count, Color color, float minRadius, float maxRadius)
	{
		for (int i = 0; i < count; i++)
		{
			Vector2 center = new(random.RandfRange(0f, width), random.RandfRange(0f, height));
			float radiusX = random.RandfRange(minRadius, maxRadius);
			float radiusY = random.RandfRange(minRadius, maxRadius);

			int left = Mathf.Max(0, Mathf.FloorToInt(center.X - radiusX));
			int right = Mathf.Min(width - 1, Mathf.CeilToInt(center.X + radiusX));
			int top = Mathf.Max(0, Mathf.FloorToInt(center.Y - radiusY));
			int bottom = Mathf.Min(height - 1, Mathf.CeilToInt(center.Y + radiusY));

			for (int y = top; y <= bottom; y++)
			{
				for (int x = left; x <= right; x++)
				{
					float dx = (x - center.X) / radiusX;
					float dy = (y - center.Y) / radiusY;
					if (dx * dx + dy * dy > 1f)
						continue;

					Color current = image.GetPixel(x, y);
					image.SetPixel(x, y, current.Lerp(color, 0.35f));
				}
			}
		}
	}

	private void AddStructuredTileLayout(Node2D parent)
	{
		Rect2 mainPlaza = new(PlayerSpawn + new Vector2(-224f, -150f), new Vector2(448f, 300f));
		Rect2 roadCross = new(PlayerSpawn + new Vector2(-560f, -82f), new Vector2(1120f, 164f));
		Rect2 graveyard = new(PlayerSpawn + new Vector2(-520f, -330f), new Vector2(310f, 235f));
		Rect2 ruinYard = new(PlayerSpawn + new Vector2(205f, 90f), new Vector2(350f, 265f));

		AddTileField(parent, "PathPebbles", roadCross, UndeadDetailTiles, GroundTileSize, 0.08f, -115, true);
		AddTileField(parent, "PlazaScars", mainPlaza, UndeadDetailTiles, GroundTileSize, 0.10f, -115, true);
		AddTileField(parent, "GraveyardScars", graveyard, UndeadDetailTiles, GroundTileSize, 0.12f, -115, true);
		AddTileField(parent, "RuinYardScars", ruinYard, UndeadDetailTiles, GroundTileSize, 0.10f, -115, true);
	}

	private void AddCursedRegion(Node2D parent)
	{
		Rect2 cursedRect = new(
			new Vector2(_mapRect.Position.X + _mapRect.Size.X * 0.52f, _mapRect.Position.Y + 180f),
			new Vector2(_mapRect.Size.X * 0.40f, _mapRect.Size.Y - 360f)
		);

		AddTileField(parent, "CursedDetails", cursedRect, CursedDetailTiles, GroundTileSize, 0.20f, -82, true);
		AddDecorationCluster(parent, CursedObjectsPath, CursedDecorationAssets, cursedRect, 36, "CursedObject", 180f);
	}

	private void SetupDecorations()
	{
		Node2D decorationsRoot = GetOrCreateDecorationsRoot();
		foreach (Node child in decorationsRoot.GetChildren())
			child.QueueFree();

		AddStructuredDecorations(decorationsRoot);
	}

	private void AddStructuredDecorations(Node2D parent)
	{
		AddStaticLandmark(parent, UndeadObjectsPath + "/Ruin_shadow3_5.png", PlayerSpawn + new Vector2(370f, 205f), 1.15f, "SouthEastRuin");
		AddStaticLandmark(parent, UndeadObjectsPath + "/Ruin_shadow2_3.png", PlayerSpawn + new Vector2(245f, 265f), 1.0f, "SouthRuinWall");
		AddStaticLandmark(parent, UndeadObjectsPath + "/Pile_sculls_shadow3.png", PlayerSpawn + new Vector2(-335f, 225f), 1.05f, "GuardPostSkullPile");
		AddStaticLandmark(parent, UndeadObjectsPath + "/Dead_tree_shadow3_3.png", PlayerSpawn + new Vector2(-525f, -285f), 1.05f, "NorthWestDeadTree");
		AddStaticLandmark(parent, UndeadObjectsPath + "/Broken_tree_shadow2_2.png", PlayerSpawn + new Vector2(520f, -255f), 1.0f, "NorthEastBrokenTree");

		Vector2[] graves =
		{
			new(-455f, -270f), new(-385f, -282f), new(-315f, -268f),
			new(-455f, -205f), new(-382f, -218f), new(-312f, -202f),
			new(-448f, -140f), new(-372f, -150f), new(-298f, -132f)
		};

		for (int i = 0; i < graves.Length; i++)
			AddStaticLandmark(parent, UndeadObjectsPath + "/Grave_shadow1_" + (1 + i % 6) + ".png", PlayerSpawn + graves[i], 0.92f, "GraveRow" + i);

		Vector2[] rocks =
		{
			new(-560f, 285f), new(-505f, 330f), new(-200f, 315f),
			new(500f, 310f), new(545f, 230f), new(535f, -330f),
			new(-555f, -330f), new(-110f, -350f)
		};

		for (int i = 0; i < rocks.Length; i++)
			AddStaticLandmark(parent, UndeadObjectsPath + "/Rock_shadow" + (1 + i % 3) + "_1.png", PlayerSpawn + rocks[i], 0.88f, "BoundaryRock" + i);

		Vector2[] thorns =
		{
			new(-585f, -85f), new(-585f, 85f), new(590f, -80f), new(590f, 90f),
			new(-185f, -365f), new(180f, -365f), new(-160f, 365f), new(155f, 365f)
		};

		for (int i = 0; i < thorns.Length; i++)
			AddStaticLandmark(parent, UndeadObjectsPath + "/Thorn_plant_shadow1_" + (1 + i % 3) + ".png", PlayerSpawn + thorns[i], 0.9f, "BoundaryThorn" + i);
	}

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

	private void AddMapLandmarks()
	{
		if (_animatedLayer == null)
			return;

		AddAnimatedAmbient(_animatedLayer, AnimatedSkullGate, PlayerSpawn + new Vector2(-500f, -20f), 0, "AnimatedSkullGate");
		AddAnimatedAmbient(_animatedLayer, AnimatedLich, PlayerSpawn + new Vector2(420f, -220f), 1, "AnimatedLichShrine");
		AddAnimatedAmbient(_animatedLayer, AnimatedCultist, PlayerSpawn + new Vector2(450f, 215f), 2, "AnimatedCursedSentinel");
		AddAnimatedAmbient(_animatedLayer, AnimatedDeadTreeLarge, PlayerSpawn + new Vector2(-520f, -285f), 0, "AnimatedNorthWestTree");
		AddAnimatedAmbient(_animatedLayer, AnimatedDeadTreeSmall, PlayerSpawn + new Vector2(520f, -255f), 1, "AnimatedNorthEastRoot");
	}

	private void AddAnimatedCluster(Node2D parent, AmbientAnimation animation, Rect2 area, int count, string prefix)
	{
		for (int i = 0; i < count; i++)
		{
			Vector2 position = new(
				_rng.RandfRange(area.Position.X + 140f, area.End.X - 140f),
				_rng.RandfRange(area.Position.Y + 120f, area.End.Y - 120f)
			);

			if (position.DistanceTo(PlayerSpawn) < 360f)
				position += new Vector2(460f, _rng.RandfRange(-220f, 220f));

			AddAnimatedAmbient(parent, animation, position, (int)_rng.RandiRange(0, animation.Rows - 1), prefix + i);
		}
	}

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

	private void AddDecorationCluster(Node2D parent, string folder, IReadOnlyList<string> assetNames, Rect2 area, int count, string prefix, float keepClearRadius = 260f)
	{
		for (int i = 0; i < count; i++)
		{
			string path = folder + "/" + assetNames[i % assetNames.Count];
			var texture = GD.Load<Texture2D>(path);
			if (texture == null)
				continue;

			Vector2 position = new(
				_rng.RandfRange(area.Position.X + 60f, area.End.X - 60f),
				_rng.RandfRange(area.Position.Y + 60f, area.End.Y - 60f)
			);

			if (position.DistanceTo(PlayerSpawn) < keepClearRadius)
				position += new Vector2(keepClearRadius + 120f, _rng.RandfRange(-180f, 180f));

			var sprite = new Sprite2D
			{
				Name = prefix + i,
				Texture = texture,
				Centered = true,
				Position = position,
				ZIndex = Mathf.RoundToInt(position.Y)
			};

			float scale = _rng.RandfRange(0.72f, 1.15f);
			sprite.Scale = new Vector2(scale, scale);
			parent.AddChild(sprite);
		}
	}

	private void AddTileField(Node2D parent, string prefix, Rect2 area, IReadOnlyList<TileAtlasRegion> tiles, int tileSize, float fillChance, int zIndex, bool scatterOnly = false)
	{
		if (tiles.Count == 0)
			return;

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
				if (!area.HasPoint(position) || _rng.Randf() > fillChance)
					continue;

				var selected = PickTile(tiles);
				var texture = GD.Load<Texture2D>(selected.SourcePath);
				if (texture == null)
					continue;

				var atlas = new AtlasTexture
				{
					Atlas = texture,
					Region = selected.Region
				};

				var sprite = new Sprite2D
				{
					Name = prefix + index++,
					Texture = atlas,
					Centered = true,
					Position = scatterOnly
						? position + new Vector2(_rng.RandfRange(-18f, 18f), _rng.RandfRange(-14f, 14f))
						: position,
					ZIndex = zIndex
				};

				if (scatterOnly)
					sprite.Rotation = _rng.RandfRange(-0.08f, 0.08f);

				parent.AddChild(sprite);
			}
		}
	}

	private TileAtlasRegion PickTile(IReadOnlyList<TileAtlasRegion> tiles)
	{
		float totalWeight = 0f;
		foreach (var tile in tiles)
			totalWeight += Mathf.Max(0.01f, tile.Weight);

		float roll = _rng.RandfRange(0f, totalWeight);
		foreach (var tile in tiles)
		{
			roll -= Mathf.Max(0.01f, tile.Weight);
			if (roll <= 0f)
				return tile;
		}

		return tiles[0];
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

		var enemy = FindChild("Enemy", true, false) as Node2D;
		if (enemy != null)
		{
			enemy.Position = PickEnemySpawnPoint();
			enemy.AddToGroup("enemy");
		}
		else
		{
			GD.PushWarning("[TilemapSetup] Could not find Enemy node.");
		}
	}

	private Vector2 PickEnemySpawnPoint()
	{
		Vector2[] candidates =
		{
			new(_arenaRect.Position.X + 230f, _arenaRect.Position.Y + 205f),
			new(_arenaRect.End.X - 230f, _arenaRect.Position.Y + 245f),
			new(_arenaRect.End.X - 260f, _arenaRect.End.Y - 230f),
			new(_arenaRect.Position.X + 275f, _arenaRect.End.Y - 215f)
		};

		int startIndex = (int)_rng.RandiRange(0, candidates.Length - 1);
		for (int offset = 0; offset < candidates.Length; offset++)
		{
			Vector2 candidate = candidates[(startIndex + offset) % candidates.Length];
			if (_arenaRect.HasPoint(candidate) && candidate.DistanceTo(PlayerSpawn) >= MinimumEnemySpawnDistance)
				return candidate;
		}

		return PlayerSpawn + new Vector2(420f, 180f);
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
			isoCamera.SetBounds(_mapRect);
			isoCamera.SetTarget(FindChild("Player", true, false) as Node2D);
			isoCamera.SnapTo(PlayerSpawn);
		}
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

	private static void AddFilledRect(Node2D parent, string name, Rect2 rect, Color color, int zIndex, float rotation = 0f)
	{
		var polygon = new Polygon2D
		{
			Name = name,
			Color = color,
			Position = rect.GetCenter(),
			Rotation = rotation,
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
}
