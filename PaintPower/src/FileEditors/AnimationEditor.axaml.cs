// AnimationEditor.axaml.cs

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PaintPower.FileEditors.Tools.AnimationEditorTools;
using PaintPower.FileEditors.Tools.AnimationEditorTools.Controls;
using PaintPower.FileEditors.Tools.AnimationEditorTools.Drawing;
using PaintPower.FileEditors.Tools.AnimationEditorTools.Saving;
using PaintPower.FileEditors.Tools.PaintEditorTools;
using PaintPower.ProjectSystem;
using PaintPower.Tools.Converters;
using Toolbox.Accessibility.Translation;
using Toolbox.Logging;
using Toolbox.Plumbing;

namespace PaintPower.FileEditors;

public partial class AnimationEditor : FileEditor, INotifyPropertyChanged, Toolbox.IControlWithImages
{
    public new event PropertyChangedEventHandler? PropertyChanged;
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
    public ObservableCollection<FrameTool> Frames { get; } = new();

    private TimelineTool _timeline;
    private PlaybackTool _playback;

    // Frame tools
    // Drawing
    private enum DrawMode
    {
        None,
        Circle,
        Brush
    }

    private DrawMode _drawMode = DrawMode.Brush;

    private enum DrawBrushSize
    {
        VerySmall = 1,
        Small = 4,
        Medium = 7,
        Normal = 10,
        Big = 16,
        VeryBig = 20,
        Huge = 30
    }

    private DrawBrushSize _brushSize = DrawBrushSize.Normal;

    private bool _isDrawing = false;
    private VectorStroke? _currentStroke = new();

    // Layer frames
    private readonly LayerManagerTool _emptyLayers = new();
    private int _selectedLayerIndex = -1;
    public LayerManagerTool Layers => SelectedFrame >= 0 && SelectedFrame < Frames.Count
        ? Frames[SelectedFrame].Layers
        : _emptyLayers;

    public int SelectedFrame => _timeline.SelectedFrame;

    private LayerTool? _selectedLayer;
    public LayerTool? SelectedLayer
    {
        get => _selectedLayer;
        set
        {
            _selectedLayer = value;
            _selectedLayerIndex = value == null ? -1 : Layers.Layers.IndexOf(value);
            Raise(nameof(SelectedLayer));
        }
    }

    // Color brush
    private Color _brushColor = Colors.Black;
    public Color BrushColor
    {
        get => _brushColor;
        set
        {
            _brushColor = value;
            Raise(nameof(BrushColor));
        }
    }

    private double _hue;
    public double Hue
    {
        get => _hue;
        set
        {
            _hue = value;
            UpdateBrushColor();
            Raise(nameof(Hue));
        }
    }

    private double _saturation;
    public double Saturation
    {
        get => _saturation;
        set
        {
            _saturation = value;
            UpdateBrushColor();
            Raise(nameof(Saturation));
        }
    }

    private double _value;
    public double Value
    {
        get => _value;
        set
        {
            _value = value;
            UpdateBrushColor();
            Raise(nameof(Value));
        }
    }
    private void UpdateBrushColor()
    {
        var rgb = SVPicker.ColorFromHSV(Hue, Saturation, Value);
        BrushColor = Color.FromRgb(rgb.R, rgb.G, rgb.B);
        Raise(nameof(BrushColor));
    }

    // Brush opacity
    private double _brushOpacity = 1.0;
    public double BrushOpacity
    {
        get => _brushOpacity;
        set
        {
            _brushOpacity = Math.Clamp(value, 0.0, 1.0);
            Raise(nameof(BrushOpacity));
        }
    }

    // Zoom

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

    // Panning
    private bool _isPanning = false;
    private Avalonia.Point _lastPanPoint;

    // Onion skinning
    private bool _onionSkinEnabled = true;
    private double _onionOpacity = 0.35;

    private int _onionPastCount = 1;
    private int _onionFutureCount = 1;

    public int OnionPastCount
    {
        get => _onionPastCount;
        set
        {
            _onionPastCount = Math.Max(1, value);
            Raise(nameof(OnionPastCount));
            RenderFrame(SelectedFrame);
        }
    }

    public int OnionFutureCount
    {
        get => _onionFutureCount;
        set
        {
            _onionFutureCount = Math.Max(1, value);
            Raise(nameof(OnionFutureCount));
            RenderFrame(SelectedFrame);
        }
    }

    public bool OnionSkinEnabled
    {
        get => _onionSkinEnabled;
        set
        {
            _onionSkinEnabled = value;
            Raise(nameof(OnionSkinEnabled));
            RenderFrame(SelectedFrame);
        }
    }

    // Undo redo
    public Stack<VectorStroke> UndoStack = new();
    public Stack<VectorStroke> RedoStack = new();

    // Override
    public void PipeAndLoadImages()
    {
        VerySmallBrushButton.Content = new Image { Source = ResourceKit.AsBitmap(ResourceKit.Images.UI.Paint_Animation_Editor.BrushSizes.BrushVerySmall) };
        SmallBrushButton.Content = new Image { Source = ResourceKit.AsBitmap(ResourceKit.Images.UI.Paint_Animation_Editor.BrushSizes.BrushSmall) };
        MediumBrushButton.Content = new Image { Source = ResourceKit.AsBitmap(ResourceKit.Images.UI.Paint_Animation_Editor.BrushSizes.BrushMedium) };
        NormalBrushButton.Content = new Image { Source = ResourceKit.AsBitmap(ResourceKit.Images.UI.Paint_Animation_Editor.BrushSizes.BrushNormal) };
        BigBrushButton.Content = new Image { Source = ResourceKit.AsBitmap(ResourceKit.Images.UI.Paint_Animation_Editor.BrushSizes.BrushBig) };
        VeryBigBrushButton.Content = new Image { Source = ResourceKit.AsBitmap(ResourceKit.Images.UI.Paint_Animation_Editor.BrushSizes.BrushVeryBig) };
        HugeBrushButton.Content = new Image { Source = ResourceKit.AsBitmap(ResourceKit.Images.UI.Paint_Animation_Editor.BrushSizes.BrushHuge) };
    }

    public AnimationEditor(string path, TempWorkspace workspace)
    {
        _workspace = workspace;

        SetRelativePath(path);
        SetFullPath(System.IO.Path.Combine(_workspace.ItemsDir, path));

        InitializeComponent();
        _timeline = new TimelineTool();
        _playback = new PlaybackTool();
        DataContext = this;

        Load();
        SelectedLayer = Layers.Layers[0];

        // When user selects a frame in the timeline
        _timeline.FrameSelected += index =>
        {
            Raise(nameof(SelectedFrame));
            Raise(nameof(Layers));
            var layers = Layers.Layers;
            SelectedLayer = layers.Count == 0
                ? null
                : layers[Math.Clamp(_selectedLayerIndex, 0, layers.Count - 1)];
            RenderFrame(index);
        };

        // When playback advances frames
        _playback.FrameChanged += index =>
        {
            if (Frames.Count == 0)
                return;

            int frame = index % Frames.Count;
            _timeline.SelectFrame(frame); // Will redraw
        };

        _playback.PlaybackStopped += () =>
        {
            if (Frames.Count > 0)
                _timeline.SelectFrame(_playback._currentFrame % Frames.Count); // Will redraw
        };

        this.AttachedToVisualTree += (_, _) => OnLoaded();
    }

    private void OnLoaded()
    {
        PipeAndLoadImages();

        var group = AnimationCanvas.RenderTransform as TransformGroup;

        _scale = group?.Children[0] as ScaleTransform;
        _translate = group?.Children[1] as TranslateTransform;

        PlayButton.Click += (_, _) => _playback.Play(SelectedFrame);
        StopButton.Click += (_, _) => _playback.Stop();

        FpsBox.PropertyChanged += (_, _) =>
        {
            if (FpsBox.Value.HasValue)
                _playback.SetFPS((int)FpsBox.Value.Value);
        };

        sv.PropertyChanged += (_, e) =>
        {
            if (e.Property == SVPicker.SaturationProperty ||
                e.Property == SVPicker.ValueProperty ||
                e.Property == SVPicker.HueProperty)
            {
                Hue = sv.Hue;
                Saturation = sv.Saturation;
                Value = sv.Value;
            }
        };

    }

    public override void Activate()
    {
        Log.QuickLog("Animation Editor Activated!");

        Translator.LanguageChanged += () => { };
    }

    public override void Load()
    {
        if (!File.Exists(FullPath))
            return;

        if (!ZipValidator.IsZipValid(FullPath))
        {
            Log.QuickLog("File invalid, creating default frames.");
            BuildInitialFrames();
            return;
        }

        Log.QuickLog("File valid, opening.");

        using (var zipStream = File.OpenRead(FullPath))
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
        {
            // 1. Read animation.json
            var animEntry = archive.GetEntry("animation.json");
            if (animEntry == null)
                return;

            SavedAnimation anim;
            using (var reader = new StreamReader(animEntry.Open()))
            {
                var json = reader.ReadToEnd();
                anim = JsonSerializer.Deserialize<SavedAnimation>(json)!;
            }

            Frames.Clear();

            // 2. Load frames
            foreach (var savedFrame in anim.Frames)
            {
                var frame = new FrameTool();
                frame.SetFrame(savedFrame.Index);

                foreach (var savedLayer in savedFrame.Layers)
                {
                    var layer = new LayerTool(savedLayer.Name)
                    {
                        Visible = savedLayer.Visible,
                        Locked = savedLayer.Locked
                    };

                    // Load strokes
                    foreach (var savedStroke in savedLayer.Strokes)
                    {
                        var stroke = new VectorStroke
                        {
                            Thickness = savedStroke.Thickness,
                            Brush = new SolidColorBrush(Color.Parse(savedStroke.Color), savedStroke.Opacity)
                        };

                        foreach (var p in savedStroke.Points)
                            stroke.Points.Add(new Point(p.X, p.Y));

                        layer.Strokes.Add(stroke);
                    }

                    frame.Layers.Layers.Add(layer);
                }

                // 3. Load thumbnail if present
                var thumbEntry = archive.GetEntry($"thumbnails/frame_{savedFrame.Index:D3}.png");
                if (thumbEntry != null)
                {
                    using (var stream = thumbEntry.Open())
                    {
                        frame.UpdateThumbnail(new Bitmap(stream), 120, 60); // Thumbnail size is fixed at 120x60
                    }
                }

                Frames.Add(frame);
            }

            _timeline.SetFrameCount(Frames.Count);
        }
    }

    public override void Save()
    {
        var anim = BuildSaveData();

        // Ensure directory exists
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FullPath)!);

        using (var zipStream = File.Open(FullPath, FileMode.Create))
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
        {
            // 1. Write animation.json
            var animEntry = archive.CreateEntry("animation.json");
            using (var writer = new StreamWriter(animEntry.Open()))
            {
                var json = JsonSerializer.Serialize(anim, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                writer.Write(json);
            }

            // 2. Write each frame JSON
            for (int i = 0; i < anim.Frames.Count; i++)
            {
                var frameEntry = archive.CreateEntry($"frames/frame_{i:D3}.json");
                using (var writer = new StreamWriter(frameEntry.Open()))
                {
                    var json = JsonSerializer.Serialize(anim.Frames[i], new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    writer.Write(json);
                }
            }

            // 3. Write thumbnails (optional)
            for (int i = 0; i < Frames.Count; i++)
            {
                var thumb = Frames[i].Thumbnail;
                if (thumb == null)
                    continue;

                var thumbEntry = archive.CreateEntry($"thumbnails/frame_{i:D3}.png");
                using (var stream = thumbEntry.Open())
                {
                    thumb.Save(stream);
                }
            }
        }

        MarkDirty();
    }

    private SavedAnimation BuildSaveData()
    {
        var anim = new SavedAnimation();

        foreach (var frame in Frames)
        {
            var savedFrame = new SavedFrame
            {
                Index = frame.Index
            };

            foreach (var layer in frame.Layers.Layers)
            {
                var savedLayer = new SavedLayer
                {
                    Name = layer.Name,
                    Visible = layer.Visible,
                    Locked = layer.Locked
                };

                // Strokes
                foreach (var stroke in layer.Strokes)
                {
                    var solid = stroke.Brush as SolidColorBrush ?? new SolidColorBrush(Colors.Black);

                    var savedStroke = new SavedStroke
                    {
                        Thickness = stroke.Thickness,
                        Color = solid.Color.ToString(),
                        Opacity = solid.Opacity
                    };

                    foreach (var p in stroke.Points)
                        savedStroke.Points.Add(new SavedPoint { X = p.X, Y = p.Y });

                    savedLayer.Strokes.Add(savedStroke);
                }

                // Shapes (only circles for now)
                foreach (var shape in layer.DrawActions)
                {
                    // You will expand this later
                }

                savedFrame.Layers.Add(savedLayer);
            }

            anim.Frames.Add(savedFrame);
        }

        return anim;
    }

    public override void TranslateGUI()
    {
        Translator.LanguageChanged += Refresh;
    }

    public override void Refresh()
    {
        //
    }

    public override void Undo()
    {
        if (UndoStack.Count > 0) RedoStack.Push(UndoStack.Pop());
        base.Undo();
    }

    public override void Redo()
    {
        if (RedoStack.Count > 0) UndoStack.Push(RedoStack.Pop());
        base.Redo();
    }

    public void Undo(object? a, RoutedEventArgs? b) => Undo();
    public void Redo(object? a, RoutedEventArgs? b) => Redo();

    public void OnFitCanvas(object? sender, RoutedEventArgs e)
    {
        for (int i = 0; i < 10; i++)
        {
            CanvasWidth = CanvasArea.Bounds.Width;
            CanvasHeight = CanvasArea.Bounds.Height;
        }
    }

    private void BuildInitialFrames()
    {
        Frames.Clear();
        for (int i = 0; i < 12; i++)
        {
            var frame = new FrameTool();
            frame.SetFrame(i);
            frame.Layers.AddLayer("Layer 1");
            frame.Layers.AddLayer("Layer 2");
            frame.Layers.AddLayer("Layer 3");
            Frames.Add(frame);
        }

        _timeline.SetFrameCount(Frames.Count);
        foreach (var frame in Frames)
            UpdateFrameThumbnail(frame);
    }

    private void RenderFrame(int index)
    {
        AnimationCanvas.Children.Clear();
        if (index < 0 || index >= Frames.Count)
            return;

        if (_onionSkinEnabled)
        {
            for (int i = 1; i <= _onionPastCount; i++)
            {
                int pastIndex = index - i;
                if (pastIndex >= 0)
                {
                    RenderLayers(Frames[pastIndex], _onionOpacity / i);
                }
            }

            for (int i = 1; i <= _onionFutureCount; i++)
            {
                int futureIndex = index + i;
                if (futureIndex < Frames.Count)
                {
                    RenderLayers(Frames[futureIndex], _onionOpacity / i);
                }
            }
        }

        RenderLayers(Frames[index], 1.0);

        UpdateFrameThumbnail(Frames[index]);
    }

    private void RenderLayers(FrameTool frame, double opacity)
    {
        foreach (var layer in frame.Layers.Layers)
        {
            if (layer.Visible)
                layer.Render(AnimationCanvas, opacity);
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

    private Bitmap RenderThumbnail(FrameTool frame)
    {
        const int thumbWidth = 120;
        const int thumbHeight = 60;

        // Create a temporary canvas
        var canvas = new Canvas
        {
            Width = CanvasWidth,
            Height = CanvasHeight,
            Background = Brushes.Transparent
        };

        // Render layers into the canvas
        foreach (var layer in frame.Layers.Layers)
        {
            if (layer.Visible)
                layer.Render(canvas, 1.0);
        }

        var canvasSize = new Size(CanvasWidth, CanvasHeight);
        canvas.Measure(canvasSize);
        canvas.Arrange(new Rect(canvasSize));

        // Render canvas into bitmap
        var bmp = new RenderTargetBitmap(new PixelSize((int)CanvasWidth, (int)CanvasHeight));
        bmp.Render(canvas);

        // Scale down to thumbnail
        var thumb = new RenderTargetBitmap(new PixelSize(thumbWidth, thumbHeight));
        using (var ctx = thumb.CreateDrawingContext(true))
        {
            double scale = Math.Min(thumbWidth / CanvasWidth, thumbHeight / CanvasHeight);
            double width = CanvasWidth * scale;
            double height = CanvasHeight * scale;
            var destination = new Rect(
                (thumbWidth - width) / 2,
                (thumbHeight - height) / 2,
                width,
                height);
            ctx.DrawImage(bmp, destination);
        }

        return thumb;
    }

    private void UpdateFrameThumbnail(FrameTool frame)
    {
        if (!frame.HasCurrentThumbnail(CanvasWidth, CanvasHeight))
            frame.UpdateThumbnail(RenderThumbnail(frame), CanvasWidth, CanvasHeight);
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
        if (sender is FrameTimelinePreview btn && btn.Index is int index)
        {
            var wasPlaying = _playback.isPlaying;
            if (wasPlaying) _playback.Stop();
            _timeline.SelectFrame(index);
            if (wasPlaying) _playback.Play();
        }
    }

    public void OnAddLayer(object? sender, RoutedEventArgs e)
    {
        int layerNumber = Frames.Count == 0 ? 1 : Frames[0].Layers.Layers.Count + 1;
        foreach (var frame in Frames)
        {
            frame.Layers.AddLayer($"Layer {layerNumber}");
            frame.InvalidateThumbnail();
        }
        RenderFrame(SelectedFrame);
    }

    public void OnRemoveLayer(object? sender, RoutedEventArgs e)
    {
        if (SelectedLayer == null)
            return;

        int layerIndex = Layers.Layers.IndexOf(SelectedLayer);
        if (layerIndex < 0)
            return;

        foreach (var frame in Frames)
        {
            if (layerIndex < frame.Layers.Layers.Count)
                frame.Layers.RemoveLayer(frame.Layers.Layers[layerIndex]);
            frame.InvalidateThumbnail();
        }

        var remainingLayers = Layers.Layers;
        SelectedLayer = remainingLayers.Count == 0
            ? null
            : remainingLayers[Math.Min(layerIndex, remainingLayers.Count - 1)];
        RenderFrame(SelectedFrame);
    }

    private bool _drawerOpen = false;

    public void OnToggleDrawer(object? sender, RoutedEventArgs e)
    {
        _drawerOpen = !_drawerOpen;

        FrameDrawer.Height = _drawerOpen ? (Frames.Count < 1) ? 72 : 160 : 32;
    }

    public void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {

        if (_isDrawing && _drawMode == DrawMode.Brush)
        {
            var p = GetLogicalCanvasPoint(e);
            _currentStroke?.Points.Add(p);

            if (SelectedFrame >= 0 && SelectedFrame < Frames.Count)
                Frames[SelectedFrame].InvalidateThumbnail();
            RenderFrame(SelectedFrame);
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

            if (_currentStroke != null)
            {
                RedoStack = new();
                UndoStack.Push(_currentStroke);

                _currentStroke = null;
            }
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

        if (SelectedLayer == null)
        {
            Log.QuickLog("No layer selected.");
            return;
        }

        // Draw attempt.

        // Stop playback on draw.
        if (_playback.isPlaying) _playback.Stop();

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
        if (frameIndex < 0 || frameIndex >= Frames.Count)
            return;

        var layer = SelectedLayer;

        switch (_drawMode)
        {
            case DrawMode.Circle:
                // Draw
                layer.DrawActions.Add(c => DrawCircle(c, logicalX, logicalY));
                break;

            case DrawMode.Brush:
                {
                    _isDrawing = true;

                    _currentStroke = new VectorStroke
                    {
                        Thickness = (int)_brushSize,
                        Brush = new SolidColorBrush(BrushColor, BrushOpacity)
                    };

                    var p = GetLogicalCanvasPoint(e);
                    _currentStroke.Points.Add(p);

                    layer.Strokes.Add(_currentStroke);
                }
                break;
        }

        Frames[frameIndex].InvalidateThumbnail();
        RenderFrame(frameIndex);
    }

    private void VerySmallBrushButton_Click(object? sender, RoutedEventArgs e)
    {
        _brushSize = DrawBrushSize.VerySmall;
    }

    private void SmallBrushButton_Click(object? sender, RoutedEventArgs e)
    {
        _brushSize = DrawBrushSize.Small;
    }

    private void MediumBrushButton_Click(object? sender, RoutedEventArgs e)
    {
        _brushSize = DrawBrushSize.Medium;
    }

    private void NormalBrushButton_Click(object? sender, RoutedEventArgs e)
    {
        _brushSize = DrawBrushSize.Normal;
    }

    private void BigBrushButton_Click(object? sender, RoutedEventArgs e)
    {
        _brushSize = DrawBrushSize.Big;
    }

    private void VeryBigBrushButton_Click(object? sender, RoutedEventArgs e)
    {
        _brushSize = DrawBrushSize.VeryBig;
    }

    private void HugeBrushButton_Click(object? sender, RoutedEventArgs e)
    {
        _brushSize = DrawBrushSize.Huge;
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        Save();
    }
}
