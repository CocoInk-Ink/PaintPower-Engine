using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PaintPower.Dialogs;

public partial class ErrorDialog : Window
{
    public ErrorDialog(string message)
    {
        InitializeComponent();
        MessageText.Text = message;
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    public static async Task ShowAsync(Window parent, string message)
    {
        var dlg = new ErrorDialog(message);
        await dlg.ShowDialog(parent);
    }
}
