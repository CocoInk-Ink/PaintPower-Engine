using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PaintPower.Tools.Keyboard;
using System;

namespace PaintPower;

public partial class MainWindow : Window
{
    // Instance of the compatibility wrapper
    public static PaintPower_Engine App { get; } = new PaintPower_Engine();

    public string? StartupProjectPath { get; set; }

    public static MainWindow window;

    public MainWindow()
    {
        InitializeComponent();
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        window = this;
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // Initialize keyboard system
        KeyPress.init();

        // Attach window + editor UI to the engine
        App.attachWindow(this);
        App.attachEditorPart(editorPart.attachPaintPower(App));

        // Start engine runtime
        App.Start();

        // Key handlers
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);

        // Load project if opened via double-click / OS association
        if (!string.IsNullOrWhiteSpace(StartupProjectPath))
        {
            await App.OpenProjectFile(StartupProjectPath);
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        KeyPress.RegisterKeyDown(e.Key);
        App.HandleKeyDown(e);
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        KeyPress.RegisterKeyUp(e.Key);
        App.HandleKeyUp(e);
    }
}
