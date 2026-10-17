using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PaintPower.ProjectSystem;

namespace PaintPower.Editors.Views;

public partial class ProjectSettingsView : UserControl
{
    public static PaintProject? _project;
    private ProjectMetadata Metadata => _project?.Metadata
        ?? throw new InvalidOperationException("Project settings are not initialized.");

    public string ProjectName { get => Metadata.name; set => Metadata.name = value; }
    public string ProjectVersion { get => Metadata.ProjectVersion; set => Metadata.ProjectVersion = value; }
    public string Author { get => Metadata.Author; set => Metadata.Author = value; }
    public string Description { get => Metadata.Description; set => Metadata.Description = value; }
    private string _paintPowerVersion { get => Metadata.PaintPowerVersion; set => Metadata.PaintPowerVersion = value; }
    public string PaintScriptVersion { get => Metadata.PaintScriptVersion; set => Metadata.PaintScriptVersion = value; }
    public int StageWidth { get => (int)(Metadata.StageWidth ?? 640); set { Metadata.StageWidth = (value < 1001 && value > 99) ? value : 640; } }
    public int StageHeight { get => (int)(Metadata.StageHeight ?? 450); set { Metadata.StageHeight = (value < 1001 && value > 99) ? value : 450; } }

    public ProjectSettingsView()
    {
        InitializeComponent();
    }

    public void SetProject(PaintProject project)
    {
        _project = project;
        DataContext = this;
    }

    private void ResetStageSize(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        StageWidth = 640;
        StageHeight = 450;
    }
}