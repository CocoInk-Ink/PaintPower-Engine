using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Folding;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.TextMate;
using PaintPower.FileEditors.Tools.ScriptEditorTools;
using PaintPower.ProjectSystem;
using TextMateSharp.Grammars;
using Toolbox.Accessibility.Translation;
using Toolbox.Plumbing;

namespace PaintPower.FileEditors;

public partial class ScriptEditor : FileEditor
{
	private readonly TempWorkspace _workspace;
	private TextMate.Installation _textMateInstallation;
	private RegistryOptions _registryOptions;

	private RainbowBracketColorizer _bracketColorizer;
	private FoldingManager _foldingManager;
	private CodeFoldingStrategy _foldingStrategy;

	private CompletionWindow _completionWindow;

	private TextEditor _editor;

	// For custom languages that need to be added directly.
	private string[] types = { ".as", ".mxml", ".jsfl", ".pxml", ".pss", ".psf", ".pxs" };

	public ScriptEditor(string relativePath, TempWorkspace workspace)
	{

		InitializeComponent();

		_workspace = workspace;

		var _ = this.FindControl<TextEditor>("Editor");

		if (_ == null)
		{
			throw new Exception("Editor is null!");
		}

		_editor = _;

		_ = null; GC.Collect();

		// Add bracket colorizer
		_bracketColorizer = new RainbowBracketColorizer(_editor.Document);
		_editor.TextArea.TextView.LineTransformers.Add(_bracketColorizer);

		// Add code folding support
		_foldingManager = FoldingManager.Install(_editor.TextArea);
		_foldingStrategy = new CodeFoldingStrategy();
		_foldingStrategy.UpdateFoldings(_foldingManager, _editor.Document);

		_editor.Text = _workspace.LoadText(FullPath);
		_editor.Focus();

		// 1. Create registry options with a theme
		_registryOptions = new RegistryOptions(ThemeName.LightPlus);
		var manager = new GrammarManager();

		// 2. Install TextMate
		_textMateInstallation = _editor.InstallTextMate(_registryOptions);

		// 3. Set theme
		_textMateInstallation.SetTheme(_registryOptions.LoadTheme(ThemeName.DarkPlus));

		SetGrammarFromExtension(Path.GetExtension(relativePath), _editor);

		// Autosave
		_editor.TextChanged += (_, __) =>
		{
			MarkDirty();

			_bracketColorizer.Update(_editor.Document);
			_editor.TextArea.TextView.InvalidateVisual();

			_foldingStrategy.UpdateFoldings(_foldingManager, _editor.Document);
		};

		_editor.TextArea.TextEntered += (sender, e) =>
		{
			_completionWindow = new CompletionWindow(_editor.TextArea);
			_completionWindow.Closed += (o, args) => _completionWindow = null;

			var data = _completionWindow.CompletionList.CompletionData;
			if (e.Text == ".")
			{
				data.Add(new MyCompletionData("ToString"));
				data.Add(new MyCompletionData("GetHashCode"));

				_completionWindow.Show();
			}

			if (e.Text == "@")
			{
				data.Add(new MyCompletionData("Start"));
				data.Add(new MyCompletionData("KeyDown"));
				data.Add(new MyCompletionData("KeyUp"));
				data.Add(new MyCompletionData("MouseDown"));
				data.Add(new MyCompletionData("MouseUp"));
				data.Add(new MyCompletionData("Click"));
				data.Add(new MyCompletionData("receive"));

				_completionWindow.Show();
			}

			if (e.Text == "#")
			{
				data.Add(new MyCompletionData("Sprite"));
				data.Add(new MyCompletionData("PaintScript 0.1"));
				data.Add(new MyCompletionData("PaintScript 0.2"));

				_completionWindow.Show();
			}
		};

	}

	public class MyCompletionData : ICompletionData
	{
		public MyCompletionData(string text) => Text = text;

		public IImage Image => null;
		public string Text { get; }
		public object Content => Text;
		public object Description => "Description for " + Text;
		public double Priority => 0;

		public void Complete(TextArea textArea, ISegment segment, EventArgs e)
		{
			textArea.Document.Replace(segment, Text);
		}
	}


	public void SetGrammarFromExtension(string ext, TextEditor editor)
	{
		// 4. Set grammar based on file extension
		var scope = _registryOptions.GetScopeByExtension(ext);

		// Add custom language support like .pxml, .pxs, .pss, etc.
		// For now, because we don't have TextMate grammars for those, we'll just use html, css, js, java, and C# grammar as a placeholder.

		// .pxml -> xml
		// .pss, psf -> css
		// .pxs -> PaintScript (new)
		if (ext == ".Coco" || ext == ".coco")
			scope = _registryOptions.GetScopeByExtension(".cs");
		else if (ext == ".CocoScript" || ext == ".cocoscript")
			scope = _registryOptions.GetScopeByExtension(".js");
		else if (ext == ".pxml")
			scope = _registryOptions.GetScopeByExtension(".xml");
		if (ext == ".psf")
			scope = _registryOptions.GetScopeByExtension(".css");
		else if (ext == ".pss")
			scope = _registryOptions.GetScopeByExtension(".css");
		else if (ext == ".jsfl")
			scope = _registryOptions.GetScopeByExtension(".js");
		else if (ext == ".asc")
			scope = _registryOptions.GetScopeByExtension(".js");

		if (scope != null)
		{
			_textMateInstallation.SetGrammar(scope);
		}
		else if (types.Contains(ext))
		{
			var assetPipe = Plumber.MainPlumber.AssetPipe;
			var grammarPath = assetPipe.LoadAsset($"Grammars/{GetGrammarFile(ext)}");

			_textMateInstallation.SetGrammarFile(grammarPath);
		}
		else
		{
			Debug.WriteLine($"No TextMate grammar found for extension: {ext}");
		}

		// 5. Remove AvaloniaEdit built-in highlighting (must NOT be used)
		editor.SyntaxHighlighting = null;
	}

	private string GetGrammarFile(string ext)
	{
		return ext switch
		{
			".as" => "AS3",
			".as2" => "AS3",
			".as3" => "AS3",
			".mxml" => "MXML",
			".pxs" => "PaintScript",
			_ => throw new Exception("Should not happen! Error loading grammar in script editor!")
		} + ".tmLanguage.json";
	}

	override public void Save()
	{
		_workspace.SaveFile(FullPath, _editor.Text);
	}

	public override void Activate()
	{
		_editor.Text = _workspace.LoadText(FullPath);
	}

	private void SaveButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
	{
		Save();
	}
}