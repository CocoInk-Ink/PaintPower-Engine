using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PaintPower.FileEditors.Tools.AnimationEditorTools.Controls;

public partial class FrameTimelinePreview : Button
{
	public int Index
	{
		get => GetValue(IndexProperty);
		set => SetValue(IndexProperty, value);
	}

	public static readonly StyledProperty<int> IndexProperty = AvaloniaProperty.Register<FrameTimelinePreview, int>(nameof(Index), 0);

	public FrameTimelinePreview()
	{
		InitializeComponent();
	}
}