using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;

namespace PaintPower.FileEditors.Tools.AnimationEditorTools.Controls;

public partial class FrameTimelinePreview : Button
{
	public int Index
	{
		get => GetValue(IndexProperty);
		set => SetValue(IndexProperty, value);
	}

	public Bitmap? Thumbnail
	{
		get => GetValue(ThumbnailProperty);
		set => SetValue(ThumbnailProperty, value);
	}

	public static readonly StyledProperty<int> IndexProperty = AvaloniaProperty.Register<FrameTimelinePreview, int>(nameof(Index), 0);
	public static readonly StyledProperty<Bitmap?> ThumbnailProperty = AvaloniaProperty.Register<FrameTimelinePreview, Bitmap?>(nameof(Thumbnail));

	public FrameTimelinePreview()
	{
		InitializeComponent();
	}
}