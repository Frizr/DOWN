using Godot;

[GlobalClass]
public partial class PlaceholderSetup : Node
{
	private const int SpriteWidth = 32;
	private const int SpriteHeight = 48;

	private static readonly Color PlayerColor = new(0f, 1f, 1f, 1f);
	private static readonly Color EnemyColor = new(1f, 0f, 0f, 1f);
	private static readonly Vector2 SpriteOffset = new(0f, -SpriteHeight / 2f);

	public override void _Ready()
	{
		GD.Print("[Placeholder] _Ready called");
		SetupEntity("Player", PlayerColor, new Vector2(400f, 300f), "player");
		SetupEntity("Enemy", EnemyColor, new Vector2(600f, 300f), "enemy");
	}

	private void SetupEntity(string nodeName, Color color, Vector2 position, string groupName)
	{
		Node2D entity = FindEntity(nodeName);
		if (nodeName == "Player")
			GD.Print("[Placeholder] Player found: " + (entity != null));
		else if (nodeName == "Enemy")
			GD.Print("[Placeholder] Enemy found: " + (entity != null));

		if (entity == null)
		{
			GD.PushWarning($"[PlaceholderSetup] Could not find node named '{nodeName}'.");
			return;
		}

		Sprite2D sprite = FindOrCreateSprite(entity);
		sprite.Texture = CreatePlaceholderTexture(color);
		sprite.Centered = true;
		sprite.Position = SpriteOffset;

		entity.Position = position;
		entity.AddToGroup(groupName);
		EnsureRectangleCollision(entity);
	}

	private Node2D FindEntity(string nodeName)
	{
		// Try direct child path first
		var direct = GetNodeOrNull<Node2D>($"YSort/{nodeName}");
		if (direct != null)
			return direct;

		// Try from scene root
		var fromRoot = GetTree().Root.GetNodeOrNull<Node2D>($"Main/YSort/{nodeName}");
		if (fromRoot != null)
			return fromRoot;

		// Try group search
		var group = GetTree().GetNodesInGroup(nodeName.ToLower());
		if (group.Count > 0)
			return group[0] as Node2D;

		// Deep search
		return FindChild(nodeName, true, false) as Node2D;
	}

	private static Sprite2D FindOrCreateSprite(Node2D entity)
	{
		Sprite2D sprite = entity.GetNodeOrNull<Sprite2D>("Sprite2D")
			?? entity.FindChild("Sprite2D", true, false) as Sprite2D;

		if (sprite != null)
			return sprite;

		sprite = new Sprite2D { Name = "Sprite2D" };
		entity.AddChild(sprite);
		return sprite;
	}

	private static void EnsureRectangleCollision(Node2D entity)
	{
		CollisionShape2D collision = entity.GetNodeOrNull<CollisionShape2D>("Collision")
			?? entity.GetNodeOrNull<CollisionShape2D>("PlaceholderCollision");

		if (collision == null)
		{
			collision = new CollisionShape2D { Name = "PlaceholderCollision" };
			entity.AddChild(collision);
		}

		collision.Shape = new RectangleShape2D
		{
			Size = new Vector2(SpriteWidth, SpriteHeight)
		};
		collision.Position = SpriteOffset;
	}

	private static ImageTexture CreatePlaceholderTexture(Color baseColor)
	{
		Image image = Image.CreateEmpty(SpriteWidth, SpriteHeight, false, Image.Format.Rgba8);
		image.Fill(Colors.Transparent);

		Color outline = new(0f, 0f, 0f, 0.9f);
		Color shadow = new(0f, 0f, 0f, 0.35f);
		Color light = baseColor.Lerp(Colors.White, 0.35f);
		Color dark = baseColor.Lerp(Colors.Black, 0.25f);

		DrawDiamond(image, 16, 43, 13, 5, shadow);

		DrawDiamond(image, 16, 12, 8, 9, outline);
		DrawDiamond(image, 16, 12, 6, 7, light);

		DrawBody(image, 18, 39, 12, outline);
		DrawBody(image, 20, 37, 9, baseColor);

		DrawDiamond(image, 11, 40, 5, 4, outline);
		DrawDiamond(image, 21, 40, 5, 4, outline);
		DrawDiamond(image, 11, 40, 3, 3, dark);
		DrawDiamond(image, 21, 40, 3, 3, dark);

		return ImageTexture.CreateFromImage(image);
	}

	private static void DrawBody(Image image, int topY, int bottomY, int maxHalfWidth, Color color)
	{
		for (int y = topY; y <= bottomY; y++)
		{
			float t = (y - topY) / (float)(bottomY - topY);
			float widthFactor = t < 0.6f
				? Mathf.Lerp(0.45f, 1f, t / 0.6f)
				: Mathf.Lerp(1f, 0.55f, (t - 0.6f) / 0.4f);

			int halfWidth = Mathf.RoundToInt(maxHalfWidth * widthFactor);
			FillSpan(image, 16 - halfWidth, y, 16 + halfWidth, color);
		}
	}

	private static void DrawDiamond(Image image, int centerX, int centerY, int radiusX, int radiusY, Color color)
	{
		for (int y = centerY - radiusY; y <= centerY + radiusY; y++)
		{
			float distance = Mathf.Abs(y - centerY) / (float)radiusY;
			int halfWidth = Mathf.RoundToInt(radiusX * (1f - distance));
			FillSpan(image, centerX - halfWidth, y, centerX + halfWidth, color);
		}
	}

	private static void FillSpan(Image image, int startX, int y, int endX, Color color)
	{
		if (y < 0 || y >= SpriteHeight)
			return;

		for (int x = Mathf.Max(0, startX); x <= Mathf.Min(SpriteWidth - 1, endX); x++)
			image.SetPixel(x, y, color);
	}
}
