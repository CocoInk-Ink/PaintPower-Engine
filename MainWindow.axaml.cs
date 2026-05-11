using Avalonia.Controls;
using System;

namespace PaintPower;

public partial class MainWindow : Window
{
    public bool saveNeeded = false;
#pragma warning disable
    public static PaintPower_Engine App = new PaintPower_Engine();

    public static MainWindow window;

    #pragma warning enable

    public MainWindow()
    {

        InitializeComponent();
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        // After, make a static reference.
        window = this;
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        App.attachWindow(this);
        
        App.attachEditorPart(editorPart.attachPaintPower(App));
        App.Start();
    }
}