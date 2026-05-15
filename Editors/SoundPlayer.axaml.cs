using PaintPower.Tools.Media;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PaintPower.ProjectSystem;
using PaintPower.Logging;

namespace PaintPower.Editors;

public partial class SoundPlayer : EditorBase
{
    private PaintPower.Tools.Media.Sound.Player.SoundPlayer? player;
    private readonly TempWorkspace _workspace;

    public SoundPlayer(string relativePath, TempWorkspace workspace)
    {
        InitializeComponent();
        _workspace = workspace;
    }

    public override void SetRelativePath(string relativePath)
    {
        var fullPath = _workspace.MapToTemp(relativePath);
        base.SetRelativePath(fullPath);

        Log.Info("Loading sound player editor with path: " + fullPath);

        if (!string.IsNullOrWhiteSpace(fullPath))
        {
            var media = new Media(fullPath);
            media.Load();

            player?.Dispose();
            player = new PaintPower.Tools.Media.Sound.Player.SoundPlayer(media);
        }
    }

    public override void Close()
    {
        Log.Info("Closing sound player editor.");
        player?.Dispose();
        player = null;
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (player == null)
            return;

        player.Loop = LoopCheckBox.IsChecked == true;

        Log.Info("Play button clicked.");
        player.Play();
    }

    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        player?.Pause();
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        player?.Stop();
    }
}
