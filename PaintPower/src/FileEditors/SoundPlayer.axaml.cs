using PaintPower.Tools.Media;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PaintPower.ProjectSystem;
using PaintPower.Logging;

namespace PaintPower.FileEditors;

public partial class SoundPlayer : FileEditor
{
    private PaintPower.Tools.Media.Sound.Player.SoundPlayer? player;
    private readonly TempWorkspace _workspace;

    public SoundPlayer(string relativePath, TempWorkspace workspace)
    {
        InitializeComponent();
        _workspace = workspace;
    }

    public override void Activate()
    {

        Log.Info("Loading sound player editor with path: " + FullPath);

        if (!string.IsNullOrWhiteSpace(FullPath))
        {
            var media = new Media(FullPath);
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
