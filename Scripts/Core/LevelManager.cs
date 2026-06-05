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
		ClearExistingEnemies();
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
		_aliveCount = 0;

		EmitSignal(SignalName.WaveStarted, CurrentWave);
		GD.Print($"[LevelManager] Wave {CurrentWave} — spawning {count} enemies.");

		for (int i = 0; i < count; i++)
		{
			SpawnEnemy();
			// Stagger spawns so they don't all appear at once
			await ToSignal(GetTree().CreateTimer(0.35f, false), SceneTreeTimer.SignalName.Timeout);
		}

		if (_aliveCount == 0 && _waveActive)
		{
			GD.Print("[LevelManager] Warning: no safe enemy spawn positions were available.");
			OnWaveCleared();
		}
	}

	private bool SpawnEnemy()
	{
		if (EnemyScene == null)
		{
			GD.Print("[LevelManager] Warning: EnemyScene is not assigned; cannot spawn enemy.");
			return false;
		}

		if (!ResolveYSort())
		{
			GD.Print($"[LevelManager] Warning: YSort node not found at '{YSortPath}'; cannot spawn enemy.");
			return false;
		}

		Vector2? spawnPosition = null;
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

		spawnPosition ??= PickFallbackSpawnPosition();
		if (spawnPosition == null)
			return false;

		// Alternate grunt / standard based on wave (every 3rd enemy is a grunt)
		PackedScene scene = (GruntScene != null && _rng.Randf() < 0.35f)
			? GruntScene
			: EnemyScene;

		var enemy = scene.Instantiate<EnemyBase>();
		_ySort.AddChild(enemy);

		enemy.GlobalPosition = spawnPosition.Value;

		// Add to group so LevelManager can track them
		enemy.AddToGroup("enemy");
		enemy.EnemyDied += OnEnemyDied;
		_aliveCount++;

		GD.Print($"[LevelManager] Spawned {enemy.Name} at {enemy.GlobalPosition}");
		return true;
	}

	private void ClearExistingEnemies()
	{
		foreach (Node node in GetTree().GetNodesInGroup("enemy"))
		{
			if (node is EnemyBase enemy)
				enemy.EnemyDied -= OnEnemyDied;

			node.QueueFree();
		}

		_aliveCount = 0;
	}

	private Vector2? PickFallbackSpawnPosition()
	{
		if (LevelLayoutData.FallbackEnemySpawns.Length == 0)
			return IsUsableSpawnPosition(LevelLayoutData.DefaultEnemySpawn)
				? LevelLayoutData.DefaultEnemySpawn
				: null;

		int startIndex = (int)_rng.RandiRange(0, LevelLayoutData.FallbackEnemySpawns.Length - 1);
		for (int offset = 0; offset < LevelLayoutData.FallbackEnemySpawns.Length; offset++)
		{
			Vector2 candidate = LevelLayoutData.FallbackEnemySpawns[(startIndex + offset) % LevelLayoutData.FallbackEnemySpawns.Length];
			candidate += new Vector2(_rng.RandfRange(-28f, 28f), _rng.RandfRange(-18f, 18f));
			candidate = ClampToPlayableBounds(candidate);
			if (IsUsableSpawnPosition(candidate))
				return candidate;
		}

		Vector2 defaultSpawn = ClampToPlayableBounds(LevelLayoutData.DefaultEnemySpawn);
		return IsUsableSpawnPosition(defaultSpawn) ? defaultSpawn : null;
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
