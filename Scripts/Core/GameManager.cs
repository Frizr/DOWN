using Godot;

/// <summary>
/// GameManager — Agent 1: Isometric Engine
/// Global singleton (autoload). Manages game state, pause, score, and combos.
///
/// Setup in Godot:
///   Project > Project Settings > Autoload
///   Path : res://Scripts/Core/GameManager.cs
///   Name : GameManager
/// Access anywhere: GameManager.Instance or the autoload name "GameManager".
/// </summary>
public partial class GameManager : Node
{
    // ─── Singleton ────────────────────────────────────────────────────────────

    /// <summary>Global access point. Set in _Ready().</summary>
    public static GameManager Instance { get; private set; }

    // ─── Game State ───────────────────────────────────────────────────────────

    /// <summary>All possible game states for the DOWN session.</summary>
    public enum GameState
    {
        MainMenu,   // Title / menus
        Playing,    // Active gameplay
        Paused,     // Tactical pause (time frozen, UI visible)
        GameOver    // Death screen / run end
    }

    /// <summary>Currently active state. Read-only externally; use transition methods.</summary>
    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    // ─── Signals ──────────────────────────────────────────────────────────────

    /// <summary>Emitted whenever the game state changes.</summary>
    [Signal] public delegate void StateChangedEventHandler(GameState newState);

    /// <summary>Emitted when score updates.</summary>
    [Signal] public delegate void ScoreChangedEventHandler(int newScore);

    /// <summary>Emitted when the combo counter changes.</summary>
    [Signal] public delegate void ComboChangedEventHandler(int comboCount, float multiplier);

    // ─── Score & Combo ────────────────────────────────────────────────────────

    /// <summary>Current raw score (before multiplier).</summary>
    public int Score { get; private set; } = 0;

    /// <summary>Current combo hit-streak count.</summary>
    public int ComboCount { get; private set; } = 0;

    // Combo multiplier tier thresholds
    private const int   ComboTier1 = 5;     // ×1.5
    private const int   ComboTier2 = 10;    // ×2.0
    private const int   ComboTier3 = 20;    // ×3.0
    private const float ComboTimer = 3.0f;  // seconds before combo resets

    private float _comboResetTimer = 0f;

    // ─── Godot Lifecycle ──────────────────────────────────────────────────────

    public override void _Ready()
    {
        // Enforce singleton — if somehow duplicated, remove the extra
        if (Instance != null && Instance != this)
        {
            GD.PushWarning("[GameManager] Duplicate singleton detected — removing extra instance.");
            QueueFree();
            return;
        }

        Instance = this;

        // Keep alive across scene changes (root-level autoload does this automatically,
        // but explicit call makes the intent clear in case of manual instantiation).
        ProcessMode = ProcessModeEnum.Always;
        CurrentState = GameState.Playing;
        GD.Print("[GameManager] State: " + CurrentState);

        GD.Print("[GameManager] Initialized.");
    }

    public override void _Process(double delta)
    {
        // Tick combo decay timer only while playing
        if (CurrentState == GameState.Playing && ComboCount > 0)
        {
            _comboResetTimer -= (float)delta;
            if (_comboResetTimer <= 0f)
                ResetCombo();
        }
    }

    // ─── State Management ────────────────────────────────────────────────────

    /// <summary>
    /// Transition to a new GameState.
    /// Handles engine-level side effects (pause tree, cursor, etc.).
    /// </summary>
    public void SetState(GameState newState)
    {
        if (CurrentState == newState)
            return;

        GameState previous = CurrentState;
        CurrentState = newState;

        // ── Side effects ────────────────────────────────────────────────────
        switch (newState)
        {
            case GameState.Playing:
                GetTree().Paused = false;
                break;

            case GameState.Paused:
                GetTree().Paused = true;    // Freeze physics & process on non-Always nodes
                break;

            case GameState.GameOver:
                GetTree().Paused = false;   // Allow GameOver UI to animate
                ResetCombo();
                break;

            case GameState.MainMenu:
                GetTree().Paused = false;
                ResetScore();
                ResetCombo();
                break;
        }

        GD.Print($"[GameManager] State: {previous} → {newState}");
        EmitSignal(SignalName.StateChanged, (int)newState);
    }

    /// <summary>
    /// Toggle between Playing ↔ Paused.
    /// This is the tactical pause feature — bind to a key (e.g. Space or Tab).
    /// </summary>
    public void TogglePause()
    {
        if (CurrentState == GameState.Playing)
            SetState(GameState.Paused);
        else if (CurrentState == GameState.Paused)
            SetState(GameState.Playing);
    }

    /// <summary>Convenience: is the game actively running (not paused/menu/dead)?</summary>
    public bool IsPlaying() => CurrentState == GameState.Playing;

    // ─── Score ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Add points to the score, automatically applying the current combo multiplier.
    /// </summary>
    /// <param name="basePoints">Raw point value before multiplier.</param>
    public void AddScore(int basePoints)
    {
        int awarded = Mathf.RoundToInt(basePoints * GetComboMultiplier());
        Score += awarded;
        EmitSignal(SignalName.ScoreChanged, Score);
        GD.Print($"[GameManager] +{awarded} pts (×{GetComboMultiplier():F1}) | Total: {Score}");
    }

    /// <summary>Reset score to zero (called on new run / main menu).</summary>
    public void ResetScore()
    {
        Score = 0;
        EmitSignal(SignalName.ScoreChanged, Score);
    }

    // ─── Combo ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Register a successful hit / kill to extend the combo chain.
    /// Resets the combo expiry timer.
    /// </summary>
    public void IncrementCombo()
    {
        ComboCount++;
        _comboResetTimer = ComboTimer;  // Refresh window
        EmitSignal(SignalName.ComboChanged, ComboCount, GetComboMultiplier());
        GD.Print($"[GameManager] Combo ×{ComboCount} | Multiplier: {GetComboMultiplier():F1}");
    }

    /// <summary>
    /// Drop the combo to zero (player took a hit, missed, or timer expired).
    /// </summary>
    public void ResetCombo()
    {
        if (ComboCount == 0)
            return;

        ComboCount = 0;
        _comboResetTimer = 0f;
        EmitSignal(SignalName.ComboChanged, 0, 1.0f);
        GD.Print("[GameManager] Combo broken.");
    }

    /// <summary>
    /// Return the score multiplier for the current combo count.
    /// Tier thresholds are defined as constants above.
    /// </summary>
    public float GetComboMultiplier()
    {
        if (ComboCount >= ComboTier3) return 3.0f;
        if (ComboCount >= ComboTier2) return 2.0f;
        if (ComboCount >= ComboTier1) return 1.5f;
        return 1.0f;
    }
}
