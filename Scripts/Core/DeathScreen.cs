using Godot;

/// <summary>
/// DeathScreen — Agent 4: UI & Polish
/// Game-over / run-end screen. Reads final score from GameManager,
/// animates entry, and handles Restart / Main Menu buttons.
///
/// Node type : CanvasLayer (layer = 20 — above HUD)
/// Children  : Root (Control, full-rect), TitleLabel, ScoreLabel,
///             ComboLabel, RestartBtn, MenuBtn, AnimationPlayer
/// </summary>
public partial class DeathScreen : CanvasLayer
{
    // ─── Node References ──────────────────────────────────────────────────────

    private Control         _root;
    private Label           _titleLabel;
    private Label           _finalScoreLabel;
    private Label           _highScoreLabel;
    private Button          _restartBtn;
    private Button          _menuBtn;
    private AnimationPlayer _anim;

    // ─── Config ───────────────────────────────────────────────────────────────

    private const string MainMenuScene = "res://Scenes/MainMenu.tscn";
    private const string GameScene     = "res://Scenes/Main.tscn";
    private const string HighScoreKey  = "high_score";

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        _root            = GetNode<Control>("Root");
        _titleLabel      = GetNode<Label>("Root/VBox/TitleLabel");
        _finalScoreLabel = GetNode<Label>("Root/VBox/FinalScoreLabel");
        _highScoreLabel  = GetNode<Label>("Root/VBox/HighScoreLabel");
        _restartBtn      = GetNode<Button>("Root/VBox/Buttons/RestartBtn");
        _menuBtn         = GetNode<Button>("Root/VBox/Buttons/MenuBtn");
        _anim            = GetNode<AnimationPlayer>("AnimationPlayer");

        // Connect buttons
        _restartBtn.Pressed += OnRestartPressed;
        _menuBtn.Pressed    += OnMenuPressed;

        // Listen for game-over state
        if (GameManager.Instance != null)
            GameManager.Instance.StateChanged += OnStateChanged;
        else
            GD.PushWarning("[DeathScreen] GameManager not ready — StateChanged not connected.");

        // Start hidden
        _root.Modulate = new Color(1, 1, 1, 0);
        _root.Visible  = false;
    }

    // ─── State Handler ────────────────────────────────────────────────────────

    private void OnStateChanged(GameManager.GameState newState)
    {
        if (newState != GameManager.GameState.GameOver)
            return;
        ShowDeathScreen();
    }

    // ─── Show / Hide ──────────────────────────────────────────────────────────

    private void ShowDeathScreen()
    {
        _root.Visible = true;

        int finalScore = GameManager.Instance.Score;
        int highScore  = LoadHighScore();

        // Update high score if beaten
        if (finalScore > highScore)
        {
            highScore = finalScore;
            SaveHighScore(highScore);
            _titleLabel.Text = "NEW BEST";
        }
        else
        {
            _titleLabel.Text = "YOU DIED";
        }

        _finalScoreLabel.Text = $"Score  {finalScore:N0}";
        _highScoreLabel.Text  = $"Best   {highScore:N0}";

        // Cinematic entry: fade + scale up
        Tween t = CreateTween().SetParallel();
        t.TweenProperty(_root, "modulate:a",
            1f, 0.6f).SetTrans(Tween.TransitionType.Quart);
        t.TweenProperty(_root, "scale",
            Vector2.One, 0.5f)
            .From(new Vector2(0.85f, 0.85f))
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);

        _anim.Play("death_screen_in");
    }

    // ─── Button Handlers ─────────────────────────────────────────────────────

    private void OnRestartPressed()
    {
        GameManager.Instance.SetState(GameManager.GameState.Playing);
        GetTree().ChangeSceneToFile(GameScene);
    }

    private void OnMenuPressed()
    {
        GameManager.Instance.SetState(GameManager.GameState.MainMenu);
        // MainMenu scene may not exist yet — fall back gracefully
        if (ResourceLoader.Exists(MainMenuScene))
            GetTree().ChangeSceneToFile(MainMenuScene);
        else
            GetTree().ChangeSceneToFile(GameScene);
    }

    // ─── High Score Persistence ───────────────────────────────────────────────

    private static int LoadHighScore()
    {
        using var cfg = new ConfigFile();
        if (cfg.Load("user://save.cfg") != Error.Ok)
            return 0;
        return (int)cfg.GetValue("player", HighScoreKey, 0);
    }

    private static void SaveHighScore(int value)
    {
        using var cfg = new ConfigFile();
        cfg.Load("user://save.cfg");   // Load existing data (OK if missing)
        cfg.SetValue("player", HighScoreKey, value);
        cfg.Save("user://save.cfg");
    }
}
