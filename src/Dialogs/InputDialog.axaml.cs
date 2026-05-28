using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;

using PaintPower.Accessibility.Translation;

namespace PaintPower.Dialogs;

public partial class InputDialog : Window
{
    public InputDialog(string title, string prompt)
    {
        InitializeComponent();
        Title = Translator.Map(title);
        PromptText.Text = Translator.Map(prompt);
    }

    public Task<string?> ShowAsync(Window parent)
    {
        return this.ShowDialog<string?>(parent);
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        Close(InputBox.Text);
    }
}