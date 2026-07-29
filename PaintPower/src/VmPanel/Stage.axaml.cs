using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using PaintPower.Tools;

namespace PaintPower.VMPanel;

public partial class Stage : UserControl
{
    public PaintPower.Display.DisplayIntegration.DIPlay Diplay { get; private set; }

    public Stage()
    {
        InitializeComponent();

        double stageWidth = 640;
        double stageHeight = 450;

        Diplay = new PaintPower.Display.DisplayIntegration.DIPlay(
            (int)stageWidth,
            (int)stageHeight,
            this,
            stageWidth,
            stageHeight
        );

        SetSize(stageWidth, stageHeight);
        Diplay.Start();
    }

    public void SetSize(double width, double height)
    {
        Width = width;
        Height = height;

        StageBorder.Width = width;
        StageBorder.Height = height;

        Diplay.StageSize = new Point(width, height);
    }

    public void SetBitmap(Bitmap bmp)
    {
        StageImage.Source = bmp;
    }
}
