using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PaintPower.Accessibility.Translation;
using PaintPower.Dialogs;
using PaintPower.FileEditors;
using PaintPower.Logging;
using PaintPower.Networking;
using PaintPower.ProjectSystem;
using PaintPower.ProjectSystem.SpriteEditor;
using PaintPower.Tools.SoundEffects;

namespace PaintPower.Editors.Logic;

public class ProjectEditorLogic
{
    private readonly ProjectEditor _view;
    private readonly MainWindow _window;

    public Server server;

    public PaintProject? Project { get; private set; }
    public TempWorkspace Workspace;
    public FileEditorManager? EditorManager { get; private set; }
    public FileEditor? CurrentEditor { get; private set; }

    public bool SaveNeeded { get; private set; }
    public bool IsNewProject { get; private set; } = true;

    public ProjectEditorLogic(ProjectEditor view)
    {
        _view = view;
        _window = MainWindow.window;

        HookSpriteEvents();
        HookTranslationEvents();
    }

    // Makes the project dirty (needs save)
    public void DirtyProject()
    {
        if (SaveNeeded) return;
        SaveNeeded = true;
        SetStatus("Save Project");
    }

    // ------------------------------------------------------------
    // Initialization
    // ------------------------------------------------------------
    private void HookSpriteEvents()
    {
        // Sprite selected
        _view.SpriteManager.SpriteSelected += sprite =>
        {
            if (Project == null) return;

            _view.SpriteProperties.LoadSprite(sprite);
            SetStatus($"Editing Sprite: {sprite.Name}");
        };

        // Skin selected
        _view.SpriteProperties.SkinSelected += (sprite, skin) =>
        {
            if (Project == null) return;

            // Open the skin editor through MainGUI
            double w = Project.Metadata.StageWidth ?? 640;
            double h = Project.Metadata.StageHeight ?? 450;

            var editor = new SkinEditorView(sprite, skin, w, h);
            OpenSpriteEditor(editor);


            SaveNeeded = true;
            SetStatus($"Editing Skin: {skin.Name}");
        };
    }

    private void HookTranslationEvents()
    {
        Translator.LanguageChanged += () => RefreshTranslations();
    }

    // ------------------------------------------------------------
    // Project lifecycle
    // ------------------------------------------------------------
    public async Task NewProject()
    {
        SoundEffects.Click.Play();

        Project = new PaintProject();
        Project.CreateNew(this);
        Workspace = Project.Workspace;
        EditorManager = new FileEditorManager(Project.Workspace);
        CurrentEditor = null;

        IsNewProject = true;
        SaveNeeded = false;

        _view.SetUIMode(EditorUIMode.ProjectEditor);
        RefreshUI();
    }

    public async Task LoadProject(string path)
    {
        try
        {
            await _view.SetProjectLoading(true);

            Project = new PaintProject();
            EditorManager = new FileEditorManager(Project.Workspace);
            CurrentEditor = null;

            // Run heavy loading on background thread
            await Task.Run(async () =>
            {
                await Project.Load(path, (processed, total) =>
                {
                    Dispatcher.UIThread.Post(async () =>
                    {
                        await _view.UpdateLoadingProgress(processed, total);
                    });
                });
            });

            Project.ProjectPath = path;
            IsNewProject = false;
            SaveNeeded = false;

            _view.SetUIMode(EditorUIMode.ProjectEditor);
            RefreshUI();
        }
        catch (Exception ex)
        {
            Log.QuickLog($"Failed to load project: {ex}");
            await ErrorDialog.ShowAsync(_window, "Invalid or corrupted project file.");
            CloseProject();
        }
        finally
        {
            await _view.SetProjectLoading(false);
        }
    }

    public async Task OpenProjectDialog()
    {
        var window = MainWindow.window;
        if (window == null) return;

        var path = await ProjectLoaderDialog.ShowAsync(window);
        if (!string.IsNullOrWhiteSpace(path))
            await LoadProject(path);
    }

    // ------------------------------------------------------------
    // Saving
    // ------------------------------------------------------------
    public async Task SaveProject()
    {
        if (Project == null)
            return;

        SoundEffects.Click.Play();

        try
        {
            await ProjectSaver.Save(Project, CurrentEditor);
            SaveNeeded = false;
            SetStatus("Project Saved!");
        }
        catch (Exception ex)
        {
            Log.QuickLog($"Error while saving project: {ex}");
            await ErrorDialog.ShowAsync(_window, "Failed to save project.");
        }
    }

    public async Task SaveProjectAs()
    {
        if (Project == null)
            return;

        SoundEffects.Click.Play();

        var savePicker = await _window.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Save Project As",
                DefaultExtension = "xPaint",
                SuggestedFileName = $"{Project.Metadata.name}.xPaint",
                ShowOverwritePrompt = true
            });

        if (savePicker == null)
            return;

        string newPath = savePicker.Path.LocalPath;

        try
        {
            await Project.SaveToDisk(newPath);
            Project.ProjectPath = newPath;
            SaveNeeded = false;

            _window.Title = $"PaintPower - {Project.Metadata.name}";
            SetStatus("Project Saved!");
        }
        catch (Exception ex)
        {
            Log.QuickLog($"Save As failed: {ex}");
            await ErrorDialog.ShowAsync(_window, "Failed to save project.");
        }
    }

    public async Task<bool> AskSaveBeforeClosing()
    {
        if (!SaveNeeded)
            return true;

        var dialog = new SaveBeforeContinueDialog();
        var result = await dialog.ShowDialog<string>(_window);

        if (result == "save")
        {
            await SaveProject();
            return true;
        }

        if (result == "discard")
            return true;

        return false; // cancel
    }

    public async void CloseProject()
    {
        SoundEffects.Click.Play();

        if (!await AskSaveBeforeClosing())
            return;

        Project = null;
        EditorManager = null;
        CurrentEditor = null;
        SaveNeeded = false;
        IsNewProject = true;

        _view.SetUIMode(EditorUIMode.NoProject);
        SetStatus("No project loaded");
        _window.Title = "PaintPower";
    }

    // ------------------------------------------------------------
    // File editor switching
    // ------------------------------------------------------------
    public void OpenFile(string path)
    {
        if (EditorManager == null)
            return;

        CurrentEditor?.Close();
        CurrentEditor = EditorManager.GetEditorFromFileType(path);
        _view.CenterHost.Content = CurrentEditor;

        CurrentEditor = EditorManager.GetEditorFromFileType(path);
        _view.CenterHost.Content = CurrentEditor;

        CurrentEditor.SaveRequested += () =>
        {

            // Only mark dirty if editing a project file
            if (Project != null)
                DirtyProject();
        };

    }

    public void CloseCurrentEditor()
    {
        CurrentEditor?.Close();
        CurrentEditor = null;
        _view.CenterHost.Content = null;
    }

    // ------------------------------------------------------------
    // UI refresh
    // ------------------------------------------------------------
    public void RefreshUI()
    {
        if (Project == null)
            return;

        _view.SpriteManager.Initialize(Project);
        // When sprite or skin changes, mark project dirty
        _view.SpriteProperties.SkinSelected += (_, _) => SaveNeeded = true;
        _window.Title = $"PaintPower - Project - {Project.Metadata.name}";
        SetStatus("Ready");
    }

    public void RefreshTranslations()
    {
        if (!Translator.refreshNeeded) return;

        _window.Title = Translator.Translate("PaintPower");
        _view.SpriteManager.TranslateGUI();

        Translator.refreshNeeded = false;
    }

    // ------------------------------------------------------------
    // Status bar
    // ------------------------------------------------------------
    public void SetStatus(string text)
    {
        _view.StatusBarText.Text = text;
    }

    // ------------------------------------------------------------
    // Login (stub)
    // ------------------------------------------------------------
    public async Task Login()
    {
        SoundEffects.Click.Play();

        var dialog = new SignInDialog(server);
        await dialog.ShowDialog<bool>(_window);
    }


    // ---------------------------------------------------------------
    //  Sprite Editor
    // ---------------------------------------------------------------

    public void OpenSpriteEditor(UserControl editor)
    {
        CloseCurrentEditor();
        _view.CenterHost.Content = editor;
    }

}
