using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PaintPower.Editors.Logic;
using PaintPower.ProjectSystem.SpriteEditor;
using PaintPower_VM.VMPanel;
using Toolbox.Accessibility.Translation;
using Toolbox.Logging;
using Toolbox.SoundEffects;

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

    public ProcessingPanel? GetProcessingPanel()
    {
        return VmPanelControl.FindControl<StageAreaPart>("StageArea")?.FindControl<ProcessingPanel>("LoadingPart");
    }

    // Loading dialog
    private bool _isProjectLoading = false;

    public async Task SetProjectLoading(bool isLoading)
    {
        _isProjectLoading = isLoading;

        EditorArea.IsVisible = !isLoading;
        VmOnlyArea.IsVisible = isLoading;

        if (isLoading)
        {
            if (GetProcessingPanel() is ProcessingPanel loader)
                loader.Reset();
        }

        // Force Avalonia to refresh layout
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            VmOnlyArea.InvalidateMeasure();
            VmOnlyArea.InvalidateArrange();
            VmOnlyArea.InvalidateVisual();

            VmPanelControl?.InvalidateVisual();
        });
    }

    public async Task UpdateLoadingProgress(int processed, int total)
    {

        if (GetProcessingPanel() is ProcessingPanel loader)
        {
            int percent = (int)((processed / (double)total) * 100);

            loader.SetPercent(percent);
            loader.SetText($"{Translator.Map("Loading Project")}...", $"{processed} of {total} assets loaded");
        } else
        {
            
        }

        // Force Avalonia to refresh layout
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            VmOnlyArea.InvalidateMeasure();
            VmOnlyArea.InvalidateArrange();
            VmOnlyArea.InvalidateVisual();

            VmPanelControl?.InvalidateVisual();
        });
    }

    public void UpdateSavingProgress(int processed, int total)
    {
        Log.QuickLog("Updating saving progress...");

        if (GetProcessingPanel() is ProcessingPanel loader)
        {
            int percent = (int)((processed / (double)total) * 100);

            if (loader != null)
            {
                loader.SetPercent(percent);
                loader.SetText($"{Translator.Map("Saving Project")}...", $"{processed} of {total} assets saved");
            }
        }
    }

    public async Task UpdateBuildingProgress(string message, int processed, int total)
    {
        if (GetProcessingPanel() is ProcessingPanel loader)
        {
            int percent = (int)((processed / (double)total) * 100);

            loader.SetPercent(percent);
            loader.SetText($"{Translator.Map("Building Project")}...", $"Project {percent}% built.", "{message}... ");
        }

        Log.QuickLog($"{Translator.Map("Building Project")}... Project {(int)((processed / (double)total) * 100)}% built. {message}... ");

        // Force Avalonia to refresh layout
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            VmOnlyArea.InvalidateMeasure();
            VmOnlyArea.InvalidateArrange();
            VmOnlyArea.InvalidateVisual();

            VmPanelControl?.InvalidateVisual();
        });
    }

    public override void SetUIMode(EditorUIMode mode)
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

    public override HeaderDefinition GetHeaderDefinition()
    {
        return new HeaderDefinition
        {
            Menus = new()
            {
                ["File"] = new()
            {
                new HeaderItem { Label = "New", Command = () => _ = Logic.NewProject() },
                new HeaderItem { IsSeparator = true },
                new HeaderItem { Label = "Open...", Command = () => _ = Logic.OpenProjectDialog() },
                new HeaderItem { IsSeparator = true },
                new HeaderItem { Label = "Save", Command = () => _ = Logic.SaveProject() },
                new HeaderItem { Label = "Save As...", Command = () => _ = Logic.SaveProjectAs() },
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
