using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Toolbox.Keyboard;
using System;

namespace PaintPower;

public partial class MainWindow : Window
{
    // Legacy compatibility wrapper
    public static PaintPower_Engine? App { get; } = new PaintPower_Engine();

    public string? StartupProjectPath { get; set; }

#pragma warning disable CS8618
    public static MainWindow window;
#pragma warning restore CS8618

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

        // Attach MainGUI to the engine wrapper
        //App?.attachMainGUI(mainGui);

        // Key handlers
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);

        // Load project if opened via double-click / OS association
        /*if (!string.IsNullOrWhiteSpace(StartupProjectPath))
        {
            await App.OpenProjectFile(StartupProjectPath);
        }*/
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        KeyPress.RegisterKeyDown(e.Key);
        //App.HandleKeyDown(e);
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        KeyPress.RegisterKeyUp(e.Key);
        //App.HandleKeyUp(e);
    }
}
