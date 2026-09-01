using Avalonia.Controls;
using Avalonia.Input;
using Toolbox.Accessibility.Translation;
using PaintPower.Editors;
using PaintPower.FileEditors;
using Toolbox.Plumbing;
using Toolbox.Keyboard;
using System;
using System.Threading.Tasks;
using Toolbox.Plumbing.Pipes;
using PaintPower.Templates.FileTemplates;
using Toolbox;

namespace PaintPower;

public partial class PaintPower_Engine : FileEditor
{
    // --------------------------------------------------------------------
    // Static references preserved for compatibility
    // --------------------------------------------------------------------
    public static PaintPower_Engine App;
    public static MainWindow? window => MainWindow.window;

    // Must not be public
    public Toolkit Toolkit { get; private set; }
    public Plumber plumber;
    public AssetPipe assetPipe;
    public PluginPipe pluginPipe;

    // --------------------------------------------------------------------
    // Constructor
    // --------------------------------------------------------------------
    public PaintPower_Engine()
    {
        Toolkit = new Toolkit();

        if (Plumber.MainPlumber == null)
        {
            plumber = new Plumber();
        } else
        {
            plumber = Plumber.MainPlumber;
        }

        plumber.MakeMainPlumber();

        Translator.load("en");

        assetPipe = plumber.AssetPipe;
        pluginPipe = plumber.PluginPipe;

        if (ResourceKit.Plumber == null) ResourceKit.Initialize(plumber);
        ResourceKit.OnReadyToLoadResources?.Invoke();

        App = this;
    }

    // --------------------------------------------------------------------
    // Key handling (still useful)
    /* --------------------------------------------------------------------
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
    }*/
}