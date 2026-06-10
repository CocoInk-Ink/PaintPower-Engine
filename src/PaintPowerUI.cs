using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using PaintPower.ProjectSystem;
using PaintPower.SpriteEditor;
using PaintPower.Editors;
using PaintPower.Accessibility.Translation;
using PaintPower.Dialogs;

namespace PaintPower;

/// <summary>
/// UI layer that connects Avalonia controls to the PaintPowerRuntime.
/// This replaces the old PaintPower_Engine UI logic.
/// </summary>
public class PaintPowerUI
{
    public PaintPowerRuntime Runtime { get; }
    public MainWindow Window { get; }
    public EditorPart EditorGui { get; }

    private SpriteEditorView? _spriteEditorView;
    private SkinEditorView? _skinEditorView;

    public PaintPowerUI(PaintPowerRuntime runtime, MainWindow window, EditorPart editorGui)
    {
        Runtime = runtime;
        Window = window;
        EditorGui = editorGui;

        HookRuntimeEvents();
    }

    // ------------------------------------------------------------
    // Runtime → UI event wiring
    // ------------------------------------------------------------
    private void HookRuntimeEvents()
    {
        Runtime.ProjectStatusChanged += s => UI(() => EditorGui.ProjectStatus.Text = s);
        Runtime.NetworkStatusChanged += s => UI(() => UpdateStatusBar());
        Runtime.UserStatusChanged += s => UI(() => UpdateStatusBar());

        Runtime.SpriteSelected += sprite => UI(() => OpenSpriteEditor(sprite));
        Runtime.SkinSelected += (sprite, skin) => UI(() => OpenSkinEditor(sprite, skin));

        Runtime.ProjectClosed += () => UI(ClearUI);
        Runtime.ProjectRefreshed += () => UI(RefreshUI);
        Runtime.ProjectStarted += () => UI(RefreshUI);
    }

    private void UI(Action action)
    {
        Dispatcher.UIThread.Post(action);
    }

    // ------------------------------------------------------------
    // Status bar
    // ------------------------------------------------------------
    private async void UpdateStatusBar()
    {
        string network = Runtime.NetworkStatus;
        string user = (await Runtime.Server.IsLoggedIn())
            ? $"Logged in as {Runtime.Server.Username}"
            : Runtime.UserStatus;

        string project = EditorGui.ProjectStatus.Text;

        EditorGui.StatusBarText.Text = $"{network} | {user} | {project}";
        Window.InvalidateVisual();
    }

    // ------------------------------------------------------------
    // Sprite Editor
    // ------------------------------------------------------------
    private void OpenSpriteEditor(PaintSprite sprite)
    {
        _spriteEditorView = new SpriteEditorView(sprite, Runtime.Project.Workspace);
        EditorGui.CenterHost.Content = _spriteEditorView;

        Window.Title = $"{Translator.Map("PaintPower")} - {sprite.Name}";
    }

    private void OpenSkinEditor(PaintSprite sprite, SkinDefinition skin)
    {
        _skinEditorView = new SkinEditorView(sprite, skin);
        EditorGui.CenterHost.Content = _skinEditorView;

        Window.Title = $"{Translator.Map("PaintPower")} - {sprite.Name}/{skin.Name}";
    }

    // ------------------------------------------------------------
    // UI Reset / Refresh
    // ------------------------------------------------------------
    private void ClearUI()
    {
        EditorGui.CenterHost.Content = null;
        EditorGui.SpriteManager.SpriteList.ItemsSource = null;
        Window.Title = "PaintPower Engine";
    }

    private void RefreshUI()
    {
        EditorGui.SpriteManager.Initialize(Runtime.Project);
        EditorGui.SpriteManager.SpriteSelected -= Runtime.OnSpriteSelected;
        EditorGui.SpriteManager.SpriteSelected += Runtime.OnSpriteSelected;

        Window.Title = $"{Translator.Map("PaintPower")} - {Runtime.Project.Metadata.name}";
    }

    // ------------------------------------------------------------
    // Public UI Actions
    // ------------------------------------------------------------
    public async Task OpenProject(string path)
    {
        await Runtime.OpenProjectFile(path);
        RefreshUI();
    }

    public async Task NewProject()
    {
        await Runtime.NewProject();
        RefreshUI();
    }

    public async Task SaveProject()
    {
        await Runtime.Save();
        RefreshUI();
    }

    public void CloseProject()
    {
        Runtime.CloseProject();
        ClearUI();
    }

    public async Task RunSavingAnimation()
    {
        await Runtime.RunSavingAnimation(async msg =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                EditorGui.ProjectStatus.Text = msg;
            });
        });
    }

    public async Task AskToLinkProject(PaintProject project)
    {
        // 1. Ask link choice
        async Task<string?> AskChoice()
        {
            var dialog = new LinkBeforeUploadDialog();
            return await dialog.ShowDialog<string>(Window);
        }

        // 2. Ask server project ID
        async Task<string?> AskServerProject()
        {
            var list = await Runtime.Server.ListUserProjects();
            var dialog = new SelectServerProjectDialog(list);
            return await dialog.ShowDialog<string>(Window);
        }

        await Runtime.AskToLinkProject(project, AskChoice, AskServerProject);
    }
}
