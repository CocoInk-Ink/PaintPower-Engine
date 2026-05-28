using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using PaintPower.Accessibility.Translation;
using PaintPower.Logging;

namespace PaintPower.VMPanel;

public partial class VmPanel : UserControl
{

    public VmPanel()
    {
        InitializeComponent();

        this.AttachedToVisualTree += (_, __) =>
        {
            Translator.LanguageChanged += translate;
            translate();
        };

        this.DetachedFromVisualTree += (_, __) =>
        {
            Translator.LanguageChanged -= translate;
        };
    }

    public void refresh()
    {
        translate();
        InvalidateVisual();
    }

    public void translate()
    {
        if (VMPanelText != null) VMPanelText.Text = Translator.Map("VM Panel");
        else
        {
            Log.QuickLog("VMPanelText is null, cannot translate.");
        }
        ;
        if (PlayButton != null) PlayButton.Content = Translator.Map("Play");
        else
        {
            Log.QuickLog("PlayButton is null, cannot translate.");
        }
        ;
        if (StopButton != null) StopButton.Content = Translator.Map("Stop");
        else
        {
            Log.QuickLog("StopButton is null, cannot translate.");
        }
        ;
        if (BuildAndRunButton != null) BuildAndRunButton.Content = Translator.Map("Build and Run");
        else
        {
            Log.QuickLog("BuildAndRunButton is null, cannot translate.");
        }
        ;
    }
}