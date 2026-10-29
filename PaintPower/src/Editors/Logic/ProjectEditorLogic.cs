using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PaintPower.Building;
using PaintPower.Dialogs;
using PaintPower.FileEditors;
using PaintPower.ProjectSystem;
using PaintPower.ProjectSystem.SpriteEditor;
using PaintPower_VM.VMPanel;
using Toolbox.Accessibility.Translation;
using Toolbox.Logging;
using Toolbox.Networking;
using Toolbox.Runtime;
using Toolbox.Sessions;
using Toolbox.SoundEffects;

namespace PaintPower.Editors.Logic;

public class ProjectEditorLogic
{
    private readonly ProjectEditor _view;
    private readonly MainWindow _window;
    private readonly Server _server = new();

    public PaintProject? Project { get; private set; }
    public Builder? builder;
    public TempWorkspace Workspace;
    public FileEditorManager? EditorManager { get; private set; }
    public FileEditor? CurrentEditor { get; private set; }

    public bool SaveNeeded { get; private set; }
    public bool IsNewProject { get; private set; } = true;
    private DispatcherTimer? _runtimeTimer;

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
        StopRuntime();
        SoundEffects.Click.Play();

        Project = new PaintProject();
        Project.CreateNew(this);
        Project.ProjectPath = "";
        Workspace = Project.Workspace;
        EditorManager = new FileEditorManager(Project.Workspace);
        CurrentEditor = null;

        IsNewProject = true;
        SaveNeeded = false;

        _view.SetUIMode(EditorUIMode.ProjectEditor);
        RefreshUI();
    }

    public async Task LoadProject(string path, bool isDefaultProject)
    {
        StopRuntime();
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
                    Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        await _view.UpdateLoadingProgress(processed, total);
                    });
                });
            });

            Project.ProjectPath = isDefaultProject ? string.Empty : path;
            IsNewProject = isDefaultProject;
            SaveNeeded = false;

            _view.SetUIMode(EditorUIMode.ProjectEditor);
        }
        catch (Exception ex)
        {
            Log.QuickLog($"Failed to load project: {ex}");
            await ErrorDialog.ShowAsync(_window, "Invalid or corrupted project file.");

            if (isDefaultProject)
            {
                // The default project has failed to load, add blank metadata
                Project = new()
                {
                    Metadata = new ProjectMetadata { name = "Untitled", OpenFile = null }
                };

            }
            else
            {
                // The project has failed to load, create a new one
                await NewProject();
            }
        }
        finally
        {
            RefreshUI();
            await _view.SetProjectLoading(false);
        }
    }

    public async Task BuildProject()
    {
        if (builder == null || builder.IsOld) builder = new Builder();

        try
        {
            await _view.SetProjectLoading(true);

            Log.QuickLog("Ready to build.");

            // Run heavy loading on background thread
            await Task.Run(async () =>
            {
                await builder.BuildProject(Project, (message, processed, total) =>
                {
                    Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        await _view.UpdateBuildingProgress(message, processed, total);
                    });
                });
            });

            _view.SetUIMode(EditorUIMode.ProjectEditor);
        }
        catch (Exception ex)
        {
            Log.QuickLog($"Failed to build project: {ex}");
            await ErrorDialog.ShowAsync(_window, $"Failed to build project project: {ex}");
        }
        finally
        {
            RefreshUI();
            await _view.SetProjectLoading(false);
        }
    }

    public void RunProject()
    {
        if (builder == null)
        {
            Log.QuickLog("No build to run.");
            return;
        }

        if (!builder.IsOld)
        {
            Log.QuickLog("Build is too young.");
            return;
        }

        Log.QuickLog("Running project");

        ProjectEditor.CurrentProjectEditor.SetUIMode(EditorUIMode.ProjectPlayer);

        StopRuntime();

        // Initialize the runtime for the project
        var runtime = new PaintProjectRuntime(Session.CurrentBuildSession);
        var stage = ProjectEditor.CurrentProjectEditor.VmPanelControl.FindControl<StageAreaPart>("StageArea");
        if (stage == null) throw new InvalidOperationException("StageAreaPart not found.");

        stage.SetRuntimeSprites(runtime.Sprites);
        // Start the project.
        runtime.Start();

        _runtimeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _runtimeTimer.Tick += (_, _) => stage.UpdateProjectState(runtime.Tick());
        _runtimeTimer.Start();
    }

    public async Task OpenProjectDialog()
    {
        var window = MainWindow.window;
        if (window == null) return;

        var path = await ProjectLoaderDialog.ShowAsync(window);
        if (!string.IsNullOrWhiteSpace(path))
            await LoadProject(path, false);
    }

    // ------------------------------------------------------------
    // Saving
    // ------------------------------------------------------------
    public async Task<bool> SaveProject()
    {
        if (Project == null)
            return false;

        SoundEffects.Click.Play();

        try
        {
            // If no path, force Save As
            if (string.IsNullOrWhiteSpace(Project.ProjectPath) || IsNewProject)
            {
                return await SaveProjectAs();
            }

            await _view.SetProjectLoading(true);

            await ProjectSaver.Save(Project, CurrentEditor, (processed, total) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Log.QuickLog("Updating...");
                    if (processed % 10 == 0) _view.UpdateSavingProgress(processed, total);
                    //await Task.Yield(); // allow UI thread to breathe
                });
            });

            SaveNeeded = false;
            SetStatus("Project Saved!");
            return true;
        }
        catch (Exception ex)
        {
            Log.QuickLog($"Error while saving project: {ex}");
            await ErrorDialog.ShowAsync(_window, "Failed to save project.");
            return false;
        }
        finally
        {
            RefreshUI();
            await _view.SetProjectLoading(false);
        }
    }

    public async Task<bool> SaveProjectAs()
    {
        if (Project == null)
            return false;

        SoundEffects.Click.Play();

        var savePicker = await _window.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Save Project As",
                DefaultExtension = "xPaint",
                SuggestedFileName = $"{Project.Metadata.name}.xPaint",
                ShowOverwritePrompt = true
            }
        );

        if (savePicker == null)
            return false;

        string newPath = savePicker.Path.LocalPath;

        try
        {
            await Project.SaveToDisk(newPath);
            Project.ProjectPath = newPath;
            SaveNeeded = false;
            IsNewProject = false;

            _window.Title = $"PaintPower - {Project.Metadata.name}";
            SetStatus("Project Saved!");
            return true;
        }
        catch (Exception ex)
        {
            Log.QuickLog($"Save As failed: {ex}");
            await ErrorDialog.ShowAsync(_window, "Failed to save project.");
            return false;
        }
    }

    public async Task<bool> AskSaveBeforeClosing()
    {
        if (!SaveNeeded)
            return true;

        var dialog = new SaveBeforeContinueDialog();
        var result = await dialog.ShowDialog<string>(_window);

        if (result == "save")
            return await SaveProject();

        if (result == "discard")
            return true;

        return false; // cancel
    }

    public void CloseProject()
    {
        SoundEffects.Click.Play();
        StopRuntime();

        // if (!await AskSaveBeforeClosing())
        // return;

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

        _view.ProjectSettings.SetProject(Project);
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
        MainGUI.mainGUI?.Header.SetStatus(text);
    }

    // ------------------------------------------------------------
    // Server
    // ------------------------------------------------------------
    public async Task MakeServerConnection()
    {
        SoundEffects.Click.Play();
        SetStatus("Connecting to server...");

        try
        {
            await _server.InitServer();
            if (!_server.IsConnected)
            {
                SetStatus("Server unavailable");
                await ErrorDialog.ShowAsync(_window, "Could not connect to the xPaint server.");
                return;
            }

            SetStatus("Connected to server");
        }
        catch (Exception ex)
        {
            Log.QuickLog($"Server connection failed: {ex}");
            SetStatus("Server connection failed");
            await ErrorDialog.ShowAsync(_window, $"Could not connect to the xPaint server: {ex.Message}");
        }
    }

    public async Task Login()
    {
        SoundEffects.Click.Play();

        try
        {
            if (!await EnsureServerConnected())
            {
                SetStatus("Server unavailable");
                await ErrorDialog.ShowAsync(_window, "Connect to the xPaint server before signing in.");
                return;
            }

            var dialog = new SignInDialog(_server);
            bool signedIn = await dialog.ShowDialog<bool>(_window);
            SetStatus(signedIn ? $"Signed in as {_server.Username}" : "Sign-in cancelled");
        }
        catch (Exception ex)
        {
            Log.QuickLog($"Server sign-in failed: {ex}");
            SetStatus("Sign-in failed");
            await ErrorDialog.ShowAsync(_window, $"Could not sign in: {ex.Message}");
        }
    }

    public async Task Logout()
    {
        SoundEffects.Click.Play();

        try
        {
            if (!await EnsureServerConnected())
            {
                SetStatus("Server unavailable");
                await ErrorDialog.ShowAsync(_window, "Could not connect to the xPaint server.");
                return;
            }

            if (!await _server.IsLoggedIn())
            {
                SetStatus("Not signed in");
                return;
            }

            await _server.Logout();
            if (await _server.IsLoggedIn())
            {
                SetStatus("Sign-out failed");
                await ErrorDialog.ShowAsync(_window, "The server did not sign you out.");
                return;
            }

            SetStatus("Signed out");
        }
        catch (Exception ex)
        {
            Log.QuickLog($"Server sign-out failed: {ex}");
            SetStatus("Sign-out failed");
            await ErrorDialog.ShowAsync(_window, $"Could not sign out: {ex.Message}");
        }
    }

    public async Task UploadProjectToServer()
    {
        SoundEffects.Click.Play();

        if (Project == null)
        {
            await ErrorDialog.ShowAsync(_window, "Open a project before uploading it.");
            return;
        }

        string? temporarySourcePath = null;
        try
        {
            if (!await EnsureServerConnected())
            {
                SetStatus("Server unavailable");
                await ErrorDialog.ShowAsync(_window, "Could not connect to the xPaint server.");
                return;
            }

            if (!await EnsureServerLogin())
                return;

            if (!await SaveProject())
                return;

            if (string.IsNullOrWhiteSpace(Project.ProjectPath) || !File.Exists(Project.ProjectPath))
            {
                await ErrorDialog.ShowAsync(_window, "Save the project to disk before uploading it.");
                return;
            }

            string sourcePath = Project.ProjectPath;
            if (!string.Equals(Path.GetExtension(sourcePath), ".xpaint", StringComparison.OrdinalIgnoreCase))
            {
                temporarySourcePath = Path.Combine(
                    Path.GetTempPath(),
                    $"xpaint-upload-{Guid.NewGuid():N}.xpaint"
                );
                File.Copy(sourcePath, temporarySourcePath);
                sourcePath = temporarySourcePath;
            }

            string? serverId = Project.Metadata.serverId;
            if (Project.Metadata.IsLinked())
            {
                var options = new UploadOptionsDialog(serverId!);
                string? action = await options.ShowDialog<string>(_window);
                if (action == "cancel" || action == null)
                    return;

                if (action == "unlink")
                {
                    Project.Metadata.serverId = null;
                    await Project.SaveToDisk();
                    SetStatus("Project unlinked from server");
                    return;
                }
            }
            else
            {
                var titleDialog = new InputDialog(
                    "Upload Project",
                    "Choose a title for the server project:",
                    Project.Metadata.name
                );
                string? title = await titleDialog.ShowAsync(_window);
                if (string.IsNullOrWhiteSpace(title))
                    return;

                serverId = await _server.CreateNewServerProject(title.Trim());
                if (string.IsNullOrWhiteSpace(serverId))
                {
                    SetStatus("Server project creation failed");
                    await ErrorDialog.ShowAsync(_window, "The server project could not be created.");
                    return;
                }

                Project.Metadata.serverId = serverId;
                await Project.SaveToDisk();
            }

            SetStatus("Uploading project...");
            string? versionId = await _server.UploadNewVersion(serverId!, sourcePath);
            if (string.IsNullOrWhiteSpace(versionId))
            {
                SetStatus("Project upload failed");
                await ErrorDialog.ShowAsync(
                    _window,
                    "The project could not be uploaded. Check your connection and sign-in, then try again."
                );
                return;
            }

            SetStatus($"Project uploaded (version {versionId})");
        }
        catch (Exception ex)
        {
            Log.QuickLog($"Project upload failed: {ex}");
            SetStatus("Project upload failed");
            await ErrorDialog.ShowAsync(_window, $"Could not upload the project: {ex.Message}");
        }
        finally
        {
            if (temporarySourcePath != null && File.Exists(temporarySourcePath))
            {
                try
                {
                    File.Delete(temporarySourcePath);
                }
                catch (Exception ex)
                {
                    Log.QuickLog($"Failed to clean up temporary upload file: {ex.Message}");
                }
            }
        }
    }

    public async Task DownloadProjectFromServer()
    {
        SoundEffects.Click.Play();

        try
        {
            if (!await EnsureServerConnected())
            {
                SetStatus("Server unavailable");
                await ErrorDialog.ShowAsync(_window, "Could not connect to the xPaint server.");
                return;
            }

            if (!await EnsureServerLogin())
                return;

            List<ProjectInfo>? projects = await _server.ListUserProjects();
            if (projects == null)
            {
                SetStatus("Could not retrieve server projects");
                await ErrorDialog.ShowAsync(_window, "The server project list could not be retrieved.");
                return;
            }

            if (projects.Count == 0)
            {
                SetStatus("No server projects found");
                return;
            }

            var selectionDialog = new SelectServerProjectDialog(projects);
            string? projectId = await selectionDialog.ShowDialog<string>(_window);
            if (string.IsNullOrWhiteSpace(projectId))
                return;

            if (!await AskSaveBeforeClosing())
                return;

            ProjectInfo? selectedProject = projects.FirstOrDefault(project => project.id == projectId);
            if (selectedProject == null)
            {
                SetStatus("Server project selection is no longer available");
                await ErrorDialog.ShowAsync(_window, "The selected server project is no longer available.");
                return;
            }

            string suggestedFileName = $"{GetSafeFileName(selectedProject.title)}.xpaint";
            var savePicker = await _window.StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Download Server Project",
                    DefaultExtension = "xpaint",
                    SuggestedFileName = suggestedFileName,
                    ShowOverwritePrompt = true
                }
            );
            if (savePicker == null)
                return;

            string destinationPath = savePicker.Path.LocalPath;
            SetStatus("Downloading project...");
            bool downloaded = await _server.DownloadLatestProjectSource(destinationPath, projectId);
            if (!downloaded)
            {
                SetStatus("Project download failed");
                await ErrorDialog.ShowAsync(_window, "The selected project could not be downloaded.");
                return;
            }

            await _window.mainGui.OpenProject(destinationPath);
            SetStatus($"Downloaded {selectedProject.title}");
        }
        catch (Exception ex)
        {
            Log.QuickLog($"Project download failed: {ex}");
            SetStatus("Project download failed");
            await ErrorDialog.ShowAsync(_window, $"Could not download the project: {ex.Message}");
        }
    }

    public async Task ManageServerVersions()
    {
        SoundEffects.Click.Play();

        if (Project == null || !Project.Metadata.IsLinked())
        {
            await ErrorDialog.ShowAsync(_window, "Open a project linked to a server project to manage its versions.");
            return;
        }

        string projectId = Project.Metadata.serverId!;
        string? temporarySourcePath = null;
        try
        {
            if (!await EnsureServerConnected())
            {
                SetStatus("Server unavailable");
                await ErrorDialog.ShowAsync(_window, "Could not connect to the xPaint server.");
                return;
            }

            if (!await EnsureServerLogin())
                return;

            List<ProjectVersionInfo>? versions = await _server.ListProjectVersions(projectId);
            if (versions == null)
            {
                SetStatus("Could not retrieve server versions");
                await ErrorDialog.ShowAsync(_window, "The server versions could not be retrieved.");
                return;
            }

            if (versions.Count == 0)
            {
                SetStatus("No server versions found");
                return;
            }

            var dialog = new ManageServerVersionsDialog(versions);
            ServerVersionActionResult? action = await dialog.ShowDialog<ServerVersionActionResult?>(_window);
            if (action == null)
                return;

            if (action.Action == ServerVersionAction.Delete)
            {
                string versionName = string.IsNullOrWhiteSpace(action.Version.Name)
                    ? action.Version.Id
                    : action.Version.Name;
                var confirmation = new DeletionConfirmationDialog($"server version \"{versionName}\"");
                if (await confirmation.ShowAsync(_window) != "delete")
                    return;

                SetStatus("Deleting server version...");
                if (!await _server.DeleteProjectVersion(projectId, action.Version.Id))
                {
                    SetStatus("Version deletion failed");
                    await ErrorDialog.ShowAsync(_window, "The selected server version could not be deleted.");
                    return;
                }

                SetStatus($"Deleted server version {action.Version.Id}");
                return;
            }

            if (!await SaveProject())
                return;

            if (Project == null ||
                string.IsNullOrWhiteSpace(Project.ProjectPath) ||
                !File.Exists(Project.ProjectPath))
            {
                await ErrorDialog.ShowAsync(_window, "Save the current project before replacing a server version.");
                return;
            }

            string sourcePath = Project.ProjectPath;
            if (!string.Equals(Path.GetExtension(sourcePath), ".xpaint", StringComparison.OrdinalIgnoreCase))
            {
                temporarySourcePath = Path.Combine(
                    Path.GetTempPath(),
                    $"xpaint-version-{Guid.NewGuid():N}.xpaint"
                );
                File.Copy(sourcePath, temporarySourcePath);
                sourcePath = temporarySourcePath;
            }

            SetStatus($"Replacing server version {action.Version.Id}...");
            if (!await _server.UpdateProjectVersionSource(projectId, action.Version.Id, sourcePath))
            {
                SetStatus("Version replacement failed");
                await ErrorDialog.ShowAsync(_window, "The selected server version could not be replaced.");
                return;
            }

            SetStatus($"Replaced server version {action.Version.Id}");
        }
        catch (Exception ex)
        {
            Log.QuickLog($"Server version management failed: {ex}");
            SetStatus("Server version management failed");
            await ErrorDialog.ShowAsync(_window, $"Could not manage server versions: {ex.Message}");
        }
        finally
        {
            if (temporarySourcePath != null && File.Exists(temporarySourcePath))
            {
                try
                {
                    File.Delete(temporarySourcePath);
                }
                catch (Exception ex)
                {
                    Log.QuickLog($"Failed to clean up temporary version file: {ex.Message}");
                }
            }
        }
    }

    private async Task<bool> EnsureServerConnected()
    {
        if (_server.IsConnected)
            return await _server.CheckConnection();

        await _server.InitServer();
        return _server.IsConnected;
    }

    private async Task<bool> EnsureServerLogin()
    {
        if (await _server.IsLoggedIn())
            return true;

        var dialog = new SignInDialog(_server);
        bool signedIn = await dialog.ShowDialog<bool>(_window);
        if (!signedIn)
            SetStatus("Sign-in cancelled");
        return signedIn;
    }

    private static string GetSafeFileName(string title)
    {
        string safeName = string.Concat(
            title.Where(character => !Path.GetInvalidFileNameChars().Contains(character))
        ).Trim();
        return string.IsNullOrWhiteSpace(safeName) ? "server-project" : safeName;
    }


    // ---------------------------------------------------------------
    //  Sprite Editor
    // ---------------------------------------------------------------

    public void OpenSpriteEditor(UserControl editor)
    {
        CloseCurrentEditor();
        _view.CenterHost.Content = editor;
    }

    private void StopRuntime()
    {
        _runtimeTimer?.Stop();
        _runtimeTimer = null;
    }
}
