using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PaintPower.ProjectSystem;

namespace PaintPower.Editors.Views;

public partial class ProjectSettingsView : UserControl
{
    private PaintProject _project => PaintProject.Current;
    public ProjectSettingsView()
    {
        InitializeComponent();
    }
}