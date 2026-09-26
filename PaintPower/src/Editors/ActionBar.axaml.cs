using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using PaintPower.Templates.FileTemplates;
using Toolbox;
using Toolbox.Accessibility.Translation;
using Toolbox.Logging;

namespace PaintPower.Editors;

public partial class ActionBar : TranslatableControl
{
	public class Parts
	{
		public TranslatableButton BuildButton = new();
		public TranslatableButton RunButton = new();
		public TranslatableButton BuildAndRunButton = new();
	};

	public readonly Parts DefaultParts = new();

	private Controls controls = new();

	public ActionBar()
	{
		InitializeComponent();
		this.AttachedToVisualTree += (_, _) => Init();
	}

	private void Init()
	{
		AddDefaultOptions();
	}

	private void AddDefaultOptions()
	{
		string[] classes = {"white", "blue"};

		DefaultParts.BuildButton = ControlCreator.CreateTranslatableButton("Build", true, (_, _) => { Log.QuickLog("Build clicked"); }, classes);
		DefaultParts.RunButton = ControlCreator.CreateTranslatableButton("Run last successful build", true, (_, _) => { Log.QuickLog("Run clicked"); }, classes);
		DefaultParts.BuildAndRunButton = ControlCreator.CreateTranslatableButton("Build and Run", true, (_, _) => { Log.QuickLog("Build and run"); });

		DefaultParts.BuildButton.Margin = new Thickness(2, 1);
		DefaultParts.RunButton.Margin = new Thickness(2, 1);
		DefaultParts.BuildAndRunButton.Margin = new Thickness(2, 1);

		ActionArea.Children.Add(DefaultParts.BuildButton);
		ActionArea.Children.Add(DefaultParts.RunButton);
		ActionArea.Children.Add(DefaultParts.BuildAndRunButton);
	}

	public void AddControl(Control control)
	{
		controls.Add(control);
	}

	public void Rebuild()
	{
		ActionArea.Children.Clear(); // Clear old stuff.
		AddDefaultOptions();

		foreach (var part in controls)
		{
			ActionArea.Children.Add(part);
		}
	}

	public override void TranslateGUI()
	{
		Translator.LanguageChanged += () =>
		{
			// Not conventional, temporary.
			foreach(var child in ActionArea.Children)
			{
				if (child is TextBlock c) c.Text = Translator.Translate(c.Text ?? "");
			}

			Rebuild();
		};
	}
}