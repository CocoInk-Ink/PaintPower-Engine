using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

using PaintPower.Accessibility.Translation;

namespace PaintPower.Dialogs;

public partial class PopupWindowDialog : Window
{
    private TaskCompletionSource<string?> _tcs;

    public PopupWindowDialog(string title, string header, string prompt)
    {
        InitializeComponent();
        Title = title;
        HeaderText.Text = header;
        PromptText.Text = prompt;
        _tcs = new TaskCompletionSource<string?>();
        this.Closed += (_, __) => _tcs.TrySetResult(null);
    }

    public Task<string?> ShowAsync(Window parent)
    {
        ShowDialog(parent);
        return _tcs.Task;
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    public void TranslateGUI()
    {
        OkButton.Content = Translator.Map("Ok");
        InvalidateVisual();
    }
}