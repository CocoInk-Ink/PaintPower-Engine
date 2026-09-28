using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Threading.Tasks;
using Toolbox;
using Toolbox.Accessibility.Translation;
using Toolbox.Math.Formulas;

namespace PaintPower_VM.VMPanel;

public partial class ProcessingPanel : TranslatableControl
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

    public void SetText(string? header = null, string? subheader = null, string? subheader2 = null)
    {
        if (header != null) SetHeaderText(header);
        if (subheader != null) SetSubheaderText(subheader);
        
        SetSubheader2Text(""); // Set blank at first.
        if (subheader2 != null) SetSubheader2Text(subheader2);
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

    public void SetSubheader2Text(string? text)
    {
        SubheaderText2.Text = text;
        SubheaderText2.InvalidateVisual();
    }

}