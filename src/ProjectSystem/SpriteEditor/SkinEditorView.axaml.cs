using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PaintPower.Display.DisplayIntegration;
using PaintPower.ProjectSystem;
using PaintPower.Tools.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PaintPower.ProjectSystem.SpriteEditor;

public partial class SkinEditorView : SpriteEditor
{

    private double _stageWidth = 640;
    private double _stageHeight = 450;

    private readonly PaintSprite _sprite;
    private readonly SkinDefinition _skin;

    // Viewport state
    private double _zoom = 1.0;
    private Point _pan = new Point(0, 0);
    private bool _panning = false;
    private Point _lastMouse;

    private SkinElement? _selectedElement;
    private bool _suppressPropertyEvents = false;

    // Gizmos
    private enum GizmoMode { None, Move, Rotate, Scale }
    private GizmoMode _gizmoMode = GizmoMode.None;

    private const double HandleSize = 10;
    private const double RotationHandleDistance = 40;

    private bool _draggingGizmo = false;
    private Point _gizmoStartMouse;
    private double _gizmoStartRotation;
    private double _gizmoStartScaleX;
    private double _gizmoStartScaleY;

    // Manual drag from asset list
    private bool _draggingAsset = false;
    private AssetEntry? _dragAssetEntry;
    private Border? _dragGhost;
    private readonly Point _dragGhostOffset = new Point(16, 16);

    public SkinEditorView(PaintSprite sprite, SkinDefinition skin, double stageWidth, double stageHeight)
    {
        InitializeComponent();

        _stageWidth = stageWidth;
        _stageHeight = stageHeight;

        _sprite = sprite;
        _skin = skin;

        NameBox.LostFocus += OnPropertyChanged;
        XBox.LostFocus += OnPropertyChanged;
        YBox.LostFocus += OnPropertyChanged;
        RotationBox.LostFocus += OnPropertyChanged;
        ScaleXBox.LostFocus += OnPropertyChanged;
        ScaleYBox.LostFocus += OnPropertyChanged;
        ZBox.LostFocus += OnPropertyChanged;

        Viewport.AttachedToVisualTree += (_, __) => Redraw();

        RefreshElementList();
        RefreshAssetList();
    }

    // ---------------------------------------------------------
    // Rendering
    // ---------------------------------------------------------
    private void Redraw()
    {
        if (Viewport == null)
            return;

        Viewport.Children.Clear();

        var ctx = new DrawingContextWrapper(Viewport);
        ctx.Clear(Colors.White);

        DrawOverlays(ctx);

        foreach (var elem in _skin.Elements)
        {
            if (elem == null)
                continue;

            var (w, h) = GetElementDisplaySize(elem);
            var sx = (elem.Transform.X * _zoom) + _pan.X;
            var sy = (elem.Transform.Y * _zoom) + _pan.Y;
            var screenW = w * _zoom;
            var screenH = h * _zoom;
            var assetImage = LoadElementAssetImage(elem);

            if (assetImage != null)
            {
                ctx.DrawImage(assetImage, sx - screenW / 2, sy - screenH / 2, screenW, screenH, elem.Transform.Rotation);
            }
            else
            {
                ctx.DrawRect(sx - screenW / 2, sy - screenH / 2, screenW, screenH, Colors.Gray, 1);
            }
        }

        if (_selectedElement != null)
        {
            var (w, h) = GetElementDisplaySize(_selectedElement);
            var sx = (_selectedElement.Transform.X * _zoom) + _pan.X;
            var sy = (_selectedElement.Transform.Y * _zoom) + _pan.Y;
            var screenW = w * _zoom;
            var screenH = h * _zoom;

            ctx.DrawRect(sx - screenW / 2, sy - screenH / 2, screenW, screenH, Colors.Cyan, 2, _selectedElement.Transform.Rotation);
            DrawGizmos(ctx, _selectedElement);
        }
    }

    private void DrawGizmos(DrawingContextWrapper ctx, SkinElement elem)
    {
        double cx = (elem.Transform.X * _zoom) + _pan.X;
        double cy = (elem.Transform.Y * _zoom) + _pan.Y;

        var (w, h) = GetElementDisplaySize(elem);
        double screenW = w * _zoom;
        double screenH = h * _zoom;
        double rotationHandleDistance = RotationHandleDistance * _zoom;

        var handles = GetElementHandlePoints(elem, cx, cy, screenW, screenH, rotationHandleDistance);

        foreach (var handle in handles)
        {
            DrawHandle(ctx, handle.X, handle.Y);
        }
    }

    private void DrawHandle(DrawingContextWrapper ctx, double x, double y)
    {
        ctx.DrawRect(x - HandleSize / 2, y - HandleSize / 2, HandleSize, HandleSize, Colors.Orange, 2);
    }

    private GizmoMode HitTestGizmo(Point mouse, SkinElement elem)
    {
        double cx = (elem.Transform.X * _zoom) + _pan.X;
        double cy = (elem.Transform.Y * _zoom) + _pan.Y;

        var (w, h) = GetElementDisplaySize(elem);
        double screenW = w * _zoom;
        double screenH = h * _zoom;
        double rotationHandleDistance = RotationHandleDistance * _zoom;

        var handles = GetElementHandlePoints(elem, cx, cy, screenW, screenH, rotationHandleDistance);

        if (IsInsideHandle(mouse, handles[4].X, handles[4].Y))
            return GizmoMode.Rotate;

        if (IsInsideHandle(mouse, handles[0].X, handles[0].Y)) return GizmoMode.Scale;
        if (IsInsideHandle(mouse, handles[1].X, handles[1].Y)) return GizmoMode.Scale;
        if (IsInsideHandle(mouse, handles[2].X, handles[2].Y)) return GizmoMode.Scale;
        if (IsInsideHandle(mouse, handles[3].X, handles[3].Y)) return GizmoMode.Scale;

        return GizmoMode.None;
    }

    private bool IsInsideHandle(Point mouse, double hx, double hy)
    {
        return Math.Abs(mouse.X - hx) < HandleSize &&
               Math.Abs(mouse.Y - hy) < HandleSize;
    }

    private void DrawOverlays(DrawingContextWrapper ctx)
    {
        double w = _stageWidth * _zoom;
        double h = _stageHeight * _zoom;

        double x = _pan.X;
        double y = _pan.Y;

        ctx.DrawRect(x, y, w, h, Colors.Gray, 2);
        ctx.DrawRect(x + w * 0.1, y + h * 0.1, w * 0.8, h * 0.8, Colors.Yellow, 1);
        ctx.DrawRect(x + w * 0.1, y + h * 0.1, w * 0.8, h * 0.8, Colors.Yellow, 1);
        ctx.DrawRect(x - w * 0.1, y - h * 0.1, w * 1.2, h * 1.2, Colors.Red, 1);
    }

    // ---------------------------------------------------------
    // Pointer (selection / pan / gizmo)
    // ---------------------------------------------------------
    private void OnPointerDown(object? sender, PointerPressedEventArgs e)
    {
        if (Viewport == null)
            return;

        var pos = e.GetPosition(Viewport);

        if (_selectedElement != null)
        {
            var gizmo = HitTestGizmo(pos, _selectedElement);
            if (gizmo != GizmoMode.None)
            {
                _gizmoMode = gizmo;
                _draggingGizmo = true;
                _gizmoStartMouse = pos;

                _gizmoStartRotation = _selectedElement.Transform.Rotation;
                _gizmoStartScaleX = _selectedElement.Transform.ScaleX;
                _gizmoStartScaleY = _selectedElement.Transform.ScaleY;

                return;
            }
        }

        var hit = HitTestElement(pos);
        if (hit != null)
        {
            _selectedElement = hit;
            LoadPropertiesFromElement();
            Redraw();
            _panning = false;
            return;
        }

        _panning = true;
        _lastMouse = pos;
    }

    private void OnPointerMove(object? sender, PointerEventArgs e)
    {
        if (Viewport == null)
            return;

        var pos = e.GetPosition(Viewport);

        if (_draggingGizmo && _selectedElement != null)
        {
            if (_gizmoMode == GizmoMode.Rotate)
            {
                double cx = (_selectedElement.Transform.X * _zoom) + _pan.X;
                double cy = (_selectedElement.Transform.Y * _zoom) + _pan.Y;

                double angle1 = Math.Atan2(_gizmoStartMouse.Y - cy, _gizmoStartMouse.X - cx);
                double angle2 = Math.Atan2(pos.Y - cy, pos.X - cx);

                double delta = (angle2 - angle1) * (180 / Math.PI);

                _selectedElement.Transform.Rotation = _gizmoStartRotation + delta;
            }
            else if (_gizmoMode == GizmoMode.Scale)
            {
                double dx = (pos.X - _gizmoStartMouse.X) / _zoom;
                double dy = (pos.Y - _gizmoStartMouse.Y) / _zoom;

                _selectedElement.Transform.ScaleX = Math.Max(0.1, _gizmoStartScaleX + dx * 0.01);
                _selectedElement.Transform.ScaleY = Math.Max(0.1, _gizmoStartScaleY + dy * 0.01);
            }

            SaveBackToSkinDefinition();
            LoadPropertiesFromElement();
            Redraw();
            return;
        }

        if (_panning)
        {
            var dx = pos.X - _lastMouse.X;
            var dy = pos.Y - _lastMouse.Y;

            _pan = new Point(_pan.X + dx, _pan.Y + dy);
            _lastMouse = pos;

            Redraw();
        }
    }

    private void OnPointerUp(object? sender, PointerReleasedEventArgs e)
    {
        _panning = false;
        _draggingGizmo = false;
        _gizmoMode = GizmoMode.None;
    }

    // ---------------------------------------------------------
    // Root-level pointer for ghost drag
    // ---------------------------------------------------------
    private void OnRootPointerMove(object? sender, PointerEventArgs e)
    {
        if (!_draggingAsset || _dragGhost == null)
            return;

        var pos = e.GetPosition(Root);
        Canvas.SetLeft(_dragGhost, pos.X + _dragGhostOffset.X);
        Canvas.SetTop(_dragGhost, pos.Y + _dragGhostOffset.Y);
    }

    private void OnRootPointerUp(object? sender, PointerReleasedEventArgs e)
    {
        if (!_draggingAsset)
            return;

        // Remove ghost
        if (_dragGhost != null)
        {
            Overlay.Children.Remove(_dragGhost);
            _dragGhost = null;
        }



        // Drop onto viewport if inside bounds
        if (_dragAssetEntry != null && Viewport != null)
        {
            var pos = e.GetPosition(Viewport);
            var bounds = new Rect(0, 0, Viewport.Bounds.Width, Viewport.Bounds.Height);

            if (bounds.Contains(pos))
            {
                AddAssetToSkin(_dragAssetEntry.FullPath, pos);
            }
        }

        _draggingAsset = false;
        _dragAssetEntry = null;
    }

    // ---------------------------------------------------------
    // Zoom
    // ---------------------------------------------------------
    private void OnZoom(object? sender, PointerWheelEventArgs e)
    {
        double delta = e.Delta.Y > 0 ? 1.1 : 0.9;
        _zoom *= delta;
        Redraw();
    }

    // ---------------------------------------------------------
    // Hit test
    // ---------------------------------------------------------
    private SkinElement? HitTestElement(Point mouse)
    {
        foreach (var elem in _skin.Elements)
        {
            if (elem == null)
                continue;

            var (w, h) = GetElementDisplaySize(elem);
            var screenW = w * _zoom;
            var screenH = h * _zoom;
            var cx = (elem.Transform.X * _zoom) + _pan.X;
            var cy = (elem.Transform.Y * _zoom) + _pan.Y;
            var points = GetElementHandlePoints(elem, cx, cy, screenW, screenH, RotationHandleDistance * _zoom);

            var left = points.Min(p => p.X);
            var right = points.Max(p => p.X);
            var top = points.Min(p => p.Y);
            var bottom = points.Max(p => p.Y);

            if (mouse.X >= left && mouse.X <= right &&
                mouse.Y >= top && mouse.Y <= bottom)
            {
                return elem;
            }
        }

        return null;
    }

    // ---------------------------------------------------------
    // Panels
    // ---------------------------------------------------------
    private void RefreshElementList()
    {
        ElementsList.ItemsSource = null;
        ElementsList.ItemsSource = _skin.Elements;
    }

    private void OnElementSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (ElementsList.SelectedItem is not SkinElement elem)
        {
            _selectedElement = null;
            return;
        }

        _selectedElement = _skin.Elements
            .FirstOrDefault(x => x.InstanceName == elem.InstanceName);

        LoadPropertiesFromElement();
        Redraw();
    }

    private void LoadPropertiesFromElement()
    {
        if (_selectedElement == null)
            return;

        _suppressPropertyEvents = true;

        NameBox.Text = _selectedElement.InstanceName;
        XBox.Text = _selectedElement.Transform.X.ToString();
        YBox.Text = _selectedElement.Transform.Y.ToString();
        RotationBox.Text = _selectedElement.Transform.Rotation.ToString();
        ScaleXBox.Text = _selectedElement.Transform.ScaleX.ToString();
        ScaleYBox.Text = _selectedElement.Transform.ScaleY.ToString();
        ZBox.Text = _selectedElement.ZIndex.ToString();

        _suppressPropertyEvents = false;
    }

    private void OnPropertyChanged(object? sender, EventArgs e)
    {
        if (_selectedElement == null || _suppressPropertyEvents)
            return;

        try
        {
            _selectedElement.InstanceName = NameBox.Text ?? "";
            _selectedElement.Transform.X = double.TryParse(XBox.Text, out var x) ? x : _selectedElement.Transform.X;
            _selectedElement.Transform.Y = double.TryParse(YBox.Text, out var y) ? y : _selectedElement.Transform.Y;
            _selectedElement.Transform.Rotation = double.TryParse(RotationBox.Text, out var rotation) ? rotation : _selectedElement.Transform.Rotation;
            _selectedElement.Transform.ScaleX = double.TryParse(ScaleXBox.Text, out var scaleX) ? scaleX : _selectedElement.Transform.ScaleX;
            _selectedElement.Transform.ScaleY = double.TryParse(ScaleYBox.Text, out var scaleY) ? scaleY : _selectedElement.Transform.ScaleY;
            _selectedElement.ZIndex = int.TryParse(ZBox.Text, out var zIndex) ? zIndex : _selectedElement.ZIndex;
        }
        catch
        {
        }

        SaveBackToSkinDefinition();
        Redraw();
    }

    private void SaveBackToSkinDefinition()
    {
        if (_selectedElement == null)
            return;

        _sprite?.SaveSkins();
    }

    // ---------------------------------------------------------
    // Asset list + placement
    // ---------------------------------------------------------
    private void BuildAssetList(string dir, int depth, List<AssetEntry> list)
    {
        foreach (var folder in Directory.GetDirectories(dir))
        {
            list.Add(new AssetEntry
            {
                Name = Path.GetFileName(folder),
                FullPath = folder,
                IsDirectory = true,
                Depth = depth
            });

            BuildAssetList(folder, depth + 1, list);
        }

        foreach (var file in Directory.GetFiles(dir))
        {
            list.Add(new AssetEntry
            {
                Name = Path.GetFileName(file),
                FullPath = file,
                IsDirectory = false,
                Depth = depth
            });
        }
    }

    private void RefreshAssetList()
    {
        AssetGrid.Children.Clear();

        if (!Directory.Exists(_sprite.ItemsFolder))
            return;

        var list = new List<AssetEntry>();
        BuildAssetList(_sprite.ItemsFolder, 0, list);

        foreach (var entry in list)
        {
            if (entry.IsDirectory)
                continue; // folders not shown in grid

            var thumb = LoadThumbnail(entry.FullPath, 96);

            var tile = new Border
            {
                Width = 96,
                Height = 120,
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(4),
                Tag = entry
            };

            var stack = new StackPanel
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            stack.Children.Add(
                thumb != null
                    ? new Image { Source = thumb, Width = 80, Height = 80 }
                    : new Image { Source = LoadFallbackImage().Source, Width = 96, Height = 96 }
            );

            stack.Children.Add(new TextBlock
            {
                Text = entry.Name,
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 80
            });

            tile.Child = stack;

            tile.PointerPressed += OnAssetPointerPressed;

            AssetGrid.Children.Add(tile);
        }
    }

    private Image LoadFallbackImage()
    {
        return new Image { Source = new Bitmap("Assets/PaintPower Filetypes/Fallback.png") };
    }

    private void OnAssetPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border tile || tile.Tag is not AssetEntry entry)
            return;

        _draggingAsset = true;
        _dragAssetEntry = entry;

        var thumb = LoadThumbnail(entry.FullPath, 96);

        _dragGhost = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(180, 30, 30, 30)),
            BorderBrush = Brushes.White,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4),
            Child = new StackPanel
            {
                Spacing = 4,
                Children =
    {
        thumb != null
            ? new Image { Source = thumb, Width = 96, Height = 96 }
            : new Image { Source = LoadFallbackImage().Source, Width = 96, Height = 96 },

        new TextBlock
        {
            Text = entry.Name,
            Foreground = Brushes.White
        }
    }
            },
            Opacity = 0.85
        };

        Overlay.Children.Add(_dragGhost);

        var pos = e.GetPosition(Root);
        Canvas.SetLeft(_dragGhost, pos.X + _dragGhostOffset.X);
        Canvas.SetTop(_dragGhost, pos.Y + _dragGhostOffset.Y);
    }

    private void AddAssetToSkin(string fullPath, Point dropPos)
    {
        string file = Path.GetFileName(fullPath);
        string relative = "items/" + file;

        bool isVideo = file.EndsWith(".mp4") || file.EndsWith(".webm");

        double worldX = (dropPos.X - _pan.X) / _zoom;
        double worldY = (dropPos.Y - _pan.Y) / _zoom;

        SkinElement newElem = isVideo
            ? new SkinVideoElement { AssetPath = relative, Loop = true, AutoPlay = true }
            : new SkinImageElement { AssetPath = relative };

        newElem.InstanceName = (isVideo ? "Video" : "Image") + _skin.Elements.Count;
        newElem.Transform = new SkinTransform { X = worldX, Y = worldY };
        newElem.ZIndex = 0;

        _skin.Elements.Add(newElem);
        _sprite.SaveSkins();

        RefreshElementList();
        Redraw();
    }

    private (double width, double height) GetElementDisplaySize(SkinElement element)
    {
        if (TryLoadElementAssetImage(element, out var assetImage) && assetImage is Bitmap bitmap)
        {
            var width = Math.Max(16, bitmap.PixelSize.Width * Math.Max(0.1, element.Transform.ScaleX));
            var height = Math.Max(16, bitmap.PixelSize.Height * Math.Max(0.1, element.Transform.ScaleY));
            return (width, height);
        }

        return (64 * Math.Max(0.1, element.Transform.ScaleX), 64 * Math.Max(0.1, element.Transform.ScaleY));
    }

    private bool TryLoadElementAssetImage(SkinElement element, out IImage? image)
    {
        image = null;

        if (string.IsNullOrWhiteSpace(element.AssetPath))
            return false;

        string fullPath = Path.Combine(_sprite.SpriteFolder, element.AssetPath);

        if (!File.Exists(fullPath))
            return false;

        try
        {
            var asset = GraphicLoader.LoadCached(fullPath);
            if (asset is Graphic graphic)
            {
                image = graphic.ToAvaloniaBitmap();
                return true;
            }
        }
        catch
        {
        }

        return false;
    }

    private IImage? LoadElementAssetImage(SkinElement element)
    {
        return TryLoadElementAssetImage(element, out var image) ? image : null;
    }

    private List<Point> GetElementHandlePoints(SkinElement elem, double cx, double cy, double screenW, double screenH, double rotationHandleDistance)
    {
        var points = new List<Point>
        {
            new Point(-screenW / 2, -screenH / 2),
            new Point(screenW / 2, -screenH / 2),
            new Point(-screenW / 2, screenH / 2),
            new Point(screenW / 2, screenH / 2),
            new Point(0, -screenH / 2 - rotationHandleDistance)
        };

        return points.Select(local => RotatePoint(local, new Point(0, 0), elem.Transform.Rotation * Math.PI / 180.0))
            .Select(rotated => new Point(cx + rotated.X, cy + rotated.Y))
            .ToList();
    }

    private Point RotatePoint(Point point, Point origin, double radians)
    {
        double cos = Math.Cos(radians);
        double sin = Math.Sin(radians);
        double dx = point.X - origin.X;
        double dy = point.Y - origin.Y;

        return new Point(origin.X + (dx * cos - dy * sin), origin.Y + (dx * sin + dy * cos));
    }

    private IImage? LoadThumbnail(string path, int size = 64)
    {
        try
        {
            using var fs = File.OpenRead(path);
            var bmp = new Avalonia.Media.Imaging.Bitmap(fs);
            double scale = Math.Min(size / (double)bmp.PixelSize.Width, size / (double)bmp.PixelSize.Height);
            int w = (int)(bmp.PixelSize.Width * scale);
            int h = (int)(bmp.PixelSize.Height * scale);

            return bmp.CreateScaledBitmap(new PixelSize(w, h));
        }
        catch
        {
            return null;
        }
    }
}
