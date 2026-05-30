using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PaintPower.VMPanel;

public partial class Stage : UserControl
{
    // DIPlay instance owned by this Stage
    public PaintPower.Display.DisplayIntegration.DIPlay Diplay { get; private set; }

    public static Stage? stage { get; private set; }

    public Stage()
    {
        InitializeComponent();

        // Create DIPlay using the stage's fixed size (or project metadata)
        Diplay = new PaintPower.Display.DisplayIntegration.DIPlay(640, 450, this);

        stage = this;

        // Start the render loop
        Diplay.Start();
    }

    public void SetSize(double width, double height)
    {
        Width = width;
        Height = height;
    }

    public void SetBitmap(Avalonia.Media.Imaging.Bitmap bmp)
    {
        StageImage.Source = bmp;
    }
}
