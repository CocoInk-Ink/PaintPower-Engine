using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PaintPower.ProjectSystem;

namespace PaintPower.Editors.Views;

public partial class ProjectSettingsView : UserControl, INotifyPropertyChanged
{
    public static PaintProject? _project;
    private ProjectMetadata Metadata => _project?.Metadata
        ?? throw new InvalidOperationException("Project settings are not initialized.");

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ProjectName { get => Metadata.name; set { Metadata.name = value; OnPropertyChanged(); } }
    public string ProjectVersion { get => Metadata.ProjectVersion; set { Metadata.ProjectVersion = value; OnPropertyChanged(); } }
    public string Author { get => Metadata.Author; set { Metadata.Author = value; OnPropertyChanged(); } }
    public string Description { get => Metadata.Description; set { Metadata.Description = value; OnPropertyChanged(); } }
    private string _paintPowerVersion { get => Metadata.PaintPowerVersion; set => Metadata.PaintPowerVersion = value; }
    public string PaintScriptVersion { get => Metadata.PaintScriptVersion; set { Metadata.PaintScriptVersion = value; OnPropertyChanged(); } }
    public int StageWidth { get => (int)(Metadata.StageWidth ?? 640); set { Metadata.StageWidth = (value < 1001 && value > 99) ? value : 640; OnPropertyChanged(); } }
    public int StageHeight { get => (int)(Metadata.StageHeight ?? 450); set { Metadata.StageHeight = (value < 1001 && value > 99) ? value : 450; OnPropertyChanged(); } }

    public ProjectSettingsView()
    {
        InitializeComponent();
    }

    public void SetProject(PaintProject project)
    {
        _project = project; 
        DataContext = this;
        OnPropertyChanged(nameof(ProjectName));
        OnPropertyChanged(nameof(ProjectVersion));
        OnPropertyChanged(nameof(Author));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(PaintScriptVersion));
        OnPropertyChanged(nameof(StageWidth));
        OnPropertyChanged(nameof(StageHeight));
    }

    private void ResetStageSize(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        StageWidth = 640;
        StageHeight = 450;
    }

    private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}