using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using PaintPower.Networking;
using PaintPower.Logging;
using System.Threading.Tasks;

namespace PaintPower.Dialogs;

public partial class SignInDialog : Window
{
    public SignInDialog()
    {
        InitializeComponent();

        if (LoginButton != null) {
        LoginButton.Click += OnLoginClicked;
        }
        if (CancelButton != null) {
        CancelButton.Click += (_, __) => Close(false);
        }
    }

    private async void OnLoginClicked(object? sender, RoutedEventArgs e)
    {
        string username = UsernameBox.Text ?? "";
        string password = PasswordBox.Text ?? "";

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            StatusText.Text = "Please enter username and password.";
            return;
        }

        StatusText.Text = "Signing in...";

        bool ok = await PaintPower_Engine.App.server.Login(username, password);

        if (ok)
        {
            StatusText.Text = "Login successful!";
            await Task.Delay(300);
            Close(true);
        }
        else
        {
            StatusText.Text = "Invalid username or password.";
        }
    }
}