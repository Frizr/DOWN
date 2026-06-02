using Godot;
using Godot.Collections;

/// <summary>
/// LevelManager — Agent 4 / Bonus
/// Manages enemy waves, spawn points, and level-clear detection.
/// Attach to Main.tscn as a child of the root Node2D.
///
/// Node type : Node
/// Reads     : spawn point Node2D children (group "spawn_point")
/// Spawns    : Enemy.tscn instances into the YSort node
/// </summary>
public partial class LevelManager : Node
{
	private static readonly Vector2 DefaultSpawnPosition = new(640f, 360f);
	private const float MinimumSpawnDistanceFromPlayer = 220f;

	// ─── Inspector ────────────────────────────────────────────────────────────

	[ExportGroup("Scenes")]
	[Export] public PackedScene EnemyScene;   // Assign res://Scenes/Enemy.tscn in editor
	[Export] public PackedScene GruntScene;   // Optional: res://Scenes/EnemyGrunt.tscn

	[ExportGroup("Wave Settings")]
	[Export] public bool  AutoStart       = true;
	[Export] public float AutoStartDelay  = 0.8f;
	[Export] public int   EnemiesPerWave  = 4;
	[Export] public float WaveClearDelay  = 2.0f;  // Seconds between wave-clear and next spawn
	[Export] public int   MaxWaves        = 10;     // 0 = endless

	[ExportGroup("References")]
	/// <summary>Set this in the Inspector to the YSort Node2D in your scene.</summary>
	[Export] public NodePath YSortPath = "World/YSort";  // relative path from Main node

	// ─── Signals ──────────────────────────────────────────────────────────────

	[Signal] public delegate void WaveStartedEventHandler(int waveNumber);
	[Signal] public delegate void WaveClearedEventHandler(int waveNumber);
	[Signal] public delegate void AllWavesClearedEventHandler();

	// ─── State ────────────────────────────────────────────────────────────────

	public  int  CurrentWave    { get; private set; } = 0;
	private int  _aliveCount    = 0;
	private bool _waveActive    = false;
	private Node2D _ySort;

	// Spawn points collected from the "spawn_point" group
	private Array<Node2D> _spawnPoints = new();
	private bool _hasPlayableBounds = false;
	private Rect2 _playableBounds;

	private RandomNumberGenerator _rng = new();
	private static readonly Vector2[] FallbackSpawnPositions =
	{
		new(420f, 320f),
		new(800f, 192f),
		new(1080f, 380f),
		new(520f, 620f),
		new(960f, 640f),
		new(320f, 520f),
		new(1120f, 224f),
		new(1152f, 672f)
	};

	// ─── Lifecycle ────────────────────────────────────────────────────────────

	public override void _Ready()
	{
		_rng.Randomize();
		EnemyScene ??= GD.Load<PackedScene>("res://Scenes/Enemy.tscn");
		if (!ResolveYSort())
			GD.Print($"[LevelManager] Warning: YSort node not found at '{YSortPath}'. Will retry when spawning enemies.");

		if (AutoStart)
			CallDeferred(nameof(StartLevelAfterDelay));
	}

	private async void StartLevelAfterDelay()
	{
		await ToSignal(GetTree().CreateTimer(AutoStartDelay, false), SceneTreeTimer.SignalName.Timeout);
		if (GameManager.Instance?.IsPlaying() == true)
			StartLevel();
	}

	private void CollectSpawnPoints()
	{
		_spawnPoints.Clear();
		_hasPlayableBounds = TilemapSetup.TryGetActivePlayableBounds(out _playableBounds);

		// Gather all nodes in the "spawn_point" group
		foreach (Node n in GetTree().GetNodesInGroup("spawn_point"))	{
			if (n is Node2D sp && IsUsableSpawnPosition(sp.GlobalPosition))
				_spawnPoints.Add(sp);
		}

		GD.Print($"[LevelManager] Found {_spawnPoints.Count} spawn points");
	}

	private bool ResolveYSort()
	{
		if (_ySort != null && IsInstanceValid(_ySort))
			return true;

		string path = YSortPath.ToString();
		_ySort = GetNodeOrNull<Node2D>(YSortPath)
			?? GetNodeOrNull<Node2D>($"/root/Main/{path}")
			?? GetNodeOrNull<Node2D>("/root/Main/World/YSort")
			?? GetNodeOrNull<Node2D>("/root/Main/YSort");

		return _ySort != null;
	}

	// ─── Public API ──────────────────────────────────────────────────────────

	/// <summary>Begin wave 1. Call from GameManager or a start-button.</summary>
	public void StartLevel()
	{
		CurrentWave = 0;
		CollectSpawnPoints();
		if (_spawnPoints.Count == 0)
			GD.Print($"[LevelManager] No spawn points found. Using default spawn position {DefaultSpawnPosition}.");
		SpawnNextWave();
	}

	/// <summary>Immediately clear all living enemies (cheat / debug).</summary>
	public void KillAll()
	{
		foreach (Node n in GetTree().GetNodesInGroup("enemy"))
			n.QueueFree();
		_aliveCount = 0;
		OnWaveCleared();
	}

	// ─── Wave Logic ───────────────────────────────────────────────────────────

	private async void SpawnNextWave()
	{
		if (MaxWaves > 0 && CurrentWave >= MaxWaves)
		{
			EmitSignal(SignalName.AllWavesCleared);
			GD.Print("[LevelManager] All waves cleared!");
			return;
		}

		CurrentWave++;
		_waveActive = true;
		(GetParent() as TilemapSetup)?.ApplyWaveTheme(CurrentWave);

		// Scale difficulty: more enemies each wave
		int count = EnemiesPerWave + (CurrentWave - 1) * 2;
		_aliveCount = count;

		EmitSignal(SignalName.WaveStarted, CurrentWave);
		GD.Print($"[LevelManager] Wave {CurrentWave} — spawning {count} enemies.");

		for (int i = 0; i < count; i++)
		{
			SpawnEnemy();
			// Stagger spawns so they don't all appear at once
			await ToSignal(GetTree().CreateTimer(0.35f, false), SceneTreeTimer.SignalName.Timeout);
		}
	}

	private void SpawnEnemy()
	{
		if (EnemyScene == null)
		{
			GD.Print("[LevelManager] Warning: EnemyScene is not assigned; cannot spawn enemy.");
			return;
		}

		if (!ResolveYSort())
		{
			GD.Print($"[LevelManager] Warning: YSort node not found at '{YSortPath}'; cannot spawn enemy.");
			return;
		}

		Vector2 spawnPosition = DefaultSpawnPosition;
		if (_spawnPoints.Count > 0)
		{
			for (int attempt = 0; attempt < 8; attempt++)
			{
				Node2D spawnPt = _spawnPoints[_rng.RandiRange(0, _spawnPoints.Count - 1)];
				float scatter = _rng.RandfRange(-24f, 24f);
				Vector2 candidate = ClampToPlayableBounds(spawnPt.GlobalPosition + new Vector2(scatter, scatter * 0.5f));
				if (IsUsableSpawnPosition(candidate))
				{
					spawnPosition = candidate;
					break;
				}
			}
		}
		else if (FallbackSpawnPositions.Length > 0)
		{
			spawnPosition = PickFallbackSpawnPosition();
		}

		// Alternate grunt / standard based on wave (every 3rd enemy is a grunt)
		PackedScene scene = (GruntScene != null && _rng.Randf() < 0.35f)
			? GruntScene
			: EnemyScene;

		var enemy = scene.Instantiate<EnemyBase>();
		_ySort.AddChild(enemy);

		enemy.GlobalPosition = spawnPosition;

		// Add to group so LevelManager can track them
		enemy.AddToGroup("enemy");
		enemy.EnemyDied += OnEnemyDied;

		GD.Print($"[LevelManager] Spawned {enemy.Name} at {enemy.GlobalPosition}");
	}

	private Vector2 PickFallbackSpawnPosition()
	{
		int startIndex = (int)_rng.RandiRange(0, FallbackSpawnPositions.Length - 1);
		for (int offset = 0; offset < FallbackSpawnPositions.Length; offset++)
		{
			Vector2 candidate = FallbackSpawnPositions[(startIndex + offset) % FallbackSpawnPositions.Length];
			candidate += new Vector2(_rng.RandfRange(-28f, 28f), _rng.RandfRange(-18f, 18f));
			candidate = ClampToPlayableBounds(candidate);
			if (IsUsableSpawnPosition(candidate))
				return candidate;
		}

		return ClampToPlayableBounds(DefaultSpawnPosition);
	}

	private bool IsUsableSpawnPosition(Vector2 position)
	{
		if (_hasPlayableBounds && !_playableBounds.HasPoint(position))
			return false;

		Node2D player = GetPlayer();
		return player == null || position.DistanceTo(player.GlobalPosition) >= MinimumSpawnDistanceFromPlayer;
	}

	private Vector2 ClampToPlayableBounds(Vector2 position)
	{
		if (!_hasPlayableBounds)
			return position;

		return new Vector2(
			Mathf.Clamp(position.X, _playableBounds.Position.X, _playableBounds.End.X),
			Mathf.Clamp(position.Y, _playableBounds.Position.Y, _playableBounds.End.Y)
		);
	}

	private Node2D GetPlayer()
	{
		foreach (Node node in GetTree().GetNodesInGroup("player"))
		{
			if (node is Node2D player && IsInstanceValid(player))
				return player;
		}

		return null;
	}

	// ─── Kill Tracking ────────────────────────────────────────────────────────

	private void OnEnemyDied(EnemyBase _)
	{
		_aliveCount = Mathf.Max(0, _aliveCount - 1);
		GD.Print($"[LevelManager] Enemies remaining: {_aliveCount}");

		if (_aliveCount == 0 && _waveActive)
			OnWaveCleared();
	}

	private async void OnWaveCleared()
	{
		_waveActive = false;
		EmitSignal(SignalName.WaveCleared, CurrentWave);
		GD.Print($"[LevelManager] Wave {CurrentWave} cleared!");

		// Brief pause before next wave
		await ToSignal(
			GetTree().CreateTimer(WaveClearDelay, false),
			SceneTreeTimer.SignalName.Timeout
		);

		if (GameManager.Instance.IsPlaying())
			SpawnNextWave();
	}
}
