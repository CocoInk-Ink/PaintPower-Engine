using System;
using System.IO;
using System.Threading.Tasks;
using PaintPower.Editors;
using PaintPower.Logging;
using PaintPower.Networking;
using PaintPower.ProjectSystem;
using PaintPower.SpriteEditor;
using PaintPower.Time;
using PaintPower.Accessibility.Translation;
using PaintPower.VMPanel;
using PaintPower.Dialogs;

namespace PaintPower;

/// <summary>
/// Core engine runtime for PaintPower.
/// Contains project, editor, VM and server logic,
/// without any direct Avalonia UI dependencies.
/// </summary>
public class PaintPowerRuntime
{
    // Version info (kept from PaintPower_Engine)
    public static readonly string VersionNumber = "1.0.1.2";
    public static readonly string BuildTime = new Date().getBuildTimestamp();
    public static readonly string DevStatus = "Pre-Alpha";
    public static string MajorVersion = $"{Translator.Map(DevStatus)} {VersionNumber}";
    public static string Version = $"{Translator.Map("Version")}: {MajorVersion} {Translator.Map("build")} {BuildTime}";

    // Core runtime state
    public Vm.Vm Vm { get; private set; } = new();
    public PaintProject Project { get; private set; }
    public Editor EditorManager { get; private set; }
    public EditorBase? CurrentEditor { get; private set; }
    public Server Server { get; private set; }

    public bool IsNewProject { get; set; } = true;
    public bool SaveNeeded { get; set; } = false;

    // Events for UI layer to subscribe to
    public event Action<string>? ProjectStatusChanged;
    public event Action<string>? NetworkStatusChanged;
    public event Action<string>? UserStatusChanged;
    public event Action<PaintSprite>? SpriteSelected;
    public event Action<PaintSprite, SkinDefinition>? SkinSelected;
    public event Action? ProjectClosed;
    public event Action? ProjectRefreshed;
    public event Action? ProjectStarted;

    public string NetworkStatus { get; private set; } = "Not connected";
    public string UserStatus { get; private set; } = "not logged in.";

    public PaintPowerRuntime()
    {
        Project = new PaintProject();
        EditorManager = new Editor(Project.Workspace);
        Server = new Server();

        Log.QuickLog(Version);
    }

    public string TranslateVersion()
    {
        MajorVersion = $"{Translator.Map("Pre-Alpha")} {VersionNumber}";
        Version = $"{Translator.Map("Version")}: {MajorVersion} {Translator.Map("build")} {BuildTime}";
        return Version;
    }

    // --------------------------------------------------------------------
    // Status helpers (UI can bind to events)
    // --------------------------------------------------------------------
    public string SetProjectStatus(string status)
    {
        ProjectStatusChanged?.Invoke(status);
        FixUserStatus();
        return status;
    }

    public string SetNetworkStatus(string status)
    {
        NetworkStatus = status;
        NetworkStatusChanged?.Invoke(status);
        FixUserStatus();
        return status;
    }

    public string SetUserStatus(string status)
    {
        UserStatus = status;
        UserStatusChanged?.Invoke(status);
        FixUserStatus();
        return status;
    }

    private async void FixUserStatus()
    {
        // UI layer can recompute its status bar using these values.
        bool loggedIn = await Server.IsLoggedIn();
        string userText = loggedIn ? $"Logged in as {Server.Username}" : UserStatus;
        Log.QuickLog($"Status: {NetworkStatus} | {userText}");
    }

    // --------------------------------------------------------------------
    // Sprite / skin selection (UI decides how to show editors)
    // --------------------------------------------------------------------
    public void OnSpriteSelected(PaintSprite sprite)
    {
        if (Project == null)
            return;

        SpriteSelected?.Invoke(sprite);
        SetProjectStatus($"{Translator.Translate("Editing Sprite:")} {sprite.Name}");
    }

    public void OpenSkinEditor(PaintSprite sprite, SkinDefinition skin)
    {
        if (Project == null)
            return;

        SkinSelected?.Invoke(sprite, skin);
        SetProjectStatus($"{Translator.Translate("Editing Skin: ")} {skin.Name} {Translator.Map(" in sprite: ")} {sprite.Name}");
    }

    // --------------------------------------------------------------------
    // Project lifecycle
    // --------------------------------------------------------------------
    public void CloseProject()
    {
        SetProjectStatus(Translator.Map("Select or create a project to get started."));
        SaveNeeded = false;

        Translator.load(null); // reset translation to default

        CloseCurrentEditor();

        Project.Sprites.Clear();

        Project = null!;
        EditorManager = null!;
        CurrentEditor = null;
        Server = null!;

        ProjectClosed?.Invoke();
    }

    public async Task OpenProjectFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("filePath must be provided by the UI layer.");

        Translator.load(null); // reset translation to default

        CloseCurrentEditor();
        Project.Sprites.Clear();

        await AnimateStatus(
            Translator.Map("Loading Project"),
            () => Task.Run(() => Project.Load(filePath))
        );

        Project.ProjectPath = filePath;

        RefreshSession(false);
    }

    public async Task NewProject(Func<Task<string?>>? askSaveBeforeNew = null)
    {
        Translator.load(null); // reset translation to default

        if (SaveNeeded && askSaveBeforeNew != null)
        {
            var result = await askSaveBeforeNew();

            switch (result)
            {
                case "save":
                    await Save();
                    break;
                case "saveas":
                    // UI layer should call SaveAs with a path
                    return;
                case "dontsave":
                    break;
                case null:
                    return;
            }
        }

        Project = new PaintProject();
        Project.CreateNew();
        EditorManager = new Editor(Project.Workspace);
        Server = new Server();

        Start();
    }

    public virtual void RefreshSession(bool makeNew = true)
    {
        CloseEditor();

        Project = makeNew ? new PaintProject() : Project;
        EditorManager = new Editor(Project.Workspace);
        CurrentEditor = null;

        ProjectRefreshed?.Invoke();

        Vm = new();
    }

    public virtual async void Start()
    {
        await Task.Yield();

        CloseEditor();

        Project = new PaintProject();
        EditorManager = new Editor(Project.Workspace);
        CurrentEditor = null;

        Project.CreateNew();

        SetProjectStatus(Translator.Translate("Not edited yet."));

        ProjectStarted?.Invoke();

        Vm = new();
    }

    // --------------------------------------------------------------------
    // Editor management
    // --------------------------------------------------------------------
    public void OpenFile(string path)
    {
        Log.Info("Opening file: " + path);
        Log.Info("Closing current editor.");
        CloseCurrentEditor();

        Log.Info("Getting new editor for file type...");
        var editor = EditorManager.GetEditorFromFileType(path);

        CurrentEditor = editor;
    }

    public void CloseCurrentEditor()
    {
        if (CurrentEditor != null)
        {
            Log.Info("Closing current editor.");
            CurrentEditor.Close();
            CurrentEditor = null;
        }
        else
        {
            Log.Info("No editor to close.");
        }
    }

    public void CloseEditor()
    {
        Log.Info("Closing current editor.");
        CurrentEditor?.Close();
        CurrentEditor = null;
    }

    // --------------------------------------------------------------------
    // Server linking / upload / download
    // --------------------------------------------------------------------
    // Compatibility overload for legacy PaintPower_Engine
    // New UI-safe version
    public async Task AskToLinkProject(
        PaintProject project,
        Func<Task<string?>> askLinkChoice,
        Func<Task<string?>> askServerProjectId)
    {
        var choice = await askLinkChoice();
        if (choice == "cancel")
            return;

        if (choice == "new")
        {
            string? id = await Server.CreateNewServerProject(project.Metadata.name);
            if (id != null)
            {
                project.Metadata.serverId = id;
                project.SaveMetadata();
            }
            return;
        }

        if (choice == "existing")
        {
            var chosenId = await askServerProjectId();
            if (!string.IsNullOrEmpty(chosenId))
            {
                project.Metadata.serverId = chosenId;
                project.SaveMetadata();
            }
        }
    }

    // Compatibility overload for PaintPower_Engine
    public void AskToLinkProject(PaintProject project)
    {
        // PaintPower_Engine will override this with UI logic
        throw new NotImplementedException(
            "AskToLinkProject(project) must be handled by PaintPowerUI.");
    }


    public async Task SaveToServer(Func<Task<string?>> askUploadChoice)
    {
        var project = Project;
        var server = Server;

        if (!await server.IsLoggedIn())
        {
            SetUserStatus("You must sign in before uploading.");
            return;
        }

        if (!project.Metadata.IsLinked())
        {
            // UI should call AskToLinkProject before this
            return;
        }

        if (project.Metadata.IsLinked())
        {
            var choice = await askUploadChoice();

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

    public async Task Login(string username, string password)
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

    public async Task DownloadProjectFromServer(string savePath, Func<Task<string?>> askServerProjectId)
    {
        var list = await Server.ListUserProjects();
        var chosenId = await askServerProjectId();

        if (string.IsNullOrEmpty(chosenId))
            return;

        Server.DownloadProject(savePath, Convert.ToInt32(chosenId));
    }

    // --------------------------------------------------------------------
    // Saving
    // --------------------------------------------------------------------
    public bool IsSavingAnimationRunning { get; set; } = false;

    // UI provides the status update callback
    public Task RunSavingAnimation(Func<string, Task> setStatus)
    {
        IsSavingAnimationRunning = true;

        string[] frames =
        {
        "Saving Project",
        "Saving Project.",
        "Saving Project..",
        "Saving Project..."
    };

        return Task.Run(async () =>
        {
            int index = 0;

            while (IsSavingAnimationRunning)
            {
                await setStatus(Translator.Map(frames[index]));
                index = (index + 1) % frames.Length;
                await Task.Delay(300);
            }
        });
    }

    // Compatibility overload for PaintPower_Engine
    public Task RunSavingAnimation()
    {
        return RunSavingAnimation(msg =>
        {
            SetProjectStatus(msg);
            return Task.CompletedTask;
        });
    }


    public async Task Save()
    {
        if (!SaveNeeded && !IsNewProject) return;

        try
        {
            Log.QuickLog("Saving Project...");

            var animationTask = RunSavingAnimation(msg => Task.Run(() => SetProjectStatus(msg)));

            await ProjectSaver.Save(Project, CurrentEditor);

            IsSavingAnimationRunning = false;
            await animationTask;

            Log.QuickLog(SetProjectStatus(Translator.Map("Project Saved!")));
        }
        catch (Exception ex)
        {
            Log.QuickLog($"Error while saving project! {ex}");
        }
    }

    // --------------------------------------------------------------------
    // Status animation helper
    // --------------------------------------------------------------------
    public async Task AnimateStatus(string baseMessage, Func<Task> action)
    {
        string[] frames =
        {
            baseMessage,
            baseMessage + ".",
            baseMessage + "..",
            baseMessage + "..."
        };

        int index = 0;
        bool isRunning = true;

        var animationLoop = Task.Run(async () =>
        {
            while (isRunning)
            {
                SetProjectStatus(frames[index]);
                index = (index + 1) % frames.Length;
                await Task.Delay(300);
            }
        });

        await action();

        isRunning = false;
        await animationLoop;

        SetProjectStatus(Translator.Map("Done!"));
    }
}