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
using PaintPower.Tools.SoundEffects;
namespace PaintPower;

public partial class EditorPart : UserControl
{
    public bool saveNeeded = false;
#pragma warning disable
    public static PaintPower_Engine App;

    MainWindow window;

    #pragma warning enable

    public EditorPart()
    {

        InitializeComponent();
        Translator.LanguageChanged += () => RefreshTranslations();

        // Display PaintPower version:
        VersionInfoTextBlock.Text = PaintPower_Engine.version;
    }

    public EditorPart attachPaintPower(PaintPower_Engine paintPower_Engine)
    {
        App = paintPower_Engine;
        return this;
    }

    public void StatusClicked(object sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        App.Save();
    }

    private void OnFileNew(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        App.newProject();
    }

    private void OnFileOpen(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        App.OpenProjectFile();
    }

    private void OnFileSave(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        App.Save();
    }

    private void OnFileSaveAs(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        App.SaveAs();
    }

    private void OnFileExit(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        MainWindow.window.Close();
    }

    private void OnEditUndo(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        // TODO: hook into editor undo
    }

    private void OnEditRedo(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        // TODO: hook into editor redo
    }

    private void OnEditCut(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        // TODO
    }

    private void OnEditCopy(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        // TODO
    }

    private void OnEditPaste(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        // TODO
    }

    private async void OnLogin(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        var dialog = new SignInDialog();
        var result = await dialog.ShowDialog<bool>(MainWindow.window);

        if (result == null)
        {
            PaintPower_Engine.App.SetUserStatus("Login cancelled.");
        }
    }

    private async void OnLogout(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        await PaintPower_Engine.App.server.Logout();
    }

    private void OnMakeConnection(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        #pragma warning disable
        // Enable connection to the server
        App.server.checkConnection();
    }

    private void OnUploadProject(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        App.SaveToServer();
    }

    private void OnDownloadProjectFromServer(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        App.DownloadProjectFromServer();
    }

    private void OnHelpDocumentation(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        // Open documentation in default browser
        Process.Start(new ProcessStartInfo
        {
            FileName = "http://www.cocoink.ink/PaintPower/docs",
            UseShellExecute = true
        });
    }

    private void OnHelpReportBug(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        // Open bug report page in default browser
        Process.Start(new ProcessStartInfo
        {
            FileName = "http://www.cocoink.ink/PaintPower/bugreport",
            UseShellExecute = true
        });
    }

    private void OnHelpAbout(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        // Show about dialog
        var aboutDialog = new PopupWindowDialog("About PaintPower", $"PaintPower Engine  {PaintPower_Engine.version}", "Created by CocoBox84 and collaborators: http://github.com/CocoBox84/PaintPower-Engine/");
        aboutDialog.ShowDialog(MainWindow.window);
    }

    private void OnCloseProject(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        App.CloseProject();
    }

    public void RefreshTranslations()
    {
        SoundEffects.Click.Play();
        if (!Translator.refreshNeeded) return;

        // Window title
        if (window?.Title != null) window.Title = Translator.Translate("PaintPower");

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
        FileClose.Header = Translator.Translate("Close Project");
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

        // VmPanelPanel.refresh();
        SpriteManager.TranslateGUI();

        InvalidateVisual();

        Translator.refreshNeeded = false;
    }
}