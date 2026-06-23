using Godot;

public partial class OptionsMenu : Control
{
    private HSlider _masterSlider;
    private HSlider _sfxSlider;
    private HSlider _musicSlider;
    private CheckButton _fullscreenToggle;

    public override void _Ready()
    {
        _masterSlider = GetNode<HSlider>("CenterPanel/VBox/MasterSection/MasterSlider");
        _sfxSlider = GetNode<HSlider>("CenterPanel/VBox/SFXSection/SFXSlider");
        _musicSlider = GetNode<HSlider>("CenterPanel/VBox/MusicSection/MusicSlider");
        _fullscreenToggle = GetNode<CheckButton>("CenterPanel/VBox/FullscreenRow/FullscreenToggle");

        _masterSlider.ValueChanged += OnMasterVolumeChanged;
        _sfxSlider.ValueChanged += OnSFXVolumeChanged;
        _musicSlider.ValueChanged += OnMusicVolumeChanged;
        _fullscreenToggle.Toggled += OnFullscreenToggled;
        GetNode<Button>("CenterPanel/VBox/BackBtn").Pressed += OnBackPressed;
    }

    private void OnMasterVolumeChanged(double value)
    {
        int busIdx = AudioServer.GetBusIndex("Master");
        if (busIdx >= 0)
            AudioServer.SetBusVolumeDb(busIdx, Mathf.LinearToDb((float)value / 100f));
    }

    private void OnSFXVolumeChanged(double value)
    {
        int busIdx = AudioServer.GetBusIndex("SFX");
        if (busIdx >= 0)
            AudioServer.SetBusVolumeDb(busIdx, Mathf.LinearToDb((float)value / 100f));
    }

    private void OnMusicVolumeChanged(double value)
    {
        int busIdx = AudioServer.GetBusIndex("Music");
        if (busIdx >= 0)
            AudioServer.SetBusVolumeDb(busIdx, Mathf.LinearToDb((float)value / 100f));
    }

    private void OnFullscreenToggled(bool toggled)
    {
        DisplayServer.WindowSetMode(
            toggled ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed
        );
    }

    private void OnBackPressed()
    {
        Visible = false;
    }

    public void ShowOptions()
    {
        Visible = true;
    }
}
