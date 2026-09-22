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
using Toolbox.Accessibility.Translation;
using Toolbox.Logging;
using Toolbox.Plumbing;

namespace PaintPower.FileEditors;

public partial class AnimationEditor : FileEditor, INotifyPropertyChanged, Toolbox.IControlWithImages
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
        Circle,
        Brush
    }

    private DrawMode _drawMode = DrawMode.Brush;

    private enum DrawBrushSize
    {
        VerySmall = 2,
        Small = 3,
        Medium = 4,
        Normal = 5,
        Big = 6,
        VeryBig = 7,
        Huge = 8
    }

    private DrawBrushSize _brushSize = DrawBrushSize.Normal;

    private bool _isDrawing = false;
    private List<Point> _currentStroke = new();

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
    private Avalonia.Point _lastPanPoint;

    // Override
    public void PipeAndLoadImages()
    {
        VerySmallBrushButton.Source = ResourceKit.AsBitmap(ResourceKit.Images.UI.Paint_Animation_Editor.BrushSizes.BrushVerySmall);
        SmallBrushButton.Source = ResourceKit.AsBitmap(ResourceKit.Images.UI.Paint_Animation_Editor.BrushSizes.BrushSmall);
        MediumBrushButton.Source = ResourceKit.AsBitmap(ResourceKit.Images.UI.Paint_Animation_Editor.BrushSizes.BrushMedium);
        NormalBrushButton.Source = ResourceKit.AsBitmap(ResourceKit.Images.UI.Paint_Animation_Editor.BrushSizes.BrushNormal);
        BigBrushButton.Source = ResourceKit.AsBitmap(ResourceKit.Images.UI.Paint_Animation_Editor.BrushSizes.BrushBig);
        VeryBigBrushButton.Source = ResourceKit.AsBitmap(ResourceKit.Images.UI.Paint_Animation_Editor.BrushSizes.BrushVeryBig);
        HugeBrushButton.Source = ResourceKit.AsBitmap(ResourceKit.Images.UI.Paint_Animation_Editor.BrushSizes.BrushHuge);
    }

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
        PipeAndLoadImages();

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
        Translator.LanguageChanged += Refresh;
    }

    public override void Refresh()
    {
        //
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
        Log.QuickLog(Translator.Translate("[AnimationEditor] Activated}"));
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

    private void DrawBrushDot(Canvas canvas, double x, double y)
    {
        Shape dot;

        if (_brushSize > DrawBrushSize.Medium && _brushSize < (DrawBrushSize.Huge + 1))
        {

            dot = new Ellipse
            {
                Width = (int)_brushSize,
                Height = (int)_brushSize,
                Fill = Brushes.Black
            };
        }
        else if (_brushSize > (DrawBrushSize.VerySmall - 1) && _brushSize < DrawBrushSize.Normal)
        {
            dot = new Rectangle
            {
                Width = (int)_brushSize,
                Height = (int)_brushSize,
                Fill = Brushes.Black
            };
        }
        else
        {
            dot = new Ellipse
            {
                Width = (int)DrawBrushSize.Normal,
                Height = (int)DrawBrushSize.Normal,
                Fill = Brushes.Black
            };
        }

        Canvas.SetLeft(dot, x - 3);
        Canvas.SetTop(dot, y - 3);

        canvas.Children.Add(dot);
    }

    private Point GetLogicalCanvasPoint(PointerEventArgs e)
    {
        var rawPoint = e.GetPosition(CanvasArea);

        double scaleX = _scale?.ScaleX ?? 1;
        double scaleY = _scale?.ScaleY ?? 1;
        double transX = _translate?.X ?? 0;
        double transY = _translate?.Y ?? 0;

        double canvasLeft = CanvasArea.Bounds.Width / 2 - (CanvasWidth * scaleX) / 2 + transX;
        double canvasTop = CanvasArea.Bounds.Height / 2 - (CanvasHeight * scaleY) / 2 + transY;

        double px = rawPoint.X - canvasLeft;
        double py = rawPoint.Y - canvasTop;

        double logicalX = px / scaleX;
        double logicalY = py / scaleY;

        logicalX = Math.Clamp(logicalX, 0, CanvasWidth);
        logicalY = Math.Clamp(logicalY, 0, CanvasHeight);

        return new Point(logicalX, logicalY);
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

    private bool _drawerOpen = false;

    public void OnToggleDrawer(object? sender, RoutedEventArgs e)
    {
        _drawerOpen = !_drawerOpen;

        FrameDrawer.Height = _drawerOpen ? (SelectedLayer.Frames.Count < 1) ? 72 : 160 : 32;
    }

    public void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {

        if (_isDrawing && _drawMode == DrawMode.Brush)
        {
            var p = GetLogicalCanvasPoint(e);
            _currentStroke.Add(p);

            int frameIndex = SelectedFrame;
            var frame = SelectedLayer.Frames[frameIndex];

            frame.DrawActions.Add(c => DrawBrushDot(c, p.X, p.Y));

            RenderFrame(frameIndex);
            return;
        }

        // Panning must be last!:
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

        if (_isDrawing && _drawMode == DrawMode.Brush)
        {
            _isDrawing = false;
            _currentStroke.Clear();
        }
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
        var rawPoint = e.GetPosition(CanvasArea); // IMPORTANT: Border, not Canvas

        // Canvas position inside the Border
        double canvasLeft = CanvasArea.Bounds.Width / 2 - (CanvasWidth * (_scale?.ScaleX ?? 1)) / 2 + (_translate?.X ?? 0);
        double canvasTop = CanvasArea.Bounds.Height / 2 - (CanvasHeight * (_scale?.ScaleY ?? 1)) / 2 + (_translate?.Y ?? 0);

        // Convert to canvas space BEFORE reversing zoom
        double px = rawPoint.X - canvasLeft;
        double py = rawPoint.Y - canvasTop;

        // Reverse zoom
        double scaleX = _scale?.ScaleX ?? 1;
        double scaleY = _scale?.ScaleY ?? 1;

        double logicalX = px / scaleX;
        double logicalY = py / scaleY;

        // Clamp to logical canvas size
        logicalX = Math.Clamp(logicalX, 0, CanvasWidth);
        logicalY = Math.Clamp(logicalY, 0, CanvasHeight);


        int frameIndex = SelectedFrame;
        if (frameIndex < 0 || frameIndex >= SelectedLayer.Frames.Count)
            return;

        var frame = SelectedLayer.Frames[frameIndex];

        switch (_drawMode)
        {
            case DrawMode.Circle:
                // Draw
                frame.DrawActions.Add(c => DrawCircle(c, logicalX, logicalY));
                break;

            case DrawMode.Brush:
                {
                    _isDrawing = true;
                    _currentStroke.Clear();

                    var p = GetLogicalCanvasPoint(e);
                    _currentStroke.Add(p);
                }
                break;
        }

        RenderFrame(frameIndex);
    }

}
