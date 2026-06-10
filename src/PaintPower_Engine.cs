using Avalonia.Controls;
using Avalonia.Input;
using PaintPower.Editors;
using PaintPower.Networking;
using PaintPower.ProjectSystem;
using PaintPower.Tools.Keyboard;
using System;
using System.Threading.Tasks;

namespace PaintPower;

/// <summary>
/// Legacy compatibility wrapper for the old PaintPower_Engine class.
/// Internally delegates to PaintPowerRuntime + PaintPowerUI.
/// </summary>
public partial class PaintPower_Engine : EditorBase
{
    // --------------------------------------------------------------------
    // Static references preserved for compatibility
    // --------------------------------------------------------------------
    public static PaintPower_Engine App { get; private set; }
    public static MainWindow window;

    public static string version => PaintPowerRuntime.Version;

    // --------------------------------------------------------------------
    // New architecture
    // --------------------------------------------------------------------
    public PaintPowerRuntime Runtime { get; }
    public PaintPowerUI UI { get; private set; }

    // --------------------------------------------------------------------
    // Legacy fields preserved for compatibility
    // These now mirror Runtime or UI values.
    // --------------------------------------------------------------------
    public Vm.Vm vm => Runtime.Vm;

    public PaintProject _project => Runtime.Project;
    public Editor _editorManager => Runtime.EditorManager;
    public EditorBase? _editor => Runtime.CurrentEditor;
    public Server server => Runtime.Server;

    public bool _isSavingAnimationRunning
    {
        get => Runtime.IsSavingAnimationRunning;
        set => Runtime.IsSavingAnimationRunning = value;
    }

    public async void RunSavingAnimation()
    {
        UI.RunSavingAnimation();
    }

    public async void AskToLinkProject(PaintProject project)
    {
        UI.AskToLinkProject(project);
    }

    public bool isNewProject
    {
        get => Runtime.IsNewProject;
        set => Runtime.IsNewProject = value;
    }

    public bool saveNeeded
    {
        get => Runtime.SaveNeeded;
        set => Runtime.SaveNeeded = value;
    }

    // UI fields (legacy)
    public EditorPart editorGui => UI.EditorGui;

    // --------------------------------------------------------------------
    // Constructor
    // --------------------------------------------------------------------
    public PaintPower_Engine()
    {
        Runtime = new PaintPowerRuntime();
        App = this;
    }

    public void Start() => Runtime.Start();

    public string translateVersion() => Runtime.TranslateVersion();

    // --------------------------------------------------------------------
    // Wiring from MainWindow
    // --------------------------------------------------------------------
    public void attachWindow(MainWindow w)
    {
        window = w;
    }

    public void attachEditorPart(EditorPart part)
    {
        UI = new PaintPowerUI(Runtime, window, part);
    }

    // --------------------------------------------------------------------
    // Project actions (delegated)
    // --------------------------------------------------------------------
    public async Task OpenProjectFile(string path = "")
    {
        if (string.IsNullOrWhiteSpace(path))
            return; // UI handles file picker now

        await UI.OpenProject(path);
    }

    public async void newProject()
    {
        await UI.NewProject();
    }

    public async Task Save()
    {
        await UI.SaveProject();
    }

    public void SaveAs()
    {
        // SaveAs is UI-specific now.
        // You can reimplement this inside PaintPowerUI if needed.
    }

    public void CloseProject()
    {
        UI.CloseProject();
    }

    // --------------------------------------------------------------------
    // Editor actions (delegated)
    // --------------------------------------------------------------------
    public void OpenFile(string path)
    {
        Runtime.OpenFile(path);
    }

    public void CloseEditor()
    {
        Runtime.CloseEditor();
    }

    public void CloseCurrentEditor()
    {
        Runtime.CloseCurrentEditor();
    }

    public void OpenSkinEditor(PaintSprite paintSprite, SkinDefinition skinDefinition)
    {
        Runtime.OpenSkinEditor(paintSprite, skinDefinition);
    }

    // --------------------------------------------------------------------
    // Networking (delegated)
    // --------------------------------------------------------------------
    public async Task login(string username, string password)
    {
        await Runtime.Login(username, password);
    }

    public async Task DownloadProjectFromServer()
    {
        // UI layer should implement dialogs
    }

    public async void SaveToServer()
    {
        // UI layer should implement dialogs
    }

    // --------------------------------------------------------------------
    // Key handling (delegated)
    // --------------------------------------------------------------------
    public void HandleKeyDown(KeyEventArgs e)
    {
        if (SKeyPress.combo(e, "ctrl", "s"))
        { Save(); KeyPress.RegisterKeyUp(e.Key); }

        if (SKeyPress.combo(e, "ctrl", "z"))
        { Runtime.CurrentEditor?.Undo(); KeyPress.RegisterKeyUp(e.Key); }

        if (SKeyPress.combo(e, "ctrl", "shift", "z"))
        { Runtime.CurrentEditor?.Redo(); KeyPress.RegisterKeyUp(e.Key); }

        if (SKeyPress.combo(e, "ctrl", "y"))
        { Runtime.CurrentEditor?.Redo(); KeyPress.RegisterKeyUp(e.Key); }

        if (SKeyPress.combo(e, "alt", "f4"))
        { window.Close(); KeyPress.RegisterKeyUp(e.Key); }

        if (SKeyPress.combo(e, "ctrl", "w"))
        { CloseEditor(); KeyPress.RegisterKeyUp(e.Key); }
    }

    public void HandleKeyUp(KeyEventArgs e)
    {
        // No-op, but preserved for compatibility
    }

    public string SetProjectStatus(string status) => Runtime.SetProjectStatus(status);
    public string SetNetworkStatus(string status) => Runtime.SetNetworkStatus(status);
    public string SetUserStatus(string status) => Runtime.SetUserStatus(status);
}
