using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace PaintPower.Dialogs;

public partial class LinkBeforeUploadDialog : Window
{
    public LinkBeforeUploadDialog()
    {
        InitializeComponent();

        CreateNewButton.Click += (_, __) => Close("new");
        LinkExistingButton.Click += (_, __) => Close("existing");
        CancelButton.Click += (_, __) => Close("cancel");
    }
}
