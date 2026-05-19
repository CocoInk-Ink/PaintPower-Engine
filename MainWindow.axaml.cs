using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;

namespace PaintPower;

public partial class MainWindow : Window
{
    public bool saveNeeded = false;
#pragma warning disable
    public static PaintPower_Engine App = new PaintPower_Engine();

    public static MainWindow window;

    public string? StartupProjectPath { get; set; }

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

        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);

        // Load project if opened via double-click
        if (!string.IsNullOrWhiteSpace(StartupProjectPath))
        {
            PaintPower_Engine.App.OpenProjectFile(StartupProjectPath);
        }
    }


    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        PaintPower_Engine.App.HandleKeyDown(e);
    }

}