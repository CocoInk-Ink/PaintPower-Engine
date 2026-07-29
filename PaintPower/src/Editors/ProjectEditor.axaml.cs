using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PaintPower.Accessibility.Translation;
using PaintPower.Editors.Logic;
using PaintPower.ProjectSystem.SpriteEditor;
using PaintPower.Tools.SoundEffects;
using PaintPower.VMPanel;

namespace PaintPower.Editors;

public partial class ProjectEditor : Editor
{
    public ProjectEditorLogic Logic { get; private set; }
    public MainWindow Window { get; private set; }

    public EditorUIMode UIMode;

    public ProjectEditor()
    {
        InitializeComponent();

        Window = MainWindow.window;
        Logic = new ProjectEditorLogic(this);

        Translator.LanguageChanged += () => RefreshTranslations();

        SpriteManager.SpriteSelected += sprite =>
        {
            SpriteProperties.LoadSprite(sprite);

            var spriteEditor = new SpriteEditorView(sprite, Logic.Workspace, Logic.EditorManager);
            Logic.OpenSpriteEditor(spriteEditor);
        };
    }

    public void DirtyProject() => Logic.DirtyProject();

    // Loading dialog
    private bool _isProjectLoading = false;

    public async Task SetProjectLoading(bool isLoading)
    {
        _isProjectLoading = isLoading;

        EditorArea.IsVisible = !isLoading;
        VmOnlyArea.IsVisible = isLoading;

        if (isLoading)
        {
            if (VmPanelControl?.StageArea?.LoadingPart is ProcessingPanel loader)
                loader.Reset();
        }

        // Force Avalonia to refresh layout
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            VmOnlyArea.InvalidateMeasure();
            VmOnlyArea.InvalidateArrange();
            VmOnlyArea.InvalidateVisual();
        });
    }

    public async Task UpdateLoadingProgress(int processed, int total)
    {
        if (VmPanelControl?.StageArea?.LoadingPart is ProcessingPanel loader)
        {
            int percent = (int)((processed / (double)total) * 100);

            loader.SetPercent(percent);
            loader.SetText($"{Translator.Map("Loading Project")}...", $"{processed} of {total} assets loaded");
        }

        // Force Avalonia to refresh layout
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            VmOnlyArea.InvalidateMeasure();
            VmOnlyArea.InvalidateArrange();
            VmOnlyArea.InvalidateVisual();
        });
    }

    public async Task UpdateSavingProgress(int processed, int total)
    {
        if (VmPanelControl?.StageArea?.LoadingPart is ProcessingPanel loader)
        {
            int percent = (int)((processed / (double)total) * 100);

            loader.SetPercent(percent);
            loader.SetText($"{Translator.Map("Saving Project")}...", $"{processed} of {total} assets saved");
        }

        // Force Avalonia to refresh layout
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            VmOnlyArea.InvalidateMeasure();
            VmOnlyArea.InvalidateArrange();
            VmOnlyArea.InvalidateVisual();
        });
    }

    public void SetUIMode(EditorUIMode mode)
    {
        UIMode = mode;

        EditorArea.IsVisible = false;
        VmOnlyArea.IsVisible = false;

        switch (mode)
        {
            case EditorUIMode.WebPlayer:
            case EditorUIMode.ProjectPlayer:
            case EditorUIMode.Loading:
                VmOnlyArea.IsVisible = true;
                break;

            case EditorUIMode.ProjectEditor:
                EditorArea.IsVisible = true;
                break;
        }

        InvalidateVisual();
    }

    // Status bar click
    public void StatusClicked(object sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        Logic.SaveProject();
    }

    public override HeaderDefinition GetHeaderDefinition()
    {
        return new HeaderDefinition
        {
            Menus = new()
            {
                ["File"] = new()
            {
                new HeaderItem { Label = "New", Command = () => Logic.NewProject() },
                new HeaderItem { IsSeparator = true },
                new HeaderItem { Label = "Open...", Command = () => Logic.OpenProjectDialog() },
                new HeaderItem { IsSeparator = true },
                new HeaderItem { Label = "Save", Command = () => Logic.SaveProject() },
                new HeaderItem { Label = "Save As...", Command = () => Logic.SaveProjectAs() },
                new HeaderItem { IsSeparator = true },
                new HeaderItem { Label = "Close Project", Command = () => MainWindow.window.mainGui.CloseProject() },
                new HeaderItem { IsSeparator = true },
                new HeaderItem { Label = "Exit", Command = () => MainWindow.window.Close() }
            },

                ["Edit"] = StandardEditMenu(),

                ["Project"] = new()
                {
                    // Commented out until the logic is implemented
                    /*new HeaderItem { Label = "Build Project", Command = () => Logic.Build() },
                    new HeaderItem { IsSeparator = true },
                    new HeaderItem { Label = "Play", Command = () => Logic.Play() },
                    new HeaderItem { Label = "Build and Run", Command = () => Logic.BuildAndRun() },
                    new HeaderItem { IsSeparator = true },
                    new HeaderItem { Label = "Package Project", Command = () => Logic.Package() }*/
                },

                ["Help"] = StandardHelpMenu(),

                ["Server"] = new()
            {
                new HeaderItem { Label = Translator.Map("Make Connection"), Command = () => {} },
                new HeaderItem { Label = Translator.Map("Upload project to server!"), Command = () => {} },
                new HeaderItem { Label = Translator.Map("Download project from server"), Command = () => {} },
                new HeaderItem { Label = Translator.Map("Login"), Command = () => {} },
                new HeaderItem { Label = Translator.Map("Logout"), Command = () => {} },
            },

                ["Language"] = LanguageMenu()
            }
        };
    }

    // Translation refresh
    public void RefreshTranslations()
    {
        if (!Translator.refreshNeeded) return;
        Window.Title = Translator.Translate("PaintPower");
        SpriteManager.TranslateGUI();
        Translator.refreshNeeded = false;
    }
}
