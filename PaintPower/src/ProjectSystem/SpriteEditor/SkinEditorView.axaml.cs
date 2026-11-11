using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Toolbox.Display.DisplayIntegration;
using Toolbox.Logging;
using PaintPower.ProjectSystem;
using PaintPower.FileExplorer;
using Toolbox.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Toolbox.Plumbing;

namespace PaintPower.ProjectSystem.SpriteEditor;

public partial class SkinEditorView : SpriteEditor
{
    public event Action? SkinChanged;

    private double _stageWidth = 640;
    private double _stageHeight = 450;

    public double StageWidth => _stageWidth;
    public double StageHeight => _stageHeight;

    private readonly PaintSprite _sprite;
    private readonly SkinDefinition _skin;

    // Viewport state
    private double _zoom = 1.0;

    private SkinElement? _selectedElement;
    private bool _suppressPropertyEvents = false;

    // Gizmos
    private enum GizmoMode
    {
        None,
        MoveHorizontal,
        Rotate,
        ScaleTopLeft,
        ScaleTopRight,
        ScaleBottomLeft,
        ScaleBottomRight
    }
    private GizmoMode _gizmoMode = GizmoMode.None;

    private const double HandleSize = 10;
    private const double RotationHandleDistance = 40;

    private bool _draggingGizmo = false;
    private bool _movingElement = false;
    private Point _gizmoStartMouse;
    private double _moveStartX;
    private double _moveStartY;
    private double _gestureStartX;
    private double _gestureStartY;
    private double _gestureStartRotation;
    private double _gestureStartScaleX;
    private double _gestureStartScaleY;
    private double _gestureBaseWidth;
    private double _gestureBaseHeight;

    private double GetCenterX() => _stageWidth / 2.0;

    private double GetCenterY() => _stageHeight / 2.0;


    // Manual drag from asset list
    private bool _draggingAsset = false;
    private string? _dragAssetPath;
    private Border? _dragGhost;
    private bool _syncingZoomSlider;
    private bool _isCompactLayout;
    private bool? _isShortCompactLayout;

    private record HistoryAction(Action Undo, Action Redo);

    private readonly Stack<HistoryAction> _undoStack = new();
    private readonly Stack<HistoryAction> _redoStack = new();

    public SkinEditorView(PaintSprite sprite, SkinDefinition skin, double stageWidth, double stageHeight)
    {
        InitializeComponent();
        DataContext = this;

        _stageWidth = stageWidth;
        _stageHeight = stageHeight;

        Viewport.Width = stageWidth;
        Viewport.Height = stageHeight;

        _sprite = sprite;
        _skin = skin;

        AssetExplorer.Initialize(_sprite.ItemsFolder, true);
        AssetExplorer.FileDragStarted += OnAssetPointerPressed;
        ZoomSlider.ValueChanged += (_, e) =>
        {
            if (!_syncingZoomSlider)
                SetZoom(e.NewValue);
        };

        UpdateViewportZoom();

        NameBox.LostFocus += OnPropertyChanged;
        XBox.LostFocus += OnPropertyChanged;
        YBox.LostFocus += OnPropertyChanged;
        RotationBox.LostFocus += OnPropertyChanged;
        ScaleXBox.LostFocus += OnPropertyChanged;
        ScaleYBox.LostFocus += OnPropertyChanged;
        ZBox.LostFocus += OnPropertyChanged;

        Root.Focusable = true;
        Root.AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        Viewport.AttachedToVisualTree += (_, __) =>
            Dispatcher.UIThread.Post(Redraw, DispatcherPriority.Render);
        Viewport.SizeChanged += (_, e) =>
        {
            if (e.NewSize.Width > 0 && e.NewSize.Height > 0)
                Redraw();
        };
        MainLayout.SizeChanged += (_, e) => UpdateResponsiveLayout(e.NewSize.Width, e.NewSize.Height);

        RefreshElementList();
        UpdateHistoryButtons();
        Redraw();
        InvalidateVisual();
    }

    private void UpdateResponsiveLayout(double width, double height)
    {
        var compact = width < 850;
        var shortCompact = height < 520;

        if (_isCompactLayout == compact &&
            (!compact || _isShortCompactLayout == shortCompact))
            return;

        _isCompactLayout = compact;
        _isShortCompactLayout = shortCompact;

        if (compact)
        {
            MainLayout.ColumnDefinitions = ColumnDefinitions.Parse("*,*");
            MainLayout.RowDefinitions = RowDefinitions.Parse(shortCompact ? "*,160" : "*,220");

            Grid.SetRow(ViewportScrollViewer, 0);
            Grid.SetColumn(ViewportScrollViewer, 0);
            Grid.SetColumnSpan(ViewportScrollViewer, 2);

            Grid.SetRow(ElementsPanel, 1);
            Grid.SetColumn(ElementsPanel, 0);
            Grid.SetRow(AssetsPanel, 1);
            Grid.SetColumn(AssetsPanel, 1);

            LeftSplitter.IsVisible = false;
            RightSplitter.IsVisible = false;
        }
        else
        {
            MainLayout.ColumnDefinitions = ColumnDefinitions.Parse("260,5,*,5,260");
            MainLayout.RowDefinitions = RowDefinitions.Parse("*");

            Grid.SetRow(ViewportScrollViewer, 0);
            Grid.SetColumn(ViewportScrollViewer, 2);
            Grid.SetColumnSpan(ViewportScrollViewer, 1);

            Grid.SetRow(ElementsPanel, 0);
            Grid.SetColumn(ElementsPanel, 0);
            Grid.SetRow(AssetsPanel, 0);
            Grid.SetColumn(AssetsPanel, 4);

            LeftSplitter.IsVisible = true;
            RightSplitter.IsVisible = true;
        }
    }

    private void PushHistory(Action undo, Action redo)
    {
        _undoStack.Push(new HistoryAction(undo, redo));
        _redoStack.Clear();
        UpdateHistoryButtons();
    }

    private void UpdateHistoryButtons()
    {
        UndoButton.IsEnabled = _undoStack.Count > 0;
        RedoButton.IsEnabled = _redoStack.Count > 0;
    }

    private void OnUndoClick(object? sender, RoutedEventArgs e) => Undo();

    private void OnRedoClick(object? sender, RoutedEventArgs e) => Redo();

    private void Undo()
    {
        if (_undoStack.Count == 0)
            return;

        var action = _undoStack.Pop();
        action.Undo();

        _redoStack.Push(action);
        RefreshElementList();
        LoadPropertiesFromElement();
        SaveSkinChanges();
        Redraw();
        UpdateHistoryButtons();
    }

    private void Redo()
    {
        if (_redoStack.Count == 0)
            return;

        var action = _redoStack.Pop();
        action.Redo();

        _undoStack.Push(action);
        RefreshElementList();
        LoadPropertiesFromElement();
        SaveSkinChanges();
        Redraw();
        UpdateHistoryButtons();
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
                undo: () =>
                {
                    _skin.Elements.Add(deleted);
                    _selectedElement = deleted;
                },
                redo: () =>
                {
                    _skin.Elements.Remove(deleted);
                    _selectedElement = null;
                }
            );

            _selectedElement = null;

            SaveSkinChanges();
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

        Viewport.RenderTransform = new ScaleTransform(_zoom, _zoom);
        Viewport.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Relative);

        // This function creates the skin, this gets passed to render overlays
        // to be drawn after the under overlays.

        object? renderSkin(DrawingContextWrapper ctx)
        {
            if (Viewport == null)
                return null;

            Viewport.Children.Clear();

            ctx.Clear(Colors.Transparent);

            foreach (var elem in _skin.Elements.OrderBy(element => element.ZIndex))
            {
                if (elem == null)
                    continue;

                var (w, h) = GetElementDisplaySize(elem);

                double sx = GetCenterX() + elem.Transform.X;
                double sy = GetCenterY() + elem.Transform.Y;

                var screenW = w;
                var screenH = h;
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

        ctx.DrawRect(cx + screenW / 2, cy - 1, RotationHandleDistance, 2, Colors.Cyan, 1);
        foreach (var handle in handles)
            DrawHandle(ctx, handle.X, handle.Y);
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

        if (IsInsideHandle(mouse, handles[5].X, handles[5].Y))
            return GizmoMode.MoveHorizontal;

        if (IsInsideHandle(mouse, handles[0].X, handles[0].Y)) return GizmoMode.ScaleTopLeft;
        if (IsInsideHandle(mouse, handles[1].X, handles[1].Y)) return GizmoMode.ScaleTopRight;
        if (IsInsideHandle(mouse, handles[2].X, handles[2].Y)) return GizmoMode.ScaleBottomLeft;
        if (IsInsideHandle(mouse, handles[3].X, handles[3].Y)) return GizmoMode.ScaleBottomRight;

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

        double cx = GetCenterX();
        double cy = GetCenterY();

        ctx.DrawRect(cx - size, cy - 1, size * 2, 2, Colors.Blue, 2);
        ctx.DrawRect(cx - 1, cy - size, 2, size * 2, Colors.Blue, 2);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete || e.Key == Key.Back)
        {
            if (e.Source is TextBox)
            {
                SaveSkinChanges();
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

        if (!e.GetCurrentPoint(Viewport).Properties.IsLeftButtonPressed)
            return;

        if (_selectedElement != null)
        {
            var gizmo = HitTestGizmo(pos, _selectedElement);
            if (gizmo != GizmoMode.None)
            {
                _gizmoMode = gizmo;
                _draggingGizmo = true;
                _gizmoStartMouse = pos;
                BeginTransformGesture(_selectedElement);
                e.Pointer.Capture(Viewport);
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
            BeginTransformGesture(_selectedElement);
            e.Pointer.Capture(Viewport);
            LoadPropertiesFromElement();
            Redraw();
            return;
        }

        _selectedElement = null;
        LoadPropertiesFromElement();
        Redraw();
    }

    private void OnPointerMove(object? sender, PointerEventArgs e)
    {
        if (Viewport == null || !e.GetCurrentPoint(Viewport).Properties.IsLeftButtonPressed)
            return;

        var pos = e.GetPosition(Viewport);

        if (_draggingGizmo && _selectedElement != null)
        {
            double cx = GetCenterX() + _gestureStartX;
            double cy = GetCenterY() + _gestureStartY;
            double rotationRadians = (_gestureStartRotation - 90) * Math.PI / 180.0;

            if (_gizmoMode == GizmoMode.Rotate)
            {
                double angle1 = Math.Atan2(_gizmoStartMouse.Y - cy, _gizmoStartMouse.X - cx);
                double angle2 = Math.Atan2(pos.Y - cy, pos.X - cx);
                double delta = Math.Atan2(Math.Sin(angle2 - angle1), Math.Cos(angle2 - angle1));
                _selectedElement.Transform.Rotation =
                    _gestureStartRotation + delta * 180.0 / Math.PI;
            }
            else if (_gizmoMode == GizmoMode.MoveHorizontal)
            {
                double limitX = (_stageWidth / 2) + 500;
                _selectedElement.Transform.X = Math.Clamp(
                    _gestureStartX + pos.X - _gizmoStartMouse.X, -limitX, limitX);
            }
            else
            {
                var worldDelta = new Point(pos.X - _gizmoStartMouse.X, pos.Y - _gizmoStartMouse.Y);
                var localDelta = RotatePoint(worldDelta, new Point(0, 0), -rotationRadians);

                double scaleDirectionX = _gizmoMode is GizmoMode.ScaleTopLeft or GizmoMode.ScaleBottomLeft ? -1 : 1;
                double scaleDirectionY = _gizmoMode is GizmoMode.ScaleTopLeft or GizmoMode.ScaleTopRight ? -1 : 1;

                _selectedElement.Transform.ScaleX = Math.Max(
                    0.1, _gestureStartScaleX + localDelta.X * scaleDirectionX / _gestureBaseWidth);
                _selectedElement.Transform.ScaleY = Math.Max(
                    0.1, _gestureStartScaleY + localDelta.Y * scaleDirectionY / _gestureBaseHeight);
            }

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

            double newX = Math.Clamp(_moveStartX + dx, -limitX, limitX);
            double newY = Math.Clamp(_moveStartY + dy, -limitY, limitY);
            _selectedElement.Transform.X = newX;
            _selectedElement.Transform.Y = newY;
            LoadPropertiesFromElement();
            Redraw();
            return;
        }
    }

    private void BeginTransformGesture(SkinElement element)
    {
        _gestureStartX = element.Transform.X;
        _gestureStartY = element.Transform.Y;
        _gestureStartRotation = element.Transform.Rotation;
        _gestureStartScaleX = element.Transform.ScaleX;
        _gestureStartScaleY = element.Transform.ScaleY;
        var (width, height) = GetElementDisplaySize(element);
        _gestureBaseWidth = width / Math.Max(0.1, _gestureStartScaleX);
        _gestureBaseHeight = height / Math.Max(0.1, _gestureStartScaleY);
    }

    private void OnPointerUp(object? sender, PointerReleasedEventArgs e)
    {
        if ((_draggingGizmo || _movingElement) && _selectedElement != null)
        {
            var element = _selectedElement;
            double startX = _gestureStartX;
            double startY = _gestureStartY;
            double startRotation = _gestureStartRotation;
            double startScaleX = _gestureStartScaleX;
            double startScaleY = _gestureStartScaleY;
            double endX = element.Transform.X;
            double endY = element.Transform.Y;
            double endRotation = element.Transform.Rotation;
            double endScaleX = element.Transform.ScaleX;
            double endScaleY = element.Transform.ScaleY;

            if (endX != _gestureStartX ||
                endY != _gestureStartY ||
                endRotation != _gestureStartRotation ||
                endScaleX != _gestureStartScaleX ||
                endScaleY != _gestureStartScaleY)
            {
                PushHistory(
                    undo: () => SetTransform(element, startX, startY, startRotation, startScaleX, startScaleY),
                    redo: () => SetTransform(element, endX, endY, endRotation, endScaleX, endScaleY));
                SaveSkinChanges();
                LoadPropertiesFromElement();
                Redraw();
            }
        }

        _draggingGizmo = false;
        _movingElement = false;
        _gizmoMode = GizmoMode.None;
        e.Pointer.Capture(null);
    }

    private static void SetTransform(SkinElement element, double x, double y, double rotation, double scaleX, double scaleY)
    {
        element.Transform.X = x;
        element.Transform.Y = y;
        element.Transform.Rotation = rotation;
        element.Transform.ScaleX = scaleX;
        element.Transform.ScaleY = scaleY;
    }

    // ---------------------------------------------------------
    // Root-level pointer for ghost drag
    // ---------------------------------------------------------
    private void OnRootPointerMove(object? sender, PointerEventArgs e)
    {
        if (!_draggingAsset || _dragGhost == null)
            return;

        var pos = e.GetPosition(Root);
        Canvas.SetLeft(_dragGhost, pos.X - _dragGhost.Width / 2);
        Canvas.SetTop(_dragGhost, pos.Y - _dragGhost.Height / 2);
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
        if (_dragAssetPath != null && Viewport != null)
        {
            var pos = e.GetPosition(Viewport);
            var bounds = new Rect(0, 0, Viewport.Bounds.Width, Viewport.Bounds.Height);

            if (bounds.Contains(pos))
            {
                AddAssetToSkin(_dragAssetPath, pos);
            }
        }

        e.Pointer.Capture(null);
        _draggingAsset = false;
        _dragAssetPath = null;
    }

    // ---------------------------------------------------------
    // Zoom
    // ---------------------------------------------------------
    private void OnZoom(object? sender, PointerWheelEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
            return;

        double delta = e.Delta.Y > 0 ? 1.1 : 0.9;
        SetZoom(_zoom * delta);
        e.Handled = true;
    }

    private void OnZoomIn(object? sender, RoutedEventArgs e) => SetZoom(_zoom * 1.1);

    private void OnZoomOut(object? sender, RoutedEventArgs e) => SetZoom(_zoom / 1.1);

    private void SetZoom(double zoom)
    {
        _zoom = Math.Clamp(zoom, 0.5, 3.0);
        if (Math.Abs(ZoomSlider.Value - _zoom) > 0.001)
        {
            _syncingZoomSlider = true;
            ZoomSlider.Value = _zoom;
            _syncingZoomSlider = false;
        }

        ZoomLabel.Text = $"{_zoom:P0}";
        UpdateViewportZoom();
        Redraw();
    }

    private void UpdateViewportZoom()
    {
        ViewportBorder.Width = _stageWidth * _zoom;
        ViewportBorder.Height = _stageHeight * _zoom;
        Viewport.RenderTransform = new ScaleTransform(_zoom, _zoom);
        Viewport.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Relative);
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
            var cx = GetCenterX() + (elem.Transform.X);
            var cy = GetCenterY() + (elem.Transform.Y);
            var local = RotatePoint(
                new Point(mouse.X - cx, mouse.Y - cy),
                new Point(0, 0),
                -(elem.Transform.Rotation - 90) * Math.PI / 180.0);

            if (Math.Abs(local.X) <= w / 2 && Math.Abs(local.Y) <= h / 2)
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
        var selected = _selectedElement;
        if (ElementsList.ItemsSource != null) ElementsList.ItemsSource = null;
        ElementsList.ItemsSource = _skin.Elements;
        ElementsList.SelectedItem = selected != null && _skin.Elements.Contains(selected) ? selected : null;
    }

    private void OnElementSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (ElementsList.SelectedItem is not SkinElement elem)
        {
            _selectedElement = null;
            return;
        }

        _selectedElement = _skin.Elements.Contains(elem) ? elem : null;

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

        SaveSkinChanges();
    }

    private void SaveSkinChanges()
    {
        _sprite.SaveSkins();
        SkinChanged?.Invoke();
    }

    // ---------------------------------------------------------
    // Asset list + placement
    // ---------------------------------------------------------
    private void OnAssetPointerPressed(string fullPath, PointerPressedEventArgs e)
    {
        if (!File.Exists(fullPath) || !e.GetCurrentPoint(AssetExplorer).Properties.IsLeftButtonPressed)
            return;

        _dragAssetPath = fullPath;
        _draggingAsset = true;

        var image = LoadAssetImage(_dragAssetPath);
        var displayWidth = image is Bitmap bitmap
            ? Math.Max(16, bitmap.PixelSize.Width * 0.5) * _zoom
            : 96 * _zoom;
        var displayHeight = image is Bitmap bitmapForHeight
            ? Math.Max(16, bitmapForHeight.PixelSize.Height * 0.5) * _zoom
            : 96 * _zoom;

        _dragGhost = new Border
        {
            Width = displayWidth,
            Height = displayHeight,
            Background = new SolidColorBrush(Color.FromArgb(180, 30, 30, 30)),
            BorderBrush = Brushes.White,
            BorderThickness = new Thickness(1),
            Child = image != null
                ? new Image { Source = image, Stretch = Stretch.Uniform }
                : new TextBlock
                {
                    Text = Path.GetFileName(fullPath),
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                },
            Opacity = 0.85,
            IsHitTestVisible = false
        };

        Overlay.Children.Add(_dragGhost);

        var pos = e.GetPosition(Root);
        Canvas.SetLeft(_dragGhost, pos.X - displayWidth / 2);
        Canvas.SetTop(_dragGhost, pos.Y - displayHeight / 2);
        e.Pointer.Capture(Root);
    }

    private void AddAssetToSkin(string fullPath, Point dropPos)
    {
        string relative = Path.GetRelativePath(_sprite.SpriteFolder, fullPath)
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');

        bool isVideo = Path.GetExtension(fullPath).Equals(".mp4", StringComparison.OrdinalIgnoreCase)
            || Path.GetExtension(fullPath).Equals(".webm", StringComparison.OrdinalIgnoreCase);

        double worldX = dropPos.X - _stageWidth / 2.0;
        double worldY = dropPos.Y - _stageHeight / 2.0;

        SkinElement newElem = isVideo
            ? new SkinVideoElement { AssetPath = relative, Loop = true, AutoPlay = true }
            : new SkinImageElement { AssetPath = relative };

        newElem.InstanceName = (isVideo ? "Video" : "Image") + _skin.Elements.Count;
        newElem.Transform = new SkinTransform { X = worldX, Y = worldY };
        newElem.ZIndex = 0;

        var elem = newElem;

        _skin.Elements.Add(elem);

        SaveSkinChanges();

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
        var rotatedHandles = new List<Point>
        {
            new Point(-screenW / 2, -screenH / 2),
            new Point(screenW / 2, -screenH / 2),
            new Point(-screenW / 2, screenH / 2),
            new Point(screenW / 2, screenH / 2),
            new Point(0, -screenH / 2 - rotationHandleDistance)
        };

        var handles = rotatedHandles.Select(local => RotatePoint(local, new Point(0, 0), (elem.Transform.Rotation - 90) * Math.PI / 180.0))
            .Select(rotated => new Point(cx + rotated.X, cy + rotated.Y))
            .ToList();
        handles.Add(new Point(cx + screenW / 2 + rotationHandleDistance, cy));
        return handles;
    }

    private Point RotatePoint(Point point, Point origin, double radians)
    {
        double cos = Math.Cos(radians);
        double sin = Math.Sin(radians);
        double dx = point.X - origin.X;
        double dy = point.Y - origin.Y;

        return new Point(origin.X + (dx * cos - dy * sin), origin.Y + (dx * sin + dy * cos));
    }

    private IImage? LoadAssetImage(string path)
    {
        try
        {
            var asset = GraphicLoader.LoadCached(path);
            return asset is Graphic graphic ? graphic.ToAvaloniaBitmap() : null;
        }
        catch
        {
            return null;
        }
    }
}
