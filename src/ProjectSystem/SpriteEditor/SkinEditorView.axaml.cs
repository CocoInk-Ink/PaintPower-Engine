using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PaintPower.Display.DisplayIntegration;
using PaintPower.ProjectSystem;
using PaintPower.Sprites;
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

    private Sprite _runtimeSprite;

    // Viewport state
    private double _zoom = 1.0;
    private Point _pan = new Point(0, 0);
    private bool _panning = false;
    private Point _lastMouse;

    private RuntimeSkinElement? _selectedElement;
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

        _runtimeSprite = _sprite.ToRuntimeSprite();
        int index = _sprite.Skins.IndexOf(_skin);
        _runtimeSprite.SetSkin(index);
        _runtimeSprite.SnapshotDirty = true;
        _runtimeSprite.RenderSnapshot();

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
        ctx.Clear(Colors.Black);

        if (_runtimeSprite?.SnapshotGraphic != null)
        {
            ctx.DrawGraphic(
                _runtimeSprite.SnapshotGraphic,
                _pan.X,
                _pan.Y,
                _zoom
            );
        }

        DrawOverlays(ctx);

        if (_runtimeSprite?.CurrentSkin?.Elements != null)
        {
            foreach (var elem in _runtimeSprite.CurrentSkin.Elements)
            {
                if (elem == null)
                    continue;

                double sx = (elem.X * _zoom) + _pan.X;
                double sy = (elem.Y * _zoom) + _pan.Y;
                double w = elem.Width * _zoom;
                double h = elem.Height * _zoom;

                ctx.DrawRect(sx - w / 2, sy - h / 2, w, h, Colors.Gray, 1);
            }
        }

        if (_selectedElement != null)
        {
            double sx = (_selectedElement.X * _zoom) + _pan.X;
            double sy = (_selectedElement.Y * _zoom) + _pan.Y;

            double w = _selectedElement.Width * _zoom;
            double h = _selectedElement.Height * _zoom;

            ctx.DrawRect(sx - w / 2, sy - h / 2, w, h, Colors.Cyan, 2);
            DrawGizmos(ctx, _selectedElement);
        }
    }

    private void DrawGizmos(DrawingContextWrapper ctx, RuntimeSkinElement elem)
    {
        double cx = (elem.X * _zoom) + _pan.X;
        double cy = (elem.Y * _zoom) + _pan.Y;

        double w = elem.Width * _zoom;
        double h = elem.Height * _zoom;

        // Scale handles
        DrawHandle(ctx, cx - w / 2, cy - h / 2);
        DrawHandle(ctx, cx + w / 2, cy - h / 2);
        DrawHandle(ctx, cx - w / 2, cy + h / 2);
        DrawHandle(ctx, cx + w / 2, cy + h / 2);

        // Rotation handle
        DrawHandle(ctx, cx, cy - h / 2 - RotationHandleDistance);
    }

    private void DrawHandle(DrawingContextWrapper ctx, double x, double y)
    {
        ctx.DrawRect(x - HandleSize / 2, y - HandleSize / 2, HandleSize, HandleSize, Colors.Orange, 2);
    }

    private GizmoMode HitTestGizmo(Point mouse, RuntimeSkinElement elem)
    {
        double cx = (elem.X * _zoom) + _pan.X;
        double cy = (elem.Y * _zoom) + _pan.Y;

        double w = elem.Width * _zoom;
        double h = elem.Height * _zoom;

        var rotX = cx;
        var rotY = cy - h / 2 - RotationHandleDistance;
        if (IsInsideHandle(mouse, rotX, rotY))
            return GizmoMode.Rotate;

        if (IsInsideHandle(mouse, cx - w / 2, cy - h / 2)) return GizmoMode.Scale;
        if (IsInsideHandle(mouse, cx + w / 2, cy - h / 2)) return GizmoMode.Scale;
        if (IsInsideHandle(mouse, cx - w / 2, cy + h / 2)) return GizmoMode.Scale;
        if (IsInsideHandle(mouse, cx + w / 2, cy + h / 2)) return GizmoMode.Scale;

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

        ctx.DrawRect(x, y, w, h, Colors.White, 2);
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

                _gizmoStartRotation = _selectedElement.Rotation;
                _gizmoStartScaleX = _selectedElement.ScaleX;
                _gizmoStartScaleY = _selectedElement.ScaleY;

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
                double cx = (_selectedElement.X * _zoom) + _pan.X;
                double cy = (_selectedElement.Y * _zoom) + _pan.Y;

                double angle1 = Math.Atan2(_gizmoStartMouse.Y - cy, _gizmoStartMouse.X - cx);
                double angle2 = Math.Atan2(pos.Y - cy, pos.X - cx);

                double delta = (angle2 - angle1) * (180 / Math.PI);

                _selectedElement.Rotation = _gizmoStartRotation + delta;
            }
            else if (_gizmoMode == GizmoMode.Scale)
            {
                double dx = (pos.X - _gizmoStartMouse.X) / _zoom;
                double dy = (pos.Y - _gizmoStartMouse.Y) / _zoom;

                _selectedElement.ScaleX = Math.Max(0.1, _gizmoStartScaleX + dx * 0.01);
                _selectedElement.ScaleY = Math.Max(0.1, _gizmoStartScaleY + dy * 0.01);
            }

            _runtimeSprite.SnapshotDirty = true;
            _runtimeSprite.RenderSnapshot();
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
    private RuntimeSkinElement? HitTestElement(Point mouse)
    {
        if (_runtimeSprite?.CurrentSkin?.Elements == null)
            return null;

        foreach (var elem in _runtimeSprite.CurrentSkin.Elements)
        {
            if (elem == null)
                continue;

            double sx = (elem.X * _zoom) + _pan.X;
            double sy = (elem.Y * _zoom) + _pan.Y;

            double w = elem.Width * _zoom;
            double h = elem.Height * _zoom;

            double left = sx - w / 2;
            double right = sx + w / 2;
            double top = sy - h / 2;
            double bottom = sy + h / 2;

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

        _selectedElement = _runtimeSprite.CurrentSkin.Elements
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
        XBox.Text = _selectedElement.X.ToString();
        YBox.Text = _selectedElement.Y.ToString();
        RotationBox.Text = _selectedElement.Rotation.ToString();
        ScaleXBox.Text = _selectedElement.ScaleX.ToString();
        ScaleYBox.Text = _selectedElement.ScaleY.ToString();
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
            _selectedElement.X = double.Parse(XBox.Text);
            _selectedElement.Y = double.Parse(YBox.Text);
            _selectedElement.Rotation = double.Parse(RotationBox.Text);
            _selectedElement.ScaleX = double.Parse(ScaleXBox.Text);
            _selectedElement.ScaleY = double.Parse(ScaleYBox.Text);
            _selectedElement.ZIndex = int.Parse(ZBox.Text);
        }
        catch
        {
        }

        _runtimeSprite.SnapshotDirty = true;
        _runtimeSprite.RenderSnapshot();

        SaveBackToSkinDefinition();
        Redraw();
    }

    private void SaveBackToSkinDefinition()
    {
        if (_selectedElement == null)
            return;

        var def = _skin.Elements
            ?.FirstOrDefault(e => e.InstanceName == _selectedElement.InstanceName);

        if (def == null)
            return;

        def.Transform.X = _selectedElement.X;
        def.Transform.Y = _selectedElement.Y;
        def.Transform.Rotation = _selectedElement.Rotation;
        def.Transform.ScaleX = _selectedElement.ScaleX;
        def.Transform.ScaleY = _selectedElement.ScaleY;
        def.ZIndex = _selectedElement.ZIndex;

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

        _runtimeSprite = _sprite.ToRuntimeSprite();
        _runtimeSprite.SetStageSize(_stageWidth, _stageHeight);
        _runtimeSprite.SetSkin(_sprite.Skins.IndexOf(_skin));
        _runtimeSprite.SnapshotDirty = true;
        _runtimeSprite.RenderSnapshot();

        RefreshElementList();
        Redraw();
    }

    private IImage? LoadThumbnail(string path, int size = 64)
    {
        try
        {
            using var fs = File.OpenRead(path);
            var bmp = new Avalonia.Media.Imaging.Bitmap(fs);

            double scale = Math.Min(size / bmp.PixelSize.Width, size / bmp.PixelSize.Height);
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
