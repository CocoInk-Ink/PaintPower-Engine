using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Toolbox;
using Toolbox.Logging;
using Toolbox.Runtime;

namespace PaintPower_VM.VMPanel;

public partial class StageAreaPart : TranslatableControl
{
    public StageAreaPart()
    {
        InitializeComponent();

        ExitButton.Click += (sender, e) =>
        {

        };
    }

    public void ShowProcessingPanel()
    {
        LoadingPart.IsVisible = true;
        InvalidateVisual();
    }

    public void HideProcessingPanel()
    {
        LoadingPart.IsVisible = false;
        InvalidateVisual();
    }

    public void HideExtraControls()
    {
        ExtraControls.IsVisible = false;
        InvalidateVisual();
    }

    public void ShowExtraControls()
    {
        ExtraControls.IsVisible = true;
        InvalidateVisual();
    }

    public void UpdateProjectState(RuntimeState state)
    {
        // 1. Log new messages from the runtime state
        foreach (var log in state.NewLogs)
        {
            Log.QuickLog($"Message from Runtime: {log}");
        }

        // 2. Update positions of stage elements.
        // Do nothing for now.

        // 3. Refresh the stage display if needed
        if (state.RefreshNeeded)
        {
            StageControl.Diplay.Tick((Bitmap b) => { this.StageControl.SetBitmap(b); });
        }
    }
}
