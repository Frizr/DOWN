using Godot;

public partial class MainMenu : Control
{
	public override void _Ready()
	{
		// Connect button signals
		GetNode<Button>("PanelLeft/VBox/ButtonContainer/StartButton").Pressed += OnStartPressed;
		GetNode<Button>("PanelLeft/VBox/ButtonContainer/OptionsButton").Pressed += OnOptionsPressed;
		GetNode<Button>("PanelLeft/VBox/ButtonContainer/QuitButton").Pressed += OnQuitPressed;

		// Subtle entrance animation: fade in from black
		Modulate = new Color(1, 1, 1, 0);
		var tween = CreateTween();
		tween.TweenProperty(this, "modulate:a", 1.0f, 0.5f)
			 .SetTrans(Tween.TransitionType.Sine)
			 .SetEase(Tween.EaseType.Out);
	}

	private void OnStartPressed()
	{
		if (GameManager.Instance != null)
			GameManager.Instance.SetState(GameManager.GameState.Playing);
			
		GetTree().ChangeSceneToFile("res://Scenes/Main.tscn");
	}

	private void OnOptionsPressed()
	{
		var options = GetNodeOrNull<OptionsMenu>("OptionsMenu");
		if (options != null)
			options.ShowOptions();
		else
			GD.PrintErr("OptionsMenu node not found in MainMenu.");
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}
}
