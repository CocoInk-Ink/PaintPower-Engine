using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PaintPower.Dialogs;
using PaintPower.Editors;
using PaintPower.FileExplorer;
using PaintPower.Logging;
using PaintPower.Networking;
using PaintPower.ProjectSystem;
using PaintPower.SpriteEditor;
using PaintPower.Time;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PaintPower.Accessibility.Translation;
using System.Reflection;
using PaintPower.VMPanel;
using PaintPower.Templates.FileTemplates;
using PaintPower.Tools.SoundEffects;
using Avalonia.Input;
using PaintPower.Tools.Keyboard;
namespace PaintPower;

public class PaintPower_Engine
{
    public static readonly string versionNumber = "1.0.1.2";
    public static readonly string buildTime = new Date().getBuildTimestamp();
    public static readonly string devStatus = "Pre-Alpha";
    public static string MajorVersion = $"{Translator.Map(devStatus)} {versionNumber}";
    public static string version = $"{Translator.Map("Version")}: {MajorVersion} {Translator.Map("build")} {buildTime}";

    public Vm.Vm vm;

    public static bool PlayerOnly = false;

    public void translateVersion()
    {
        MajorVersion = $"{Translator.Map("Pre-Alpha")} {versionNumber}";
        version = $"{Translator.Map("Version")}: {MajorVersion} {Translator.Map("build")} {buildTime}";
    }

    private Editor _editorManager;
    private EditorBase _editor;
    public PaintProject _project;
    public Server server;
    public VmPanel vmAreaPart;
    public EditorPart editorGui;

    public bool isNewProject = true;

    private SpriteEditorView _spriteEditorView;

    public bool saveNeeded = false;

#pragma warning disable
    public static PaintPower_Engine App { get; private set; }
    public static MainWindow window;

    public PaintPower_Engine()
    {
        _project = new PaintProject();
        _editorManager = new Editor(_project.Workspace);
        server = new Server();

        Log.QuickLog(version);

        // After, make a static reference.
        App = this;
    }

    public void setupTranslation()
    {
        // Set-up translation
        Translator.load(null);

        var langs = Translator.GetAvailableLanguages();

        foreach (var pair in langs)
        {
            string fullName = pair.Key;
            string code = pair.Value;

            var item = new MenuItem { Header = fullName };
            item.Click += (_, __) =>
            {
                Translator.load(code);
            };

            editorGui.LanguageDropdown.Items.Add(item);
        }
    }

    public void attachWindow(MainWindow w)
    {
        window = w;
    }

    public void attachEditorPart(EditorPart p)
    {
        editorGui = p;
    }

    public string SetProjectStatus(string status)
    {
        editorGui.ProjectStatus.Text = status;
        editorGui.InvalidateVisual();
        return status;
    }

    public string NetworkStatus = "Not connected";
    public string UserStatus = "not logged in.";

    public string SetNetworkStatus(string status)
    {
        NetworkStatus = status;
        FixUserStatus();
        return status;
    }

    public string SetUserStatus(string status)
    {
        UserStatus = status;
        FixUserStatus();
        return status;
    }

    public async void FixUserStatus()
    {
        if (await server.IsLoggedIn())
        {
            editorGui.UserStatus.Text = server.Username;
        }
        else
        {
            editorGui.UserStatus.Text = $"{NetworkStatus}, {UserStatus}";
        }
        editorGui.InvalidateVisual();
    }

    private void OnSpriteSelected(PaintSprite sprite)
    {
        SoundEffects.Click.Play();
        // Create the sprite editor panel
        _spriteEditorView = new SpriteEditorView(sprite, _project.Workspace);

        // Replace the center panel with the sprite editor
        editorGui.CenterHost.Content = _spriteEditorView;

        SetProjectStatus($"{Translator.Translate("Editing Sprite:")} {sprite.Name}");
    }

    public async Task OpenProjectFile(string filePath = "")
    {
        SoundEffects.Click.Play();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            var result = await window.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = Translator.Map("Open PaintPower Project"),
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                    new FilePickerFileType(Translator.Map("PaintPower Project"))
                    {
                        Patterns = new[] { "*.xPaint" }
                    }
                    }
                });

            if (result.Count == 0)
                return; // user cancelled

            filePath = result[0].Path.LocalPath;
        }

        Translator.load(null); // reset translation to default (in case project has different language)

        // Reset current project/editor
        CloseEditor();

        // Clear existing sprites to avoid duplicates when loading.
        _project.Sprites.Clear();

        // Load project
        _project.Load(filePath);
        _project.ProjectPath = filePath;

        RefreshSession(false);

        MainWindow.window.InvalidateVisual();

        // Update window title
        window.Title = $"{Translator.Map("PaintPower")} - {_project.Metadata.name}";
    }

    public async void newProject()
    {
        Translator.load(null); // reset translation to default (in case current project has different language)

        if (saveNeeded)
        {
            var dialog = new SaveBeforeContinueDialog();
            var result = await dialog.ShowAsync(MainWindow.window);

            switch (result)
            {
                case "save":
                    await Save();
                    break;

                case "saveas":
                    SaveAs();
                    break;

                case "dontsave":
                    break; // continue without saving

                case null:
                    return; // cancel new project
            }
        }

        // Reset everything
        _project = new PaintProject();
        _editorManager = new Editor(_project.Workspace);
        server = new Server();

        Start();
    }

    public virtual async void RefreshSession(bool makeNew = true)
    {
        CloseEditor();

        _project = makeNew ? new PaintProject() : _project;
        _editorManager = new Editor(_project.Workspace);
        _editor = null;

        window.Title = $"{Translator.Map("PaintPower")} - {_project.Metadata.name}";

        editorGui.SpriteManager.Initialize(_project);
        editorGui.SpriteManager.SpriteSelected -= OnSpriteSelected;
        editorGui.SpriteManager.SpriteSelected += OnSpriteSelected;

        MainWindow.window.InvalidateVisual();

        // Set up translation
        setupTranslation();
    }

    public virtual async void Start()
    {

        await Task.Yield();

        CloseEditor();

        _project = new PaintProject();
        _editorManager = new Editor(_project.Workspace);
        _editor = null;

        _project.CreateNew();

        window.Title = $"{Translator.Map("PaintPower")} - {_project.Metadata.name}";

        SetProjectStatus(Translator.Translate("Not edited yet."));

        editorGui.SpriteManager.Initialize(_project);
        editorGui.SpriteManager.SpriteSelected += OnSpriteSelected;

        MainWindow.window.InvalidateVisual();

        // Set up translation
        setupTranslation();
        //vm = new Vm.Vm();
    }

    /*public void OpenEditor(EditorBase editor)
    {
        _editor = editor;
        CenterHost.Content = editor;
    }*/

    public void OpenFile(string path)
    {
        Log.Info("Opening file: " + path);
        Log.Info("Closing current editor.");
        CloseCurrentEditor(); // Close current editor if open...
        Log.Info("Getting new editor for file type...");
        var editor = _editorManager.GetEditorFromFileType(path);

        _editor = editor;

        if (_spriteEditorView != null)
            _spriteEditorView.OpenEditor(editor, path);
    }

    public void CloseCurrentEditor()
    {
        if (_editor != null)
        {
            Log.Info("Closing current editor.");
            _editor.Close();
            _editor = null;
        } else
        {
            Log.Info("No editor to close.");
        }
    }

    public void CloseEditor()
    {
        Log.Info("Closing current editor.");
        _editor?.Close();
        editorGui.CenterHost.Content = null;
        _editor = null;
    }

    public async void AskToLinkProject(PaintProject project)
    {
        var dialog = new LinkBeforeUploadDialog();
        var result = await dialog.ShowDialog<string>(MainWindow.window);

        if (result == "cancel")
            return;

        if (result == "new")
        {
            string? id = await server.CreateNewServerProject(project.Metadata.name);
            if (id != null)
            {
                project.Metadata.serverId = id;
                project.SaveMetadata();
            }
            return;
        }

        if (result == "existing")
        {
            var list = await server.ListUserProjects();
            var selectDialog = new SelectServerProjectDialog(list);
            var chosenId = await selectDialog.ShowDialog<string>(MainWindow.window);
            if (!string.IsNullOrEmpty(chosenId))
            {
                project.Metadata.serverId = chosenId;
                project.SaveMetadata();
            }
        }
    }

    public bool _isSavingAnimationRunning = false;
    public bool _isUploadAnimationRunning = false;

    public async Task RunSavingAnimation()
    {
        _isSavingAnimationRunning = true;

        string[] frames = new[]
        {
            "Saving Project",
            "Saving Project.",
            "Saving Project..",
            "Saving Project..."
        };

        int index = 0;

        while (_isSavingAnimationRunning)
        {
            SetProjectStatus(Translator.Map(frames[index]));
            index = (index + 1) % frames.Length;
            await Task.Delay(300); // smooth animation
        }
    }

    // Save function. Don't even care about what it returns, but C#
    // Requires it to be a Task in order to await it. C# core.
    async public Task Save()
    {
        if (!saveNeeded && !isNewProject) return;

        try
        {
            var dialog = new DoSaveWindowDialog();
            var result = await dialog.ShowAsync(window);

            if (result == "saveas")
            {
                SaveAs();
                return;
            }

            if (result != "save")
            {
                Log.QuickLog($"Not saving. {result}");
                return;
            }

            // If no path yet -> ask user where to save
            if (string.IsNullOrWhiteSpace(_project.ProjectPath) || isNewProject)
            {
                isNewProject = true; // Keep isNewProject checks

                var res = await _project.SaveNewProject(MainWindow.window);

                if (string.IsNullOrWhiteSpace(res.Path))
                    return; // user cancelled

                _project.ProjectPath = res.Path;
                isNewProject = false;
            }

            Log.QuickLog("Saving Project...");

            // Start animation (non-blocking)
            var animationTask = RunSavingAnimation();

            // Run save off UI thread
            await ProjectSaver.Save(_project, _editor);

            // Stop animation
            _isSavingAnimationRunning = false;
            await animationTask; // wait for animation loop to exit

            // Final status
            Log.QuickLog(SetProjectStatus(Translator.Map("Project Saved!")));
        }
        catch (Exception ex)
        {
            Log.QuickLog($"Error while saving project! {ex}");
        }
    }

    async public void SaveAs()
    {
        var savePicker = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = $"Save {_project.Metadata.name} As",
            DefaultExtension = "xPaint",
            SuggestedFileName = $"{_project.Metadata.name}.xPaint",
            ShowOverwritePrompt = true
        });

        if (savePicker == null)
            return;

        string newPath = savePicker.Path.LocalPath;

        // Update project path
        _project.ProjectPath = newPath;

        try
        {
            // Start animation
            var animationTask = RunSavingAnimation();

            // Run save off UI thread

            await ProjectSaver.Save(_project, _editor);

            // Stop animation
            _isSavingAnimationRunning = false;
            await animationTask;

            // Update metadata + UI
            _project.Metadata.name = Path.GetFileNameWithoutExtension(newPath);
            window.Title = $"PaintPower - {_project.Metadata.name}";
            SetProjectStatus("Project Saved!");
            Log.QuickLog($"Project saved as {newPath}");
        }
        catch (Exception ex)
        {
            Log.QuickLog($"SaveAs failed: {ex}");
        }
    }

    public async void SaveToServer()
    {
        var project = _project;
        var server = App.server;

        // 1. Must be signed in
        if (!await server.IsLoggedIn())
        {
            new PopupWindowDialog("Upload", "You must sign in before uploading.", "").ShowDialog(MainWindow.window);
            return;
        }

        // 2. Project not linked → ask to link
        if (!project.Metadata.IsLinked)
        {
            var linkDialog = new LinkBeforeUploadDialog();
            var linkChoice = await linkDialog.ShowDialog<string>(MainWindow.window);

            if (linkChoice == "cancel")
                return;

            if (linkChoice == "new")
            {
                string? id = await server.CreateNewServerProject(project.Metadata.name);
                if (id != null)
                {
                    project.Metadata.serverId = id;
                    project.SaveMetadata();
                }
                else
                {
                    new PopupWindowDialog("Upload", "Failed to create server project.", "").ShowDialog(MainWindow.window);
                    return;
                }
            }
            else if (linkChoice == "existing")
            {
                var list = await server.ListUserProjects();
                var selectDialog = new SelectServerProjectDialog(list);
                var chosenId = await selectDialog.ShowDialog<string>(MainWindow.window);

                if (string.IsNullOrEmpty(chosenId))
                    return;

                project.Metadata.serverId = chosenId;
                project.SaveMetadata();
            }
        }

        // 3. Project is linked → ask overwrite/unlink
        if (project.Metadata.IsLinked)
        {
            var uploadDialog = new UploadOptionsDialog(project.Metadata.serverId!);
            var choice = await uploadDialog.ShowDialog<string>(MainWindow.window);

            if (choice == "cancel")
                return;

            if (choice == "unlink")
            {
                project.Metadata.serverId = null;
                project.SaveMetadata();
                return;
            }

            if (choice == "overwrite")
            {
                await server.UploadProject(project);
                return;
            }
        }
    }

    public async Task login(string username, string password)
    {
        bool ok = await Net.Login(username, password);

        if (ok)
        {
            SetUserStatus($"Logged in as {username}");
        }
        else
        {
            SetUserStatus("Login failed");
        }

    }

    public async Task DownloadProjectFromServer()
    {


        var savePicker = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = $"Save {_project.Metadata.name} As",
            DefaultExtension = "xPaint",
            SuggestedFileName = $"{_project.Metadata.name}.xPaint",
            ShowOverwritePrompt = true
        });

        if (savePicker == null)
            return;

        string path = savePicker.Path.LocalPath;

        var list = await server.ListUserProjects();
        var selectDialog = new SelectServerProjectDialog(list);
        var chosenId = await selectDialog.ShowDialog<string>(MainWindow.window);

        if (string.IsNullOrEmpty(chosenId))
            return;

        server.DownloadProject(path, Convert.ToInt32(chosenId));
    }

   public void HandleKeyDown(KeyEventArgs e)
    {
        KeyPress p = new KeyPress(e);

        if (p.isPressed("s") && p.isPressed("ctrl"))
        {
            Save();
        }
    }
}