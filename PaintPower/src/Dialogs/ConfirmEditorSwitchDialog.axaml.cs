using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PaintPower.Dialogs;

public partial class ConfirmEditorSwitchDialog : Window
{
    public ConfirmEditorSwitchDialog()
    {
        InitializeComponent();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);

    private void OnSwitch(object? sender, RoutedEventArgs e) => Close(true);
}
