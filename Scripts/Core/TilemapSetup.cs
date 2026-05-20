using Godot;

[GlobalClass]
public partial class TilemapSetup : Node
{
	private const string TileMapPath = "World/TileMap";
	private const string FloorTexturePath = "res://Assets/Tiles/Floor_title.png";
	private const int TileSize = 128;
	private const int TextureRegionSize = 1024;
	private const int AtlasColumns = 1;
	private const int AtlasRows = 1;
	private const int GridWidth = 30;
	private const int GridHeight = 20;
	private const int SpriteWidth = 32;
	private const int SpriteHeight = 48;
	private const int FloorSourceId = 0;
	private static readonly Vector2I FloorAtlasCoords = new(0, 0);

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
		tileMap.Clear();

		for (int y = 0; y < GridHeight; y++)
		{
			for (int x = 0; x < GridWidth; x++)
				tileMap.SetCell(new Vector2I(x, y), FloorSourceId, FloorAtlasCoords);
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

		source.CreateTile(new Vector2I(0, 0));

		tileSet.AddSource(source, FloorSourceId);
		return tileSet;
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
