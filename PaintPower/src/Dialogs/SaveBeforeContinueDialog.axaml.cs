using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;

using PaintPower.Accessibility.Translation;

namespace PaintPower.Dialogs;

public partial class SaveBeforeContinueDialog : Window
{
    public SaveBeforeContinueDialog()
    {
        InitializeComponent();
        Title = Translator.Map("Save Before Continuing?");
        Translator.LanguageChanged += TranslateGUI;
    }

    public Task<string?> ShowAsync(Window parent)
    {
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

    private void OnDontSave(object? sender, RoutedEventArgs e)
    {
        Close("dontsave");
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    public void TranslateGUI()
    {
        SaveButton.Content = Translator.Map("Save");
        SaveAsButton.Content = Translator.Map("Save As");
        DontSaveButton.Content = Translator.Map("Don't Save");
        CancelButton.Content = Translator.Map("Cancel");

        SaveBeforeContinueText.Text = Translator.Map("Save before continuing?");
        ChangesLostText.Text = Translator.Map("Your changes will be lost if you don't save.");

        InvalidateVisual();
    }
}