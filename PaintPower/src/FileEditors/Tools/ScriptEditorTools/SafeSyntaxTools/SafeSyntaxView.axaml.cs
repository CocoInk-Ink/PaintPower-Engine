using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace PaintPower.FileEditors.Tools.ScriptEditorTools.SafeSyntaxTools;

public partial class SafeSyntaxView : UserControl
{
	public static readonly DataFormat<string> SnippetDataFormat =
		DataFormat.CreateStringApplicationFormat("xpaint-safe-syntax");
	public static readonly DataFormat<string> SnippetContextDataFormat =
		DataFormat.CreateStringApplicationFormat("xpaint-safe-syntax-context");
	private static readonly Regex VariableDeclaration = new(
		@"\b(?:var|const)\s+([A-Za-z_][A-Za-z0-9_]*)(?:\s*:\s*([A-Za-z_][A-Za-z0-9_]*|\*))?",
		RegexOptions.Compiled);
	private static readonly Regex PaintScriptHeader = new(
		@"(?m)^#PaintScript[^\r\n]*\r?\n#Sprite[^\r\n]*(?:\r?\n|$)",
		RegexOptions.Compiled);
	private static readonly Regex ParameterList = new(
		@"(?:\bfunction\s+[A-Za-z_][A-Za-z0-9_]*|@[A-Za-z_][A-Za-z0-9_]*)\s*\(([^)]*)\)",
		RegexOptions.Compiled);
	private static readonly Regex FunctionDeclaration = new(
		@"\b(?:strict\s+)?function\s+([A-Za-z_][A-Za-z0-9_]*)",
		RegexOptions.Compiled);

	private readonly List<VariableInfo> _variables = new();
	private readonly Dictionary<string, int> _functions = new(StringComparer.Ordinal);
	private string? _suggestedVariableName;

	private static readonly SnippetCategory[] SnippetCategories =
	{
		new("Events",
			new("Start event", "@Start {\n\t\n}\n", true),
			new("Key down event", "@Keydown(key:KeyboardKey) {\n\t\n}\n", true),
			new("Key up event", "@Keyup(key:KeyboardKey) {\n\t\n}\n", true),
			new("Mouse down event", "@MouseDown {\n\t\n}\n", true),
			new("Mouse up event", "@MouseUp {\n\t\n}\n", true),
			new("Click event", "@Click {\n\t\n}\n", true),
			new("Receive message", "@receive Tick {\n\t\n}\n", true)),
		new("Conditions and loops",
			new("If", "if (isEnabled) {\n\t\n}\n"),
			new("If not", "if not (isEnabled) {\n\t\n}\n"),
			new("If / else", "if (isEnabled) {\n\t\n} else {\n\t\n}\n"),
			new("Repeat", "repeat(3) {\n\t\n}\n"),
			new("Repeat until", "repeat until(isEnabled) {\n\t\n}\n"),
			new("While", "while (isEnabled) {\n\t\n}\n"),
			new("Forever", "forever {\n\t\n}\n")),
		new("Functions and actions",
			new("Function", "function functionName() {\n\t\n}\n", true),
			new("Strict function", "strict function functionName() {\n\t\n}\n", true),
			new("Call function", "functionName();\n"),
			new("Call event", "call @refresh;\n"),
			new("Broadcast", "Broadcast Tick;\n"),
			new("Broadcast and wait", "BroadcastAndWait Tick;\n"),
			new("Wait", "wait(1000);\n"),
			new("Wait until", "wait until(isEnabled);\n"),
			new("Say text", "say(\"Hello!\");\n"),
			new("Assign variable", "count = 0;\n"),
			new("Increase variable", "count++;\n"),
			new("Add to variable", "count = count + 1;\n"),
			new("Return", "return;\n"))
	};

	public event Action<string, bool>? SnippetRequested;
	public event Action<string, string, string>? VariableRequested;

	public SafeSyntaxView()
	{
		InitializeComponent();
		BuildSnippetList();
		UpdateSuggestedName();
	}

	public bool HasVariable(string name) =>
		_variables.Any(variable => string.Equals(variable.Name, name, StringComparison.Ordinal));

	public bool HasAmbiguousVariable(string name) =>
		_variables.Any(variable =>
			string.Equals(variable.Name, name, StringComparison.Ordinal)
			&& variable.DeclarationCount > 1);

	public bool HasFunction(string name) => _functions.ContainsKey(name);

	public bool HasAmbiguousFunction(string name) =>
		_functions.TryGetValue(name, out var declarationCount) && declarationCount > 1;

	public int GetTopLevelInsertionOffset(string source)
	{
		var match = PaintScriptHeader.Match(source);
		return match.Success ? match.Index + match.Length : -1;
	}

	public int GetBlockDepth(string source, int offset)
	{
		offset = Math.Clamp(offset, 0, source.Length);
		var code = MaskCommentsAndStrings(source.Substring(0, offset));
		var depth = 0;
		foreach (var character in code)
		{
			if (character == '{')
				depth++;
			else if (character == '}')
				depth--;
		}

		return depth;
	}

	public void RefreshVariables(string source)
	{
		_variables.Clear();
		_functions.Clear();
		var code = MaskCommentsAndStrings(source);
		foreach (Match match in VariableDeclaration.Matches(code))
		{
			var name = match.Groups[1].Value;
			var existingVariable = _variables.FirstOrDefault(variable =>
				string.Equals(variable.Name, name, StringComparison.Ordinal));
			if (existingVariable != null)
			{
				existingVariable.DeclarationCount++;
				continue;
			}

			var type = match.Groups[2].Success
				? match.Groups[2].Value
				: InferVariableType(source, match.Index + match.Length);
			_variables.Add(new VariableInfo(name, type));
		}

		foreach (Match function in ParameterList.Matches(code))
		{
			foreach (var parameter in function.Groups[1].Value.Split(','))
			{
				var parts = parameter.Trim().Split(':', 2);
				var name = parts[0].Trim();
				if (!Regex.IsMatch(name, @"^[A-Za-z_][A-Za-z0-9_]*$"))
				{
					continue;
				}

				var existingVariable = _variables.FirstOrDefault(variable =>
					string.Equals(variable.Name, name, StringComparison.Ordinal));
				if (existingVariable != null)
				{
					existingVariable.DeclarationCount++;
					continue;
				}

				_variables.Add(new VariableInfo(name, parts.Length == 2 ? parts[1].Trim() : "*"));
			}
		}

		foreach (Match function in FunctionDeclaration.Matches(code))
		{
			var name = function.Groups[1].Value;
			_functions[name] = _functions.TryGetValue(name, out var count) ? count + 1 : 1;
		}

		VariableList.Children.Clear();
		if (_variables.Count == 0)
		{
			VariableList.Children.Add(new TextBlock
			{
				Foreground = Avalonia.Media.Brushes.LightGray,
				Text = "No variables declared"
			});
		}
		else
		{
			foreach (var variable in _variables)
			{
				VariableList.Children.Add(new TextBlock
				{
					Foreground = Avalonia.Media.Brushes.White,
					Text = $"{variable.Name} : {variable.Type}"
				});
			}
		}

		UpdateSuggestedName();
	}

	private static string InferVariableType(string source, int offset)
	{
		var end = source.IndexOf(';', offset);
		if (end < 0)
			end = source.Length;

		var initializer = source.Substring(offset, end - offset);
		var assignment = initializer.IndexOf('=');
		if (assignment < 0)
			return "*";

		var value = initializer.Substring(assignment + 1).TrimStart();
		if (value.StartsWith("\"", StringComparison.Ordinal) || value.StartsWith("'", StringComparison.Ordinal))
			return "String";
		if (value.StartsWith("true", StringComparison.Ordinal) || value.StartsWith("false", StringComparison.Ordinal))
			return "Boolean";
		if (value.Length > 0 && (char.IsDigit(value[0]) || value[0] == '-'))
			return "Number";
		return "*";
	}

	private static string MaskCommentsAndStrings(string source)
	{
		var chars = source.ToCharArray();
		for (var i = 0; i < chars.Length;)
		{
			if (chars[i] == '/' && i + 1 < chars.Length && chars[i + 1] == '/')
			{
				while (i < chars.Length && chars[i] != '\n')
					chars[i++] = ' ';
				continue;
			}

			if (chars[i] == '/' && i + 1 < chars.Length && chars[i + 1] == '*')
			{
				chars[i++] = ' ';
				chars[i++] = ' ';
				while (i < chars.Length)
				{
					if (chars[i] == '*' && i + 1 < chars.Length && chars[i + 1] == '/')
					{
						chars[i++] = ' ';
						chars[i++] = ' ';
						break;
					}

					if (chars[i] != '\n' && chars[i] != '\r')
						chars[i] = ' ';
					i++;
				}

				continue;
			}

			if (chars[i] == '"' || chars[i] == '\'')
			{
				var quote = chars[i];
				chars[i++] = ' ';
				while (i < chars.Length)
				{
					var current = chars[i];
					if (current != '\n' && current != '\r')
						chars[i] = ' ';
					i++;
					if (current == '\\' && i < chars.Length)
					{
						if (chars[i] != '\n' && chars[i] != '\r')
							chars[i] = ' ';
						i++;
						continue;
					}

					if (current == quote)
						break;
				}
			}
			else
			{
				i++;
			}
		}

		return new string(chars);
	}

	private void BuildSnippetList()
	{
		foreach (var category in SnippetCategories)
		{
			SnippetList.Children.Add(new TextBlock
			{
				FontWeight = Avalonia.Media.FontWeight.Bold,
				Foreground = Avalonia.Media.Brushes.White,
				Text = category.Name
			});

			foreach (var snippet in category.Snippets)
			{
				var row = new Grid
				{
					ColumnDefinitions = ColumnDefinitions.Parse("*,Auto"),
					ColumnSpacing = 4
				};
				var addButton = new Button
				{
					Content = snippet.Name,
					HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Left,
					HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
					Padding = new Avalonia.Thickness(6, 4)
				};
				addButton.Click += (_, _) => SnippetRequested?.Invoke(snippet.Code, snippet.IsTopLevel);

				var dragButton = new Button
				{
					Content = "Drag",
					Padding = new Avalonia.Thickness(6, 4)
				};
				dragButton.PointerPressed += async (_, e) =>
				{
					if (!e.GetCurrentPoint(dragButton).Properties.IsLeftButtonPressed)
						return;

					var item = new DataTransferItem();
					item.Set(SnippetDataFormat, snippet.Code);
					item.Set(SnippetContextDataFormat, snippet.IsTopLevel ? "top-level" : "statement");
					var data = new DataTransfer();
					data.Add(item);
					await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Copy);
				};

				Grid.SetColumn(dragButton, 1);
				row.Children.Add(addButton);
				row.Children.Add(dragButton);
				SnippetList.Children.Add(row);
			}
		}
	}

	private void AddVariableClicked(object? sender, RoutedEventArgs e)
	{
		var type = GetSelectedType();
		var scope = GetSelectedScope();
		var name = VariableNameBox.Text?.Trim() ?? string.Empty;
		VariableRequested?.Invoke(name, type, scope);
	}

	private void VariableTypeChanged(object? sender, SelectionChangedEventArgs e)
	{
		UpdateSuggestedName();
	}

	private void UpdateSuggestedName()
	{
		if (VariableNameBox is null)
			return;

		var type = GetSelectedType();
		var candidates = type switch
		{
			"Number" => new[] { "count", "score", "amount", "number" },
			"String" => new[] { "name", "message", "text", "label" },
			"Boolean" => new[] { "isEnabled", "hasStarted", "canContinue", "isReady" },
			_ => new[] { "value", "result", "item", "data" }
		};
		var suggestion = candidates.FirstOrDefault(candidate => !HasVariable(candidate)) ?? string.Empty;
		if (string.IsNullOrWhiteSpace(VariableNameBox.Text)
			|| string.Equals(VariableNameBox.Text, _suggestedVariableName, StringComparison.Ordinal))
		{
			VariableNameBox.Text = suggestion;
		}

		_suggestedVariableName = suggestion;
	}

	private string GetSelectedType() =>
		(VariableTypeSelector.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "*";

	private string GetSelectedScope() =>
		(VariableScopeSelector.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "local";

	private sealed class VariableInfo
	{
		public VariableInfo(string name, string type)
		{
			Name = name;
			Type = type;
		}

		public string Name { get; }
		public string Type { get; }
		public int DeclarationCount { get; set; } = 1;
	}

	private sealed class SnippetCategory
	{
		public SnippetCategory(string name, params Snippet[] snippets)
		{
			Name = name;
			Snippets = snippets;
		}

		public string Name { get; }
		public IReadOnlyList<Snippet> Snippets { get; }
	}

	private sealed class Snippet
	{
		public Snippet(string name, string code)
			: this(name, code, false)
		{
		}

		public Snippet(string name, string code, bool isTopLevel)
		{
			Name = name;
			Code = code;
			IsTopLevel = isTopLevel;
		}

		public string Name { get; }
		public string Code { get; }
		public bool IsTopLevel { get; }
	}
}
