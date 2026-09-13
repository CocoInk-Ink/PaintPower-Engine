// AnimationEditor.axaml.cs

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using PaintPower.FileEditors.Tools.AnimationEditorTools;
using PaintPower.ProjectSystem;
using PaintPower.Tools.Converters;

namespace PaintPower.FileEditors;

public partial class AnimationEditor : FileEditor, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private readonly TempWorkspace _workspace;

    // Simple placeholder model: later replace with real WXA data
    public ObservableCollection<string> Frames { get; } = new();

    private TimelineTool _timeline;
    private PlaybackTool _playback;
    private List<FrameTool> _frameTools = new();
    private LayerManagerTool _layers;

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



    public AnimationEditor(string path, TempWorkspace workspace)
    {
        _workspace = workspace;

        SetRelativePath(path);
        SetFullPath(System.IO.Path.Combine(_workspace.ItemsDir, path));

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

        _layers = new LayerManagerTool();
        _layers.AddLayer("Layer 1");
        _layers.AddLayer("Layer 2");
        _layers.AddLayer("Layer 3");

        SelectedLayer = _layers.Layers[0];

        this.AttachedToVisualTree += (_, _) => OnLoaded();
    }

    private void OnLoaded()
    {
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

            layer.Frames.Add(new LayerFrameTool(c => DrawCircle(c, 50, 50)));
            layer.Frames.Add(new LayerFrameTool(c => DrawCircle(c, 70, 50)));
            layer.Frames.Add(new LayerFrameTool(c => DrawCircle(c, 90, 50)));
            layer.Frames.Add(new LayerFrameTool(c => DrawCircle(c, 110, 50)));
            layer.Frames.Add(new LayerFrameTool(c => DrawCircle(c, 130, 50)));
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

            layer.Frames[index].DrawAction.Invoke(AnimationCanvas);
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

}
