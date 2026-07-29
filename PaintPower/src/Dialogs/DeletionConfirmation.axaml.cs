using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

using PaintPower.Accessibility.Translation;

namespace PaintPower.Dialogs;

public partial class DeletionConfirmationDialog : Window
{
    public DeletionConfirmationDialog()
    {
        InitializeComponent();
        Title = Translator.Map("Delete this item?!");
        PromptText.Text = Translator.Map("Are you sure you want to this item?");
        PromptText2.Text = Translator.Map("(You may not be able to recover this after deletion!)");
    }

    public Task<string?> ShowAsync(Window parent)
    {
        // This returns a Task<string?> that completes when Close(result) is called
        return this.ShowDialog<string?>(parent);
    }

    private void OnDelete(object? sender, RoutedEventArgs e)
    {
        Close("delete");
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}