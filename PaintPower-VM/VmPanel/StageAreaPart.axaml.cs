using Avalonia.Controls;
using Toolbox;

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
}
