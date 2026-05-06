using Avalonia.Controls;

using PaintPower.Logging;
using PaintPower.Accessibility.Translation;
using Avalonia.Markup.Xaml;
using System.Threading.Tasks;

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
        for (int i = 0; i <= 100; i++)
        {
            Loader.SetPercent(i);
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