using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace PaintPower.Dialogs;

public partial class LinkBeforeUploadDialog : Window
{
    public LinkBeforeUploadDialog()
    {
        InitializeComponent();

        if (CreateNewButton != null) CreateNewButton.Click += (_, __) => this.Close("new");
        if (LinkExistingButton != null) LinkExistingButton.Click += (_, __) => this.Close("existing");
        if (CancelButton != null) CancelButton.Click += (_, __) => this.Close("cancel");
    }
}
