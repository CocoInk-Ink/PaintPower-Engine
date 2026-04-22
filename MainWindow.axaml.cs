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
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PaintPower.Accessibility.Translation;
using PaintPower.VMPanel;
namespace PaintPower;

public partial class MainWindow : Window
{
    private readonly Editor _editorManager;
    private readonly PaintProject _project;
    private EditorBase _editor;
    public Server server;

    private SpriteEditorView _spriteEditorView;

    public bool saveNeeded = false;

    public static PaintPower_Engine App = new PaintPower_Engine();
    public static MainWindow window;

    public MainWindow()
    {
        InitializeComponent();
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        server = new Server();

        Translator.LanguageChanged += () => RefreshTranslations();

        // Display PaintPower version:
        VersionInfoTextBlock.Text = PaintPower_Engine.version;

        // After, make a static reference.
        window = this;
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        App.attachWindow(this);
        App.Start();
    }

    private void OnFileNew(object? sender, RoutedEventArgs e)
    {
        App.newProject();
    }

    private void OnFileOpen(object? sender, RoutedEventArgs e)
    {
        App.OpenProjectFile();
    }

    private void OnFileSave(object? sender, RoutedEventArgs e)
    {
        App.Save();
    }

    private void OnFileSaveAs(object? sender, RoutedEventArgs e)
    {
        App.SaveAs();
    }

    private void OnFileExit(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnEditUndo(object? sender, RoutedEventArgs e)
    {
        // TODO: hook into editor undo
    }

    private void OnEditRedo(object? sender, RoutedEventArgs e)
    {
        // TODO: hook into editor redo
    }

    private void OnEditCut(object? sender, RoutedEventArgs e)
    {
        // TODO
    }

    private void OnEditCopy(object? sender, RoutedEventArgs e)
    {
        // TODO
    }

    private void OnEditPaste(object? sender, RoutedEventArgs e)
    {
        // TODO
    }

    private async void OnLogin(object? sender, RoutedEventArgs e)
    {
        var dialog = new SignInDialog();
        var result = await dialog.ShowDialog<bool>(this);

        if (result)
        {
            ProjectStatus.Text = $"Logged in as {PaintPower_Engine.App.server.Username}";
        }
        else
        {
            ProjectStatus.Text = "Login cancelled.";
        }
    }

    private async void OnLogout(object? sender, RoutedEventArgs e)
    {
        await PaintPower_Engine.App.server.Logout();
    }

    private void OnMakeConnection(object? sender, RoutedEventArgs e)
    {
        // Enable connection to the server
        App.server.checkConnection();
    }

    private void OnUploadProject(object? sender, RoutedEventArgs e)
    {
        App.SaveToServer();
    }

    private void OnHelpDocumentation(object? sender, RoutedEventArgs e)
    {
        // Open documentation in default browser
        Process.Start(new ProcessStartInfo
        {
            FileName = "http://www.cocoink.ink/PaintPower/docs",
            UseShellExecute = true
        });
    }

    private void OnHelpReportBug(object? sender, RoutedEventArgs e)
    {
        // Open bug report page in default browser
        Process.Start(new ProcessStartInfo
        {
            FileName = "http://www.cocoink.ink/PaintPower/bugreport",
            UseShellExecute = true
        });
    }

    private void OnHelpAbout(object? sender, RoutedEventArgs e)
    {
        // Show about dialog
        var aboutDialog = new PopupWindowDialog("About PaintPower", $"PaintPower Engine  {PaintPower_Engine.version}", "Created by CocoBox84 and collaborators: http://github.com/CocoBox84/PaintPower-Engine/");
        aboutDialog.ShowDialog(this);
    }

    public void RefreshTranslations()
    {
        if (!Translator.refreshNeeded) return;

        // Window title
        Title = Translator.Translate("PaintPower");

        // Display PaintPower version:
        PaintPower_Engine.App.translateVersion();
        VersionInfoTextBlock.Text = PaintPower_Engine.version;

        // Menus
        FileMenu.Header = Translator.Translate("File");
        EditMenu.Header = Translator.Translate("Edit");
        ServerMenu.Header = Translator.Translate("Server");
        LanguageDropdown.Header = Translator.Translate("Language");
        HelpMenu.Header = Translator.Translate("Help");

        // File submenu
        FileNew.Header = Translator.Translate("New");
        FileOpen.Header = Translator.Translate("Open...");
        FileSave.Header = Translator.Translate("Save");
        FileSaveAs.Header = Translator.Translate("Save As...");
        FileExit.Header = Translator.Translate("Exit");

        // Edit submenu
        EditUndo.Header = Translator.Translate("Undo");
        EditRedo.Header = Translator.Translate("Redo");
        EditCut.Header = Translator.Translate("Cut");
        EditCopy.Header = Translator.Translate("Copy");
        EditPaste.Header = Translator.Translate("Paste");

        // Server submenu
        MakeConnection.Header = Translator.Translate("Make Connection");
        UploadProject.Header = Translator.Translate("Upload Project to server!");
        OpenCollaborators.Header = Translator.Translate("Open collaborators");

        // Help submenu
        HelpDocumentation.Header = Translator.Translate("Documentation");
        HelpReportBug.Header = Translator.Translate("Report a Bug");
        HelpAbout.Header = Translator.Translate("About");

        // Status text
        ProjectStatus.Text = Translator.Translate("Not edited yet.");

        VmPanelPanel.refresh();
        SpriteManager.TranslateGUI();

        InvalidateVisual();

        Translator.refreshNeeded = false;
    }
}