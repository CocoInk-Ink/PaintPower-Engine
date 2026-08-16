using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Toolbox.Display.DisplayIntegration;
using Toolbox.Logging;
using PaintPower.ProjectSystem;
using Toolbox.Graphics;
using SixLabors.ImageSharp.ColorSpaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Toolbox.Plumbing;

namespace PaintPower.ProjectSystem.SpriteEditor;

public partial class SkinEditorView : SpriteEditor
{

    private double _stageWidth = 640;
    private double _stageHeight = 450;

    public double StageWidth => _stageWidth;
    public double StageHeight => _stageHeight;

    private readonly PaintSprite _sprite;
    private readonly SkinDefinition _skin;

    // Viewport state
    private double _zoom = 1.0;
    private Point _lastMouse;

    private SkinElement? _selectedElement;
    private bool _suppressPropertyEvents = false;

    // Gizmos
    private enum GizmoMode { None, Move, Rotate, Scale }
    private GizmoMode _gizmoMode = GizmoMode.None;

    private const double HandleSize = 10;
    private const double RotationHandleDistance = 40;

    private bool _draggingGizmo = false;
    private bool _movingElement = false;
    private Point _gizmoStartMouse;
    private double _gizmoStartRotation;
    private double _gizmoStartScaleX;
    private double _gizmoStartScaleY;
    private double _moveStartX;
    private double _moveStartY;

    private double GetCenterX()
    {
        return (Viewport.Bounds.Width / 2.0);
    }

    private double GetCenterY()
    {
        return (Viewport.Bounds.Height / 2.0);
    }


    // Manual drag from asset list
    private bool _draggingAsset = false;
    private AssetEntry? _dragAssetEntry;
    private Border? _dragGhost;
    private readonly Point _dragGhostOffset = new Point(-16, -16);

    private record HistoryAction(Action Undo, Action Redo);

    private readonly Stack<HistoryAction> _undoStack = new();
    private readonly Stack<HistoryAction> _redoStack = new();

    public SkinEditorView(PaintSprite sprite, SkinDefinition skin, double stageWidth, double stageHeight)
    {
        InitializeComponent();

        _stageWidth = stageWidth;
        _stageHeight = stageHeight;

        Viewport.Width = stageWidth;
        Viewport.Height = stageHeight;

        _sprite = sprite;
        _skin = skin;

        ViewportBorder.Width = _stageWidth * _zoom;
        ViewportBorder.Height = _stageHeight * _zoom;

        NameBox.LostFocus += OnPropertyChanged;
        XBox.LostFocus += OnPropertyChanged;
        YBox.LostFocus += OnPropertyChanged;
        RotationBox.LostFocus += OnPropertyChanged;
        ScaleXBox.LostFocus += OnPropertyChanged;
        ScaleYBox.LostFocus += OnPropertyChanged;
        ZBox.LostFocus += OnPropertyChanged;

        Root.Focusable = true;
        Root.AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        Viewport.AttachedToVisualTree += (_, __) => Redraw();

        RefreshElementList();
        RefreshAssetList();

        InvalidateVisual();
    }

    private void PushHistory(Action undo, Action redo)
    {
        _undoStack.Push(new HistoryAction(undo, redo));
        _redoStack.Clear();
    }

    private void Undo()
    {
        if (_undoStack.Count == 0)
            return;

        var action = _undoStack.Pop();
        action.Undo();

        _redoStack.Push(action);

        Redraw();
    }

    private void Redo()
    {
        if (_redoStack.Count == 0)
            return;

        var action = _redoStack.Pop();
        action.Redo();

        _undoStack.Push(action);

        Redraw();
    }


    public void DeleteSelectedElement()
    {
        if (_selectedElement != null)
        {
            // Clear selection
            SkinElement element = _selectedElement;

            ElementsList.SelectedItem = null;
            ElementsList.SelectedIndex = -1;

            var deleted = _selectedElement;

            _skin.Elements.Remove(deleted);

            PushHistory(
                undo: () => _skin.Elements.Add(deleted),
                redo: () => _skin.Elements.Remove(deleted)
            );

            _selectedElement = null;

            _sprite.SaveSkins();
            RefreshElementList();
            LoadPropertiesFromElement();
            Redraw();
        }
    }

    // ---------------------------------------------------------
    // Rendering
    // ---------------------------------------------------------
    private void Redraw()
    {
        // Create context
        var ctx = new DrawingContextWrapper(Viewport);

        ViewportBorder.RenderTransform = new ScaleTransform(_zoom, _zoom);
        ViewportBorder.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

        // This function creates the skin, this gets passed to render overlays
        // to be drawn after the under overlays.

        object? renderSkin(DrawingContextWrapper ctx)
        {
            if (Viewport == null)
                return null;

            Viewport.Children.Clear();

            ctx.Clear(Colors.Transparent);

            foreach (var elem in _skin.Elements)
            {
                if (elem == null)
                    continue;

                var (w, h) = GetElementDisplaySize(elem);

                double sx = (Viewport.Bounds.Width / 2.0) + elem.Transform.X;
                double sy = (Viewport.Bounds.Height / 2.0) + elem.Transform.Y;

                var screenW = w;
                var screenH = h;
                var assetImage = LoadElementAssetImage(elem);

                var previewControl = CreateElementPreview(elem);
                if (previewControl is Image imageControl)
                {
                    ctx.DrawImage(imageControl.Source as IImage, sx - screenW / 2, sy - screenH / 2, screenW, screenH, elem.Transform.Rotation - 90);
                }
                else if (previewControl != null)
                {
                    ctx.DrawControl(previewControl, sx - screenW / 2, sy - screenH / 2, screenW, screenH, elem.Transform.Rotation - 90);
                }
                else
                {
                    ctx.DrawRect(sx - screenW / 2, sy - screenH / 2, screenW, screenH, Colors.Gray, elem.Transform.Rotation - 90);
                }
            }

            if (_selectedElement != null)
            {
                var (w, h) = GetElementDisplaySize(_selectedElement);
                var sx = GetCenterX() + _selectedElement.Transform.X;
                var sy = GetCenterY() + _selectedElement.Transform.Y;
                var screenW = w;
                var screenH = h;

                ctx.DrawRect(sx - screenW / 2, sy - screenH / 2, screenW, screenH, Colors.Cyan, 2, _selectedElement.Transform.Rotation - 90);
                DrawGizmos(ctx, _selectedElement);
            }

            return null;
        }

        renderSkin(ctx);
        DrawCenterCross(ctx);

        InvalidateVisual();
    }

    private void DrawGizmos(DrawingContextWrapper ctx, SkinElement elem)
    {
        double cx = GetCenterX() + (elem.Transform.X);
        double cy = GetCenterY() + (elem.Transform.Y);

        var (w, h) = GetElementDisplaySize(elem);
        double screenW = w;
        double screenH = h;
        double rotationHandleDistance = RotationHandleDistance;

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
        double cx = GetCenterX() + (elem.Transform.X);
        double cy = GetCenterY() + (elem.Transform.Y);

        var (w, h) = GetElementDisplaySize(elem);
        double screenW = w;
        double screenH = h;
        double rotationHandleDistance = RotationHandleDistance;

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
        double stageW = _stageWidth;
        double stageH = _stageHeight;

        double viewportW = Viewport.Bounds.Width;
        double viewportH = Viewport.Bounds.Height;

        double x = GetCenterX() - stageW / 2.0;
        double y = GetCenterY() - stageH / 2.0;

        double padding = Math.Max(24, stageW * 0.1);

        double innerX = x - padding * 0.5;
        double innerY = y - padding * 0.5;
        double innerW = stageW + padding;
        double innerH = stageH + padding;

        // Draw inner overlay border (surrounding the stage)
        ctx.DrawRect(innerX, innerY, innerW, innerH, Colors.Gray, 2);

        // Tint overflow area (but NOT the stage)
        // Top
        ctx.DrawOverlayRect(innerX, innerY, innerW, padding * 0.5, Color.FromArgb(40, 200, 200, 200), 0);

        // Bottom
        ctx.DrawOverlayRect(innerX, innerY + stageH + padding * 0.5, innerW, padding * 0.5, Color.FromArgb(40, 200, 200, 200), 0);

        // Left
        ctx.DrawOverlayRect(innerX, innerY + padding * 0.5, padding * 0.5, stageH, Color.FromArgb(40, 200, 200, 200), 0);

        // Right
        ctx.DrawOverlayRect(innerX + stageW + padding * 0.5, innerY + padding * 0.5, padding * 0.5, stageH, Color.FromArgb(40, 200, 200, 200), 0);
    }

    private void DrawCenterCross(DrawingContextWrapper ctx)
    {

        double size = 16;

        double cx = ViewportBorder.Bounds.Width / 2.0;
        double cy = ViewportBorder.Bounds.Height / 2.0;

        ctx.DrawRect(cx - size, cy - 1, size * 2, 2, Colors.Blue, 2);
        ctx.DrawRect(cx - 1, cy - size, 2, size * 2, Colors.Blue, 2);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete || e.Key == Key.Back)
        {
            if (e.Source is TextBox)
            {
                _sprite.SaveSkins();
                LoadPropertiesFromElement();
                return;
            }

            DeleteSelectedElement();

            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            if (_selectedElement != null)
            {
                _selectedElement = null;
                ElementsList.SelectedItem = null;
                LoadPropertiesFromElement();
                Redraw();
            }

            e.Handled = true;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (e.Key == Key.Y || (e.KeyModifiers.HasFlag(KeyModifiers.Shift) && e.Key == Key.Z))
            {
                Redo();
                e.Handled = true;
                return;
            }
            
            if (e.Key == Key.Z)
            {
                Undo();
                e.Handled = true;
                return;
            }
        }
    }

    // ---------------------------------------------------------
    // Pointer (selection / pan / gizmo)
    // ---------------------------------------------------------
    private void OnPointerDown(object? sender, PointerPressedEventArgs e)
    {
        if (Viewport == null)
            return;

        var pos = e.GetPosition(Viewport);
        Root.Focus();

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
            _movingElement = true;
            _moveStartX = _selectedElement.Transform.X;
            _moveStartY = _selectedElement.Transform.Y;
            _gizmoStartMouse = pos;
            LoadPropertiesFromElement();
            Redraw();
            return;
        }

        _selectedElement = null;
        LoadPropertiesFromElement();
        Redraw();
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
                double cx = (_selectedElement.Transform.X);
                double cy = (_selectedElement.Transform.Y);

                double angle1 = Math.Atan2(_gizmoStartMouse.Y - cy, _gizmoStartMouse.X - cx);
                double angle2 = Math.Atan2(pos.Y - cy, pos.X - cx);

                double delta = (angle2 - angle1) * (180 / Math.PI);

                double newRotation = _gizmoStartRotation + delta;

                double oldRotation = _selectedElement.Transform.Rotation;

                _selectedElement.Transform.Rotation = newRotation;

                var elem = _selectedElement;

                PushHistory(
                    undo: () =>
                    {
                        elem.Transform.Rotation = oldRotation;
                    },

                    redo: () =>
                    {
                        elem.Transform.Rotation = newRotation;
                    }
                );
            }
            else if (_gizmoMode == GizmoMode.Scale)
            {
                double dx = (pos.X - _gizmoStartMouse.X);
                double dy = (pos.Y - _gizmoStartMouse.Y);

                double oldSX = _selectedElement.Transform.ScaleX;
                double oldSY = _selectedElement.Transform.ScaleY;

                double newSX = Math.Max(0.1, _gizmoStartScaleX + dx * 0.01);
                double newSY = Math.Max(0.1, _gizmoStartScaleY + dy * 0.01);

                _selectedElement.Transform.ScaleX = newSX;
                _selectedElement.Transform.ScaleY = newSY;

                var elem = _selectedElement;

                PushHistory(
                    undo: () =>
                    {
                        elem.Transform.ScaleX = oldSX;
                        elem.Transform.ScaleY = oldSY;
                    },
                    redo: () =>
                    {
                        elem.Transform.ScaleX = newSX;
                        elem.Transform.ScaleY = newSY;
                    }
                );
            }

            SaveBackToSkinDefinition();
            LoadPropertiesFromElement();
            Redraw();
            return;
        }

        if (_movingElement && _selectedElement != null)
        {
            double dx = (pos.X - _gizmoStartMouse.X);
            double dy = (pos.Y - _gizmoStartMouse.Y);

            double limitX = (_stageWidth / 2) + 500;
            double limitY = (_stageHeight / 2) + 500;

            double oldX = _selectedElement.Transform.X;
            double oldY = _selectedElement.Transform.Y;

            double newX = Math.Clamp(_moveStartX + dx, -limitX, limitX);
            double newY = Math.Clamp(_moveStartY + dy, -limitY, limitY);

            var elem = _selectedElement;
            
            elem.Transform.X = newX;
            elem.Transform.Y = newY;

            PushHistory(
                undo: () =>
                {
                    elem.Transform.X = oldX;
                    elem.Transform.Y = oldY;
                },
                redo: () =>
                {
                    elem.Transform.X = newX;
                    elem.Transform.Y = newY;
                }
            );

            SaveBackToSkinDefinition();
            LoadPropertiesFromElement();
            Redraw();
            return;
        }
    }

    private void OnPointerUp(object? sender, PointerReleasedEventArgs e)
    {
        _draggingGizmo = false;
        _movingElement = false;
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
        if (_zoom > 3.0) _zoom = 3.0;
        if (_zoom < 0.5) _zoom = 0.5;
        Redraw();
    }

    // ---------------------------------------------------------
    // Hit test
    // ---------------------------------------------------------
    private SkinElement? HitTestElement(Point mouse)
    {
        for (int i = _skin.Elements.Count - 1; i >= 0; i--)
        {
            var elem = _skin.Elements[i];

            if (elem == null)
                continue;

            var (w, h) = GetElementDisplaySize(elem);
            var screenW = w;
            var screenH = h;
            var cx = GetCenterX() + (elem.Transform.X);
            var cy = GetCenterY() + (elem.Transform.Y);
            var points = GetElementHandlePoints(elem, cx, cy, screenW, screenH, RotationHandleDistance);

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
        ElementsList.SelectedIndex = -1;
        if (ElementsList.ItemsSource != null) ElementsList.ItemsSource = null;
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
        _suppressPropertyEvents = true;

        if (_selectedElement == null)
        {
            NameBox.Text = string.Empty;
            XBox.Text = string.Empty;
            YBox.Text = string.Empty;
            RotationBox.Text = string.Empty;
            ScaleXBox.Text = string.Empty;
            ScaleYBox.Text = string.Empty;
            ZBox.Text = string.Empty;
            _suppressPropertyEvents = false;
            return;
        }

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

        // Store old data into undo
        double oldX = _selectedElement.Transform.X;
        double oldY = _selectedElement.Transform.Y;
        double oldRot = _selectedElement.Transform.Rotation;
        double oldSX = _selectedElement.Transform.ScaleX;
        double oldSY = _selectedElement.Transform.ScaleY;
        int oldZ = _selectedElement.ZIndex;
        string oldName = _selectedElement.InstanceName;


        // Apply changes
        try
        {
            string newName = NameBox.Text ?? "";
            double newX = double.TryParse(XBox.Text, out var x) ? x : _selectedElement.Transform.X;
            double newY = double.TryParse(YBox.Text, out var y) ? y : _selectedElement.Transform.Y;
            double newRotation = double.TryParse(RotationBox.Text, out var rotation) ? rotation : _selectedElement.Transform.Rotation;
            double newScaleX = double.TryParse(ScaleXBox.Text, out var scaleX) ? scaleX : _selectedElement.Transform.ScaleX;
            double newScaleY = double.TryParse(ScaleYBox.Text, out var scaleY) ? scaleY : _selectedElement.Transform.ScaleY;
            int newZIndex = int.TryParse(ZBox.Text, out var zIndex) ? zIndex : _selectedElement.ZIndex;

            _selectedElement.InstanceName = newName;
            _selectedElement.Transform.X = newX;
            _selectedElement.Transform.Y = newY;
            _selectedElement.Transform.Rotation = newRotation;
            _selectedElement.Transform.ScaleX = newScaleX;
            _selectedElement.Transform.ScaleY = newScaleY;
            _selectedElement.ZIndex = newZIndex;

            var elem = _selectedElement;

            PushHistory(
                undo: () =>
                {
                    elem.Transform.X = oldX;
                    elem.Transform.Y = oldY;
                    elem.Transform.Rotation = oldRot;
                    elem.Transform.ScaleX = oldSX;
                    elem.Transform.ScaleY = oldSY;
                    elem.ZIndex = oldZ;
                    elem.InstanceName = oldName;
                },
                redo: () =>
                {
                    elem.Transform.X = newX;
                    elem.Transform.Y = newY;
                    elem.Transform.Rotation = newRotation;
                    elem.Transform.ScaleX = newScaleX;
                    elem.Transform.ScaleY = newScaleY;
                    elem.ZIndex = newZIndex;
                    elem.InstanceName = newName;
                }
            );
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
        return new Image { Source = new Bitmap(new Plumber().GetAssetPipe().LoadAsset("PaintPower Filetypes/Fallback.png")) };
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

        double worldX = (dropPos.X);
        double worldY = (dropPos.Y);

        SkinElement newElem = isVideo
            ? new SkinVideoElement { AssetPath = relative, Loop = true, AutoPlay = true }
            : new SkinImageElement { AssetPath = relative };

        newElem.InstanceName = (isVideo ? "Video" : "Image") + _skin.Elements.Count;
        newElem.Transform = new SkinTransform { X = worldX, Y = worldY };
        newElem.ZIndex = 0;

        var elem = newElem;

        _skin.Elements.Add(elem);

        _sprite.SaveSkins();

        PushHistory(
            undo: () => _skin.Elements.Remove(elem),
            redo: () => _skin.Elements.Add(elem)
        );


        RefreshElementList();
        Redraw();
    }

    private (double width, double height) GetElementDisplaySize(SkinElement element)
    {
        double width = element is SkinVideoElement ? 96 : 64;
        double height = element is SkinVideoElement ? 96 : 64;

        if (TryLoadElementAssetImage(element, out var assetImage) && assetImage is Bitmap bitmap)
        {
            width = Math.Max(16, bitmap.PixelSize.Width * 0.5);
            height = Math.Max(16, bitmap.PixelSize.Height * 0.5);
        }

        width = Math.Max(16, width * Math.Max(0.1, element.Transform.ScaleX));
        height = Math.Max(16, height * Math.Max(0.1, element.Transform.ScaleY));
        return (width, height);
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

    private Control? CreateElementPreview(SkinElement element)
    {
        if (element is SkinVideoElement)
        {
            return new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(220, 25, 25, 35)),
                BorderBrush = Brushes.Orange,
                BorderThickness = new Thickness(2),
                Padding = new Thickness(8),
                Child = new StackPanel
                {
                    Spacing = 4,
                    Children =
                    {
                        new TextBlock { Text = "▶", Foreground = Brushes.White, FontSize = 28, HorizontalAlignment = HorizontalAlignment.Center },
                        new TextBlock { Text = "Video Preview", Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center }
                    }
                }
            };
        }

        if (TryLoadElementAssetImage(element, out var image) && image != null)
        {
            return new Image { Source = image, Stretch = Stretch.UniformToFill };
        }

        return null;
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

        return points.Select(local => RotatePoint(local, new Point(0, 0), (elem.Transform.Rotation - 90) * Math.PI / 180.0))
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
