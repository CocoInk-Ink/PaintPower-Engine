using System;
using Avalonia.Controls;
using Toolbox;

namespace PaintPower_VM.VMPanel;

public partial class TopBarStagePart : TranslatableControl
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
