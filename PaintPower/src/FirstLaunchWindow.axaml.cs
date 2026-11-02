using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PaintPower;

public partial class FirstLaunchWindow : Window
{
    public event Action? ProceedRequested;

    public FirstLaunchWindow()
    {
        InitializeComponent();
    }

    public void Proceed()
    {
        ProceedRequested?.Invoke();
    }

    private void Proceed_Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
	{
		Proceed();
	}

	protected override void OnOpened(EventArgs e)
	{
		base.OnOpened(e);
	}
}