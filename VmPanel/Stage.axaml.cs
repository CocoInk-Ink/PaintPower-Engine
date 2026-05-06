using Avalonia.Controls;

using PaintPower.Logging;
using PaintPower.Accessibility.Translation;
using Avalonia.Markup.Xaml;

namespace PaintPower.VMPanel;

public partial class Stage : UserControl
{
    public Stage()
    {
        AvaloniaXamlLoader.Load(this);
    }
}