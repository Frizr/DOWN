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

	// ─── Inspector ────────────────────────────────────────────────────────────

	[ExportGroup("Scenes")]
	[Export] public PackedScene EnemyScene;   // Assign res://Scenes/Enemy.tscn in editor
	[Export] public PackedScene GruntScene;   // Optional: res://Scenes/EnemyGrunt.tscn

	[ExportGroup("Wave Settings")]
	[Export] public int   EnemiesPerWave  = 5;
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

	private RandomNumberGenerator _rng = new();

	// ─── Lifecycle ────────────────────────────────────────────────────────────

	public override void _Ready()
	{
		_rng.Randomize();
		if (!ResolveYSort())
			GD.Print($"[LevelManager] Warning: YSort node not found at '{YSortPath}'. Will retry when spawning enemies.");
	}

	private void CollectSpawnPoints()
	{
		_spawnPoints.Clear();

		// Gather all nodes in the "spawn_point" group
		foreach (Node n in GetTree().GetNodesInGroup("spawn_point"))	{
			if (n is Node2D sp)
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
			// Pick a random spawn point
			Node2D spawnPt = _spawnPoints[_rng.RandiRange(0, _spawnPoints.Count - 1)];
			float scatter = _rng.RandfRange(-24f, 24f);
			spawnPosition = spawnPt.GlobalPosition + new Vector2(scatter, scatter * 0.5f);
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
