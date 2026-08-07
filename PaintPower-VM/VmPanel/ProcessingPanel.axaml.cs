using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Threading.Tasks;
using Toolbox.Accessibility.Translation;
using Toolbox.Math.Formulas;

namespace PaintPower_VM.VMPanel;

public partial class ProcessingPanel : UserControl
{
    public ProcessingPanel()
    {
        InitializeComponent();
    }

    public void Reset()
    {
        SetPercent(0);
        SetSubheaderText($"0 {("of")} 0 {("assets loaded")}");
    }

    public void SetPercent(int percent)
    {
        Loader.SetPercent(percent);
    }

    public void SetText(string? header = null, string? subheader = null)
    {
        if (header != null) SetHeaderText(header);
        if (subheader != null) SetSubheaderText(subheader);
    }

    public void SetHeaderText(string? text)
    {
        HeaderText.Text = text;
        HeaderText.InvalidateVisual();
    }

    public void SetSubheaderText(string? text)
    {
        SubHeaderText.Text = text;
        SubHeaderText.InvalidateVisual();
    }

}