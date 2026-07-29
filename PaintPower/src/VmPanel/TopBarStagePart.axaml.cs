using System;
using Avalonia.Controls;

namespace PaintPower.VMPanel;

public partial class TopBarStagePart : UserControl
{
    public event Action? PlayClicked;
    public event Action? StopClicked;
    public event Action? FullscreenClicked;

    public TopBarStagePart()
    {
        InitializeComponent();

        GreenFlagButton.Click += (_, _) => PlayClicked?.Invoke();
        StopButton.Click += (_, _) => StopClicked?.Invoke();
        FullscreenButton.Click += (_, _) => FullscreenClicked?.Invoke();
    }
}
