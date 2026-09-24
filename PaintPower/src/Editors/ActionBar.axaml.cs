using System;
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
		var buildButton = DefaultParts.BuildButton;
		var runButton = DefaultParts.RunButton;
		var buildAndRunButton = DefaultParts.BuildAndRunButton;

		buildButton.Classes.Add("white");

		buildButton.Content = Translator.Map("Build");
		runButton.Content = Translator.Map("Run last successful build");
		buildAndRunButton.Content = Translator.Map("Build and Run");

		buildButton.Click += (_, _) => { Log.QuickLog("Build clicked"); };
		runButton.Click += (_, _) => { Log.QuickLog("Run clicked"); };
		buildAndRunButton.Click += (_, _) => { Log.QuickLog("Build and run"); };

		buildButton.Margin = new Thickness(2, 1);
		runButton.Margin = new Thickness(2, 1);
		buildAndRunButton.Margin = new Thickness(2, 1);

		buildButton.AddTranslateHandler(() =>
		{
			buildButton.Content = Translator.Map("Build");
		});

		runButton.AddTranslateHandler(() =>
		{
			runButton.Content = Translator.Map("Run last successful build");
		});

		buildAndRunButton.AddTranslateHandler(() =>
		{
			buildAndRunButton.Content = Translator.Map("Build and Run");
		});

		ActionArea.Children.Add(buildAndRunButton);
		ActionArea.Children.Add(buildButton);
		ActionArea.Children.Add(runButton);
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