using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PaintPower.ProjectSystem;
using System;
using System.Collections.Generic;
using System.IO;

using PaintPower.Accessibility.Translation;
using PaintPower.EditorTools.PaintEditorTools;
using System.Runtime.CompilerServices;

namespace PaintPower.Editors;

public partial class PaintEditor : EditorBase
{
    private readonly TempWorkspace _workspace;

    private WriteableBitmap _bitmap;

    private bool _isDrawing;
    private Avalonia.Point _lastPoint;
    private Color _currentColor = Colors.Black;

    private bool _isPanning;
    private Avalonia.Point _panStart;
    private Vector _scrollStart;

    private readonly Stack<WriteableBitmap> _undoStack = new();
    private readonly Stack<WriteableBitmap> _redoStack = new();

    private enum ToolMode { Brush, Eraser, Bucket, Hand }
    private ToolMode _tool = ToolMode.Brush;

    public PaintEditor(string relativePath, TempWorkspace workspace)
    {
        _workspace = workspace;

        InitializeComponent();

        HueSlider.PropertyChanged += (_, __) =>
        {
            SVPicker.Hue = HueSlider.Value;
            UpdateColor();
        };

        SVPicker.PropertyChanged += (_, __) =>
        {
            UpdateColor();
        };

        OpacitySlider.PropertyChanged += (_, __) =>
        {
            UpdateColor();
        };

        CanvasImage.PointerPressed += OnPointerPressed;
        CanvasImage.PointerReleased += OnPointerReleased;
        CanvasImage.PointerMoved += OnPointerMoved;

        SaveButton.Click += (_, __) => Save();
        UndoButton.Click += (_, __) => Undo();
        RedoButton.Click += (_, __) => Redo();
        ClearButton.Click += (_, __) => Clear();

        ZoomSlider.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty)
            {
                double scale = ZoomSlider.Value;
                ZoomContainer.LayoutTransform = new ScaleTransform(scale, scale);
                CheckerZoom.LayoutTransform = new ScaleTransform(scale, scale);
                CheckerZoom.InvalidateVisual();

                BrushCursor.RenderTransform = new ScaleTransform(scale, scale);
                BrushCursor.InvalidateVisual();

                pixelGrid.Zoom = scale;
                pixelGrid.InvalidateVisual();
            }
        };

        var scroll = this.FindControl<ScrollViewer>("CanvasScroll");
        scroll.PointerWheelChanged += OnPointerWheelChanged;
        scroll.PointerPressed += OnScrollPointerPressed;
        scroll.PointerReleased += OnScrollPointerReleased;
        scroll.PointerMoved += OnScrollPointerMoved;

        void ResetCursor()
        {
            CanvasImage.Cursor = new Cursor(StandardCursorType.Arrow);
            BrushCursor.IsVisible = false;
        }

        BrushButton.Click += (_, __) =>
        {
            _tool = ToolMode.Brush;
            _currentColor = Colors.Black;
            _isPanning = false;

            CanvasImage.Cursor = new Cursor(StandardCursorType.None);
            BrushCursor.IsVisible = true;
        };
        EraserButton.Click += (_, __) => { _tool = ToolMode.Eraser; _currentColor = Colors.White; _isPanning = false; ResetCursor(); };
        HandToolButton.Click += (_, __) => { _tool = ToolMode.Hand; _isPanning = true; ResetCursor(); };
        BucketButton.Click += (_, __) => { _tool = ToolMode.Bucket; _isPanning = false; ResetCursor(); };
    }

    public override void TranslateGUI()
    {
        base.TranslateGUI();

        BrushButton.Content = Translator.Map("Brush");
        EraserButton.Content = Translator.Map("Eraser");
        HandToolButton.Content = Translator.Map("Hand Tool");
        BucketButton.Content = Translator.Map("Bucket Fill");
        SaveButton.Content = Translator.Map("Save");
        UndoButton.Content = Translator.Map("Undo");
        RedoButton.Content = Translator.Map("Redo");
        ClearButton.Content = Translator.Map("Clear");
        ZoomText.Text = Translator.Map("Zoom");
    }


    private void LoadOrCreateImage()
    {
        var fullPath = _workspace.MapToTemp(RelativePath);

        try
        {
            if (File.Exists(fullPath))
            {
                using var fs = File.OpenRead(fullPath);
                var src = new Bitmap(fs);

                _bitmap = new WriteableBitmap(src.PixelSize, src.Dpi, PixelFormat.Bgra8888, AlphaFormat.Premul);

                using var fb = _bitmap.Lock();
                src.CopyPixels(new PixelRect(0, 0, src.PixelSize.Width, src.PixelSize.Height),
                               fb.Address, fb.RowBytes * fb.Size.Height, fb.RowBytes);
            }
            else
            {
                _bitmap = new WriteableBitmap(new PixelSize(800, 600), new Vector(96, 96),
                                              PixelFormat.Bgra8888, AlphaFormat.Premul);
            }
        }
        catch
        {
            _bitmap = new WriteableBitmap(new PixelSize(800, 600), new Vector(96, 96),
                                          PixelFormat.Bgra8888, AlphaFormat.Premul);
        }

        pixelGrid.PixelWidth = _bitmap.PixelSize.Width;
        pixelGrid.PixelHeight = _bitmap.PixelSize.Height;

        CanvasImage.Source = _bitmap;
    }

    override public void Save()
    {
        var fullPath = _workspace.MapToTemp(RelativePath);

        using var fs = File.Open(fullPath, FileMode.Create);
        _bitmap.Save(fs);

        if (!PaintPower_Engine.App.saveNeeded)
        {
            PaintPower_Engine.App.SetProjectStatus("Save Project");
            PaintPower_Engine.App.saveNeeded = true;
        }
    }

    public override void SetRelativePath(string path)
    {
        base.SetRelativePath(path);
        LoadOrCreateImage();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_tool == ToolMode.Hand)
            return;

        if (!PaintPower_Engine.App.saveNeeded)
        {
            PaintPower_Engine.App.SetProjectStatus("Save Project");
            PaintPower_Engine.App.saveNeeded = true;
        }

        _undoStack.Push(CloneBitmap(_bitmap));
        _redoStack.Clear();

        var point = ToBitmapSpace(e);

        if (_tool == ToolMode.Bucket)
        {
            FloodFill((int)point.X, (int)point.Y, _currentColor);
            CanvasImage.InvalidateVisual();
            return;
        }

        _isDrawing = true;
        _lastPoint = point;
        DrawPoint(point);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDrawing = false;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var pos = e.GetPosition(CanvasImage);
        if (BrushCursor.IsVisible)
        {
            // Offset so the tip of the pencil matches the brush point
            double radius = BrushSizeSlider.Value;
            double offsetX = -radius;
            double offsetY = -radius;

            BrushCursor.Margin = new Thickness(
                pos.X + offsetX,
                pos.Y + offsetY,
                0, 0);
        }

        if (!_isDrawing || _tool == ToolMode.Hand)
            return;

        if (!PaintPower_Engine.App.saveNeeded)
        {
            PaintPower_Engine.App.SetProjectStatus("Save Project");
            PaintPower_Engine.App.saveNeeded = true;
        }

        var point = ToBitmapSpace(e);
        DrawLine(_lastPoint, point);
        _lastPoint = point;
    }

    private void DrawPoint(Avalonia.Point p)
    {
        using var fb = _bitmap.Lock();
        int size = (int)BrushSizeSlider.Value;
        uint color = _currentColor.ToUInt32();

        unsafe
        {
            uint* ptr = (uint*)fb.Address;
            int width = _bitmap.PixelSize.Width;
            int height = _bitmap.PixelSize.Height;

            for (int y = -size; y < size; y++)
            {
                for (int x = -size; x < size; x++)
                {
                    int px = (int)p.X + x;
                    int py = (int)p.Y + y;

                    if (px < 0 || py < 0 || px >= width || py >= height)
                            continue;

                    // May change to a global variable later to allow switching between square and circular brushes
                    bool isSquareBrush = false; // Change to true for a square brush

                    if (isSquareBrush)
                    {
                        ptr[py * width + px] = color;
                    }
                    else
                    {
                        if (x * x + y * y <= size * size)
                            ptr[py * width + px] = color;
                    }
                }
            }
        }

        CanvasImage.InvalidateVisual();
    }

    private void DrawLine(Avalonia.Point a, Avalonia.Point b)
    {
        int steps = (int)(Math.Max(Math.Abs(b.X - a.X), Math.Abs(b.Y - a.Y)) * 1.5);

        for (int i = 0; i < steps; i++)
        {
            double t = i / (double)steps;
            DrawPoint(new Avalonia.Point(a.X + (b.X - a.X) * t,
                                         a.Y + (b.Y - a.Y) * t));
        }
    }

    private Avalonia.Point ToBitmapSpace(PointerEventArgs e)
    {
        var scroll = this.FindControl<ScrollViewer>("CanvasScroll");
        var pos = e.GetPosition(scroll);

        var transform = scroll.TransformToVisual(ZoomContainer);
        var zoomSpace = transform?.Transform(pos) ?? pos;

        double scale = ZoomSlider.Value;
        return new Avalonia.Point(zoomSpace.X / scale, zoomSpace.Y / scale);
    }

    private unsafe void FloodFill(int x, int y, Color newColor)
    {
        using var fb = _bitmap.Lock();

        int width = _bitmap.PixelSize.Width;
        int height = _bitmap.PixelSize.Height;

        uint* ptr = (uint*)fb.Address;
        uint target = ptr[y * width + x];
        uint replacement = newColor.ToUInt32();

        if (target == replacement)
            return;

        Stack<(int X, int Y)> stack = new();
        stack.Push((x, y));

        while (stack.Count > 0)
        {
            var (px, py) = stack.Pop();

            int left = px;
            int right = px;

            while (left >= 0 && ptr[py * width + left] == target) left--;
            while (right < width && ptr[py * width + right] == target) right++;

            for (int i = left + 1; i < right; i++)
            {
                ptr[py * width + i] = replacement;

                if (py > 0 && ptr[(py - 1) * width + i] == target)
                    stack.Push((i, py - 1));

                if (py < height - 1 && ptr[(py + 1) * width + i] == target)
                    stack.Push((i, py + 1));
            }
        }

        CanvasImage.InvalidateVisual();
    }

    private void OnSwatchClicked(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border b && b.Background is SolidColorBrush brush)
            _currentColor = brush.Color;
    }

    private WriteableBitmap CloneBitmap(WriteableBitmap source)
    {
        var clone = new WriteableBitmap(source.PixelSize, source.Dpi,
                                        source.Format, source.AlphaFormat);

        unsafe
        {
            using var src = source.Lock();
            using var dst = clone.Lock();

            Buffer.MemoryCopy(src.Address.ToPointer(),
                              dst.Address.ToPointer(),
                              dst.RowBytes * dst.Size.Height,
                              src.RowBytes * src.Size.Height);
        }

        return clone;
    }

    public override void Undo()
    {
        if (_undoStack.Count == 0)
            return;

        if (!PaintPower_Engine.App.saveNeeded)
        {
            PaintPower_Engine.App.SetProjectStatus("Save Project");
            PaintPower_Engine.App.saveNeeded = true;
        }

        _redoStack.Push(CloneBitmap(_bitmap));
        _bitmap = _undoStack.Pop();

        CanvasImage.Source = _bitmap;
        CanvasImage.InvalidateVisual();
    }

    public override void Redo()
    {
        if (_redoStack.Count == 0)
            return;

        if (!PaintPower_Engine.App.saveNeeded)
        {
            PaintPower_Engine.App.SetProjectStatus("Save Project");
            PaintPower_Engine.App.saveNeeded = true;
        }

        _undoStack.Push(CloneBitmap(_bitmap));
        _bitmap = _redoStack.Pop();

        CanvasImage.Source = _bitmap;
        CanvasImage.InvalidateVisual();
    }

    private void Clear()
    {
        _undoStack.Push(CloneBitmap(_bitmap));
        _redoStack.Clear();

        using var fb = _bitmap.Lock();

        unsafe
        {
            Unsafe.InitBlock(fb.Address.ToPointer(), 0, (uint)(fb.RowBytes * fb.Size.Height));
        }

        if (!PaintPower_Engine.App.saveNeeded)
        {
            PaintPower_Engine.App.SetProjectStatus("Save Project");
            PaintPower_Engine.App.saveNeeded = true;
        }

        CanvasImage.InvalidateVisual();
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
            return;

        var scroll = (ScrollViewer)sender;
        var pos = e.GetPosition(scroll);

        double oldZoom = ZoomSlider.Value;
        double delta = e.Delta.Y > 0 ? 1.1 : 0.9;
        double newZoom = Math.Clamp(oldZoom * delta, ZoomSlider.Minimum, ZoomSlider.Maximum);

        ZoomSlider.Value = newZoom;

        double factor = newZoom / oldZoom;

        scroll.Offset = new Vector(
            (scroll.Offset.X + pos.X) * factor - pos.X,
            (scroll.Offset.Y + pos.Y) * factor - pos.Y
        );

        e.Handled = true;
    }

    private void OnScrollPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var scroll = (ScrollViewer)sender;

        if (e.GetCurrentPoint(scroll).Properties.IsMiddleButtonPressed || _isPanning)
        {
            _panStart = e.GetPosition(scroll);
            _scrollStart = scroll.Offset;
            scroll.Cursor = new Cursor(StandardCursorType.Hand);
            e.Pointer.Capture(scroll);
        }

        if (!PaintPower_Engine.App.saveNeeded)
        {
            PaintPower_Engine.App.SetProjectStatus("Save Project");
            PaintPower_Engine.App.saveNeeded = true;
        }
    }

    private void OnScrollPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var scroll = (ScrollViewer)sender;
        scroll.Cursor = Cursor.Default;
        e.Pointer.Capture(null);
    }

    private void OnScrollPointerMoved(object? sender, PointerEventArgs e)
    {
        var scroll = (ScrollViewer)sender;

        if (e.Pointer.Captured == scroll)
        {
            var pos = e.GetPosition(scroll);
            var delta = pos - _panStart;

            scroll.Offset = new Vector(
                _scrollStart.X - delta.X,
                _scrollStart.Y - delta.Y
            );
        }
    }

    private void UpdateColor()
    {
        var hue = HueSlider.Value;
        var sat = SVPicker.Saturation;
        var val = SVPicker.Value;
        var alpha = (byte)OpacitySlider.Value;

        var rgb = SVPicker.ColorFromHSV(hue, sat, val);

        _currentColor = Color.FromArgb(alpha, rgb.R, rgb.G, rgb.B);

        ColorPreview.Background = new SolidColorBrush(_currentColor);
    }
}