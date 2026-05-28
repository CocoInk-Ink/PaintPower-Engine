using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Threading.Tasks;

using PaintPower.Logging;
using PaintPower.Accessibility.Translation;
using PaintPower.Tools.Math.Formulas;


namespace PaintPower.VMPanel;

public partial class ProcessingPanel : UserControl
{
    public ProcessingPanel()
    {
        InitializeComponent();
        RunTest();
    }

    private async void RunTest()
    {
        int total = (!false) ? 398484 : 712;
        //SetText("Loading Project...", "{processed} of {total} assets loaded");
        SetText("Compiling Scripts...", "Compiled {processed} of {total} total scripts");
        for (int processed = (false) ? 29449 : 0; processed <= total; processed++)
        {
            Loader.SetPercent(Percent.calc(processed, total));
            //SetSubheaderText($"{processed} of {total} assets loaded");
            SetSubheaderText($"Compiled {processed} of {total} total scripts");
            await Task.Delay(100);
        }
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