using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Toolbox.Networking;
using System.Threading.Tasks;

namespace PaintPower.Dialogs;

public partial class SignInDialog : Window
{
    private readonly Server _server;

    public SignInDialog(Server server)
    {
        _server = server;
        InitializeComponent();

        LoginButton.Click += OnLoginClicked;
        CancelButton.Click += (_, __) => Close(false);
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
        LoginButton.IsEnabled = false;

        try
        {
            bool ok = await _server.Login(username, password);
            if (ok)
            {
                StatusText.Text = "Login successful!";
                await Task.Delay(300);
                Close(true);
                return;
            }

            StatusText.Text = await _server.CheckConnection()
                ? "Invalid username or password."
                : "Could not reach the server. Please try again.";
        }
        catch (System.Exception ex)
        {
            StatusText.Text = $"Sign-in failed: {ex.Message}";
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }
}
