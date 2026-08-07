using Toolbox;
using Toolbox.Accessibility.Translation;
using Toolbox.Logging;

namespace PaintPower_VM.VMPanel;

public partial class VmPanel : TranslatableControl
{

    public VmPanel()
    {
        InitializeComponent();
    }

    public override void Translate()
    {
        if (VMPanelText != null) VMPanelText.Text = Translator.Map("VM Panel");
    }
}