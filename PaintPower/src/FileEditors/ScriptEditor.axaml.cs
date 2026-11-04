using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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
using PaintPower.FileEditors.Tools.ScriptEditorTools.SafeSyntaxTools;
using PaintPower.ProjectSystem;
using TextMateSharp.Grammars;
using Toolbox.Accessibility.Translation;
using Toolbox.Plumbing;
using PaintPower.Dialogs;

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
	private SafeSyntaxView _safeSyntaxView;
	private bool _safeSyntaxPanelOpen;
	private double _safeSyntaxPanelWidth = 240;

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
		_safeSyntaxView = this.FindControl<SafeSyntaxView>("SafeSyntaxView")
			?? throw new InvalidOperationException("Safe syntax panel is missing.");

		var isPaintScript = string.Equals(
			Path.GetExtension(relativePath),
			".pxs",
			StringComparison.OrdinalIgnoreCase);
		var safeSyntaxToggle = this.FindControl<CheckBox>("SafeSyntaxToggle")
			?? throw new InvalidOperationException("Safe syntax toggle is missing.");
		var syntaxPanelButton = this.FindControl<Button>("ToggleSyntaxPanelButton")
			?? throw new InvalidOperationException("Safe syntax panel button is missing.");
		var syntaxSplitter = this.FindControl<GridSplitter>("SyntaxSplitter")
			?? throw new InvalidOperationException("Safe syntax splitter is missing.");
		var editorLayout = this.FindControl<Grid>("EditorLayout")
			?? throw new InvalidOperationException("Editor layout is missing.");

		safeSyntaxToggle.IsEnabled = isPaintScript;
		syntaxPanelButton.IsEnabled = isPaintScript;
		_safeSyntaxPanelOpen = isPaintScript;
		_safeSyntaxView.IsVisible = isPaintScript;
		syntaxSplitter.IsVisible = isPaintScript;
		editorLayout.ColumnDefinitions[2].Width = new GridLength(isPaintScript ? _safeSyntaxPanelWidth : 0);

		_safeSyntaxView.SnippetRequested += InsertSafeSyntaxSnippet;
		_safeSyntaxView.VariableRequested += AddSafeSyntaxVariable;
		_safeSyntaxView.RefreshVariables(string.Empty);
		_editor.TextArea.TextView.PointerReleased += EditorPointerReleased;
		DragDrop.SetAllowDrop(_editor, true);
		_editor.AddHandler(DragDrop.DragOverEvent, EditorDragOver, RoutingStrategies.Bubble);
		_editor.AddHandler(DragDrop.DropEvent, EditorDrop, RoutingStrategies.Bubble);

		_ = null; GC.Collect();

		// Add bracket colorizer
		_bracketColorizer = new RainbowBracketColorizer(_editor.Document);
		_editor.TextArea.TextView.LineTransformers.Add(_bracketColorizer);

		// Add code folding support
		_foldingManager = FoldingManager.Install(_editor.TextArea);
		_foldingStrategy = new CodeFoldingStrategy();
		_foldingStrategy.UpdateFoldings(_foldingManager, _editor.Document);

		_editor.Text = _workspace.LoadText(FullPath);
		_safeSyntaxView.RefreshVariables(_editor.Text);
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
			_safeSyntaxView.RefreshVariables(_editor.Text);

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
		_safeSyntaxView.RefreshVariables(_editor.Text);
	}

	private void SaveButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
	{
		Save();
	}

	private void SafeSyntaxToggled(object? sender, RoutedEventArgs e)
	{
		var toggle = this.FindControl<CheckBox>("SafeSyntaxToggle");
		if (toggle != null)
			_editor.IsReadOnly = toggle.IsChecked == true;
	}

	private void ToggleSyntaxPanel(object? sender, RoutedEventArgs e)
	{
		var editorLayout = this.FindControl<Grid>("EditorLayout")
			?? throw new InvalidOperationException("Editor layout is missing.");
		var panel = this.FindControl<SafeSyntaxView>("SafeSyntaxView")
			?? throw new InvalidOperationException("Safe syntax panel is missing.");
		var splitter = this.FindControl<GridSplitter>("SyntaxSplitter")
			?? throw new InvalidOperationException("Safe syntax splitter is missing.");
		var button = this.FindControl<Button>("ToggleSyntaxPanelButton")
			?? throw new InvalidOperationException("Safe syntax panel button is missing.");
		var syntaxColumn = editorLayout.ColumnDefinitions[2];

		if (_safeSyntaxPanelOpen && syntaxColumn.ActualWidth > 0)
		{
			_safeSyntaxPanelWidth = Math.Max(180, syntaxColumn.ActualWidth);
			_safeSyntaxPanelOpen = false;
			syntaxColumn.Width = new GridLength(0);
			panel.IsVisible = false;
			splitter.IsVisible = false;
			button.Content = "Show syntax panel";
		}
		else
		{
			_safeSyntaxPanelOpen = true;
			syntaxColumn.Width = new GridLength(_safeSyntaxPanelWidth);
			panel.IsVisible = true;
			splitter.IsVisible = true;
			button.Content = "Hide syntax panel";
		}
	}

	private async void InsertSafeSyntaxSnippet(string snippet, bool topLevel)
	{
		if (topLevel)
		{
			var declaration = Regex.Match(snippet, @"\bfunction\s+([A-Za-z_][A-Za-z0-9_]*)");
			if (declaration.Success)
			{
				var nameGroup = declaration.Groups[1];
				var functionName = nameGroup.Value;
				var suffix = 2;
				while (_safeSyntaxView.HasVariable(functionName) || _safeSyntaxView.HasFunction(functionName))
					functionName = $"{nameGroup.Value}{suffix++}";

				if (functionName != nameGroup.Value)
					snippet = snippet.Remove(nameGroup.Index, nameGroup.Length)
						.Insert(nameGroup.Index, functionName);
			}
		}

		var offset = topLevel
			? _safeSyntaxView.GetTopLevelInsertionOffset(_editor.Text)
			: _editor.TextArea.Caret.Offset;
		if (offset < 0)
		{
			await ShowSafeSyntaxError("Add a PaintScript version and sprite header before inserting events, functions, or global variables.");
			return;
		}

		if (!topLevel && _safeSyntaxView.GetBlockDepth(_editor.Text, offset) <= 0)
		{
			await ShowSafeSyntaxError("Place the caret inside an event or function block before adding a statement or local variable.");
			return;
		}

		if (!topLevel && !IsBlankInsertionLine(_editor.Text, offset))
		{
			await ShowSafeSyntaxError("Place the caret on an empty line so the new statement does not split existing code.");
			return;
		}

		var insertion = offset > 0 && _editor.Text[offset - 1] != '\n'
			? "\n" + snippet
			: snippet;
		_editor.Document.Insert(offset, insertion);
		_editor.TextArea.Caret.Offset = offset + insertion.Length;
		_editor.TextArea.Focus();
	}

	private static bool IsBlankInsertionLine(string text, int offset)
	{
		offset = Math.Clamp(offset, 0, text.Length);
		var lineStart = offset == 0 ? 0 : text.LastIndexOf('\n', offset - 1) + 1;
		var lineEnd = text.IndexOf('\n', offset);
		if (lineEnd < 0)
			lineEnd = text.Length;

		for (var i = lineStart; i < lineEnd; i++)
		{
			if (!char.IsWhiteSpace(text[i]))
				return false;
		}

		return true;
	}

	private void EditorDragOver(object? sender, DragEventArgs e)
	{
		e.DragEffects = e.DataTransfer.Contains(SafeSyntaxView.SnippetDataFormat)
			? DragDropEffects.Copy
			: DragDropEffects.None;
	}

	private void EditorDrop(object? sender, DragEventArgs e)
	{
		if (e.DataTransfer.TryGetValue(SafeSyntaxView.SnippetDataFormat) is not string snippet
			|| e.DataTransfer.TryGetValue(SafeSyntaxView.SnippetContextDataFormat) is not string context
			|| (context != "top-level" && context != "statement"))
		{
			return;
		}

		e.Handled = true;
		InsertSafeSyntaxSnippet(snippet, context == "top-level");
	}

	private async void AddSafeSyntaxVariable(string name, string type, string scope)
	{
		if (!Regex.IsMatch(name, @"^[A-Za-z_][A-Za-z0-9_]*$"))
		{
			await ShowSafeSyntaxError("Variable names must start with a letter or underscore and contain only letters, numbers, or underscores.");
			return;
		}

		if (!new[] { "Number", "String", "Boolean", "*" }.Contains(type))
		{
			await ShowSafeSyntaxError("Choose a supported PaintScript variable type.");
			return;
		}

		if (!new[] { "local", "public", "private", "global", "const" }.Contains(scope))
		{
			await ShowSafeSyntaxError("Choose a supported PaintScript variable scope.");
			return;
		}

		if (_safeSyntaxView.HasVariable(name) || _safeSyntaxView.HasFunction(name))
		{
			await ShowSafeSyntaxError($"A symbol named '{name}' is already declared.");
			return;
		}

		var declaration = scope switch
		{
			"local" => $"var {name}:{type};\n",
			"const" => $"const {name}:{type};\n",
			_ => $"{scope} var {name}:{type};\n"
		};
		InsertSafeSyntaxSnippet(declaration, scope != "local");
	}

	private async void EditorPointerReleased(object? sender, PointerReleasedEventArgs e)
	{
		if (this.FindControl<CheckBox>("SafeSyntaxToggle")?.IsChecked != true)
			return;

		await System.Threading.Tasks.Task.Yield();
		var name = GetIdentifierAtOffset(_editor.Text, _editor.TextArea.Caret.Offset);
		if (name == null
			|| (!_safeSyntaxView.HasVariable(name) && !_safeSyntaxView.HasFunction(name)))
			return;

		if (_safeSyntaxView.HasAmbiguousVariable(name)
			|| _safeSyntaxView.HasAmbiguousFunction(name)
			|| (_safeSyntaxView.HasVariable(name) && _safeSyntaxView.HasFunction(name)))
		{
			await ShowSafeSyntaxError($"'{name}' is declared more than once, so the editor cannot safely distinguish which declaration to rename.");
			return;
		}

		if (TopLevel.GetTopLevel(this) is not Window owner)
			throw new InvalidOperationException("The script editor must be hosted in a window to rename variables.");

		var dialog = new InputDialog("Rename variable", $"Enter a new name for '{name}'.", name);
		var replacement = await dialog.ShowAsync(owner);
		if (string.IsNullOrWhiteSpace(replacement) || replacement == name)
			return;

		replacement = replacement.Trim();
		if (!Regex.IsMatch(replacement, @"^[A-Za-z_][A-Za-z0-9_]*$"))
		{
			await ShowSafeSyntaxError("Variable names must start with a letter or underscore and contain only letters, numbers, or underscores.");
			return;
		}

		if (_safeSyntaxView.HasVariable(replacement) || _safeSyntaxView.HasFunction(replacement))
		{
			await ShowSafeSyntaxError($"A variable named '{replacement}' is already declared.");
			return;
		}

		_editor.Text = ReplaceIdentifier(_editor.Text, name, replacement);
	}

	private static string? GetIdentifierAtOffset(string text, int offset)
	{
		if (text.Length == 0)
			return null;

		offset = Math.Clamp(offset, 0, text.Length);
		if (offset == text.Length || !IsIdentifierCharacter(text[offset]))
		{
			if (offset == 0 || !IsIdentifierCharacter(text[offset - 1]))
				return null;
			offset--;
		}

		var start = offset;
		var end = offset + 1;
		while (start > 0 && IsIdentifierCharacter(text[start - 1]))
			start--;
		while (end < text.Length && IsIdentifierCharacter(text[end]))
			end++;

		return IsIdentifierStart(text[start]) ? text.Substring(start, end - start) : null;
	}

	private static string ReplaceIdentifier(string text, string oldName, string newName)
	{
		var result = new StringBuilder(text.Length);
		for (var i = 0; i < text.Length;)
		{
			if (text[i] == '/' && i + 1 < text.Length && text[i + 1] == '/')
			{
				var end = text.IndexOf('\n', i);
				if (end < 0)
					end = text.Length;
				result.Append(text, i, end - i);
				i = end;
				continue;
			}

			if (text[i] == '/' && i + 1 < text.Length && text[i + 1] == '*')
			{
				var end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
				end = end < 0 ? text.Length : end + 2;
				result.Append(text, i, end - i);
				i = end;
				continue;
			}

			if (text[i] == '"' || text[i] == '\'')
			{
				var quote = text[i];
				result.Append(text[i++]);
				while (i < text.Length)
				{
					var current = text[i++];
					result.Append(current);
					if (current == '\\' && i < text.Length)
					{
						result.Append(text[i++]);
						continue;
					}

					if (current == quote)
						break;
				}

				continue;
			}

			if (IsIdentifierStart(text[i]))
			{
				var start = i++;
				while (i < text.Length && IsIdentifierCharacter(text[i]))
					i++;

				var identifier = text.Substring(start, i - start);
				result.Append(string.Equals(identifier, oldName, StringComparison.Ordinal)
					? newName
					: identifier);
				continue;
			}

			result.Append(text[i++]);
		}

		return result.ToString();
	}

	private static bool IsIdentifierStart(char value) =>
		value == '_' || value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';

	private static bool IsIdentifierCharacter(char value) =>
		IsIdentifierStart(value) || value >= '0' && value <= '9';

	private async System.Threading.Tasks.Task ShowSafeSyntaxError(string message)
	{
		if (TopLevel.GetTopLevel(this) is Window owner)
		{
			await ErrorDialog.ShowAsync(owner, message);
			return;
		}

		throw new InvalidOperationException("The script editor must be hosted in a window to show validation errors.");
	}
}