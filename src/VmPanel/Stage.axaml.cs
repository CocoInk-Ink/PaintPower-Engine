using Avalonia.Controls;

using PaintPower.Logging;
using PaintPower.Accessibility.Translation;
using Avalonia.Markup.Xaml;

namespace PaintPower.VMPanel;

public partial class Stage : UserControl
{
    public Stage()
    {
        InitializeComponent();
    }

    public void SetSize(double width, double height)
    {
        Width = width;
        Height = height;
    }
}