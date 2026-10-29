using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Toolbox.Networking;

namespace PaintPower.Dialogs;

public enum ServerVersionAction
{
    Replace,
    Delete
}

public sealed class ServerVersionActionResult
{
    public ServerVersionAction Action { get; }
    public ProjectVersionInfo Version { get; }

    public ServerVersionActionResult(ServerVersionAction action, ProjectVersionInfo version)
    {
        Action = action;
        Version = version;
    }
}

public partial class ManageServerVersionsDialog : Window
{
    public ManageServerVersionsDialog(IReadOnlyList<ProjectVersionInfo> versions)
    {
        InitializeComponent();
        VersionList.ItemsSource = versions;

        VersionList.SelectionChanged += (_, _) =>
        {
            bool hasSelection = VersionList.SelectedItem is ProjectVersionInfo;
            ReplaceButton.IsEnabled = hasSelection;
            DeleteButton.IsEnabled = hasSelection;
        };
        ReplaceButton.Click += OnReplaceClicked;
        DeleteButton.Click += OnDeleteClicked;
        CancelButton.Click += (_, _) => Close(null);
    }

    private void OnReplaceClicked(object? sender, RoutedEventArgs e)
    {
        if (VersionList.SelectedItem is ProjectVersionInfo version)
            Close(new ServerVersionActionResult(ServerVersionAction.Replace, version));
    }

    private void OnDeleteClicked(object? sender, RoutedEventArgs e)
    {
        if (VersionList.SelectedItem is ProjectVersionInfo version)
            Close(new ServerVersionActionResult(ServerVersionAction.Delete, version));
    }
}
