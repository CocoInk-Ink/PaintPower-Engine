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
using PaintPower.Accessibility.Translation;
using PaintPower.VMPanel;
using PaintPower.Tools.SoundEffects;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace PaintPower;

public partial class EditorPart : EditorBase
{
    // Instance reference to the engine wrapper
    public PaintPower_Engine Engine { get; private set; }

    // Reference to the window (no longer static)
    public MainWindow Window { get; private set; }

    public EditorPart()
    {
        InitializeComponent();

        Translator.LanguageChanged += () => RefreshTranslations();

        // Display version
        VersionInfoTextBlock.Text = PaintPower_Engine.version;

        SpriteManager.SpriteSelected += sprite =>
        {
            SpriteProperties.LoadSprite(sprite);
        };
    }

    // Called by MainWindow
    public EditorPart attachPaintPower(PaintPower_Engine engine)
    {
        Engine = engine;
        Window = MainWindow.window;
        return this;
    }

    public void StatusClicked(object sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        Engine.Save();
    }


    // ------------------------------------------------------------
    // File menu
    // ------------------------------------------------------------
    private void OnFileNew(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        Engine.newProject();
    }

    private void OnFileOpen(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        Engine.OpenProjectFile();
    }

    private void OnFileSave(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        Engine.Save();
    }

    private void OnFileSaveAs(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        Engine.SaveAs();
    }

    private void OnFileExit(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        Window.Close();
    }

    private void OnCloseProject(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        Engine.CloseProject();
    }

    // ------------------------------------------------------------
    // Edit menu
    // ------------------------------------------------------------
    private void OnEditUndo(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        Engine._editor?.Undo();
    }

    private void OnEditRedo(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        Engine._editor?.Redo();
    }

    private void OnEditCut(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        Engine._editor?.Cut();
    }

    private void OnEditCopy(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        Engine._editor?.Copy();
    }

    private void OnEditPaste(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        Engine._editor?.Paste();
    }

    // ------------------------------------------------------------
    // Server menu
    // ------------------------------------------------------------
    private async void OnLogin(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        var dialog = new SignInDialog();
        var result = await dialog.ShowDialog<bool>(Window);

        if (result == null)
        {
            Engine.SetUserStatus("Login cancelled.");
        }
    }

    private async void OnLogout(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        await Engine.server.Logout();
    }

    private void OnMakeConnection(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        Engine.server.checkConnection();
    }

    private void OnUploadProject(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        Engine.SaveToServer();
    }

    private void OnDownloadProjectFromServer(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        Engine.DownloadProjectFromServer();
    }

    // ------------------------------------------------------------
    // Help menu
    // ------------------------------------------------------------
    private void OnHelpDocumentation(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        Process.Start(new ProcessStartInfo
        {
            FileName = "http://www.cocoink.ink/PaintPower/docs",
            UseShellExecute = true
        });
    }

    private void OnHelpReportBug(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        Process.Start(new ProcessStartInfo
        {
            FileName = "http://www.cocoink.ink/PaintPower/bugreport",
            UseShellExecute = true
        });
    }

    private void OnHelpAbout(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        var aboutDialog = new PopupWindowDialog(
            "About PaintPower",
            $"PaintPower Engine {PaintPower_Engine.version}",
            "Created by CocoBox84 — http://github.com/CocoBox84/PaintPower-Engine/"
        );
        aboutDialog.ShowDialog(Window);
    }

    // ------------------------------------------------------------
    // Translation refresh
    // ------------------------------------------------------------
    public void RefreshTranslations()
    {
        if (!Translator.refreshNeeded) return;

        Window.Title = Translator.Translate("PaintPower");

        PaintPower_Engine.App.translateVersion();
        VersionInfoTextBlock.Text = PaintPower_Engine.version;

        FileMenu.Header = Translator.Translate("File");
        EditMenu.Header = Translator.Translate("Edit");
        ServerMenu.Header = Translator.Translate("Server");
        LanguageDropdown.Header = Translator.Translate("Language");
        HelpMenu.Header = Translator.Translate("Help");

        FileNew.Header = Translator.Translate("New");
        FileOpen.Header = Translator.Translate("Open...");
        FileSave.Header = Translator.Translate("Save");
        FileSaveAs.Header = Translator.Translate("Save As...");
        FileClose.Header = Translator.Translate("Close Project");
        FileExit.Header = Translator.Translate("Exit");

        EditUndo.Header = Translator.Translate("Undo");
        EditRedo.Header = Translator.Translate("Redo");
        EditCut.Header = Translator.Translate("Cut");
        EditCopy.Header = Translator.Translate("Copy");
        EditPaste.Header = Translator.Translate("Paste");

        MakeConnection.Header = Translator.Translate("Make Connection");
        UploadProject.Header = Translator.Translate("Upload Project to server!");
        OpenCollaborators.Header = Translator.Translate("Open collaborators");

        HelpDocumentation.Header = Translator.Translate("Documentation");
        HelpReportBug.Header = Translator.Translate("Report a Bug");
        HelpAbout.Header = Translator.Translate("About");

        ProjectStatus.Text = Translator.Translate("Not edited yet.");

        SpriteManager.TranslateGUI();

        InvalidateVisual();
        Translator.refreshNeeded = false;
    }
}
