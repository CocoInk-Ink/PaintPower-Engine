using Avalonia.Controls;
using PaintPower.Editors;
using PaintPower.Editors.Logic;
using PaintPower.ProjectSystem;
using System.Threading.Tasks;

namespace PaintPower.Editors;

/// <summary>
/// Global UI controller for PaintPower.
/// Handles switching between:
///   - HomeView
///   - ProjectEditor
///   - FolderEditor
///   - SingleFileEditor
///   - WorkspaceEditor
/// </summary>
public partial class MainGUI : UserControl
{
    public Header Header { get; private set; }
    public ProjectEditor? projectEditor;
    public Editor? CurrentEditor { get; private set; }

    public MainGUI()
    {
        InitializeComponent();

        Header = new Header();
        ShowHeader();

        // Default view
        SetEditor(new HomeView());
    }

    // ------------------------------------------------------------
    // Header control
    // ------------------------------------------------------------
    public void HideHeader()
    {
        HeaderPart.Content = null;
    }

    public void ShowHeader()
    {
        HeaderPart.Content = Header;
    }

    // ------------------------------------------------------------
    // Editor switching
    // ------------------------------------------------------------
    public void SetEditor(Editor editorInstance)
    {
        CurrentEditor = editorInstance;
        MainPart.Content = editorInstance;

        Header.LoadDefinition(editorInstance.GetHeaderDefinition());
    }

    public void CloseEditor()
    {
        CurrentEditor = null;
        MainPart.Content = null;
        SetEditor(new HomeView());
    }

    // ------------------------------------------------------------
    // Project actions
    // ------------------------------------------------------------
    public async Task NewProject()
    {
        var editor = new ProjectEditor();
        SetEditor(editor);

        await editor.Logic.NewProject();
        ShowHeader();
    }

    public async Task OpenProject(string path)
    {
        var editor = new ProjectEditor();
        SetEditor(editor);

        await editor.Logic.LoadProject(path);
        ShowHeader();
    }

    public async Task SaveProject()
    {
        if (CurrentEditor is ProjectEditor projectEditor)
            await projectEditor.Logic.SaveProject();
    }

    public void CloseProject()
    {
        if (CurrentEditor is ProjectEditor projectEditor)
            projectEditor.Logic.CloseProject();

        SetEditor(new HomeView());
    }

    // ------------------------------------------------------------
    // File actions
    // ------------------------------------------------------------
    public async Task OpenFolder(string folderPath)
    {
        var editor = new FolderEditor(folderPath);
        SetEditor(editor);
        ShowHeader();
    }

    public async Task NewSingleFile()
    {
        var editor = new SingleFileEditor();
        SetEditor(editor);
        ShowHeader();
    }

    public async Task OpenSingleFile(string filePath)
    {
        var editor = new SingleFileEditor(filePath);
        SetEditor(editor);
        ShowHeader();
    }

    public void OpenFile(string path)
    {
        if (CurrentEditor is ProjectEditor projectEditor)
            projectEditor.Logic.OpenFile(path);
    }

    public void CloseCurrentEditor()
    {
        if (CurrentEditor is ProjectEditor projectEditor)
            projectEditor.Logic.CloseCurrentEditor();
    }

    // ------------------------------------------------------------
    // Login
    // ------------------------------------------------------------
    public async Task Login(string username, string password)
    {
        if (CurrentEditor is ProjectEditor projectEditor)
            await projectEditor.Logic.Login();
    }
}
