using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Collections.Generic;
using PaintPower.Networking;

namespace PaintPower.Dialogs;

public partial class SelectServerProjectDialog : Window
{
    public SelectServerProjectDialog(List<ProjectInfo> projects)
    {
        InitializeComponent();

        ProjectList.ItemsSource = projects;

        CancelButton.Click += (_, __) => Close(null);
        SelectButton.Click += OnSelectClicked;
    }

    private void OnSelectClicked(object? sender, RoutedEventArgs e)
    {
        if (ProjectList.SelectedItem is ProjectInfo info)
            Close(info.id);
        else
            Close(null);
    }
}
