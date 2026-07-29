using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace PaintPower.Dialogs;

public partial class UploadOptionsDialog : Window
{
    public UploadOptionsDialog(string serverId)
    {
        InitializeComponent();

        InfoText.Text = $"This project is linked to server project #{serverId}.";

        OverwriteButton.Click += (_, __) => Close("overwrite");
        UnlinkButton.Click += (_, __) => Close("unlink");
        CancelButton.Click += (_, __) => Close("cancel");
    }
}
