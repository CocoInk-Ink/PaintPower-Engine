// AnimationEditor.axaml.cs

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using PaintPower.FileEditors.Tools.AnimationEditorTools;
using PaintPower.ProjectSystem;
using PaintPower.Tools.Converters;
using Toolbox.Logging;

namespace PaintPower.FileEditors;

public partial class AnimationEditor : FileEditor, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private ScaleTransform? _scale;
    private TranslateTransform? _translate;

    private double _canvasWidth = 300;
    public double CanvasWidth
    {
        get => _canvasWidth;
        set
        {
            _canvasWidth = Math.Clamp(value, 50, 1920);
            Raise(nameof(CanvasWidth));
            AnimationCanvas.Width = _canvasWidth;
        }
    }

    private double _canvasHeight = 300;
    public double CanvasHeight
    {
        get => _canvasHeight;
        set
        {
            _canvasHeight = Math.Clamp(value, 50, 1080);
            Raise(nameof(CanvasHeight));
            AnimationCanvas.Height = _canvasHeight;
        }
    }

    private readonly TempWorkspace _workspace;

    // Simple placeholder model: later replace with real WXA data
    public ObservableCollection<string> Frames { get; } = new();

    private TimelineTool _timeline;
    private PlaybackTool _playback;
    private List<FrameTool> _frameTools = new();

    private enum DrawMode
    {
        None,
        Circle
    }

    private DrawMode _drawMode = DrawMode.Circle; // default for now


    private LayerManagerTool _layers;
    public LayerManagerTool Layers => _layers;

    public int SelectedFrame => _timeline.SelectedFrame;

    private LayerTool? _selectedLayer;
    public LayerTool? SelectedLayer
    {
        get => _selectedLayer;
        set
        {
            _selectedLayer = value;
            Raise(nameof(SelectedLayer));
        }
    }

    private double _zoomLevel = 1.0;
    public double ZoomLevel
    {
        get => _zoomLevel;
        set
        {
            _zoomLevel = Math.Clamp(value, 0.1, 4.0);
            Raise(nameof(ZoomLevel));

            if (_scale != null)
            {
                _scale.ScaleX = _zoomLevel;
                _scale.ScaleY = _zoomLevel;
            }
        }
    }

    private bool _isPanning = false;
    private Point _lastPanPoint;

    public AnimationEditor(string path, TempWorkspace workspace)
    {
        _workspace = workspace;

        SetRelativePath(path);
        SetFullPath(System.IO.Path.Combine(_workspace.ItemsDir, path));

        _layers = new LayerManagerTool();
        _layers.AddLayer("Layer 1");
        _layers.AddLayer("Layer 2");
        _layers.AddLayer("Layer 3");

        SelectedLayer = _layers.Layers[0];

        InitializeComponent();
        DataContext = this;

        Load();
        BuildInitialFrames();

        _timeline = new TimelineTool();
        _playback = new PlaybackTool();

        // When user selects a frame in the timeline
        _timeline.FrameSelected += index =>
        {
            RenderFrame(index);
        };

        // When playback advances frames
        _playback.FrameChanged += index =>
        {
            if (SelectedLayer == null || SelectedLayer.Frames.Count == 0)
                return;

            int frame = index % SelectedLayer.Frames.Count;
            RenderFrame(frame);

        };

        this.AttachedToVisualTree += (_, _) => OnLoaded();
    }

    private void OnLoaded()
    {
        var group = AnimationCanvas.RenderTransform as TransformGroup;

        _scale = group.Children[0] as ScaleTransform;
        _translate = group.Children[1] as TranslateTransform;

        PlayButton.Click += (_, _) => _playback.Play();
        StopButton.Click += (_, _) => _playback.Stop();

        FpsBox.PropertyChanged += (_, _) =>
        {
            if (FpsBox.Value.HasValue)
                _playback.SetFPS((int)FpsBox.Value.Value);
        };

        BuildInitialFrameTools();
    }

    private void BuildInitialFrameTools()
    {
        foreach (var layer in _layers.Layers)
        {
            layer.Frames.Clear();

            // Create 12 frames for each layer
            for (int i = 0; i < 12; i++)
                layer.Frames.Add(new LayerFrameTool());
        }

        _timeline.SetFrameCount(_layers.Layers[0].Frames.Count);
    }

    public override void Load()
    {
        // TODO: load WXA zip, parse animation.json, shapes.json, symbols.json, etc.
        // For now, just log and keep placeholder behavior.
        Console.WriteLine($"[AnimationEditor] Load: {FullPath}");

        if (!File.Exists(FullPath))
            return;

        // Later: open ZIP and read JSON files.
    }

    public override void Save()
    {
        Console.WriteLine($"[AnimationEditor] Save: {FullPath}");

        // TODO: write WXA zip with animation.json, shapes.json, symbols.json, assets/
        // For now, just mark as dirty and do nothing.
        MarkDirty();
    }

    private void BuildInitialFrames()
    {
        // Placeholder timeline: later bind to real keyframes from WXA
        Frames.Clear();
        for (int i = 0; i < 12; i++)
            Frames.Add($"F{i}");
    }

    public override void TranslateGUI()
    {
        // TODO: hook into your localization system if needed
    }

    public void OnFitCanvas(object? sender, RoutedEventArgs e)
    {
        for (int i = 0; i < 10; i++)
        {
            CanvasWidth = CanvasArea.Bounds.Width;
            CanvasHeight = CanvasArea.Bounds.Height;
        }
    }

    public override void Activate()
    {
        // Called when this editor becomes active
        Console.WriteLine("[AnimationEditor] Activated");
    }

    private void RenderFrame(int index)
    {
        AnimationCanvas.Children.Clear();

        foreach (var layer in _layers.Layers)
        {
            if (!layer.Visible)
                continue;

            if (index < 0 || index >= layer.Frames.Count)
                continue;

            layer.Frames[index].Render(AnimationCanvas);
        }
    }

    private void DrawCircle(Canvas canvas, double x, double y)
    {
        var ellipse = new Ellipse
        {
            Width = 40,
            Height = 40,
            Fill = Brushes.Red
        };

        Canvas.SetLeft(ellipse, x);
        Canvas.SetTop(ellipse, y);

        canvas.Children.Add(ellipse);
    }

    public void OnFrameClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int index)
        {
            _timeline.SelectFrame(index);
        }
    }

    public void OnAddLayer(object? sender, RoutedEventArgs e)
    {
        _layers.AddLayer($"Layer {_layers.Layers.Count + 1}");
    }

    public void OnRemoveLayer(object? sender, RoutedEventArgs e)
    {
        if (SelectedLayer != null)
            _layers.RemoveLayer(SelectedLayer);
    }

    public void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPanning || _translate == null)
            return;

        var point = e.GetPosition(AnimationCanvas);
        var delta = point - _lastPanPoint;

        _translate.X += delta.X;
        _translate.Y += delta.Y;

        _lastPanPoint = point;
    }

    public void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isPanning = false;
    }

    public void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var props = e.GetCurrentPoint(AnimationCanvas).Properties;

        // Right mouse button = pan
        if (props.IsRightButtonPressed)
        {
            _isPanning = true;
            _lastPanPoint = e.GetPosition(AnimationCanvas);
            return;
        }

        Log.QuickLog("Mouse down on canvas.");

        if (SelectedLayer == null)
        {
            Log.QuickLog("No layer selected.");
            return;
        }

        // Convert pointer position to unscaled/untranslated canvas space
        var rawPoint = e.GetPosition(AnimationCanvas);
        var point = new Point(
            (rawPoint.X - (_translate?.X ?? 0)) / (_scale?.ScaleX ?? 1),
            (rawPoint.Y - (_translate?.Y ?? 0)) / (_scale?.ScaleY ?? 1)
        );


        point = new Point(
            Math.Clamp(point.X, 0, AnimationCanvas.Bounds.Width),
            Math.Clamp(point.Y, 0, AnimationCanvas.Bounds.Height)
        );

        int frameIndex = SelectedFrame;
        if (frameIndex < 0 || frameIndex >= SelectedLayer.Frames.Count)
            return;

        var frame = SelectedLayer.Frames[frameIndex];

        switch (_drawMode)
        {
            case DrawMode.Circle:
                frame.DrawActions.Add(c => DrawCircle(c, point.X, point.Y));
                break;
        }

        RenderFrame(frameIndex);
    }

}
