using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Toolbox;
using Toolbox.Display.DisplayIntegration;

namespace PaintPower_VM.VMPanel;

public partial class Stage : TranslatableControl
{
    public DIPlay Diplay { get; private set; }

    private int stageWidth = 640;
    private int stageHeight = 450;

    public Stage()
    {
        InitializeComponent();

        SetSize(stageWidth, stageHeight);
        CreateRenderSystem();
    }

    public void SetSize(double width, double height)
    {
        Width = width;
        Height = height;

        stageWidth = (int)width;
        stageHeight = (int)height;

        StageBorder.Width = width;
        StageBorder.Height = height;

        if (Diplay != null) Diplay.StageSize = new Point(width, height);
    }

    public void SetBitmap(Bitmap bmp)
    {
        StageImage.Source = bmp;
        StageImage.InvalidateVisual();
        InvalidateVisual();
    }

    public void CreateRenderSystem()
    {
        Diplay = new DIPlay(
            (int)stageWidth,
            (int)stageHeight,
            this,
            stageWidth,
            stageHeight
        );
    }
}
