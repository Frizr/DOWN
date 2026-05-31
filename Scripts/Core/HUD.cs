using Godot;

/// <summary>
/// In-game HUD for player health, score, combo feedback, pause, and game over.
/// </summary>
public partial class HUD : CanvasLayer
{
	private ProgressBar _healthBar;
	private Label _healthLabel;
	private Label _scoreLabel;
	private Control _comboPanel;
	private Label _comboLabel;
	private Label _comboMultLabel;
	private Control _pauseBanner;
	private Label _bannerLabel;
	private AnimationPlayer _anim;
	private Health _playerHealth;

	private Tween _scoreTween;
	private Tween _comboTween;
	private int _displayedScore = 0;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;

		_healthBar = GetNodeOrNull<ProgressBar>("HUDRoot/HealthSection/HealthBar");
		_healthLabel = GetNodeOrNull<Label>("HUDRoot/HealthSection/HealthLabel");
		_scoreLabel = GetNodeOrNull<Label>("HUDRoot/TopRight/ScoreLabel");
		_comboPanel = GetNodeOrNull<Control>("HUDRoot/ComboPanel");
		_comboLabel = GetNodeOrNull<Label>("HUDRoot/ComboPanel/ComboVBox/ComboLabel");
		_comboMultLabel = GetNodeOrNull<Label>("HUDRoot/ComboPanel/ComboVBox/MultLabel");
		_pauseBanner = GetNodeOrNull<Control>("PauseBanner");
		_bannerLabel = GetNodeOrNull<Label>("PauseBanner/BannerLabel");
		_anim = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");

		if (_comboPanel != null)
		{
			_comboPanel.Visible = false;
			_comboPanel.Modulate = new Color(1, 1, 1, 0);
			_comboPanel.Scale = Vector2.One;
		}

		if (_pauseBanner != null)
			_pauseBanner.Visible = false;

		RefreshHealth(1.0f, 100, 100);
		SetScoreText(0);

		var gm = GameManager.Instance;
		if (gm != null)
		{
			_displayedScore = gm.Score;
			SetScoreText(gm.Score);
			gm.ScoreChanged += OnScoreChanged;
			gm.ComboChanged += OnComboChanged;
			gm.StateChanged += OnStateChanged;
			OnStateChanged(gm.CurrentState);
		}

		TryConnectPlayerHealth();
	}

	public override void _ExitTree()
	{
		_scoreTween?.Kill();
		_comboTween?.Kill();

		var gm = GameManager.Instance;
		if (gm != null)
		{
			gm.ScoreChanged -= OnScoreChanged;
			gm.ComboChanged -= OnComboChanged;
			gm.StateChanged -= OnStateChanged;
		}

		if (_playerHealth != null)
			_playerHealth.HealthChanged -= OnPlayerHealthChanged;
	}

	private void TryConnectPlayerHealth()
	{
		if (_playerHealth != null)
			return;

		foreach (Node player in GetTree().GetNodesInGroup("player"))
		{
			_playerHealth = player.GetNodeOrNull<Health>("Health");
			if (_playerHealth != null)
				break;
		}

		_playerHealth ??= GetNodeOrNull<Health>("/root/Main/World/YSort/Player/Health");

		if (_playerHealth == null)
			return;

		_playerHealth.HealthChanged += OnPlayerHealthChanged;
		OnPlayerHealthChanged(_playerHealth.Current, _playerHealth.MaxHealth);
	}

	private void OnPlayerHealthChanged(int current, int max)
	{
		float percent = max > 0 ? (float)current / max : 0f;
		RefreshHealth(percent, current, max);

		if (current <= 0)
			ShowBanner("GAME OVER");
	}

	private void OnScoreChanged(int newScore)
	{
		if (_scoreLabel == null)
		{
			_displayedScore = newScore;
			return;
		}

		_scoreTween?.Kill();
		_scoreTween = CreateTween();
		_scoreTween.TweenMethod(
			Callable.From((int value) => SetScoreText(value)),
			_displayedScore,
			newScore,
			0.4f
		).SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);

		_displayedScore = newScore;
	}

	private void OnComboChanged(int count, float multiplier)
	{
		if (_comboPanel == null)
			return;

		_comboTween?.Kill();

		if (count == 0)
		{
			_comboTween = CreateTween();
			_comboTween.TweenProperty(_comboPanel, "modulate:a", 0f, 0.4f);
			_comboTween.Finished += () =>
			{
				if (_comboPanel != null)
					_comboPanel.Visible = false;
			};
			return;
		}

		if (_comboLabel != null)
		{
			_comboLabel.Text = $"x{count}  COMBO";
			_comboLabel.AddThemeColorOverride("font_color", MultiplierColor(multiplier));
		}

		if (_comboMultLabel != null)
			_comboMultLabel.Text = $"({multiplier:F1}x DMG)";

		_comboPanel.Visible = true;
		_comboPanel.Scale = Vector2.One;

		_comboTween = CreateTween().SetParallel();
		_comboTween.TweenProperty(_comboPanel, "modulate:a", 1f, 0.1f);
		_comboTween.TweenProperty(_comboPanel, "scale", new Vector2(1.18f, 1.18f), 0.05f)
			.SetTrans(Tween.TransitionType.Back);

		Tween settle = CreateTween();
		settle.TweenProperty(_comboPanel, "scale", Vector2.One, 0.15f)
			.SetDelay(0.05f)
			.SetTrans(Tween.TransitionType.Elastic)
			.SetEase(Tween.EaseType.Out);
	}

	private void OnStateChanged(GameManager.GameState newState)
	{
		switch (newState)
		{
			case GameManager.GameState.Paused:
				ShowBanner("PAUSED");
				PlayPauseAnimation(forward: true);
				break;

			case GameManager.GameState.GameOver:
				ShowBanner("GAME OVER");
				break;

			case GameManager.GameState.Playing:
				HideBanner();
				PlayPauseAnimation(forward: false);
				break;
		}
	}

	public void RefreshHealth(float percent, int current, int max)
	{
		max = Mathf.Max(max, 1);
		current = Mathf.Clamp(current, 0, max);
		percent = Mathf.Clamp(percent, 0f, 1f);

		if (_healthBar != null)
		{
			_healthBar.MinValue = 0;
			_healthBar.MaxValue = max;
			_healthBar.Value = current;
			SetHealthFillColor(percent < 0.3f
				? new Color(0.9f, 0.12f, 0.1f)
				: new Color(0.22f, 0.72f, 0.28f));
		}

		if (_healthLabel != null)
			_healthLabel.Text = $"{current} / {max}";
	}

	private void SetScoreText(int score)
	{
		if (_scoreLabel != null)
			_scoreLabel.Text = $"SCORE  {score:N0}";
	}

	private void ShowBanner(string text)
	{
		if (_bannerLabel != null)
			_bannerLabel.Text = text;

		if (_pauseBanner != null)
			_pauseBanner.Visible = true;
	}

	private void HideBanner()
	{
		if (_pauseBanner != null)
			_pauseBanner.Visible = false;
	}

	private void PlayPauseAnimation(bool forward)
	{
		if (_anim == null || !_anim.HasAnimation("pause_in"))
			return;

		if (forward)
			_anim.Play("pause_in");
		else
			_anim.PlayBackwards("pause_in");
	}

	private void SetHealthFillColor(Color color)
	{
		if (_healthBar == null)
			return;

		var existingFill = _healthBar.GetThemeStylebox("fill") as StyleBoxFlat;
		var fill = existingFill?.Duplicate() as StyleBoxFlat ?? new StyleBoxFlat();
		fill.BgColor = color;
		fill.CornerRadiusTopLeft = 4;
		fill.CornerRadiusTopRight = 4;
		fill.CornerRadiusBottomRight = 4;
		fill.CornerRadiusBottomLeft = 4;
		_healthBar.AddThemeStyleboxOverride("fill", fill);
	}

	private static Color MultiplierColor(float multiplier) => multiplier switch
	{
		>= 3.0f => new Color(1.0f, 0.28f, 0.08f),
		>= 2.0f => new Color(1.0f, 0.7f, 0.0f),
		>= 1.5f => new Color(0.6f, 1.0f, 0.4f),
		_ => Colors.White
	};
}
