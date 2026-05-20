using Godot;

/// <summary>
/// HUD — Agent 4: UI & Polish
/// Connects to GameManager signals and animates all in-game UI elements:
/// health bar, score counter, combo display, and tactical pause banner.
///
/// Node type : CanvasLayer (or Control child of CanvasLayer)
/// Expects the following child nodes (see HUD.tscn):
///   HealthBar (TextureProgressBar), HealthLabel (Label)
///   ScoreLabel (Label), ComboPanel (PanelContainer),
///   ComboLabel (Label), ComboMultLabel (Label),
///   PauseBanner (Control), AnimationPlayer
/// </summary>
public partial class HUD : CanvasLayer
{
	// ─── Node References ──────────────────────────────────────────────────────

	private TextureProgressBar _healthBar;
	private Label              _healthLabel;
	private Label              _scoreLabel;
	private Control            _comboPanel;
	private Label              _comboLabel;
	private Label              _comboMultLabel;
	private Control            _pauseBanner;
	private AnimationPlayer    _anim;

	// ─── Tween Handles ────────────────────────────────────────────────────────

	private Tween _scoreTween;
	private Tween _comboTween;
	private int   _displayedScore = 0;

	// ─── Lifecycle ────────────────────────────────────────────────────────────

	public override void _Ready()
	{
		// Wire child nodes
		_healthBar      = GetNode<TextureProgressBar>("HUDRoot/HealthSection/HealthBar");
		_healthLabel    = GetNode<Label>("HUDRoot/HealthSection/HealthLabel");
		_scoreLabel     = GetNode<Label>("HUDRoot/TopRight/ScoreLabel");
		_comboPanel     = GetNode<Control>("HUDRoot/ComboPanel");
		_comboLabel     = GetNode<Label>("HUDRoot/ComboPanel/ComboVBox/ComboLabel");
		_comboMultLabel = GetNode<Label>("HUDRoot/ComboPanel/ComboVBox/MultLabel");
		_pauseBanner    = GetNode<Control>("PauseBanner");
		_anim           = GetNode<AnimationPlayer>("AnimationPlayer");

		// Connect to GameManager signals
		var gm = GameManager.Instance;
		if (gm != null)
		{
			gm.ScoreChanged  += OnScoreChanged;
			gm.ComboChanged  += OnComboChanged;
			gm.StateChanged  += OnStateChanged;
		}

		// Hide combo panel until first combo hit
		_comboPanel.Modulate = new Color(1, 1, 1, 0);
		_pauseBanner.Visible = false;

		RefreshHealth(1.0f, 100, 100);
	}

	// ─── Signal Handlers ─────────────────────────────────────────────────────

	/// <summary>Animate score rolling up to the new value.</summary>
	private void OnScoreChanged(int newScore)
	{
		_scoreTween?.Kill();
		_scoreTween = CreateTween();
		_scoreTween.TweenMethod(
			Callable.From((int v) => _scoreLabel.Text = $"SCORE  {v:N0}"),
			_displayedScore,
			newScore,
			0.4f
		).SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
		_displayedScore = newScore;
	}

	/// <summary>Pop the combo counter on every hit; fade it if combo breaks.</summary>
	private void OnComboChanged(int count, float multiplier)
	{
		if (count == 0)
		{
			// Fade out
			_comboTween?.Kill();
			_comboTween = CreateTween();
			_comboTween.TweenProperty(_comboPanel, "modulate:a", 0f, 0.4f);
			return;
		}

		// Update text
		_comboLabel.Text     = $"×{count}  COMBO";
		_comboMultLabel.Text = $"({multiplier:F1}× DMG)";

		// Pop animation: scale up then back
		_comboTween?.Kill();
		_comboTween = CreateTween().SetParallel();
		_comboTween.TweenProperty(_comboPanel, "modulate:a", 1f, 0.1f);
		_comboTween.TweenProperty(_comboPanel, "scale",
			new Vector2(1.18f, 1.18f), 0.05f)
			.SetTrans(Tween.TransitionType.Back);

		// Settle back
		Tween settle = CreateTween();
		settle.TweenProperty(_comboPanel, "scale",
			Vector2.One, 0.15f)
			.SetDelay(0.05f)
			.SetTrans(Tween.TransitionType.Elastic)
			.SetEase(Tween.EaseType.Out);

		// Color by tier
		_comboLabel.AddThemeColorOverride("font_color", MultiplierColor(multiplier));
	}

	private void OnStateChanged(GameManager.GameState newState)
	{
	_pauseBanner.Visible = (newState == GameManager.GameState.Paused);
	if (newState == GameManager.GameState.Paused)
		_anim.Play("pause_in");
	else if (newState == GameManager.GameState.Playing)
		_anim.PlayBackwards("pause_in");
	}

	// ─── Public API ───────────────────────────────────────────────────────────

	/// <summary>
	/// Call this every frame from the Player (or via signal) to update the health bar.
	/// percent  : 0.0 → 1.0
	/// </summary>
	public void RefreshHealth(float percent, int current, int max)
	{
		_healthBar.Value = percent * _healthBar.MaxValue;
		_healthLabel.Text = $"{current} / {max}";

		// Pulse red when low health
		Color target = percent < 0.3f
			? new Color(1f, 0.2f, 0.2f)
			: new Color(0.2f, 0.9f, 0.4f);

		_healthBar.TintProgress = target;
	}

	// ─── Helpers ─────────────────────────────────────────────────────────────

	private static Color MultiplierColor(float mult) => mult switch
	{
		>= 3.0f => new Color(1.0f, 0.3f, 0.1f),   // Fire red — ×3
		>= 2.0f => new Color(1.0f, 0.7f, 0.0f),   // Gold    — ×2
		>= 1.5f => new Color(0.6f, 1.0f, 0.4f),   // Lime    — ×1.5
		_       => Colors.White
	};
}
