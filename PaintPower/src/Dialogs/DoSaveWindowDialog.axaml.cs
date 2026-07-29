using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

using PaintPower.Accessibility.Translation;

namespace PaintPower.Dialogs;

public partial class DoSaveWindowDialog : Window
{
    public DoSaveWindowDialog()
    {
        InitializeComponent();
        Title = Translator.Map("Overwrite your old data?!");
        PromptText.Text = Translator.Map("Do you want to overwrite your save data?");
        PromptText2.Text = Translator.Map("(This will save your work in the currently open editor.)");
    }

    public Task<string?> ShowAsync(Window parent)
    {
        // This returns a Task<string?> that completes when Close(result) is called
        return this.ShowDialog<string?>(parent);
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        Close("save");
    }

    private void OnSaveAs(object? sender, RoutedEventArgs e)
    {
        Close("saveas");
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}