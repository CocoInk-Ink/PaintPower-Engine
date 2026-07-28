using Avalonia.Controls;
using Avalonia.Input;
using PaintPower.Accessibility.Translation;
using PaintPower.Editors;
using PaintPower.FileEditors;
using PaintPower.Tools.Keyboard;
using System;
using System.Threading.Tasks;

namespace PaintPower;

/// <summary>
/// Legacy compatibility wrapper for the old PaintPower_Engine class.
/// Now acts only as a thin bridge for old code and keyboard shortcuts.
/// All real logic lives in:
///   - MainGUI
///   - ProjectEditorLogic
///   - FileEditors
/// </summary>
public partial class PaintPower_Engine : FileEditor
{
    // --------------------------------------------------------------------
    // Static references preserved for compatibility
    // --------------------------------------------------------------------
    public static PaintPower_Engine App;
    public static MainWindow window => MainWindow.window;

    public static string version => "PaintPower Engine vPre-Alpha 1.1.0.0";

    // Reference to the active GUI (set by MainWindow)
    public MainGUI MainGui { get; private set; }

    // --------------------------------------------------------------------
    // Constructor
    // --------------------------------------------------------------------
    public PaintPower_Engine()
    {
        Translator.load("en");
        App = this;
        Header.header.SetVersion(version);
    }

    // --------------------------------------------------------------------
    // Wiring from MainWindow
    // --------------------------------------------------------------------
    public void attachMainGUI(MainGUI gui)
    {
        MainGui = gui;
    }

    // --------------------------------------------------------------------
    // Legacy project actions (forwarded to MainGUI)
    // --------------------------------------------------------------------
    public async Task OpenProjectFile(string path)
    {
        if (!string.IsNullOrWhiteSpace(path))
            await MainGui.OpenProject(path);
    }

    public async void newProject()
    {
        await MainGui.NewProject();
    }

    public async Task Save()
    {
        await MainGui.SaveProject();
    }

    public void SaveAs()
    {
        // SaveAs is handled by MainGUI or ProjectEditorLogic
    }

    public void CloseProject()
    {
        MainGui.CloseProject();
    }

    // --------------------------------------------------------------------
    // Legacy editor actions (forwarded to MainGUI)
    // --------------------------------------------------------------------
    public void OpenFile(string path)
    {
        MainGui.OpenFile(path);
    }

    public void CloseEditor()
    {
        MainGui.CloseCurrentEditor();
    }

    public void CloseCurrentEditor()
    {
        MainGui.CloseCurrentEditor();
    }

    // --------------------------------------------------------------------
    // Key handling (still useful)
    // --------------------------------------------------------------------
    public void HandleKeyDown(KeyEventArgs e)
    {
        if (SKeyPress.combo(e, "ctrl", "s"))
        { Save(); KeyPress.RegisterKeyUp(e.Key); }

        if (SKeyPress.combo(e, "ctrl", "z"))
        { MainGui.CurrentEditor?.Undo(); KeyPress.RegisterKeyUp(e.Key); }

        if (SKeyPress.combo(e, "ctrl", "shift", "z"))
        { MainGui.CurrentEditor?.Redo(); KeyPress.RegisterKeyUp(e.Key); }

        if (SKeyPress.combo(e, "ctrl", "y"))
        { MainGui.CurrentEditor?.Redo(); KeyPress.RegisterKeyUp(e.Key); }

        if (SKeyPress.combo(e, "alt", "f4"))
        { window.Close(); KeyPress.RegisterKeyUp(e.Key); }

        if (SKeyPress.combo(e, "ctrl", "w"))
        { CloseEditor(); KeyPress.RegisterKeyUp(e.Key); }
    }

    public void HandleKeyUp(KeyEventArgs e)
    {
        // No-op
    }
}
