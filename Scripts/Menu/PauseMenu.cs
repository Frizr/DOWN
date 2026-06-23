using Godot;

public partial class PauseMenu : CanvasLayer
{
    private Control _root;

    public override void _Ready()
    {
        _root = GetNode<Control>("Root");
        _root.Visible = false;

        GetNode<Button>("Root/CenterPanel/VBox/ResumeBtn").Pressed += OnResumePressed;
        GetNode<Button>("Root/CenterPanel/VBox/RestartBtn").Pressed += OnRestartPressed;
        GetNode<Button>("Root/CenterPanel/VBox/MenuBtn").Pressed += OnMenuPressed;
        GetNode<Button>("Root/CenterPanel/VBox/QuitBtn").Pressed += OnQuitPressed;

        // Sync with GameManager's global state
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StateChanged += OnGameStateChanged;
        }
    }

    private void OnGameStateChanged(GameManager.GameState state)
    {
        _root.Visible = (state == GameManager.GameState.Paused);
    }

    private void OnResumePressed()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameManager.GameState.Playing);
    }

    private void OnRestartPressed()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameManager.GameState.Playing);
        
        GetTree().ReloadCurrentScene();
    }

    private void OnMenuPressed()
    {
        // Tell GameManager we are returning to main menu
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameManager.GameState.MainMenu);
            
        GetTree().ChangeSceneToFile("res://Scenes/Menu/MainMenu.tscn");
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
