using Godot;
using System.Collections.Generic;

/// <summary>
/// Small signal-driven audio layer for generated combat/UI SFX.
/// </summary>
public partial class GeneratedAudioFeedback : Node
{
    [Export] public string AttackSlashPath = "res://Assets/Generated/Audio/attack_slash.wav";
    [Export] public string HitImpactPath = "res://Assets/Generated/Audio/hit_impact.wav";
    [Export] public string PlayerHurtPath = "res://Assets/Generated/Audio/player_hurt.wav";
    [Export] public string PlayerDeathPath = "res://Assets/Generated/Audio/player_death.wav";
    [Export] public string EnemyDeathPath = "res://Assets/Generated/Audio/enemy_death.wav";
    [Export] public string WaveStartPath = "res://Assets/Generated/Audio/wave_start.wav";
    [Export] public string UiFeedbackPath = "res://Assets/Generated/Audio/ui_feedback.wav";

    [Export] public float MasterVolumeDb = -8f;
    [Export] public float PitchVariance = 0.04f;
    [Export] public int VoicesPerSound = 4;

    private enum Sfx
    {
        AttackSlash,
        HitImpact,
        PlayerHurt,
        PlayerDeath,
        EnemyDeath,
        WaveStart,
        UiFeedback
    }

    private readonly Dictionary<Sfx, string> _paths = new();
    private readonly Dictionary<Sfx, AudioStream> _streams = new();
    private readonly Dictionary<Sfx, AudioStreamPlayer[]> _players = new();
    private readonly Dictionary<Sfx, int> _nextVoice = new();
    private readonly HashSet<ulong> _connectedPlayers = new();
    private readonly HashSet<ulong> _connectedEnemies = new();
    private readonly HashSet<ulong> _connectedManagers = new();
    private readonly RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        _rng.Randomize();
        ProcessMode = ProcessModeEnum.Always;

        _paths[Sfx.AttackSlash] = AttackSlashPath;
        _paths[Sfx.HitImpact] = HitImpactPath;
        _paths[Sfx.PlayerHurt] = PlayerHurtPath;
        _paths[Sfx.PlayerDeath] = PlayerDeathPath;
        _paths[Sfx.EnemyDeath] = EnemyDeathPath;
        _paths[Sfx.WaveStart] = WaveStartPath;
        _paths[Sfx.UiFeedback] = UiFeedbackPath;

        LoadStreams();
        CallDeferred(nameof(ConnectExistingNodes));

        if (GetTree() != null)
            GetTree().NodeAdded += OnNodeAdded;
    }

    public override void _ExitTree()
    {
        if (GetTree() != null)
            GetTree().NodeAdded -= OnNodeAdded;
    }

    private void LoadStreams()
    {
        int voiceCount = Mathf.Max(1, VoicesPerSound);

        foreach ((Sfx sfx, string path) in _paths)
        {
            var stream = GD.Load<AudioStream>(path);
            if (stream == null)
            {
                GD.PushWarning($"[GeneratedAudioFeedback] Missing SFX asset: {path}");
                continue;
            }

            _streams[sfx] = stream;
            _nextVoice[sfx] = 0;

            var voices = new AudioStreamPlayer[voiceCount];
            for (int i = 0; i < voiceCount; i++)
            {
                var player = new AudioStreamPlayer
                {
                    Name = $"{sfx}Voice{i + 1}",
                    Stream = stream,
                    VolumeDb = MasterVolumeDb
                };
                AddChild(player);
                voices[i] = player;
            }

            _players[sfx] = voices;
        }
    }

    private void ConnectExistingNodes()
    {
        foreach (Node node in GetTree().GetNodesInGroup("player"))
            TryConnectPlayer(node);

        foreach (Node node in GetTree().GetNodesInGroup("enemy"))
            TryConnectEnemy(node);

        foreach (Node node in GetTree().GetNodesInGroup("level_manager"))
            TryConnectLevelManager(node);

        var levelManager = GetTree().CurrentScene?.GetNodeOrNull<LevelManager>("LevelManager")
            ?? GetNodeOrNull<LevelManager>("/root/Main/LevelManager");
        TryConnectLevelManager(levelManager);
    }

    private void OnNodeAdded(Node node)
    {
        TryConnectPlayer(node);
        TryConnectEnemy(node);
        TryConnectLevelManager(node);
    }

    private void TryConnectPlayer(Node node)
    {
        if (node is not PlayerController player || !_connectedPlayers.Add(player.GetInstanceId()))
            return;

        var attack = player.GetNodeOrNull<AttackSystem>("AttackSystem");
        if (attack != null)
        {
            attack.AttackStarted += OnAttackStarted;
            attack.HitConnected += OnHitConnected;
        }

        var health = player.GetNodeOrNull<Health>("Health");
        if (health != null)
        {
            health.DamageTaken += OnPlayerDamageTaken;
            health.Died += OnPlayerDied;
        }
    }

    private void TryConnectEnemy(Node node)
    {
        if (node is not EnemyBase enemy || !_connectedEnemies.Add(enemy.GetInstanceId()))
            return;

        enemy.EnemyDied += OnEnemyDied;
    }

    private void TryConnectLevelManager(Node node)
    {
        if (node is not LevelManager levelManager || !_connectedManagers.Add(levelManager.GetInstanceId()))
            return;

        levelManager.WaveStarted += OnWaveStarted;
        levelManager.WaveCleared += OnWaveCleared;
    }

    private void OnAttackStarted(int comboStep) => Play(Sfx.AttackSlash);

    private void OnHitConnected(Node target, int damage) => Play(Sfx.HitImpact);

    private void OnPlayerDamageTaken(int amount, int currentHp)
    {
        if (currentHp > 0)
            Play(Sfx.PlayerHurt);
    }

    private void OnPlayerDied() => Play(Sfx.PlayerDeath);

    private void OnEnemyDied(EnemyBase enemy) => Play(Sfx.EnemyDeath);

    private void OnWaveStarted(int waveNumber) => Play(Sfx.WaveStart);

    private void OnWaveCleared(int waveNumber) => Play(Sfx.UiFeedback);

    private void Play(Sfx sfx)
    {
        if (!_players.TryGetValue(sfx, out AudioStreamPlayer[] voices) || voices.Length == 0)
            return;

        int index = _nextVoice[sfx] % voices.Length;
        _nextVoice[sfx] = index + 1;

        var player = voices[index];
        player.Stop();
        player.PitchScale = 1f + _rng.RandfRange(-PitchVariance, PitchVariance);
        player.Play();
    }
}
