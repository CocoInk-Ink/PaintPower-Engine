using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PaintPower.FileEditors.Tools.AnimationEditorTools.Controls;

public partial class FrameTimelinePreview : UserControl
{
	public event EventHandler<RoutedEventArgs>? Click;

	public int index = 0;
	public FrameTimelinePreview()
	{
		InitializeComponent();
	}

	//public void OnFrameClicked(object? sender, RoutedEventArgs e) {}
}