using System.Collections.Generic;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Toolbox;
using Toolbox.Display.Sprites;
using Toolbox.Logging;
using Toolbox.Runtime;

namespace PaintPower_VM.VMPanel;

public partial class StageAreaPart : TranslatableControl
{

    private readonly Dictionary<string, Sprite> _sprites = new();

    public StageAreaPart()
    {
        InitializeComponent();

        HideExtraControls();
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
        foreach (var target in state.Targets)
        {
            if (_sprites.TryGetValue(target.Name, out var sprite))
            {
                sprite.x = target.Position.X;
                sprite.y = target.Position.Y;
            }
        }

        // 3. Refresh the stage display if needed
        if (state.RefreshNeeded)
        {
            StageControl.Diplay.Tick((Bitmap b) => { this.StageControl.SetBitmap(b); });
        }
    }

    public void SetRuntimeSprites(IReadOnlyDictionary<string, Sprite> sprites)
    {
        StageControl.Diplay.items.Clear();
        StageControl.Diplay.items.AddRange(sprites.Values);
    }
}
