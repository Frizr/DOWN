using Godot;

[GlobalClass]
public partial class TilemapSetup : Node
{
	private const string TileMapPath = "World/TileMap";
	private const string FloorTexturePath = "res://Assets/Tiles/Undead/Ground_rocks.png";
	private const int TileSize = 16;
	private const int TextureRegionSize = 16;
	private const int GridWidth = 40;
	private const int GridHeight = 30;
	private const int SpriteWidth = 32;
	private const int SpriteHeight = 48;
	private const int FloorSourceId = 0;
	private static readonly Vector2 TileMapScale = new(4f, 4f);
	private static readonly Vector2I[] FloorAtlasCoords =
	{
		new(5, 7), new(6, 7), new(7, 7), new(8, 7),
		new(9, 7), new(10, 7), new(11, 7), new(12, 7),
		new(5, 8), new(6, 8), new(7, 8), new(8, 8),
		new(9, 8), new(10, 8), new(11, 8), new(12, 8),
		new(13, 14), new(14, 14), new(15, 14), new(16, 14),
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
		tileMap.Position = new Vector2(-960f, -640f);
		tileMap.Scale = TileMapScale;
		tileMap.Clear();

		for (int y = 0; y < GridHeight; y++)
		{
			for (int x = 0; x < GridWidth; x++)
				tileMap.SetCell(new Vector2I(x, y), FloorSourceId, PickFloorTile(x, y));
		}

		SetupPlaceholders();

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

		foreach (Vector2I atlasCoords in FloorAtlasCoords)
			source.CreateTile(atlasCoords);

		tileSet.AddSource(source, FloorSourceId);
		return tileSet;
	}

	private static Vector2I PickFloorTile(int x, int y)
	{
		int index = Mathf.Abs((x * 17) + (y * 31) + ((x + y) % 3)) % FloorAtlasCoords.Length;
		return FloorAtlasCoords[index];
	}

	private void SetupPlaceholders()
	{
		var player = FindChild("Player", true, false) as Node2D;
		var enemy = FindChild("Enemy", true, false) as Node2D;

		if (player != null)
		{
			SetSpriteTexture(player, CreateSolidTexture(new Color(0f, 1f, 1f, 1f)));
			player.Position = new Vector2(960f, 640f);
			player.AddToGroup("player");
		}
		else
		{
			GD.PushWarning("[TilemapSetup] Could not find Player node.");
		}

		if (enemy != null)
		{
			SetSpriteTexture(enemy, CreateSolidTexture(new Color(1f, 0f, 0f, 1f)));
			enemy.Position = new Vector2(1100f, 640f);
			enemy.AddToGroup("enemy");
		}
		else
		{
			GD.PushWarning("[TilemapSetup] Could not find Enemy node.");
		}
	}

	private static void SetSpriteTexture(Node2D entity, Texture2D texture)
	{
		var sprite = entity.GetNodeOrNull<Sprite2D>("Sprite2D")
			?? entity.FindChild("Sprite2D", true, false) as Sprite2D;

		if (sprite == null)
		{
			sprite = new Sprite2D { Name = "Sprite2D" };
			entity.AddChild(sprite);
		}

		sprite.Texture = texture;
		sprite.Centered = true;
	}

	private static ImageTexture CreateSolidTexture(Color color)
	{
		var image = Image.CreateEmpty(SpriteWidth, SpriteHeight, false, Image.Format.Rgba8);
		image.Fill(color);
		return ImageTexture.CreateFromImage(image);
	}
}
